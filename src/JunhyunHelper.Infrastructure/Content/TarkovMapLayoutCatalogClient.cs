using System.Net.Http;
using System.Text.Json;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Core.Reference;

namespace JunhyunHelper.Infrastructure.Content;

public sealed record MapLayoutCatalogResult(
    IReadOnlyList<MapLayoutDefinition> Layouts,
    IReadOnlyList<string> Warnings);

public sealed class TarkovMapLayoutCatalogClient
{
    public const string MetadataUrl =
        "https://raw.githubusercontent.com/the-hideout/tarkov-dev/refs/heads/main/src/data/maps.json";

    private const string SvgAssetPrefix = "https://assets.tarkov.dev/maps/svg/";
    private const string SvgRepositoryPrefix =
        "https://raw.githubusercontent.com/the-hideout/tarkov-dev-svg-maps/refs/heads/main/";

    private readonly HttpClient _httpClient;
    private readonly LegacyTarkovHelperMapCatalogClient _legacyMapClient;

    public TarkovMapLayoutCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _legacyMapClient = new LegacyTarkovHelperMapCatalogClient(httpClient);
    }

    public async Task<MapLayoutCatalogResult> LoadAsync(
        IReadOnlyList<MapReference> maps,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maps);

        using var response = await _httpClient.GetAsync(MetadataUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Tarkov.dev map metadata root must be an array.");

        var templates = ParseTemplates(document.RootElement);
        if (templates.Count == 0)
            throw new InvalidDataException("Tarkov.dev map metadata contains no interactive layouts.");

        var warnings = new List<string>();
        var layouts = new List<MapLayoutDefinition>();
        foreach (var map in maps)
        {
            var candidates = CandidateKeys(map).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var template = templates.FirstOrDefault(candidate =>
                candidate.MatchKeys.Any(candidates.Contains));
            if (template is null)
            {
                warnings.Add($"No interactive Tarkov.dev layout matched map '{map.Id}' ({map.NameEn ?? map.NormalizedKey}).");
                continue;
            }

            layouts.Add(new MapLayoutDefinition(
                map.Id,
                template.Key,
                template.NormalizedName,
                template.MinZoom,
                template.MaxZoom,
                template.Transform,
                template.CoordinateRotation,
                template.Bounds,
                template.SvgBounds,
                template.SvgUrl,
                template.BaseSvgLayer,
                template.Floors,
                template.Attribution,
                template.AttributionUrl));
        }

        if (layouts.Count == 0)
            throw new InvalidDataException("No canonical maps could be matched to Tarkov.dev interactive layouts.");

        // Current Tarkov.dev metadata remains authoritative for canonical map identity and
        // spatial floor extents. The selected presentation source is the legacy Tarkov-Helper
        // map bundle, resolved atomically at one GitHub revision so artwork and calibration
        // can update together without ever mixing revisions.
        return await _legacyMapClient.ApplyLatestAsync(
            new MapLayoutCatalogResult(layouts, warnings),
            cancellationToken);
    }

    private static IReadOnlyList<LayoutTemplate> ParseTemplates(JsonElement root)
    {
        var result = new List<LayoutTemplate>();
        foreach (var group in root.EnumerateArray())
        {
            if (group.ValueKind != JsonValueKind.Object ||
                !group.TryGetProperty("normalizedName", out var normalizedProperty) ||
                normalizedProperty.ValueKind != JsonValueKind.String ||
                !group.TryGetProperty("maps", out var mapsProperty) ||
                mapsProperty.ValueKind != JsonValueKind.Array)
                continue;

            var normalizedName = normalizedProperty.GetString()!;
            foreach (var map in mapsProperty.EnumerateArray())
            {
                if (map.ValueKind != JsonValueKind.Object ||
                    !string.Equals(ReadString(map, "projection"), "interactive", StringComparison.OrdinalIgnoreCase))
                    continue;

                var key = ReadString(map, "key");
                var svgPath = ReadString(map, "svgPath");
                var transform = ReadDoubleArray(map, "transform");
                var bounds = ReadBounds(map, "bounds");
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(svgPath) ||
                    transform.Count != 4 || bounds.Count != 2)
                    continue;

                var svgBounds = ReadBounds(map, "svgBounds");
                if (svgBounds.Count != 2)
                    svgBounds = bounds;

                var matchKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    NormalizeKey(normalizedName),
                    NormalizeKey(key),
                };
                foreach (var alt in ReadStringArray(map, "altMaps"))
                    matchKeys.Add(NormalizeKey(alt));

                var floors = ReadFloors(map);
                result.Add(new LayoutTemplate(
                    key,
                    normalizedName,
                    matchKeys,
                    ReadDouble(map, "minZoom") ?? 0.5,
                    ReadDouble(map, "maxZoom") ?? 5,
                    transform,
                    ReadDouble(map, "coordinateRotation") ?? 0,
                    bounds,
                    svgBounds,
                    ToRepositorySvgUrl(svgPath),
                    ReadString(map, "svgLayer"),
                    floors,
                    ReadString(map, "author"),
                    ReadString(map, "authorLink")));
            }
        }
        return result;
    }

    private static IReadOnlyList<MapFloorDefinition> ReadFloors(JsonElement map)
    {
        var floors = new List<MapFloorDefinition>();
        var baseRange = ReadRange(map, "heightRange") ?? (double.MinValue, double.MaxValue);
        var baseLayer = ReadString(map, "svgLayer");
        floors.Add(new MapFloorDefinition(
            "main",
            "기본층",
            baseLayer,
            baseRange.Item1,
            baseRange.Item2,
            true,
            [new MapFloorExtent(baseRange.Item1, baseRange.Item2, Array.Empty<MapWorldBounds>())]));

        if (!map.TryGetProperty("layers", out var layers) || layers.ValueKind != JsonValueKind.Array)
            return floors;

        foreach (var layer in layers.EnumerateArray())
        {
            if (layer.ValueKind != JsonValueKind.Object)
                continue;
            var svgLayer = ReadString(layer, "svgLayer");
            if (string.IsNullOrWhiteSpace(svgLayer))
                continue;

            var extents = ReadLayerExtents(layer);
            if (extents.Count == 0)
                extents = [new MapFloorExtent(double.MinValue, double.MaxValue, Array.Empty<MapWorldBounds>())];
            var minHeight = extents.Min(extent => extent.MinHeight);
            var maxHeight = extents.Max(extent => extent.MaxHeight);
            var name = ReadString(layer, "name") ?? svgLayer.Replace('_', ' ');
            floors.Add(new MapFloorDefinition(
                svgLayer,
                name,
                svgLayer,
                minHeight,
                maxHeight,
                false,
                extents));
        }
        return floors;
    }

    private static IReadOnlyList<MapFloorExtent> ReadLayerExtents(JsonElement layer)
    {
        if (!layer.TryGetProperty("extents", out var extents) || extents.ValueKind != JsonValueKind.Array)
            return Array.Empty<MapFloorExtent>();

        var result = new List<MapFloorExtent>();
        foreach (var extent in extents.EnumerateArray())
        {
            var range = ReadRange(extent, "height");
            if (range is null)
                continue;
            result.Add(new MapFloorExtent(
                range.Value.Item1,
                range.Value.Item2,
                ReadExtentBounds(extent)));
        }
        return result;
    }

    private static IReadOnlyList<MapWorldBounds> ReadExtentBounds(JsonElement extent)
    {
        if (!extent.TryGetProperty("bounds", out var bounds) || bounds.ValueKind != JsonValueKind.Array)
            return Array.Empty<MapWorldBounds>();

        var result = new List<MapWorldBounds>();
        foreach (var rawBounds in bounds.EnumerateArray())
        {
            if (rawBounds.ValueKind != JsonValueKind.Array)
                continue;
            var parts = rawBounds.EnumerateArray().Take(2).ToArray();
            if (parts.Length != 2 ||
                !TryReadCoordinatePair(parts[0], out var first) ||
                !TryReadCoordinatePair(parts[1], out var second))
                continue;
            result.Add(new MapWorldBounds(first, second));
        }
        return result;
    }

    private static (double, double)? ReadRange(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return null;
        var values = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out _))
            .Select(item => item.GetDouble())
            .Take(2)
            .ToArray();
        return values.Length == 2 && values.All(double.IsFinite)
            ? (Math.Min(values[0], values[1]), Math.Max(values[0], values[1]))
            : null;
    }

    private static IReadOnlyList<MapBoundsPoint> ReadBounds(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<MapBoundsPoint>();
        var result = new List<MapBoundsPoint>();
        foreach (var point in value.EnumerateArray())
        {
            if (TryReadCoordinatePair(point, out var pair))
                result.Add(pair);
        }
        return result;
    }

    private static bool TryReadCoordinatePair(JsonElement point, out MapBoundsPoint pair)
    {
        pair = new MapBoundsPoint(0, 0);
        if (point.ValueKind != JsonValueKind.Array)
            return false;
        var values = point.EnumerateArray()
            .Take(2)
            .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out _))
            .Select(item => item.GetDouble())
            .ToArray();
        if (values.Length != 2 || !values.All(double.IsFinite))
            return false;
        pair = new MapBoundsPoint(values[0], values[1]);
        return true;
    }

    private static IReadOnlyList<double> ReadDoubleArray(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<double>();
        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out _))
            .Select(item => item.GetDouble())
            .Where(double.IsFinite)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();
        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToArray();
    }

    private static string? ReadString(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? ReadDouble(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var number) && double.IsFinite(number)
            ? number
            : null;

    private static IEnumerable<string> CandidateKeys(MapReference map)
    {
        if (!string.IsNullOrWhiteSpace(map.NormalizedKey))
            yield return NormalizeKey(map.NormalizedKey);
        if (!string.IsNullOrWhiteSpace(map.NameEn))
            yield return NormalizeKey(map.NameEn);
        if (!string.IsNullOrWhiteSpace(map.NameKo))
            yield return NormalizeKey(map.NameKo);
    }

    private static string NormalizeKey(string value) =>
        new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string ToRepositorySvgUrl(string sourceUrl)
    {
        if (sourceUrl.StartsWith(SvgAssetPrefix, StringComparison.OrdinalIgnoreCase))
            return SvgRepositoryPrefix + sourceUrl[SvgAssetPrefix.Length..];
        return sourceUrl;
    }

    private sealed record LayoutTemplate(
        string Key,
        string NormalizedName,
        IReadOnlySet<string> MatchKeys,
        double MinZoom,
        double MaxZoom,
        IReadOnlyList<double> Transform,
        double CoordinateRotation,
        IReadOnlyList<MapBoundsPoint> Bounds,
        IReadOnlyList<MapBoundsPoint> SvgBounds,
        string SvgUrl,
        string? BaseSvgLayer,
        IReadOnlyList<MapFloorDefinition> Floors,
        string? Attribution,
        string? AttributionUrl);
}
