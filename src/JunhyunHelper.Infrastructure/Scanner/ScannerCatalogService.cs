using System.Net.Http;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Compact non-sensitive diagnostics for the most recent Scanner catalog load/refresh.
/// Market coverage is reported for troubleshooting, but never participates in identity
/// catalog health: missing prices fail closed per field instead of disabling recognition.
/// </summary>
public sealed record ScannerCatalogDiagnostics(
    string Outcome,
    int ItemCount,
    int TraderPriceCount,
    int FleaPriceCount,
    bool UsedExistingCatalog);

/// <summary>
/// Owns the Scanner full-item identity/market cache. Network synchronization is an
/// explicit pre-scan operation; recognition and item lookup are memory/local-cache only.
/// </summary>
public sealed class ScannerCatalogService : IDisposable
{
    public const int MinimumHealthyItemCount = 4000;
    private static readonly TimeSpan DefaultRefreshAge = TimeSpan.FromHours(12);

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly object _dataGate = new();
    private readonly ScannerItemMatcher _matcher = new();
    private readonly ScannerOcrCharacterPolicy _ocrPolicy = new();

    private Dictionary<string, ScannerCatalogItem> _itemsById = new(StringComparer.Ordinal);
    private GameMode? _loadedMode;
    private DateTimeOffset? _generatedAtUtc;
    private ScannerCatalogDiagnostics _lastDiagnostics = new("not-run", 0, 0, 0, false);
    private bool _disposed;

    public ScannerCatalogService(HttpClient httpClient, string rootDirectory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _cacheDirectory = Path.Combine(Path.GetFullPath(rootDirectory), "scanner", "catalog");
    }

    public event Action? DataChanged;

    public GameMode? LoadedMode
    {
        get
        {
            lock (_dataGate)
                return _loadedMode;
        }
    }

    public int Count
    {
        get
        {
            lock (_dataGate)
                return _itemsById.Count;
        }
    }

    public DateTimeOffset? GeneratedAtUtc
    {
        get
        {
            lock (_dataGate)
                return _generatedAtUtc;
        }
    }

    public ScannerCatalogDiagnostics LastDiagnostics
    {
        get
        {
            lock (_dataGate)
                return _lastDiagnostics;
        }
    }

    public bool HasHealthyCatalog => Count >= MinimumHealthyItemCount;

    public bool IsStale(TimeSpan? maximumAge = null)
    {
        var generated = GeneratedAtUtc;
        return generated is null || DateTimeOffset.UtcNow - generated.Value >= (maximumAge ?? DefaultRefreshAge);
    }

    public Task<bool> EnsureLoadedAsync(
        GameMode mode,
        CancellationToken cancellationToken = default)
    {
        if (LoadedMode == mode && HasHealthyCatalog)
            return Task.FromResult(true);
        return LoadCacheAsync(mode, cancellationToken);
    }

    public async Task<bool> LoadCacheAsync(
        GameMode mode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCts.Token);
        var gateEntered = false;
        try
        {
            // Cache loads and network refreshes both replace the same in-memory identity
            // state. Serialize them so a profile transition cannot be overwritten by an
            // older refresh that was already in flight for another game mode.
            await _refreshGate.WaitAsync(operation.Token);
            gateEntered = true;
            operation.Token.ThrowIfCancellationRequested();

            var path = GetCachePath(mode);
            if (!File.Exists(path) && !File.Exists(path + ".bak"))
            {
                ClearForMode(mode);
                SetDiagnostics("cache-missing", []);
                return false;
            }

            ScannerCatalogCache cache;
            try
            {
                cache = new AtomicJsonFileStore(path).LoadOrDefault(() => new ScannerCatalogCache());
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                ClearForMode(mode);
                SetDiagnostics("cache-read-failure", []);
                return false;
            }

            operation.Token.ThrowIfCancellationRequested();
            if (!IsHealthyCache(cache, mode))
            {
                ClearForMode(mode);
                SetDiagnostics("cache-invalid", cache.Items);
                return false;
            }

            ReplaceData(mode, cache.Items, cache.GeneratedAtUtc);
            SetDiagnostics("cache-loaded", cache.Items, usedExistingCatalog: true);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            if (gateEntered)
                _refreshGate.Release();
        }
    }

    public async Task<bool> RefreshIfStaleAsync(
        GameMode mode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await EnsureLoadedAsync(mode, cancellationToken);
        if (LoadedMode == mode && HasHealthyCatalog && !IsStale())
            return true;
        return await RefreshAsync(mode, cancellationToken);
    }

    public async Task<bool> RefreshAsync(
        GameMode mode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCts.Token);
        var gateEntered = false;
        try
        {
            await _refreshGate.WaitAsync(operation.Token);
            gateEntered = true;

            // Do not mutate the loaded mode before entering the operation gate. A newer
            // cache load may be waiting behind this refresh and must be the final writer.
            if (LoadedMode != mode)
                ClearForMode(mode);

            if (LoadedMode == mode && HasHealthyCatalog && !IsStale())
            {
                SetDiagnostics("fresh-cache", GetItemsSnapshot(), usedExistingCatalog: true);
                return true;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(operation.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            var token = timeout.Token;

            var modeKey = mode.ToDataKey();
            var root = $"https://json.tarkov.dev/{modeKey}/";
            var baseTask = DownloadStringAsync(root + "items", token);
            var koreanTask = DownloadStringAsync(root + "items_ko", token);
            var englishTask = TryDownloadStringAsync(root + "items_en", token);

            await Task.WhenAll(new Task[] { baseTask, koreanTask, englishTask });

            using var baseDocument = JsonDocument.Parse(await baseTask);
            using var koreanDocument = JsonDocument.Parse(await koreanTask);
            var englishJson = await englishTask;
            using var englishDocument = string.IsNullOrWhiteSpace(englishJson)
                ? null
                : JsonDocument.Parse(englishJson);

            var korean = ReadTranslationDictionary(koreanDocument.RootElement);
            var english = englishDocument is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : ReadTranslationDictionary(englishDocument.RootElement);
            var items = ParseItems(baseDocument.RootElement, korean, english);
            if (!IsHealthyItemSet(items))
                return CompleteFailedRefresh(mode, "identity-invalid", items);

            var generatedAt = DateTimeOffset.UtcNow;
            var cache = new ScannerCatalogCache
            {
                SchemaVersion = 2,
                Source = "https://json.tarkov.dev",
                Language = "ko",
                GameMode = modeKey,
                GeneratedAtUtc = generatedAt,
                Items = items,
            };

            var path = GetCachePath(mode);
            new AtomicJsonFileStore(path).Save(cache);

            // Read-back is deliberate: never replace an in-memory healthy catalog with
            // a write that cannot be recovered as the same validated document.
            var verified = new AtomicJsonFileStore(path).LoadOrDefault(() => new ScannerCatalogCache());
            if (!IsHealthyCache(verified, mode))
                return CompleteFailedRefresh(mode, "cache-readback-invalid", verified.Items);

            ReplaceData(mode, verified.Items, verified.GeneratedAtUtc);
            SetDiagnostics("success", verified.Items);
            DataChanged?.Invoke();
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompleteFailedRefresh(mode, "timeout-or-shutdown");
        }
        catch (HttpRequestException)
        {
            return CompleteFailedRefresh(mode, "http-failure");
        }
        catch (IOException)
        {
            return CompleteFailedRefresh(mode, "io-failure");
        }
        catch (UnauthorizedAccessException)
        {
            return CompleteFailedRefresh(mode, "access-failure");
        }
        catch (JsonException)
        {
            return CompleteFailedRefresh(mode, "json-invalid");
        }
        catch (InvalidDataException)
        {
            return CompleteFailedRefresh(mode, "payload-invalid");
        }
        finally
        {
            if (gateEntered)
                _refreshGate.Release();
        }
    }

    public bool TryGetItem(string itemId, out ScannerCatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            item = null!;
            return false;
        }

        lock (_dataGate)
            return _itemsById.TryGetValue(itemId.Trim(), out item!);
    }

    public IReadOnlyList<ScannerCatalogItem> GetItemsSnapshot()
    {
        lock (_dataGate)
            return _itemsById.Values.ToArray();
    }

    public ScannerOcrTextAssessment AssessOcrText(string? text) => _ocrPolicy.Assess(text);

    public ScannerRecognition ResolveOcrText(string text) =>
        ResolveOcrText(text, out _);

    public ScannerRecognition ResolveOcrText(
        string text,
        out ScannerOcrTextAssessment assessment)
    {
        assessment = _ocrPolicy.Assess(text);
        if (string.IsNullOrWhiteSpace(text))
            return ScannerRecognition.Failed("EMPTY_OCR");
        if (!assessment.HasPlausibleVariant)
            return ScannerRecognition.Failed("OCR_INVALID_CHARACTERS");

        lock (_dataGate)
        {
            var ordinary = _matcher.Resolve(assessment.FilteredText);
            if (ordinary.Success || !assessment.HasSingleUnknownGlyphPattern)
                return ordinary;

            var unknownGlyph = _matcher.ResolveSingleUnknownGlyph(assessment.UnknownGlyphPatternText);
            return unknownGlyph.Success ? unknownGlyph : ordinary;
        }
    }

    private async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidDataException($"Scanner catalog response was empty: {url}");
        return content;
    }

    private async Task<string?> TryDownloadStringAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await DownloadStringAsync(url, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private bool CompleteFailedRefresh(
        GameMode mode,
        string outcome,
        IReadOnlyCollection<ScannerCatalogItem>? candidateItems = null)
    {
        var useExisting = LoadedMode == mode && HasHealthyCatalog;
        SetDiagnostics(outcome, candidateItems, useExisting);
        return useExisting;
    }

    private void SetDiagnostics(
        string outcome,
        IReadOnlyCollection<ScannerCatalogItem>? items = null,
        bool usedExistingCatalog = false)
    {
        var measuredItems = items ?? GetItemsSnapshot();
        var diagnostics = new ScannerCatalogDiagnostics(
            outcome,
            measuredItems.Count,
            measuredItems.Count(item => item.BestTraderSellPrice is > 0),
            measuredItems.Count(item => item.FleaAveragePrice is > 0),
            usedExistingCatalog);

        lock (_dataGate)
            _lastDiagnostics = diagnostics;
    }

    private void ReplaceData(
        GameMode mode,
        IReadOnlyList<ScannerCatalogItem> items,
        DateTimeOffset generatedAtUtc)
    {
        var byId = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        lock (_dataGate)
        {
            _loadedMode = mode;
            _generatedAtUtc = generatedAtUtc;
            _itemsById = byId;
            _matcher.ReplaceCatalog(byId.Values);
            _ocrPolicy.ReplaceCatalog(byId.Values);
        }
    }

    private void ClearForMode(GameMode mode)
    {
        lock (_dataGate)
        {
            _loadedMode = mode;
            _generatedAtUtc = null;
            _itemsById = new Dictionary<string, ScannerCatalogItem>(StringComparer.Ordinal);
            _matcher.ReplaceCatalog([]);
            _ocrPolicy.ReplaceCatalog([]);
        }
    }

    private string GetCachePath(GameMode mode)
    {
        Directory.CreateDirectory(_cacheDirectory);
        return Path.Combine(_cacheDirectory, $"items-{mode.ToDataKey()}-ko.json");
    }

    private static bool IsHealthyCache(ScannerCatalogCache cache, GameMode mode) =>
        cache.SchemaVersion is 1 or 2 &&
        string.Equals(cache.Source, "https://json.tarkov.dev", StringComparison.Ordinal) &&
        string.Equals(cache.Language, "ko", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(cache.GameMode, mode.ToDataKey(), StringComparison.Ordinal) &&
        cache.GeneratedAtUtc != default &&
        IsHealthyItemSet(cache.Items);

    private static bool IsHealthyItemSet(IReadOnlyCollection<ScannerCatalogItem> items) =>
        items.Count >= MinimumHealthyItemCount &&
        !items.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.OfficialName));

    private static Dictionary<string, string> ReadTranslationDictionary(JsonElement envelope)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var property in data.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
                continue;
            var value = property.Value.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                result[property.Name] = value;
        }
        return result;
    }

    private static List<ScannerCatalogItem> ParseItems(
        JsonElement envelope,
        IReadOnlyDictionary<string, string> koreanTranslations,
        IReadOnlyDictionary<string, string> englishTranslations)
    {
        if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Scanner items.data is missing.");
        if (!data.TryGetProperty("items", out var itemsElement))
            throw new InvalidDataException("Scanner items.data.items is missing.");

        IEnumerable<JsonElement> records = itemsElement.ValueKind switch
        {
            JsonValueKind.Array => itemsElement.EnumerateArray().ToArray(),
            JsonValueKind.Object => itemsElement.EnumerateObject().Select(property => property.Value).ToArray(),
            _ => [],
        };

        var result = new List<ScannerCatalogItem>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in records)
        {
            if (raw.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetString(raw, "id");
            var nameKey = GetString(raw, "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nameKey) || !ids.Add(id))
                continue;

            var officialName = Translate(nameKey, koreanTranslations, englishTranslations);
            if (string.IsNullOrWhiteSpace(officialName))
                continue;

            var shortName = Translate(GetString(raw, "shortName"), koreanTranslations, englishTranslations);
            result.Add(new ScannerCatalogItem(
                id,
                officialName,
                shortName,
                NullIfEmpty(GetString(raw, "iconLink")),
                PositiveOrNull(GetInt(raw, "avg24hPrice")),
                ReadBestTraderSellPrice(raw),
                PositiveDimensionOrZero(GetInt(raw, "width")),
                PositiveDimensionOrZero(GetInt(raw, "height"))));
        }

        return result;
    }

    private static int? ReadBestTraderSellPrice(JsonElement item)
    {
        // json.tarkov.dev exposes raw traderPrices in its item data. The GraphQL layer
        // currently derives sellFor from traderPrices and appends a flea row. Accept both
        // representations so the Scanner is insulated from which layer produced a dump.
        if (item.TryGetProperty("traderPrices", out var traderPrices) &&
            traderPrices.ValueKind == JsonValueKind.Array)
        {
            var rawTraderBest = ReadBestOfferPrice(traderPrices, excludeFlea: false);
            if (rawTraderBest.HasValue)
                return rawTraderBest;
        }

        if (item.TryGetProperty("sellFor", out var sellFor) && sellFor.ValueKind == JsonValueKind.Array)
            return ReadBestOfferPrice(sellFor, excludeFlea: true);

        return null;
    }

    private static int? ReadBestOfferPrice(JsonElement offers, bool excludeFlea)
    {
        int? best = null;
        foreach (var offer in offers.EnumerateArray())
        {
            if (offer.ValueKind != JsonValueKind.Object)
                continue;

            var source = ReadSourceName(offer);
            if (excludeFlea && source.Contains("flea", StringComparison.OrdinalIgnoreCase))
                continue;

            var roubles = GetInt(offer, "priceRUB");
            if (!roubles.HasValue)
            {
                var currency = GetString(offer, "currency");
                if (string.IsNullOrWhiteSpace(currency) ||
                    currency.Equals("RUB", StringComparison.OrdinalIgnoreCase) ||
                    currency.Equals("₽", StringComparison.Ordinal))
                {
                    roubles = GetInt(offer, "price");
                }
            }

            if (roubles is > 0 && (!best.HasValue || roubles.Value > best.Value))
                best = roubles.Value;
        }

        return best;
    }

    private static string ReadSourceName(JsonElement offer)
    {
        if (offer.TryGetProperty("source", out var source))
        {
            if (source.ValueKind == JsonValueKind.String)
                return source.GetString() ?? string.Empty;
            if (source.ValueKind == JsonValueKind.Object)
            {
                var name = GetString(source, "name");
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
                var id = GetString(source, "id");
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
        }

        if (offer.TryGetProperty("vendor", out var vendor) && vendor.ValueKind == JsonValueKind.Object)
        {
            var name = GetString(vendor, "name");
            if (!string.IsNullOrWhiteSpace(name))
                return name;
            var id = GetString(vendor, "id");
            if (!string.IsNullOrWhiteSpace(id))
                return id;
            return GetString(vendor, "trader");
        }

        return string.Empty;
    }

    private static string Translate(
        string key,
        IReadOnlyDictionary<string, string> primary,
        IReadOnlyDictionary<string, string> fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;
        if (primary.TryGetValue(key, out var primaryValue) && !string.IsNullOrWhiteSpace(primaryValue))
            return primaryValue;
        if (fallback.TryGetValue(key, out var fallbackValue) && !string.IsNullOrWhiteSpace(fallbackValue))
            return fallbackValue;
        return key;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return string.Empty;
        return property.GetString() ?? string.Empty;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            return null;
        if (property.TryGetInt32(out var integer))
            return integer;
        if (property.TryGetDouble(out var number) && double.IsFinite(number))
            return (int)Math.Round(number);
        return null;
    }

    private static int? PositiveOrNull(int? value) => value is > 0 ? value : null;

    private static int PositiveDimensionOrZero(int? value) => value is > 0 ? value.Value : 0;

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
        // Do not dispose _refreshGate here: an in-flight canceled refresh or cache load
        // may still execute its finally block and release the gate during app shutdown.
        GC.SuppressFinalize(this);
    }

    private sealed class ScannerCatalogCache
    {
        public int SchemaVersion { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Language { get; set; } = "ko";
        public string GameMode { get; set; } = string.Empty;
        public DateTimeOffset GeneratedAtUtc { get; set; }
        public List<ScannerCatalogItem> Items { get; set; } = [];
    }
}