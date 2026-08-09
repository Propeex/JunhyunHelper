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
    private const int MaxSvgBytes = 32 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

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
        ResetCandidate();
        Directory.CreateDirectory(CandidateDirectory);

        try
        {
            progress?.Report(new MapAssetUpdateProgress(0, 1, "지도 레이아웃 정보를 확인하는 중..."));
            var catalog = await _layoutClient.LoadAsync(content.Maps, cancellationToken);
            var layouts = catalog.Layouts;
            if (layouts.Count == 0)
                throw new InvalidDataException("No usable Map layouts were returned.");

            var total = layouts.Count;
            var completed = 0;
            foreach (var layout in layouts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var filePath = GetRawSvgPath(CandidateDirectory, layout);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await DownloadSvgAsync(layout.SvgUrl, filePath, cancellationToken);
                completed++;
                progress?.Report(new MapAssetUpdateProgress(
                    completed,
                    total,
                    $"지도 다운로드 중... {completed}/{total}"));
            }

            await File.WriteAllTextAsync(
                Path.Combine(CandidateDirectory, "layouts.json"),
                JsonSerializer.Serialize(layouts, JsonOptions),
                cancellationToken);
            await ValidateDirectoryAsync(CandidateDirectory, layouts, cancellationToken);
            ActivateCandidate();
            return new MapAssetUpdateResult(true, layouts, catalog.Warnings);
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

    private async Task DownloadSvgAsync(
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
