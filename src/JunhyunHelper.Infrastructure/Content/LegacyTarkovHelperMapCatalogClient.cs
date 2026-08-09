using System.Net.Http;
using System.Text.Json;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Infrastructure.Content;

/// <summary>
/// Loads the legacy Tarkov-Helper map artwork and its affine calibration as one atomic
/// repository revision. The commit SHA is resolved first, then both map_configs.json and
/// every SVG URL are pinned to that exact SHA so an upstream update can never mix a new
/// image with an old transform (or vice versa).
/// </summary>
internal sealed class LegacyTarkovHelperMapCatalogClient
{
    private const string CommitApiUrl =
        "https://api.github.com/repos/Propeex/Tarkov-Helper/commits/main";
    private const string RepositoryRoot =
        "https://raw.githubusercontent.com/Propeex/Tarkov-Helper/";
    private const string ConfigRelativePath =
        "TarkovHelper/Assets/DB/Data/map_configs.json";
    private const string MapsRelativePath =
        "TarkovHelper/Assets/DB/Maps/";

    private readonly HttpClient _httpClient;

    public LegacyTarkovHelperMapCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<MapLayoutCatalogResult> ApplyLatestAsync(
        MapLayoutCatalogResult current,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);

        try
        {
            var revision = await ResolveMainRevisionAsync(cancellationToken);
            var templates = await LoadTemplatesAsync(revision, cancellationToken);
            if (templates.Count == 0)
                throw new InvalidDataException("Legacy Tarkov-Helper map config contained no usable map definitions.");

            return ApplyRevision(current, revision, templates);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The pinned adapter is the last known-good presentation/calibration pair.
            // An online source problem must not remove maps that were already validated.
            var fallback = LegacyTarkovHelperMapLayoutAdapter.Apply(current);
            var warnings = fallback.Warnings
                .Prepend(
                    $"Latest Propeex/Tarkov-Helper map revision could not be validated; " +
                    $"the pinned known-good map bundle was kept: {exception.Message}")
                .ToArray();
            return new MapLayoutCatalogResult(fallback.Layouts, warnings);
        }
    }

    private async Task<string> ResolveMainRevisionAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CommitApiUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", "JunhyunHelper/1.0");
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var sha = document.RootElement.ValueKind == JsonValueKind.Object &&
                  document.RootElement.TryGetProperty("sha", out var shaProperty) &&
                  shaProperty.ValueKind == JsonValueKind.String
            ? shaProperty.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(sha) ||
            sha.Length is < 7 or > 64 ||
            !sha.All(Uri.IsHexDigit))
            throw new InvalidDataException("GitHub did not return a valid Tarkov-Helper revision SHA.");

        return sha.ToLowerInvariant();
    }

    private async Task<IReadOnlyList<LegacyTemplate>> LoadTemplatesAsync(
        string revision,
        CancellationToken cancellationToken)
    {
        var url = RepositoryRoot + revision + "/" + ConfigRelativePath;
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("maps", out var maps) ||
            maps.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Legacy Tarkov-Helper map config root is invalid.");

        var result = new List<LegacyTemplate>();
        foreach (var map in maps.EnumerateArray())
        {
            if (TryParseTemplate(map, out var template) && template is not null)
                result.Add(template);
        }
        return result;
    }

    private static bool TryParseTemplate(JsonElement map, out LegacyTemplate? template)
    {
        template = null;
        if (map.ValueKind != JsonValueKind.Object)
            return false;

        var key = ReadString(map, "key");
        var svgFileName = ReadString(map, "svgFileName");
        var width = ReadDouble(map, "imageWidth");
        var height = ReadDouble(map, "imageHeight");
        var transform = ReadDoubleArray(map, "playerMarkerTransform");
        if (string.IsNullOrWhiteSpace(key) ||
            !IsSafeSvgFileName(svgFileName) ||
            width is not > 0 || height is not > 0 ||
            width > 20000 || height > 20000 ||
            transform.Count != 6)
            return false;

        var matchKeys = ReadStringArray(map, "aliases")
            .Append(key)
            .Select(Normalize)
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (matchKeys.Count == 0)
            return false;

        var floors = new List<LegacyFloor>();
        if (map.TryGetProperty("floors", out var floorArray) && floorArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var floor in floorArray.EnumerateArray())
            {
                var layerId = ReadString(floor, "layerId");
                var displayName = ReadString(floor, "displayName");
                var order = ReadInt(floor, "order");
                if (string.IsNullOrWhiteSpace(layerId) || string.IsNullOrWhiteSpace(displayName) || order is null)
                    continue;
                floors.Add(new LegacyFloor(
                    layerId,
                    displayName,
                    order.Value,
                    ReadBool(floor, "isDefault") ?? false));
            }
        }

        if (floors.Count(floor => floor.IsDefault) > 1)
            return false;

        template = new LegacyTemplate(
            key,
            svgFileName!,
            width.Value,
            height.Value,
            transform,
            matchKeys,
            floors);
        return true;
    }

    private static MapLayoutCatalogResult ApplyRevision(
        MapLayoutCatalogResult current,
        string revision,
        IReadOnlyList<LegacyTemplate> templates)
    {
        var warnings = current.Warnings.ToList();
        var adapted = new List<MapLayoutDefinition>();
        var pinnedFallback = LegacyTarkovHelperMapLayoutAdapter.Apply(current);
        var pinnedByMapId = pinnedFallback.Layouts.ToDictionary(layout => layout.MapId, StringComparer.Ordinal);

        foreach (var layout in current.Layouts)
        {
            var template = FindTemplate(layout, templates);
            if (template is null)
            {
                if (pinnedByMapId.TryGetValue(layout.MapId, out var pinned))
                {
                    adapted.Add(pinned);
                    warnings.Add(
                        $"Latest legacy map bundle has no usable entry for '{layout.NormalizedName}'; pinned validated map kept for this map.");
                }
                else
                {
                    warnings.Add(
                        $"Neither latest nor pinned legacy Tarkov-Helper has an approved map for '{layout.NormalizedName}'.");
                }
                continue;
            }

            adapted.Add(layout with
            {
                Key = "legacy-" + template.Key,
                SvgUrl = RepositoryRoot + revision + "/" + MapsRelativePath + template.SvgFileName,
                BaseSvgLayer = template.Floors.FirstOrDefault(floor => floor.IsDefault)?.LayerId,
                Floors = BuildFloors(layout.Floors, template.Floors),
                Attribution = $"Propeex/Tarkov-Helper map · {revision[..Math.Min(8, revision.Length)]}",
                AttributionUrl =
                    "https://github.com/Propeex/Tarkov-Helper/tree/" + revision +
                    "/TarkovHelper/Assets/DB/Maps",
                LegacyPlayerTransform = template.PlayerTransform,
                SurfaceWidth = template.Width,
                SurfaceHeight = template.Height,
            });
        }

        if (adapted.Count == 0)
            throw new InvalidDataException("No current Tarkov maps matched a validated legacy map bundle.");

        return new MapLayoutCatalogResult(adapted, warnings);
    }

    private static LegacyTemplate? FindTemplate(
        MapLayoutDefinition layout,
        IReadOnlyList<LegacyTemplate> templates)
    {
        var keys = new[] { layout.Key, layout.NormalizedName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return templates.FirstOrDefault(template => template.MatchKeys.Any(keys.Contains));
    }

    private static IReadOnlyList<MapFloorDefinition> BuildFloors(
        IReadOnlyList<MapFloorDefinition> currentFloors,
        IReadOnlyList<LegacyFloor> legacyFloors)
    {
        if (legacyFloors.Count == 0)
        {
            var source = currentFloors.FirstOrDefault(floor => floor.IsDefault)
                         ?? currentFloors.FirstOrDefault();
            return source is null
                ? [new MapFloorDefinition("main", "Ground Floor", "main", double.MinValue, double.MaxValue, true)]
                : [source with { Id = "main", Name = "Ground Floor", SvgLayer = "main", IsDefault = true }];
        }

        var remaining = currentFloors.ToList();
        var result = new List<MapFloorDefinition>(legacyFloors.Count);
        foreach (var legacy in legacyFloors.OrderBy(floor => floor.Order))
        {
            var source = FindNamedFloor(legacy, remaining);
            if (source is null && legacy.IsDefault)
                source = remaining.FirstOrDefault(floor => floor.IsDefault);
            if (source is null)
            {
                var ordered = remaining
                    .OrderBy(RepresentativeHeight)
                    .ToArray();
                source = legacy.Order < 0
                    ? ordered.FirstOrDefault(LooksBelowGround)
                    : legacy.Order > 0
                        ? ordered.FirstOrDefault(floor => !floor.IsDefault && !LooksBelowGround(floor))
                        : ordered.FirstOrDefault();
            }

            if (source is not null)
                remaining.Remove(source);

            if (source is null)
            {
                result.Add(new MapFloorDefinition(
                    legacy.LayerId,
                    legacy.DisplayName,
                    legacy.LayerId,
                    1,
                    0,
                    legacy.IsDefault,
                    [new MapFloorExtent(1, 0, Array.Empty<MapWorldBounds>())]));
                continue;
            }

            result.Add(source with
            {
                Id = legacy.LayerId,
                Name = legacy.DisplayName,
                SvgLayer = legacy.LayerId,
                IsDefault = legacy.IsDefault,
            });
        }

        return result;
    }

    private static MapFloorDefinition? FindNamedFloor(
        LegacyFloor legacy,
        IReadOnlyList<MapFloorDefinition> current)
    {
        var legacyLayer = Normalize(legacy.LayerId);
        var legacyName = Normalize(legacy.DisplayName);
        return current.FirstOrDefault(floor =>
        {
            var candidate = Normalize($"{floor.Id} {floor.Name} {floor.SvgLayer}");
            return candidate.Contains(legacyLayer, StringComparison.OrdinalIgnoreCase) ||
                   legacyLayer.Contains(candidate, StringComparison.OrdinalIgnoreCase) ||
                   candidate.Contains(legacyName, StringComparison.OrdinalIgnoreCase) ||
                   legacyName.Contains(candidate, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool LooksBelowGround(MapFloorDefinition floor)
    {
        var key = Normalize(floor.Id + floor.Name + floor.SvgLayer);
        return key.Contains("basement", StringComparison.Ordinal) ||
               key.Contains("bunker", StringComparison.Ordinal) ||
               key.Contains("underground", StringComparison.Ordinal) ||
               key.Contains("garage", StringComparison.Ordinal) ||
               key.Contains("parking", StringComparison.Ordinal) ||
               key.Contains("cellar", StringComparison.Ordinal);
    }

    private static double RepresentativeHeight(MapFloorDefinition floor)
    {
        var values = floor.Extents
            .Where(extent => double.IsFinite(extent.MinHeight) && double.IsFinite(extent.MaxHeight))
            .Select(extent => (extent.MinHeight + extent.MaxHeight) / 2)
            .ToArray();
        return values.Length == 0 ? 0 : values.Average();
    }

    private static bool IsSafeSvgFileName(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal) &&
        string.Equals(Path.GetExtension(value), ".svg", StringComparison.OrdinalIgnoreCase);

    private static string? ReadString(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? ReadDouble(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var number) && double.IsFinite(number)
            ? number
            : null;

    private static int? ReadInt(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : null;

    private static bool? ReadBool(JsonElement entity, string propertyName) =>
        entity.TryGetProperty(propertyName, out var value) &&
        (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : null;

    private static IReadOnlyList<double> ReadDoubleArray(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
            return Array.Empty<double>();
        var values = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out _))
            .Select(item => item.GetDouble())
            .ToArray();
        return values.All(double.IsFinite) ? values : Array.Empty<double>();
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

    private static string Normalize(string value) =>
        new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private sealed record LegacyTemplate(
        string Key,
        string SvgFileName,
        double Width,
        double Height,
        IReadOnlyList<double> PlayerTransform,
        IReadOnlySet<string> MatchKeys,
        IReadOnlyList<LegacyFloor> Floors);

    private sealed record LegacyFloor(
        string LayerId,
        string DisplayName,
        int Order,
        bool IsDefault);
}
