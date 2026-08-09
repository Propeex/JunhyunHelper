using System.Globalization;
using System.Text.Json;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Infrastructure.TarkovJson.Maps;

public sealed class TarkovMapMarkerImporter
{
    private static readonly HashSet<string> SpecialAiBossNames = new(
        ["cultist-priest", "rogue", "black-div", "af", "bloodhound"],
        StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MapMarkerDefinition> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var markers = new List<MapMarkerDefinition>();
        foreach (var map in TarkovJsonReader.ReadCollection(baseDocument.Data, "maps"))
        {
            var mapId = TarkovJsonReader.RequiredString(map, "id", "Map");
            ImportExtracts(markers, map, mapId, localization);
            ImportTransits(markers, map, mapId, localization);
            ImportSpawns(markers, map, mapId);
            ImportPositionCollection(markers, map, mapId, "hazards", MapMarkerKind.Hazard, "위험 구역", localization, includeOutline: true);
            ImportArtillery(markers, map, mapId, localization);
            ImportPositionCollection(markers, map, mapId, "locks", MapMarkerKind.Lock, "잠금 지점", localization);
            ImportPositionCollection(markers, map, mapId, "switches", MapMarkerKind.Switch, "스위치", localization);
            ImportPositionCollection(markers, map, mapId, "stationaryWeapons", MapMarkerKind.StationaryWeapon, "고정 화기", localization);
            ImportBtrStops(markers, map, mapId, localization);
            ImportPositionCollection(markers, map, mapId, "lootContainers", MapMarkerKind.LootContainer, "루팅 컨테이너", localization);
            ImportPositionCollection(markers, map, mapId, "lootLoose", MapMarkerKind.LooseLoot, "루즈 루트", localization);
        }

        return markers
            .DistinctBy(marker => marker.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ImportExtracts(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId,
        TarkovLocalization localization)
    {
        var index = 0;
        foreach (var raw in ReadOptionalCollection(map, "extracts"))
        {
            if (!TryReadPosition(raw, out var position))
                continue;

            var faction = TarkovJsonReader.OptionalString(raw, "faction")?.Trim().ToLowerInvariant();
            var kind = faction switch
            {
                "scav" => MapMarkerKind.ScavExtract,
                "shared" or "all" or "both" => MapMarkerKind.SharedExtract,
                _ => MapMarkerKind.PmcExtract,
            };
            var name = ResolveName(raw, localization, kind switch
            {
                MapMarkerKind.ScavExtract => "Scav 탈출구",
                MapMarkerKind.SharedExtract => "공용 탈출구",
                _ => "PMC 탈출구",
            });
            var id = StableId(raw, mapId, "extract", index++);
            var detail = BuildExtractDetail(raw);
            markers.Add(new MapMarkerDefinition(
                id,
                mapId,
                kind,
                name,
                position,
                ReadOutline(raw),
                ReadNullableDouble(raw, "top"),
                ReadNullableDouble(raw, "bottom"),
                detail));
        }
    }

    private static void ImportTransits(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId,
        TarkovLocalization localization)
    {
        var index = 0;
        foreach (var raw in ReadOptionalCollection(map, "transits"))
        {
            if (!TryReadPosition(raw, out var position))
                continue;
            markers.Add(new MapMarkerDefinition(
                StableId(raw, mapId, "transit", index++),
                mapId,
                MapMarkerKind.Transit,
                ResolveName(raw, localization, "Transit"),
                position,
                ReadOutline(raw),
                ReadNullableDouble(raw, "top"),
                ReadNullableDouble(raw, "bottom"),
                null));
        }
    }

    private static void ImportSpawns(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId)
    {
        var (bossSpawnKeys, specialAiSpawnKeys) = ReadBossSpawnKeys(map);
        var index = 0;
        foreach (var raw in ReadOptionalCollection(map, "spawns"))
        {
            if (!TryReadPosition(raw, out var position))
                continue;

            var categories = ReadStringArray(raw, "categories");
            var sides = ReadStringArray(raw, "sides");
            var zoneName = TarkovJsonReader.OptionalString(raw, "zoneName");
            var kind = ClassifySpawn(
                categories,
                sides,
                zoneName,
                bossSpawnKeys,
                specialAiSpawnKeys);
            if (kind is null)
                continue;

            var name = kind.Value switch
            {
                MapMarkerKind.PmcSpawn => "PMC 스폰",
                MapMarkerKind.ScavSpawn => "Scav 스폰",
                MapMarkerKind.SniperScav => "저격 Scav",
                MapMarkerKind.Boss => "Boss 스폰",
                MapMarkerKind.SpecialAi => "특수 AI 스폰",
                _ => "스폰",
            };
            var detail = string.IsNullOrWhiteSpace(zoneName) ? null : zoneName;
            markers.Add(new MapMarkerDefinition(
                StableId(raw, mapId, "spawn", index++),
                mapId,
                kind.Value,
                name,
                position,
                Array.Empty<MapOutlinePoint>(),
                position.Y,
                position.Y,
                detail));
        }
    }

    private static (IReadOnlySet<string> BossSpawnKeys, IReadOnlySet<string> SpecialAiSpawnKeys)
        ReadBossSpawnKeys(JsonElement map)
    {
        var bossSpawnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var specialAiSpawnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var boss in ReadOptionalCollection(map, "bosses"))
        {
            var normalizedName = TarkovJsonReader.OptionalString(boss, "normalizedName") ?? string.Empty;
            var target = SpecialAiBossNames.Contains(normalizedName)
                ? specialAiSpawnKeys
                : bossSpawnKeys;

            foreach (var spawnLocation in ReadOptionalCollection(boss, "spawnLocations"))
            {
                var spawnKey = TarkovJsonReader.OptionalString(spawnLocation, "spawnKey");
                if (!string.IsNullOrWhiteSpace(spawnKey))
                    target.Add(spawnKey);
            }
        }

        return (bossSpawnKeys, specialAiSpawnKeys);
    }

    private static MapMarkerKind? ClassifySpawn(
        IReadOnlySet<string> categories,
        IReadOnlySet<string> sides,
        string? zoneName,
        IReadOnlySet<string> bossSpawnKeys,
        IReadOnlySet<string> specialAiSpawnKeys)
    {
        if (categories.Contains("boss"))
        {
            if (!string.IsNullOrWhiteSpace(zoneName) && specialAiSpawnKeys.Contains(zoneName))
                return MapMarkerKind.SpecialAi;
            if (!string.IsNullOrWhiteSpace(zoneName) && bossSpawnKeys.Contains(zoneName))
                return MapMarkerKind.Boss;

            // The current Tarkov.dev map also treats an unmatched boss-category
            // spawn as a normal Scav spawn when it is otherwise a Scav bot zone.
            if (categories.Contains("bot") && sides.Contains("scav"))
                return MapMarkerKind.ScavSpawn;
            return MapMarkerKind.Boss;
        }

        if (categories.Contains("player") && (sides.Contains("pmc") || sides.Contains("all")))
            return MapMarkerKind.PmcSpawn;
        if (categories.Contains("sniper"))
            return MapMarkerKind.SniperScav;
        if (sides.Contains("scav") &&
            (categories.Contains("bot") || categories.Contains("all") || categories.Count == 0))
            return MapMarkerKind.ScavSpawn;
        return null;
    }

    private static void ImportArtillery(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId,
        TarkovLocalization localization)
    {
        if (map.ValueKind != JsonValueKind.Object ||
            !map.TryGetProperty("artillery", out var artillery) ||
            artillery.ValueKind != JsonValueKind.Object)
            return;

        var index = 0;
        foreach (var raw in ReadOptionalCollection(artillery, "zones"))
        {
            if (!TryReadPosition(raw, out var position))
                continue;

            markers.Add(new MapMarkerDefinition(
                StableId(raw, mapId, "artillery", index++),
                mapId,
                MapMarkerKind.Hazard,
                ResolveName(raw, localization, "박격포 위험 구역"),
                position,
                ReadOutline(raw),
                ReadNullableDouble(raw, "top") ?? position.Y,
                ReadNullableDouble(raw, "bottom") ?? position.Y,
                "artillery"));
        }
    }

    private static void ImportPositionCollection(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId,
        string propertyName,
        MapMarkerKind kind,
        string fallbackName,
        TarkovLocalization localization,
        bool includeOutline = false)
    {
        var index = 0;
        foreach (var raw in ReadOptionalCollection(map, propertyName))
        {
            if (!TryReadPosition(raw, out var position))
                continue;

            markers.Add(new MapMarkerDefinition(
                StableId(raw, mapId, propertyName, index++),
                mapId,
                kind,
                ResolveName(raw, localization, fallbackName),
                position,
                includeOutline ? ReadOutline(raw) : Array.Empty<MapOutlinePoint>(),
                ReadNullableDouble(raw, "top") ?? position.Y,
                ReadNullableDouble(raw, "bottom") ?? position.Y,
                ReadDetail(raw, propertyName)));
        }
    }

    private static void ImportBtrStops(
        ICollection<MapMarkerDefinition> markers,
        JsonElement map,
        string mapId,
        TarkovLocalization localization)
    {
        var index = 0;
        foreach (var raw in ReadOptionalCollection(map, "btrStops"))
        {
            if (!TryReadPosition(raw, out var position))
                continue;
            markers.Add(new MapMarkerDefinition(
                StableId(raw, mapId, "btr", index++),
                mapId,
                MapMarkerKind.BtrStop,
                ResolveName(raw, localization, "BTR 정류장"),
                position,
                Array.Empty<MapOutlinePoint>(),
                position.Y,
                position.Y,
                null));
        }
    }

    private static string? BuildExtractDetail(JsonElement raw)
    {
        var parts = new List<string>();
        var chance = ReadNullableDouble(raw, "chance");
        if (chance is > 0 and < 1)
            parts.Add($"확률 {chance.Value * 100:0.#}%");
        var minTime = ReadNullableDouble(raw, "minTime");
        var maxTime = ReadNullableDouble(raw, "maxTime");
        if (minTime is not null || maxTime is not null)
            parts.Add($"활성 시간 {minTime?.ToString("0", CultureInfo.InvariantCulture) ?? "?"}–{maxTime?.ToString("0", CultureInfo.InvariantCulture) ?? "?"}분");
        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string? ReadDetail(JsonElement raw, string collection)
    {
        if (collection == "hazards")
            return TarkovJsonReader.OptionalString(raw, "hazardType");
        if (collection == "lootContainers" && raw.TryGetProperty("lootContainer", out var container))
            return TarkovJsonReader.ReferenceId(container);
        return null;
    }

    private static IReadOnlyList<JsonElement> ReadOptionalCollection(JsonElement entity, string propertyName)
    {
        if (entity.ValueKind != JsonValueKind.Object ||
            !entity.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return Array.Empty<JsonElement>();

        return value.ValueKind is JsonValueKind.Array or JsonValueKind.Object
            ? TarkovJsonReader.ReadCollectionValue(value, propertyName)
            : Array.Empty<JsonElement>();
    }

    private static bool TryReadPosition(JsonElement entity, out MapWorldPosition position)
    {
        position = new MapWorldPosition(0, 0, 0);
        var value = entity;
        if (entity.ValueKind == JsonValueKind.Object && entity.TryGetProperty("position", out var nested) &&
            nested.ValueKind == JsonValueKind.Object)
        {
            value = nested;
        }

        if (!TryReadDouble(value, "x", out var x) || !TryReadDouble(value, "z", out var z))
            return false;
        _ = TryReadDouble(value, "y", out var y);
        position = new MapWorldPosition(x, y, z);
        return true;
    }

    private static IReadOnlyList<MapOutlinePoint> ReadOutline(JsonElement entity)
    {
        if (entity.ValueKind != JsonValueKind.Object ||
            !entity.TryGetProperty("outline", out var value) ||
            value.ValueKind != JsonValueKind.Array)
            return Array.Empty<MapOutlinePoint>();

        var result = new List<MapOutlinePoint>();
        foreach (var point in value.EnumerateArray())
        {
            if (TryReadDouble(point, "x", out var x) && TryReadDouble(point, "z", out var z))
                result.Add(new MapOutlinePoint(x, z));
        }
        return result;
    }

    private static IReadOnlySet<string> ReadStringArray(JsonElement entity, string propertyName)
    {
        if (entity.ValueKind != JsonValueKind.Object ||
            !entity.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.Array)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string StableId(JsonElement raw, string mapId, string kind, int index)
    {
        var rawId = TarkovJsonReader.OptionalString(raw, "id") ??
                    TarkovJsonReader.OptionalString(raw, "zoneName") ??
                    TarkovJsonReader.OptionalString(raw, "name");
        return string.IsNullOrWhiteSpace(rawId)
            ? $"{mapId}:{kind}:{index.ToString(CultureInfo.InvariantCulture)}"
            : $"{mapId}:{kind}:{rawId}";
    }

    private static string ResolveName(
        JsonElement raw,
        TarkovLocalization localization,
        string fallback)
    {
        var value = TarkovJsonReader.OptionalString(raw, "name");
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        var localized = localization.Resolve(value);
        return !string.IsNullOrWhiteSpace(localized.Korean)
            ? localized.Korean!
            : !string.IsNullOrWhiteSpace(localized.English)
                ? localized.English!
                : value;
    }

    private static bool TryReadDouble(JsonElement entity, string propertyName, out double value)
    {
        value = 0;
        return entity.ValueKind == JsonValueKind.Object &&
               entity.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetDouble(out value) &&
               double.IsFinite(value);
    }

    private static double? ReadNullableDouble(JsonElement entity, string propertyName) =>
        TryReadDouble(entity, propertyName, out var value) ? value : null;
}
