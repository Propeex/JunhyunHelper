using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Infrastructure.TarkovJson.Quests;

public sealed class TarkovQuestImporter
{
    public IReadOnlyList<QuestDefinition> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var prestigeLevels = ReadPrestigeLevels(baseDocument.Data);
        var tasks = TarkovJsonReader.ReadCollection(baseDocument.Data, "tasks");
        var result = new List<QuestDefinition>(tasks.Count);
        var taskIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawTask in tasks)
        {
            if (rawTask.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Quest entries must be objects.");

            var taskId = TarkovJsonReader.RequiredString(rawTask, "id", "Quest");
            if (!taskIds.Add(taskId))
                throw new InvalidDataException($"Duplicate quest id '{taskId}'.");

            var name = localization.Resolve(TarkovJsonReader.OptionalString(rawTask, "name"));

            result.Add(new QuestDefinition(
                taskId,
                name.Korean,
                name.English,
                ReadOptionalReference(rawTask, "trader", taskId),
                ReadOptionalReference(rawTask, "map", taskId),
                TarkovJsonReader.OptionalString(rawTask, "wikiLink"),
                TarkovJsonReader.OptionalInt(rawTask, "experience"),
                TarkovJsonReader.OptionalBool(rawTask, "kappaRequired") ?? false,
                TarkovJsonReader.OptionalBool(rawTask, "lightkeeperRequired") ?? false,
                TarkovJsonReader.OptionalBool(rawTask, "disabled") ?? false,
                Math.Max(0, TarkovJsonReader.OptionalInt(rawTask, "minPlayerLevel") ?? 0),
                ParseFaction(TarkovJsonReader.OptionalString(rawTask, "factionName"), taskId),
                ResolveRequiredPrestige(rawTask, prestigeLevels, taskId),
                ReadTaskRequirements(rawTask, taskId),
                ReadTraderStandingRequirements(rawTask, taskId),
                ReadTraderLoyaltyRequirements(rawTask, taskId)));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, int> ReadPrestigeLevels(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("prestige", out var rawPrestige) ||
            rawPrestige.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var raw in TarkovJsonReader.ReadCollectionValue(rawPrestige, "prestige"))
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Prestige entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Prestige");
            var level = TarkovJsonReader.OptionalInt(raw, "prestigeLevel") ??
                        TarkovJsonReader.OptionalInt(raw, "level") ??
                        throw new InvalidDataException($"Prestige '{id}' is missing its level.");
            if (level < 0)
                throw new InvalidDataException($"Prestige '{id}' has negative level '{level}'.");
            if (!result.TryAdd(id, level))
                throw new InvalidDataException($"Duplicate prestige id '{id}'.");
        }

        return result;
    }

    private static int? ResolveRequiredPrestige(
        JsonElement task,
        IReadOnlyDictionary<string, int> prestigeLevels,
        string taskId)
    {
        if (!task.TryGetProperty("requiredPrestige", out var raw) ||
            raw.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (raw.ValueKind == JsonValueKind.Object)
        {
            if (raw.TryGetProperty("prestigeLevel", out var directLevel) &&
                directLevel.ValueKind == JsonValueKind.Number &&
                directLevel.TryGetInt32(out var directPrestigeLevel))
            {
                return Math.Max(0, directPrestigeLevel);
            }

            if (raw.TryGetProperty("level", out var directLegacyLevel) &&
                directLegacyLevel.ValueKind == JsonValueKind.Number &&
                directLegacyLevel.TryGetInt32(out var legacyLevel))
            {
                return Math.Max(0, legacyLevel);
            }
        }

        var prestigeId = TarkovJsonReader.ReferenceId(raw);
        if (string.IsNullOrWhiteSpace(prestigeId))
            throw new InvalidDataException($"Quest '{taskId}' has invalid requiredPrestige.");

        if (!prestigeLevels.TryGetValue(prestigeId, out var level))
        {
            throw new InvalidDataException(
                $"Quest '{taskId}' references unknown prestige '{prestigeId}'.");
        }

        return level;
    }

    private static PmcFaction? ParseFaction(string? rawFaction, string taskId)
    {
        if (string.IsNullOrWhiteSpace(rawFaction))
            return null;

        return rawFaction.Trim().ToLowerInvariant() switch
        {
            "usec" => PmcFaction.Usec,
            "bear" => PmcFaction.Bear,
            "any" or "any target" or "all" or "both" or "pmc" => null,
            _ => throw new InvalidDataException(
                $"Quest '{taskId}' has unsupported faction '{rawFaction}'."),
        };
    }

    private static IReadOnlyList<QuestTaskRequirement> ReadTaskRequirements(
        JsonElement task,
        string taskId)
    {
        if (!task.TryGetProperty("taskRequirements", out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<QuestTaskRequirement>();
        }

        return TarkovJsonReader.ReadCollectionValue(
                rawRequirements,
                $"quest {taskId} task requirements")
            .Select(raw => ReadTaskRequirement(raw, taskId))
            .ToArray();
    }

    private static QuestTaskRequirement ReadTaskRequirement(JsonElement raw, string taskId)
    {
        if (raw.ValueKind != JsonValueKind.Object || !raw.TryGetProperty("task", out var rawTask))
            throw new InvalidDataException($"Quest '{taskId}' has invalid task requirement.");

        var requiredTaskId = TarkovJsonReader.ReferenceId(rawTask);
        if (string.IsNullOrWhiteSpace(requiredTaskId))
            throw new InvalidDataException($"Quest '{taskId}' has task requirement without task id.");

        var statuses = ReadRequiredStatuses(raw, taskId, requiredTaskId);
        return new QuestTaskRequirement(requiredTaskId, statuses);
    }

    private static IReadOnlySet<QuestRequiredStatus> ReadRequiredStatuses(
        JsonElement requirement,
        string taskId,
        string requiredTaskId)
    {
        if (!requirement.TryGetProperty("status", out var rawStatus) ||
            rawStatus.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete };
        }

        IEnumerable<string?> statusNames = rawStatus.ValueKind switch
        {
            JsonValueKind.String => new[] { rawStatus.GetString() },
            JsonValueKind.Array => rawStatus.EnumerateArray().Select(static value =>
                value.ValueKind == JsonValueKind.String ? value.GetString() : null),
            _ => throw new InvalidDataException(
                $"Quest '{taskId}' requirement for '{requiredTaskId}' has invalid status shape."),
        };

        var result = new HashSet<QuestRequiredStatus>();
        foreach (var statusName in statusNames)
        {
            if (string.IsNullOrWhiteSpace(statusName))
                throw new InvalidDataException(
                    $"Quest '{taskId}' requirement for '{requiredTaskId}' contains invalid status.");

            result.Add(statusName.Trim().ToLowerInvariant() switch
            {
                "complete" or "completed" => QuestRequiredStatus.Complete,
                "active" or "accept" or "accepted" => QuestRequiredStatus.Active,
                "failed" => QuestRequiredStatus.Failed,
                _ => throw new InvalidDataException(
                    $"Quest '{taskId}' requirement for '{requiredTaskId}' has unsupported status '{statusName}'."),
            });
        }

        if (result.Count == 0)
            result.Add(QuestRequiredStatus.Complete);

        return result;
    }

    private static IReadOnlyList<QuestTraderStandingRequirement> ReadTraderStandingRequirements(
        JsonElement task,
        string taskId)
    {
        if (!task.TryGetProperty("traderRequirements", out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<QuestTraderStandingRequirement>();
        }

        return TarkovJsonReader.ReadCollectionValue(
                rawRequirements,
                $"quest {taskId} trader requirements")
            .Select(raw =>
            {
                var traderId = ReadRequiredReference(raw, "trader", taskId, "trader standing");
                var standing = TarkovJsonReader.RequiredDecimal(
                    raw,
                    "value",
                    $"Quest '{taskId}' trader standing requirement");
                return new QuestTraderStandingRequirement(traderId, standing);
            })
            .ToArray();
    }

    private static IReadOnlyList<QuestTraderLoyaltyRequirement> ReadTraderLoyaltyRequirements(
        JsonElement task,
        string taskId)
    {
        if (!task.TryGetProperty("traderLevelRequirements", out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<QuestTraderLoyaltyRequirement>();
        }

        return TarkovJsonReader.ReadCollectionValue(
                rawRequirements,
                $"quest {taskId} trader loyalty requirements")
            .Select(raw =>
            {
                var traderId = ReadRequiredReference(raw, "trader", taskId, "trader loyalty");
                var level = TarkovJsonReader.RequiredInt(
                    raw,
                    "level",
                    $"Quest '{taskId}' trader loyalty requirement");
                if (level < 0)
                    throw new InvalidDataException($"Quest '{taskId}' has negative trader loyalty level.");
                return new QuestTraderLoyaltyRequirement(traderId, level);
            })
            .ToArray();
    }

    private static string? ReadOptionalReference(JsonElement entity, string propertyName, string taskId)
    {
        if (!entity.TryGetProperty(propertyName, out var raw) ||
            raw.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var id = TarkovJsonReader.ReferenceId(raw);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"Quest '{taskId}' has invalid {propertyName} reference.");
        return id;
    }

    private static string ReadRequiredReference(
        JsonElement entity,
        string propertyName,
        string taskId,
        string description)
    {
        if (!entity.TryGetProperty(propertyName, out var raw))
            throw new InvalidDataException($"Quest '{taskId}' is missing {description} reference.");

        var id = TarkovJsonReader.ReferenceId(raw);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"Quest '{taskId}' has invalid {description} reference.");
        return id;
    }
}
