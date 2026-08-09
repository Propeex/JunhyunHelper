using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Infrastructure.Content;

/// <summary>
/// Adapts the current online Tarkov map/floor metadata to the exact map artwork and
/// affine player-position calibration used by the legacy Propeex/Tarkov-Helper.
///
/// Online layout metadata remains useful for current floor spatial extents, but it
/// no longer decides the presentation artwork or player-marker calibration. Those
/// are pinned to the verified legacy repository revision selected by the user.
/// </summary>
internal static class LegacyTarkovHelperMapLayoutAdapter
{
    private const string LegacyCommit = "9371c4769d8da8acb9df864a2c88f83ecdd42818";
    private const string LegacyMapRoot =
        "https://raw.githubusercontent.com/Propeex/Tarkov-Helper/" + LegacyCommit +
        "/TarkovHelper/Assets/DB/Maps/";

    private static readonly LegacyMapTemplate[] Templates =
    [
        Map("Woods", "Woods.svg", 4800, 4800, [-2, 0, 0, 2, 2200, 2841.5],
            ["woods"]),
        Map("Customs", "Customs.svg", 4400, 3200, [-2, 0, 0, 2, 2600, 1601.5],
            ["customs", "bigmap"],
            Floor("basement", "Basement", -1), Floor("main", "Ground Floor", 0, true),
            Floor("level2", "Level 2", 1), Floor("level3", "Level 3", 2)),
        Map("Shoreline", "Shoreline.svg", 3700, 3100, [-1, 0, 0, 1, 1570, 1451.5],
            ["shoreline"],
            Floor("basement", "Basement", -1), Floor("main", "Ground Floor", 0, true),
            Floor("level2", "Level 2", 1), Floor("level3", "Level 3", 2)),
        Map("Interchange", "Interchange.svg", 4000, 3900, [-2, 0, 0, 2, 2166, 2004],
            ["interchange", "shoppingmall"],
            Floor("main", "Ground Floor", 0, true), Floor("level2", "Level 2", 1),
            Floor("level3", "Level 3", 2)),
        Map("Reserve", "Reserve.svg", 3200, 3000, [-1.932, 0.518, 0.517, 1.932, 1600.02, 1520.08],
            ["reserve", "rezervbase"],
            Floor("bunker", "Bunker", -1), Floor("main", "Ground Floor", 0, true),
            Floor("level2", "Level 2", 1), Floor("level3", "Level 3", 2),
            Floor("level4", "Level 4", 3)),
        Map("Lighthouse", "Lighthouse.svg", 3100, 3700, [-1, 0, 0, 1, 1550, 2051.5],
            ["lighthouse"]),
        Map("StreetsOfTarkov", "StreetsOfTarkov.svg", 3260, 3500, [-2, 0, 0, 2, 1660, 1421.5],
            ["streets", "tarkovstreets", "streetsoftarkov", "streets-of-tarkov"]),
        Map("Factory", "Factory.svg", 3600, 3600, [0, -10, -10, 0, 1800, 1851.5],
            ["factory", "factory4day", "factory4night", "nightfactory"],
            Floor("basement", "Basement", -1), Floor("main", "Ground Floor", 0, true),
            Floor("level2", "Level 2", 1), Floor("level3", "Level 3", 2)),
        Map("GroundZero", "GroundZero.svg", 2800, 3100, [-2, 0, 0, 2, 1600, 1301.5],
            ["groundzero", "ground-zero", "sandbox", "sandboxhigh", "groundzero21", "ground-zero-21"],
            Floor("basement", "Basement", -1), Floor("main", "Ground Floor", 0, true),
            Floor("level2", "Level 2", 1), Floor("level3", "Level 3", 2)),
        Map("Labs", "Labs.svg", 5500, 4200, [0, 10, 10, 0, 6100, 4051.5],
            ["labs", "lab", "laboratory", "thelab", "the-lab"],
            Floor("basement", "Basement", -1), Floor("main", "Main Floor", 0, true),
            Floor("level2", "Level 2", 1)),
        Map("Labyrinth", "Labyrinth.svg", 3300, 3200, [0.0159, 9.863, 9.8655, 0.0502, 1482.9, 1597.9],
            ["labyrinth", "thelabyrinth", "the-labyrinth"]),
        Map("Terminal", "Terminal.svg", 2663, 3132, [-0.992, 0, 0, 0.994, 1346, 1615.5],
            ["terminal"]),
    ];

    public static MapLayoutCatalogResult Apply(MapLayoutCatalogResult current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var warnings = current.Warnings.ToList();
        var adapted = new List<MapLayoutDefinition>();

        foreach (var layout in current.Layouts)
        {
            var template = FindTemplate(layout);
            if (template is null)
            {
                warnings.Add(
                    $"Legacy Tarkov-Helper has no approved Map asset for '{layout.NormalizedName}'; the Map is omitted instead of mixing presentation systems.");
                continue;
            }

            adapted.Add(layout with
            {
                Key = "legacy-" + template.Key,
                SvgUrl = LegacyMapRoot + template.SvgFileName,
                BaseSvgLayer = template.Floors.FirstOrDefault(floor => floor.IsDefault)?.LayerId,
                Floors = BuildFloors(layout.Floors, template.Floors),
                Attribution = "Propeex/Tarkov-Helper legacy map",
                AttributionUrl = "https://github.com/Propeex/Tarkov-Helper/tree/" + LegacyCommit + "/TarkovHelper/Assets/DB/Maps",
                LegacyPlayerTransform = template.PlayerTransform,
                SurfaceWidth = template.Width,
                SurfaceHeight = template.Height,
            });
        }

        if (adapted.Count == 0)
            throw new InvalidDataException("No current Tarkov maps matched the approved legacy Tarkov-Helper map set.");

        return new MapLayoutCatalogResult(adapted, warnings);
    }

    private static LegacyMapTemplate? FindTemplate(MapLayoutDefinition layout)
    {
        var keys = new[] { layout.Key, layout.NormalizedName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Templates.FirstOrDefault(template =>
            template.MatchKeys.Any(keys.Contains));
    }

    private static IReadOnlyList<MapFloorDefinition> BuildFloors(
        IReadOnlyList<MapFloorDefinition> currentFloors,
        IReadOnlyList<LegacyFloorTemplate> legacyFloors)
    {
        if (legacyFloors.Count == 0)
        {
            var current = currentFloors.FirstOrDefault(floor => floor.IsDefault)
                          ?? currentFloors.FirstOrDefault();
            return current is null
                ? [new MapFloorDefinition("main", "Ground Floor", "main", double.MinValue, double.MaxValue, true)]
                : [current with { Id = "main", Name = "Ground Floor", SvgLayer = "main", IsDefault = true }];
        }

        var defaultSource = currentFloors.FirstOrDefault(floor => floor.IsDefault)
                            ?? currentFloors.FirstOrDefault();
        var belowCandidates = currentFloors
            .Where(floor => !ReferenceEquals(floor, defaultSource) && LooksBelowGround(floor))
            .OrderBy(RepresentativeHeight)
            .ToList();
        var aboveCandidates = currentFloors
            .Where(floor => !ReferenceEquals(floor, defaultSource) && !LooksBelowGround(floor))
            .OrderBy(RepresentativeHeight)
            .ToList();

        var result = new List<MapFloorDefinition>(legacyFloors.Count);
        var belowIndex = 0;
        var aboveIndex = 0;
        foreach (var legacy in legacyFloors.OrderBy(floor => floor.Order))
        {
            MapFloorDefinition? source;
            if (legacy.IsDefault)
                source = defaultSource;
            else if (legacy.Order < 0)
                source = belowIndex < belowCandidates.Count ? belowCandidates[belowIndex++] : null;
            else
                source = aboveIndex < aboveCandidates.Count ? aboveCandidates[aboveIndex++] : null;

            if (source is null)
            {
                // Manual floor selection must still be available even when the online
                // metadata has no matching detection extent. An intentionally empty
                // height range prevents this synthetic floor from winning auto-detect.
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
        var finite = floor.Extents
            .Where(extent => double.IsFinite(extent.MinHeight) && double.IsFinite(extent.MaxHeight))
            .Select(extent => (extent.MinHeight + extent.MaxHeight) / 2)
            .ToArray();
        return finite.Length == 0 ? 0 : finite.Average();
    }

    private static LegacyMapTemplate Map(
        string key,
        string svgFileName,
        double width,
        double height,
        IReadOnlyList<double> transform,
        IReadOnlyList<string> aliases,
        params LegacyFloorTemplate[] floors)
    {
        var matchKeys = aliases
            .Append(key)
            .Select(Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new LegacyMapTemplate(key, svgFileName, width, height, transform, matchKeys, floors);
    }

    private static LegacyFloorTemplate Floor(string layerId, string displayName, int order, bool isDefault = false) =>
        new(layerId, displayName, order, isDefault);

    private static string Normalize(string value) =>
        new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private sealed record LegacyMapTemplate(
        string Key,
        string SvgFileName,
        double Width,
        double Height,
        IReadOnlyList<double> PlayerTransform,
        IReadOnlySet<string> MatchKeys,
        IReadOnlyList<LegacyFloorTemplate> Floors);

    private sealed record LegacyFloorTemplate(
        string LayerId,
        string DisplayName,
        int Order,
        bool IsDefault);
}