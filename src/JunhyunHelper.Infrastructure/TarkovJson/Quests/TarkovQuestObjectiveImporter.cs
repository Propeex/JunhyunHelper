using System.Text.Json;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Infrastructure.TarkovJson.Quests;

public sealed class TarkovQuestObjectiveImporter
{
    public QuestObjectiveImport Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var tasks = TarkovJsonReader.ReadCollection(baseDocument.Data, "tasks");
        var objectives = new List<QuestObjective>();
        var itemRequirements = new List<QuestItemRequirement>();

        foreach (var rawTask in tasks)
        {
            if (rawTask.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Quest entries must be objects.");

            var questId = TarkovJsonReader.RequiredString(rawTask, "id", "Quest");
            if (!rawTask.TryGetProperty("objectives", out var rawObjectives) ||
                rawObjectives.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var objectiveIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rawObjective in TarkovJsonReader.ReadCollectionValue(
                         rawObjectives,
                         $"quest {questId} objectives"))
            {
                var objective = ImportObjective(rawObjective, questId, localization);
                if (!objectiveIds.Add(objective.ObjectiveId))
                {
                    throw new InvalidDataException(
                        $"Quest '{questId}' contains duplicate objective id '{objective.ObjectiveId}'.");
                }

                objectives.Add(objective);

                if (objective.ItemKind != QuestItemObjectiveKind.Submit || objective.Optional)
                    continue;

                if (objective.ItemIds.Count == 0)
                    continue;

                if (objective.Count is null or <= 0)
                {
                    throw new InvalidDataException(
                        $"Quest '{questId}' submit objective '{objective.ObjectiveId}' has no valid count.");
                }

                itemRequirements.Add(new QuestItemRequirement(
                    questId,
                    objective.ObjectiveId,
                    objective.ItemIds,
                    objective.Count.Value,
                    objective.FoundInRaid));
            }
        }

        return new QuestObjectiveImport(objectives, itemRequirements);
    }

    private static QuestObjective ImportObjective(
        JsonElement raw,
        string questId,
        TarkovLocalization localization)
    {
        if (raw.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Quest '{questId}' objective must be an object.");

        var objectiveId = TarkovJsonReader.RequiredString(raw, "id", $"Quest '{questId}' objective");
        var type = TarkovJsonReader.RequiredString(raw, "type", $"Quest '{questId}' objective '{objectiveId}'");
        var description = localization.Resolve(TarkovJsonReader.OptionalString(raw, "description"));

        return new QuestObjective(
            questId,
            objectiveId,
            type,
            description.Korean,
            description.English,
            TarkovJsonReader.OptionalBool(raw, "optional") ?? false,
            TarkovJsonReader.OptionalInt(raw, "count"),
            TarkovJsonReader.OptionalBool(raw, "foundInRaid") ?? false,
            ReadReferenceArray(raw, "maps"),
            ReadItemIds(raw),
            ReadOptionalReference(raw, "questItem"),
            ClassifyItemKind(type));
    }

    private static QuestItemObjectiveKind ClassifyItemKind(string type)
    {
        return type.Trim().ToLowerInvariant() switch
        {
            "giveitem" => QuestItemObjectiveKind.Submit,
            "finditem" or "collect" => QuestItemObjectiveKind.FindOrCollect,
            "sellitem" => QuestItemObjectiveKind.Sell,
            _ => QuestItemObjectiveKind.Other,
        };
    }

    private static IReadOnlyList<string> ReadItemIds(JsonElement objective)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        if (objective.TryGetProperty("item", out var singleItem))
        {
            var itemId = TarkovJsonReader.ReferenceId(singleItem);
            if (!string.IsNullOrWhiteSpace(itemId))
                result.Add(itemId);
        }

        if (objective.TryGetProperty("items", out var items) &&
            items.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            if (items.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Quest objective items must be an array when present.");

            foreach (var item in items.EnumerateArray())
            {
                var itemId = TarkovJsonReader.ReferenceId(item);
                if (string.IsNullOrWhiteSpace(itemId))
                    throw new InvalidDataException("Quest objective contains an invalid item reference.");
                result.Add(itemId);
            }
        }

        return result.ToArray();
    }

    private static IReadOnlyList<string> ReadReferenceArray(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var values) ||
            values.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<string>();
        }

        if (values.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Quest objective '{propertyName}' must be an array.");

        return values.EnumerateArray()
            .Select(TarkovJsonReader.ReferenceId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ReadOptionalReference(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var id = TarkovJsonReader.ReferenceId(value);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"Quest objective has invalid '{propertyName}' reference.");
        return id;
    }
}
