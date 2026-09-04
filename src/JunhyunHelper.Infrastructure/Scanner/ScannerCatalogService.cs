using System.Net;
using System.Net.Http;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Compact non-sensitive diagnostics for the most recent Scanner catalog load/refresh.
/// Market coverage is reported for troubleshooting. Identity health remains independent,
/// while an established healthy market baseline is protected from severe coverage loss.
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
    private const int CurrentCacheSchemaVersion = 4;
    private const int RequiredDownloadAttempts = 3;
    private const int OptionalDownloadAttempts = 2;
    private static readonly TimeSpan DefaultRefreshAge = TimeSpan.FromHours(12);
    private static readonly TimeSpan RequiredRequestTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan OptionalRequestTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SecondRetryDelay = TimeSpan.FromMilliseconds(750);

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly object _dataGate = new();
    private readonly ScannerItemMatcher _matcher = new();
    private readonly ScannerOcrCharacterPolicy _ocrPolicy = new();

    private Dictionary<string, ScannerCatalogItem> _itemsById = new(StringComparer.Ordinal);
    private IReadOnlyList<ScannerCatalogItem> _itemsSnapshot = Array.Empty<ScannerCatalogItem>();
    private GameMode? _loadedMode;
    private DateTimeOffset? _generatedAtUtc;
    private int _loadedCacheSchemaVersion;
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

    private int LoadedCacheSchemaVersion
    {
        get
        {
            lock (_dataGate)
                return _loadedCacheSchemaVersion;
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
        // Older caches remain readable so an offline upgrade can still recognize items,
        // but they predate the current market-presentation fields and must be refreshed at
        // the next online opportunity instead of being trusted as a fresh market cache.
        if (LoadedCacheSchemaVersion < CurrentCacheSchemaVersion)
            return true;

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

            ReplaceData(mode, cache.Items, cache.GeneratedAtUtc, cache.SchemaVersion);
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
        ScannerCatalogCache? baseline = null;
        try
        {
            await _refreshGate.WaitAsync(operation.Token);
            gateEntered = true;

            // Capture the same-mode last-known-good cache before any mode transition.
            // Data Update may run while Scanner is disabled, in which case the healthy
            // baseline can exist only on disk and still must survive a failed refresh.
            baseline = CaptureHealthyBaseline(mode);

            // Do not mutate the loaded mode before entering the operation gate. A newer
            // cache load may be waiting behind this refresh and must be the final writer.
            if (LoadedMode != mode)
                ClearForMode(mode);

            if (LoadedMode == mode && HasHealthyCatalog && !IsStale())
            {
                SetDiagnostics("fresh-cache", GetItemsSnapshot(), usedExistingCatalog: true);
                return true;
            }

            var token = operation.Token;
            var modeKey = mode.ToDataKey();
            var root = $"https://json.tarkov.dev/{modeKey}/";

            // Korean identity data is required. English localization and trader display
            // names enrich presentation only, so their timeout/schema failures must not
            // cancel an otherwise healthy Korean full-item refresh.
            var baseTask = DownloadWithRetryAsync(
                root + "items",
                RequiredDownloadAttempts,
                RequiredRequestTimeout,
                token);
            var koreanTask = DownloadWithRetryAsync(
                root + "items_ko",
                RequiredDownloadAttempts,
                RequiredRequestTimeout,
                token);
            var englishTask = TryDownloadOptionalStringAsync(root + "items_en", token);
            var traderNamesTask = TryDownloadTraderNamesAsync(root, token);

            await Task.WhenAll(new Task[] { baseTask, koreanTask, englishTask, traderNamesTask });

            using var baseDocument = JsonDocument.Parse(await baseTask);
            using var koreanDocument = JsonDocument.Parse(await koreanTask);
            var english = TryReadTranslationDictionary(await englishTask);

            var korean = ReadTranslationDictionary(koreanDocument.RootElement);
            var items = ParseItems(
                baseDocument.RootElement,
                korean,
                english,
                await traderNamesTask);
            if (!IsHealthyItemSet(items))
                return CompleteFailedRefresh(mode, "identity-invalid", items, baseline);

            if (baseline is not null)
            {
                var marketCoverage = ScannerMarketCoverageGuard.Assess(items, baseline.Items);
                if (!marketCoverage.IsAcceptable)
                    return CompleteFailedRefresh(mode, "market-regression", items, baseline);
            }

            var generatedAt = DateTimeOffset.UtcNow;
            var cache = new ScannerCatalogCache
            {
                SchemaVersion = CurrentCacheSchemaVersion,
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
                return CompleteFailedRefresh(mode, "cache-readback-invalid", verified.Items, baseline);

            ReplaceData(mode, verified.Items, verified.GeneratedAtUtc, verified.SchemaVersion);
            SetDiagnostics("success", verified.Items);
            DataChanged?.Invoke();
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CompleteFailedRefresh(mode, "timeout-or-shutdown", baseline: baseline);
        }
        catch (TimeoutException)
        {
            return CompleteFailedRefresh(mode, "timeout-or-shutdown", baseline: baseline);
        }
        catch (HttpRequestException)
        {
            return CompleteFailedRefresh(mode, "http-failure", baseline: baseline);
        }
        catch (IOException)
        {
            return CompleteFailedRefresh(mode, "io-failure", baseline: baseline);
        }
        catch (UnauthorizedAccessException)
        {
            return CompleteFailedRefresh(mode, "access-failure", baseline: baseline);
        }
        catch (JsonException)
        {
            return CompleteFailedRefresh(mode, "json-invalid", baseline: baseline);
        }
        catch (InvalidDataException)
        {
            return CompleteFailedRefresh(mode, "payload-invalid", baseline: baseline);
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
            return _itemsSnapshot;
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

    private async Task<string> DownloadWithRetryAsync(
        string url,
        int maximumAttempts,
        TimeSpan requestTimeout,
        CancellationToken cancellationToken)
    {
        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var attemptTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptTimeout.CancelAfter(requestTimeout);

            try
            {
                return await DownloadStringAsync(url, attemptTimeout.Token);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastFailure = new TimeoutException(
                    $"Scanner catalog request timed out after {requestTimeout.TotalSeconds:0.#} seconds: {url}",
                    exception);
            }
            catch (HttpRequestException exception) when (IsRetryableHttpFailure(exception))
            {
                lastFailure = exception;
            }
            catch (InvalidDataException exception)
            {
                lastFailure = exception;
            }

            if (attempt >= maximumAttempts)
                break;

            await Task.Delay(GetRetryDelay(attempt), cancellationToken);
        }

        throw lastFailure ?? new HttpRequestException($"Scanner catalog request failed: {url}");
    }

    private async Task<string?> TryDownloadOptionalStringAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DownloadWithRetryAsync(
                url,
                OptionalDownloadAttempts,
                OptionalRequestTimeout,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidDataException or TimeoutException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> TryDownloadTraderNamesAsync(
        string root,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseTask = TryDownloadOptionalStringAsync(root + "traders", cancellationToken);
            var koreanTask = TryDownloadOptionalStringAsync(root + "traders_ko", cancellationToken);
            var englishTask = TryDownloadOptionalStringAsync(root + "traders_en", cancellationToken);
            await Task.WhenAll(new Task[] { baseTask, koreanTask, englishTask });

            var baseJson = await baseTask;
            if (string.IsNullOrWhiteSpace(baseJson))
                return new Dictionary<string, string>(StringComparer.Ordinal);

            using var baseDocument = JsonDocument.Parse(baseJson);
            var koreanJson = await koreanTask;
            using var koreanDocument = string.IsNullOrWhiteSpace(koreanJson)
                ? null
                : JsonDocument.Parse(koreanJson);
            var englishJson = await englishTask;
            using var englishDocument = string.IsNullOrWhiteSpace(englishJson)
                ? null
                : JsonDocument.Parse(englishJson);

            var korean = koreanDocument is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : ReadTranslationDictionary(koreanDocument.RootElement);
            var english = englishDocument is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : ReadTranslationDictionary(englishDocument.RootElement);
            return ReadTraderNames(baseDocument.RootElement, korean, english);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException or InvalidDataException or TimeoutException)
        {
            // Friendly trader names enrich presentation only. A missing/changed trader
            // endpoint must never disable an otherwise valid item identity/market cache.
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static Dictionary<string, string> TryReadTranslationDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            using var document = JsonDocument.Parse(json);
            return ReadTranslationDictionary(document.RootElement);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static bool IsRetryableHttpFailure(HttpRequestException exception)
    {
        if (exception.StatusCode is null)
            return true;

        var statusCode = exception.StatusCode.Value;
        return statusCode is HttpStatusCode.RequestTimeout or
               HttpStatusCode.TooManyRequests ||
               (int)statusCode >= 500;
    }

    private static TimeSpan GetRetryDelay(int completedAttempt) => completedAttempt switch
    {
        <= 1 => FirstRetryDelay,
        _ => SecondRetryDelay,
    };

    private ScannerCatalogCache? CaptureHealthyBaseline(GameMode mode)
    {
        if (LoadedMode == mode &&
            HasHealthyCatalog &&
            GeneratedAtUtc is { } generatedAt &&
            LoadedCacheSchemaVersion >= 1 &&
            LoadedCacheSchemaVersion <= CurrentCacheSchemaVersion)
        {
            return new ScannerCatalogCache
            {
                SchemaVersion = LoadedCacheSchemaVersion,
                Source = "https://json.tarkov.dev",
                Language = "ko",
                GameMode = mode.ToDataKey(),
                GeneratedAtUtc = generatedAt,
                Items = GetItemsSnapshot().ToList(),
            };
        }

        var path = GetCachePath(mode);
        if (!File.Exists(path) && !File.Exists(path + ".bak"))
            return null;

        try
        {
            var cache = new AtomicJsonFileStore(path).LoadOrDefault(() => new ScannerCatalogCache());
            return IsHealthyCache(cache, mode) ? cache : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private bool CompleteFailedRefresh(
        GameMode mode,
        string outcome,
        IReadOnlyCollection<ScannerCatalogItem>? candidateItems = null,
        ScannerCatalogCache? baseline = null)
    {
        var useExisting = LoadedMode == mode && HasHealthyCatalog;
        if (!useExisting && baseline is not null && IsHealthyCache(baseline, mode))
        {
            ReplaceData(mode, baseline.Items, baseline.GeneratedAtUtc, baseline.SchemaVersion);
            useExisting = true;
        }

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
        DateTimeOffset generatedAtUtc,
        int schemaVersion)
    {
        var byId = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var snapshot = Array.AsReadOnly(byId.Values.ToArray());

        lock (_dataGate)
        {
            _loadedMode = mode;
            _generatedAtUtc = generatedAtUtc;
            _loadedCacheSchemaVersion = schemaVersion;
            _itemsById = byId;
            _itemsSnapshot = snapshot;
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
            _loadedCacheSchemaVersion = 0;
            _itemsById = new Dictionary<string, ScannerCatalogItem>(StringComparer.Ordinal);
            _itemsSnapshot = Array.Empty<ScannerCatalogItem>();
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
        cache.SchemaVersion >= 1 &&
        cache.SchemaVersion <= CurrentCacheSchemaVersion &&
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

    private static IReadOnlyDictionary<string, string> ReadTraderNames(
        JsonElement envelope,
        IReadOnlyDictionary<string, string> koreanTranslations,
        IReadOnlyDictionary<string, string> englishTranslations)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!envelope.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var property in data.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
                continue;

            var id = GetString(property.Value, "id");
            if (string.IsNullOrWhiteSpace(id))
                id = property.Name;
            var nameKey = GetString(property.Value, "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nameKey))
                continue;

            var name = Translate(nameKey, koreanTranslations, englishTranslations);
            if (!string.IsNullOrWhiteSpace(name))
                result[id] = name;
        }

        return result;
    }

    private static List<ScannerCatalogItem> ParseItems(
        JsonElement envelope,
        IReadOnlyDictionary<string, string> koreanTranslations,
        IReadOnlyDictionary<string, string> englishTranslations,
        IReadOnlyDictionary<string, string> traderNames)
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
            var bestTrader = ReadBestTraderSellOffer(raw);
            result.Add(new ScannerCatalogItem(
                id,
                officialName,
                shortName,
                NullIfEmpty(GetString(raw, "iconLink")),
                PositiveOrNull(GetInt(raw, "avg24hPrice")),
                bestTrader?.PriceRoubles,
                PositiveDimensionOrZero(GetInt(raw, "width")),
                PositiveDimensionOrZero(GetInt(raw, "height")))
            {
                BestTraderId = bestTrader?.TraderId,
                BestTraderName = ResolveTraderDisplayName(bestTrader, traderNames),
                FleaMinimumPrice = PositiveOrNull(GetInt(raw, "lastLowPrice")),
            });
        }

        return result;
    }

    private static TraderSellOffer? ReadBestTraderSellOffer(JsonElement item)
    {
        // The current json.tarkov.dev static /items endpoint maps its internal
        // traderPrices to sellToTrader and deletes traderPrices before publishing.
        // Prefer that public shape, while retaining compatibility with historical raw
        // dumps and the GraphQL sellFor representation.
        if (item.TryGetProperty("sellToTrader", out var sellToTrader) &&
            sellToTrader.ValueKind == JsonValueKind.Array)
        {
            var staticBest = ReadBestOffer(sellToTrader, excludeFlea: false);
            if (staticBest is not null)
                return staticBest;
        }

        if (item.TryGetProperty("traderPrices", out var traderPrices) &&
            traderPrices.ValueKind == JsonValueKind.Array)
        {
            var rawTraderBest = ReadBestOffer(traderPrices, excludeFlea: false);
            if (rawTraderBest is not null)
                return rawTraderBest;
        }

        if (item.TryGetProperty("sellFor", out var sellFor) && sellFor.ValueKind == JsonValueKind.Array)
            return ReadBestOffer(sellFor, excludeFlea: true);

        return null;
    }

    private static TraderSellOffer? ReadBestOffer(JsonElement offers, bool excludeFlea)
    {
        TraderSellOffer? best = null;
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

            if (roubles is not > 0 || best is not null && roubles.Value <= best.PriceRoubles)
                continue;

            best = new TraderSellOffer(
                roubles.Value,
                NullIfEmpty(ReadTraderId(offer)),
                NullIfEmpty(source));
        }

        return best;
    }

    private static string ReadTraderId(JsonElement offer)
    {
        var direct = GetString(offer, "trader");
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        if (offer.TryGetProperty("vendor", out var vendor) && vendor.ValueKind == JsonValueKind.Object)
        {
            var trader = GetString(vendor, "trader");
            if (!string.IsNullOrWhiteSpace(trader))
                return trader;
            var id = GetString(vendor, "id");
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        if (offer.TryGetProperty("source", out var source) && source.ValueKind == JsonValueKind.Object)
        {
            var trader = GetString(source, "trader");
            if (!string.IsNullOrWhiteSpace(trader))
                return trader;
            return GetString(source, "id");
        }

        return string.Empty;
    }

    private static string? ResolveTraderDisplayName(
        TraderSellOffer? offer,
        IReadOnlyDictionary<string, string> traderNames)
    {
        if (offer is null)
            return null;
        if (!string.IsNullOrWhiteSpace(offer.TraderId) &&
            traderNames.TryGetValue(offer.TraderId, out var name) &&
            !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(offer.SourceName) &&
            !offer.SourceName.Contains("flea", StringComparison.OrdinalIgnoreCase))
        {
            return offer.SourceName;
        }

        return null;
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

    private sealed record TraderSellOffer(
        int PriceRoubles,
        string? TraderId,
        string? SourceName);

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
