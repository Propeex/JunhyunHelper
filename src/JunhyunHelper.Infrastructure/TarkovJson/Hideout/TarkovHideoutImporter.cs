using System.Text.Json;
using JunhyunHelper.Core.Hideout;

namespace JunhyunHelper.Infrastructure.TarkovJson.Hideout;

public sealed class TarkovHideoutImporter
{
    public IReadOnlyList<HideoutStation> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var stations = TarkovJsonReader.ReadCollectionValue(baseDocument.Data, "hideout");
        var result = new List<HideoutStation>(stations.Count);
        var stationIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawStation in stations)
        {
            if (rawStation.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Hideout station entries must be objects.");

            var stationId = TarkovJsonReader.RequiredString(rawStation, "id", "Hideout station");
            if (!stationIds.Add(stationId))
                throw new InvalidDataException($"Duplicate hideout station id '{stationId}'.");

            var stationName = localization.Resolve(
                TarkovJsonReader.OptionalString(rawStation, "name"));

            var levels = ReadLevels(rawStation, stationId);
            result.Add(new HideoutStation(
                stationId,
                stationName.Korean,
                stationName.English,
                TarkovJsonReader.OptionalString(rawStation, "imageLink"),
                levels));
        }

        return result;
    }

    private static IReadOnlyList<HideoutLevel> ReadLevels(
        JsonElement station,
        string stationId)
    {
        if (!station.TryGetProperty("levels", out var rawLevels))
            throw new InvalidDataException($"Hideout station '{stationId}' is missing levels.");

        var levels = TarkovJsonReader.ReadCollectionValue(rawLevels, $"hideout station {stationId} levels");
        var levelNumbers = new HashSet<int>();
        var result = new List<HideoutLevel>(levels.Count);

        foreach (var rawLevel in levels)
        {
            if (rawLevel.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException($"Hideout station '{stationId}' level must be an object.");

            var level = TarkovJsonReader.RequiredInt(rawLevel, "level", $"Hideout station '{stationId}' level");
            if (level < 0)
                throw new InvalidDataException($"Hideout station '{stationId}' has negative level '{level}'.");
            if (!levelNumbers.Add(level))
                throw new InvalidDataException($"Hideout station '{stationId}' has duplicate level '{level}'.");

            result.Add(new HideoutLevel(
                stationId,
                level,
                TarkovJsonReader.OptionalInt(rawLevel, "constructionTime"),
                ReadItemRequirements(rawLevel, stationId, level)));
        }

        return result.OrderBy(static level => level.Level).ToArray();
    }

    private static IReadOnlyList<HideoutItemRequirement> ReadItemRequirements(
        JsonElement level,
        string stationId,
        int targetLevel)
    {
        if (!level.TryGetProperty("itemRequirements", out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<HideoutItemRequirement>();
        }

        var requirements = TarkovJsonReader.ReadCollectionValue(
            rawRequirements,
            $"hideout station {stationId} level {targetLevel} item requirements");

        return requirements.Select(raw =>
        {
            if (raw.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException(
                    $"Hideout station '{stationId}' level '{targetLevel}' item requirement must be an object.");
            }

            if (!raw.TryGetProperty("item", out var rawItem))
            {
                throw new InvalidDataException(
                    $"Hideout station '{stationId}' level '{targetLevel}' item requirement is missing item.");
            }

            var itemId = TarkovJsonReader.ReferenceId(rawItem);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new InvalidDataException(
                    $"Hideout station '{stationId}' level '{targetLevel}' has invalid item reference.");
            }

            var count = TarkovJsonReader.OptionalInt(raw, "count") ??
                        TarkovJsonReader.OptionalInt(raw, "quantity") ??
                        throw new InvalidDataException(
                            $"Hideout station '{stationId}' level '{targetLevel}' item requirement is missing count.");
            if (count < 0)
            {
                throw new InvalidDataException(
                    $"Hideout station '{stationId}' level '{targetLevel}' has negative item count.");
            }

            return new HideoutItemRequirement(
                stationId,
                targetLevel,
                itemId,
                count,
                ReadFoundInRaid(raw));
        }).ToArray();
    }

    private static bool ReadFoundInRaid(JsonElement requirement)
    {
        // json.tarkov.dev represents hideout item requirement metadata under
        // attributes (for example attributes.foundInRaid). Older fixtures and
        // compatible mirrors may expose foundInRaid at the requirement root,
        // so retain that shape as a fallback rather than flattening every
        // hideout material to non-FIR.
        if (requirement.TryGetProperty("attributes", out var attributes) &&
            attributes.ValueKind == JsonValueKind.Object)
        {
            var nested = TarkovJsonReader.OptionalBool(attributes, "foundInRaid");
            if (nested.HasValue)
                return nested.Value;
        }

        return TarkovJsonReader.OptionalBool(requirement, "foundInRaid") ?? false;
    }
}