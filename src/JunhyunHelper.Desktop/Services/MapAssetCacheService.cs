using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Infrastructure.Content;

namespace JunhyunHelper.Desktop.Services;

public sealed record MapAssetUpdateProgress(int Completed, int Total, string Message);

public sealed record MapAssetUpdateResult(
    bool Applied,
    IReadOnlyList<MapLayoutDefinition> Layouts,
    IReadOnlyList<string> Warnings);

public sealed class MapAssetCacheService
{
    private const int MaxSvgBytes = 64 * 1024 * 1024;
    private const int MaxIconBytes = 2 * 1024 * 1024;
    private const string MarkerAssetBaseUrl =
        "https://raw.githubusercontent.com/the-hideout/tarkov-dev/refs/heads/main/public/maps/interactive/";
    private const string SvgAssetPrefix = "https://assets.tarkov.dev/maps/svg/";
    private const string SvgRepositoryPrefix =
        "https://raw.githubusercontent.com/the-hideout/tarkov-dev-svg-maps/refs/heads/main/";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly IReadOnlyDictionary<MapMarkerKind, string> MarkerIconFiles =
        new Dictionary<MapMarkerKind, string>
        {
            [MapMarkerKind.PmcExtract] = "extract_pmc.png",
            [MapMarkerKind.ScavExtract] = "extract_scav.png",
            [MapMarkerKind.SharedExtract] = "extract_shared.png",
            [MapMarkerKind.Transit] = "extract_transit.png",
            [MapMarkerKind.PmcSpawn] = "spawn_pmc.png",
            [MapMarkerKind.ScavSpawn] = "spawn_scav.png",
            [MapMarkerKind.SniperScav] = "spawn_sniper_scav.png",
            [MapMarkerKind.Boss] = "spawn_boss.png",
            [MapMarkerKind.SpecialAi] = "spawn_rogue.png",
            [MapMarkerKind.Hazard] = "hazard.png",
            [MapMarkerKind.Lock] = "lock.png",
            [MapMarkerKind.Switch] = "switch.png",
            [MapMarkerKind.StationaryWeapon] = "stationarygun.png",
            [MapMarkerKind.BtrStop] = "btr_stop.png",
            [MapMarkerKind.LootContainer] = "container_crate.png",
            [MapMarkerKind.LooseLoot] = "loose_loot.png",
        };
    private static readonly string[] SupplementalIconFiles =
    [
        "quest_item.png",
        "quest_objective.png",
        "player-position.png",
    ];

    private readonly HttpClient _httpClient;
    private readonly TarkovMapLayoutCatalogClient _layoutClient;
    private readonly string _root;

    public MapAssetCacheService(HttpClient httpClient, string rootDirectory)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _layoutClient = new TarkovMapLayoutCatalogClient(httpClient);
        _root = Path.Combine(Path.GetFullPath(rootDirectory), "map-cache");
        Directory.CreateDirectory(_root);
    }

    public string ActiveDirectory => Path.Combine(_root, "active");
    private string CandidateDirectory => Path.Combine(_root, "candidate");
    private string PreviousDirectory => Path.Combine(_root, "previous");

    public async Task<MapAssetUpdateResult> UpdateAsync(
        GameContentCatalog content,
        IProgress<MapAssetUpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var previousLayouts = await TryLoadActiveLayoutsAsync(cancellationToken);
        ResetCandidate();
        Directory.CreateDirectory(CandidateDirectory);

        try
        {
            progress?.Report(new MapAssetUpdateProgress(0, 1, "지도 레이아웃 정보를 확인하는 중..."));
            var catalog = await _layoutClient.LoadAsync(content.Maps, cancellationToken);
            var requestedLayouts = catalog.Layouts;
            if (requestedLayouts.Count == 0)
                throw new InvalidDataException("No usable Map layouts were returned.");

            var warnings = catalog.Warnings.ToList();
            var iconFiles = MarkerIconFiles.Values
                .Concat(SupplementalIconFiles)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var total = requestedLayouts.Count + iconFiles.Length;
            var completed = 0;
            var effectiveLayouts = new List<MapLayoutDefinition>();

            foreach (var layout in requestedLayouts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = GetRawSvgPath(CandidateDirectory, layout);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                try
                {
                    await DownloadSvgWithFallbackAsync(layout.SvgUrl, destination, cancellationToken);
                    effectiveLayouts.Add(layout);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    if (File.Exists(destination))
                        File.Delete(destination);

                    if (TryReusePreviousLayout(layout, previousLayouts, destination, out var previousLayout))
                    {
                        effectiveLayouts.Add(previousLayout);
                        warnings.Add(
                            $"Map '{layout.NormalizedName}' could not be refreshed; previous validated asset was kept: {exception.Message}");
                    }
                    else
                    {
                        warnings.Add(
                            $"Map '{layout.NormalizedName}' could not be downloaded and has no previous validated asset; this map is temporarily unavailable: {exception.Message}");
                    }
                }

                completed++;
                progress?.Report(new MapAssetUpdateProgress(
                    completed,
                    total,
                    $"지도 다운로드 중... {completed}/{total}"));
            }

            if (effectiveLayouts.Count == 0)
                throw new InvalidDataException(
                    "지도 SVG를 하나도 준비하지 못했습니다. 네트워크 또는 지도 원천 상태를 확인해주세요.");

            foreach (var iconFile in iconFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = GetIconPath(CandidateDirectory, iconFile);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                try
                {
                    await DownloadPngAsync(MarkerAssetBaseUrl + iconFile, destination, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    if (File.Exists(destination))
                        File.Delete(destination);

                    var previousIcon = GetIconPath(ActiveDirectory, iconFile);
                    if (File.Exists(previousIcon))
                    {
                        File.Copy(previousIcon, destination, overwrite: true);
                        warnings.Add(
                            $"Map marker icon '{iconFile}' could not be refreshed; previous icon was kept: {exception.Message}");
                    }
                    else
                    {
                        warnings.Add(
                            $"Map marker icon '{iconFile}' could not be refreshed; fallback marker will be used: {exception.Message}");
                    }
                }

                completed++;
                progress?.Report(new MapAssetUpdateProgress(
                    completed,
                    total,
                    $"지도 마커 아이콘 다운로드 중... {completed}/{total}"));
            }

            var finalLayouts = effectiveLayouts
                .DistinctBy(layout => (layout.MapId, layout.Key))
                .ToArray();
            await File.WriteAllTextAsync(
                Path.Combine(CandidateDirectory, "layouts.json"),
                JsonSerializer.Serialize(finalLayouts, JsonOptions),
                cancellationToken);
            await ValidateDirectoryAsync(CandidateDirectory, finalLayouts, cancellationToken);
            ActivateCandidate();
            return new MapAssetUpdateResult(true, finalLayouts, warnings.ToArray());
        }
        catch
        {
            ResetCandidate();
            throw;
        }
    }

    public async Task<IReadOnlyList<MapLayoutDefinition>> LoadActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ActiveDirectory, "layouts.json");
        if (!File.Exists(path))
            return Array.Empty<MapLayoutDefinition>();

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var layouts = JsonSerializer.Deserialize<MapLayoutDefinition[]>(json, JsonOptions)
                      ?? Array.Empty<MapLayoutDefinition>();
        await ValidateDirectoryAsync(ActiveDirectory, layouts, cancellationToken);
        return layouts;
    }

    public async Task<bool> HasUsableActiveAssetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return (await LoadActiveAsync(cancellationToken)).Count > 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    public string? GetMarkerIconPath(MapMarkerKind kind) =>
        MarkerIconFiles.TryGetValue(kind, out var fileName)
            ? ExistingIconPath(fileName)
            : null;

    public string? GetQuestObjectiveIconPath() => ExistingIconPath("quest_objective.png");

    public string? GetQuestItemIconPath() => ExistingIconPath("quest_item.png");

    public string? GetPlayerIconPath() => ExistingIconPath("player-position.png");

    private string? ExistingIconPath(string fileName)
    {
        var path = GetIconPath(ActiveDirectory, fileName);
        return File.Exists(path) ? path : null;
    }

    public string? GetRenderedSvgPath(MapLayoutDefinition layout, string? floorId)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var source = GetRawSvgPath(ActiveDirectory, layout);
        if (!File.Exists(source))
            return null;

        var floor = layout.Floors.FirstOrDefault(candidate =>
                        string.Equals(candidate.Id, floorId, StringComparison.Ordinal))
                    ?? layout.Floors.FirstOrDefault(candidate => candidate.IsDefault)
                    ?? layout.Floors.FirstOrDefault();
        if (floor is null || layout.Floors.Count <= 1)
            return source;

        var renderedDirectory = Path.Combine(ActiveDirectory, "rendered");
        Directory.CreateDirectory(renderedDirectory);
        var destination = Path.Combine(
            renderedDirectory,
            $"{Sanitize(layout.Key)}.{Sanitize(floor.Id)}.svg");
        if (File.Exists(destination) && File.GetLastWriteTimeUtc(destination) >= File.GetLastWriteTimeUtc(source))
            return destination;

        try
        {
            var document = XDocument.Load(source, LoadOptions.PreserveWhitespace);
            var knownLayers = layout.Floors
                .Select(candidate => candidate.SvgLayer)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToHashSet(StringComparer.Ordinal);
            foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "g"))
            {
                var id = element.Attribute("id")?.Value;
                if (string.IsNullOrWhiteSpace(id) || !knownLayers.Contains(id))
                    continue;
                SetDisplay(element, string.Equals(id, floor.SvgLayer, StringComparison.Ordinal));
            }
            document.Save(destination, SaveOptions.DisableFormatting);
            ValidateSvg(destination);
            return destination;
        }
        catch
        {
            return source;
        }
    }

    private async Task<IReadOnlyList<MapLayoutDefinition>> TryLoadActiveLayoutsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await LoadActiveAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Array.Empty<MapLayoutDefinition>();
        }
    }

    private bool TryReusePreviousLayout(
        MapLayoutDefinition requestedLayout,
        IReadOnlyList<MapLayoutDefinition> previousLayouts,
        string destination,
        out MapLayoutDefinition previousLayout)
    {
        previousLayout = previousLayouts.FirstOrDefault(layout =>
                             string.Equals(layout.MapId, requestedLayout.MapId, StringComparison.Ordinal) &&
                             string.Equals(layout.Key, requestedLayout.Key, StringComparison.Ordinal))
                         ?? previousLayouts.FirstOrDefault(layout =>
                             string.Equals(layout.MapId, requestedLayout.MapId, StringComparison.Ordinal) &&
                             string.Equals(layout.NormalizedName, requestedLayout.NormalizedName, StringComparison.OrdinalIgnoreCase))!;

        if (previousLayout is null)
            return false;

        var previousPath = GetRawSvgPath(ActiveDirectory, previousLayout);
        if (!File.Exists(previousPath))
            return false;

        try
        {
            ValidateSvg(previousPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(previousPath, destination, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task DownloadSvgWithFallbackAsync(
        string configuredUrl,
        string destination,
        CancellationToken cancellationToken)
    {
        var candidates = BuildSvgSourceCandidates(configuredUrl).ToArray();
        Exception? lastException = null;

        foreach (var url in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await DownloadSvgCoreAsync(url, destination, cancellationToken);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                lastException = exception;
                if (File.Exists(destination))
                    File.Delete(destination);
            }
        }

        throw new InvalidDataException(
            $"Map SVG download failed from {candidates.Length} source(s): {string.Join(" | ", candidates)}",
            lastException);
    }

    private static IEnumerable<string> BuildSvgSourceCandidates(string configuredUrl)
    {
        if (configuredUrl.StartsWith(SvgRepositoryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relative = configuredUrl[SvgRepositoryPrefix.Length..];
            yield return SvgAssetPrefix + relative;
            yield return configuredUrl;
            yield break;
        }

        if (configuredUrl.StartsWith(SvgAssetPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relative = configuredUrl[SvgAssetPrefix.Length..];
            yield return configuredUrl;
            yield return SvgRepositoryPrefix + relative;
            yield break;
        }

        yield return configuredUrl;
    }

    private async Task DownloadSvgCoreAsync(
        string url,
        string destination,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxSvgBytes)
            throw new InvalidDataException($"Map SVG is too large: {url}");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        var buffer = new byte[81920];
        var total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            total += read;
            if (total > MaxSvgBytes)
                throw new InvalidDataException($"Map SVG exceeded the maximum size: {url}");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        await output.FlushAsync(cancellationToken);
        ValidateSvg(destination);
    }

    private async Task DownloadPngAsync(
        string url,
        string destination,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxIconBytes)
            throw new InvalidDataException($"Map marker icon is too large: {url}");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 32768,
            useAsync: true);
        var buffer = new byte[32768];
        var total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            total += read;
            if (total > MaxIconBytes)
                throw new InvalidDataException($"Map marker icon exceeded the maximum size: {url}");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        await output.FlushAsync(cancellationToken);
        ValidatePng(destination);
    }

    private static Task ValidateDirectoryAsync(
        string directory,
        IReadOnlyList<MapLayoutDefinition> layouts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var layout in layouts)
        {
            if (string.IsNullOrWhiteSpace(layout.MapId) ||
                string.IsNullOrWhiteSpace(layout.Key) ||
                layout.Transform.Count != 4 ||
                layout.Bounds.Count != 2 ||
                layout.SvgBounds.Count != 2)
                throw new InvalidDataException($"Invalid Map layout '{layout.Key}'.");

            var svg = GetRawSvgPath(directory, layout);
            if (!File.Exists(svg))
                throw new FileNotFoundException($"Map SVG missing for '{layout.Key}'.", svg);
            ValidateSvg(svg);
        }
        return Task.CompletedTask;
    }

    private static void ValidateSvg(string path)
    {
        var document = XDocument.Load(path, LoadOptions.None);
        if (document.Root is null || !string.Equals(document.Root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Map asset '{path}' is not a valid SVG document.");
    }

    private static void ValidatePng(string path)
    {
        Span<byte> header = stackalloc byte[8];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Read(header) != header.Length ||
            !header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            throw new InvalidDataException($"Map marker asset '{path}' is not a valid PNG file.");
    }

    private void ActivateCandidate()
    {
        if (Directory.Exists(PreviousDirectory))
            Directory.Delete(PreviousDirectory, recursive: true);
        if (Directory.Exists(ActiveDirectory))
            Directory.Move(ActiveDirectory, PreviousDirectory);

        try
        {
            Directory.Move(CandidateDirectory, ActiveDirectory);
        }
        catch
        {
            if (Directory.Exists(ActiveDirectory))
                Directory.Delete(ActiveDirectory, recursive: true);
            if (Directory.Exists(PreviousDirectory))
                Directory.Move(PreviousDirectory, ActiveDirectory);
            throw;
        }
    }

    private void ResetCandidate()
    {
        if (Directory.Exists(CandidateDirectory))
            Directory.Delete(CandidateDirectory, recursive: true);
    }

    private static string GetRawSvgPath(string directory, MapLayoutDefinition layout) =>
        Path.Combine(directory, "svg", $"{Sanitize(layout.MapId)}-{Sanitize(layout.Key)}.svg");

    private static string GetIconPath(string directory, string fileName) =>
        Path.Combine(directory, "icons", Sanitize(fileName));

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static void SetDisplay(XElement element, bool visible)
    {
        var style = element.Attribute("style")?.Value ?? string.Empty;
        var parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("display:", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!visible)
            parts.Add("display:none");
        if (parts.Count == 0)
            element.Attribute("style")?.Remove();
        else
            element.SetAttributeValue("style", string.Join(';', parts));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
