using System.Text.Json;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson.Ammo;
using JunhyunHelper.Infrastructure.TarkovJson.Hideout;
using JunhyunHelper.Infrastructure.TarkovJson.Items;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using JunhyunHelper.Infrastructure.TarkovJson.Reference;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed class TarkovGameContentImporter
{
    private const string LightkeeperTraderId = "638f541a29ffd1183d187f57";
    private const string LightkeeperUnlockQuestId = "625d700cc48e6c62a440fab5";
    private const string BtrDriverTraderId = "656f0f98d80a697f855d34b1";
    private const string BtrDriverUnlockQuestId = "6752f6d83038f7df520c83e8";
    private const string RefTraderId = "6617beeaa9cfa777ca915b7c";
    private const string RefRegularUnlockQuestId = "66058cb22cee99303f1ba067";
    private const string RefPveUnlockQuestId = "6834145ebc1f443d7603c8a7";
    private const string AvailabilityDelayRequirementType = "availabilityDelay";

    private readonly TarkovItemImporter _itemImporter = new();
    private readonly TarkovTraderImporter _traderImporter = new();
    private readonly TarkovMapReferenceImporter _mapImporter = new();
    private readonly TarkovQuestImporter _questImporter = new();
    private readonly TarkovQuestObjectiveImporter _questObjectiveImporter = new();
    private readonly TarkovHideoutImporter _hideoutImporter = new();
    private readonly TarkovAmmoImporter _ammoImporter = new();

    public GameContentCatalog Import(
        TarkovEndpointSource items,
        TarkovEndpointSource traders,
        TarkovEndpointSource maps,
        TarkovEndpointSource tasks,
        TarkovEndpointSource hideout,
        TarkovEndpointSource barters,
        TarkovEndpointSource crafts,
        IReadOnlyList<EditionDefinition> editions,
        GameMode gameMode = GameMode.Regular)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(traders);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(hideout);
        ArgumentNullException.ThrowIfNull(barters);
        ArgumentNullException.ThrowIfNull(crafts);
        ArgumentNullException.ThrowIfNull(editions);

        var questObjectives = _questObjectiveImporter.Import(
            tasks.BaseDocument,
            tasks.Localization);
        var quests = ApplySpecialTraderAccessRequirements(
            ApplyUnsupportedAvailabilityRequirements(
                _questImporter.Import(tasks.BaseDocument, tasks.Localization),
                tasks.BaseDocument.Data),
            gameMode);

        return new GameContentCatalog(
            _itemImporter.Import(items.BaseDocument, items.Localization),
            _traderImporter.Import(traders.BaseDocument, traders.Localization),
            _mapImporter.Import(maps.BaseDocument, maps.Localization),
            quests,
            questObjectives.Objectives,
            questObjectives.ItemRequirements,
            _hideoutImporter.Import(hideout.BaseDocument, hideout.Localization),
            _ammoImporter.Import(
                items.BaseDocument,
                barters.BaseDocument,
                crafts.BaseDocument),
            editions);
    }

    internal static IReadOnlyList<QuestDefinition> ApplySpecialTraderAccessRequirements(
        IReadOnlyList<QuestDefinition> quests,
        GameMode gameMode)
    {
        ArgumentNullException.ThrowIfNull(quests);

        // Special trader access is a real game gate, but the upstream task feed does not
        // repeat it on every quest sold by that trader. Fill only the missing monotonic
        // BTR/Ref gates and never overwrite a more precise upstream requirement.
        // Lightkeeper is different: access can be lost and restored after the initial
        // unlock, so its access gate is modeled separately from ordinary monotonic quest
        // prerequisites and may use one sparse user override fact.
        var questIds = quests
            .Select(static quest => quest.Id)
            .ToHashSet(StringComparer.Ordinal);
        var hasLightkeeperUnlock = questIds.Contains(LightkeeperUnlockQuestId);
        var hasBtrUnlock = questIds.Contains(BtrDriverUnlockQuestId);
        var refUnlockQuestId = gameMode == GameMode.Pve
            ? RefPveUnlockQuestId
            : RefRegularUnlockQuestId;
        var hasRefUnlock = questIds.Contains(refUnlockQuestId);

        return quests
            .Select(quest =>
            {
                if (hasLightkeeperUnlock &&
                    string.Equals(quest.TraderId, LightkeeperTraderId, StringComparison.Ordinal))
                {
                    return AttachRecoverableTraderAccess(
                        quest,
                        LightkeeperTraderId,
                        LightkeeperUnlockQuestId,
                        QuestRequiredStatus.Complete);
                }

                if (hasBtrUnlock &&
                    string.Equals(quest.TraderId, BtrDriverTraderId, StringComparison.Ordinal))
                {
                    return AddMissingMonotonicTraderGate(
                        quest,
                        BtrDriverUnlockQuestId,
                        QuestRequiredStatus.Active);
                }

                if (hasRefUnlock &&
                    string.Equals(quest.TraderId, RefTraderId, StringComparison.Ordinal))
                {
                    return AddMissingMonotonicTraderGate(
                        quest,
                        refUnlockQuestId,
                        QuestRequiredStatus.Complete);
                }

                return quest;
            })
            .ToArray();
    }

    /// <summary>
    /// v5 and older snapshots contain the previous compatibility overlay, which forced
    /// every special-trader gate to Complete. Normalize those already-built snapshots in
    /// memory so an application upgrade does not require a network data refresh merely to
    /// obtain the corrected prerequisite semantics.
    /// </summary>
    internal static IReadOnlyList<QuestDefinition> UpgradeLegacySpecialTraderAccessRequirements(
        IReadOnlyList<QuestDefinition> quests,
        GameMode gameMode)
    {
        ArgumentNullException.ThrowIfNull(quests);

        var normalized = quests
            .Select(quest =>
            {
                if (!string.Equals(quest.TraderId, BtrDriverTraderId, StringComparison.Ordinal))
                    return quest;

                return quest with
                {
                    TaskRequirements = quest.TaskRequirements
                        .Where(requirement =>
                            !string.Equals(
                                requirement.RequiredQuestId,
                                BtrDriverUnlockQuestId,
                                StringComparison.Ordinal))
                        .ToArray(),
                };
            })
            .ToArray();

        return ApplySpecialTraderAccessRequirements(normalized, gameMode);
    }

    private static QuestDefinition AddMissingMonotonicTraderGate(
        QuestDefinition quest,
        string unlockQuestId,
        QuestRequiredStatus fallbackStatus)
    {
        if (string.Equals(quest.Id, unlockQuestId, StringComparison.Ordinal) ||
            quest.TaskRequirements.Any(requirement =>
                string.Equals(requirement.RequiredQuestId, unlockQuestId, StringComparison.Ordinal)))
        {
            // The source already knows more than the compatibility overlay. Preserve it.
            return quest;
        }

        return quest with
        {
            TaskRequirements = quest.TaskRequirements
                .Append(new QuestTaskRequirement(
                    unlockQuestId,
                    new HashSet<QuestRequiredStatus> { fallbackStatus }))
                .ToArray(),
        };
    }

    private static QuestDefinition AttachRecoverableTraderAccess(
        QuestDefinition quest,
        string traderId,
        string unlockQuestId,
        QuestRequiredStatus fallbackStatus)
    {
        if (string.Equals(quest.Id, unlockQuestId, StringComparison.Ordinal))
            return quest;

        var sourceGate = quest.TaskRequirements.FirstOrDefault(requirement =>
            string.Equals(requirement.RequiredQuestId, unlockQuestId, StringComparison.Ordinal));
        var acceptedStatuses = sourceGate?.AcceptedStatuses ??
            new HashSet<QuestRequiredStatus> { fallbackStatus };

        // If upstream starts repeating the initial Lightkeeper unlock on individual
        // quests, move that condition into the recoverable access gate instead of
        // evaluating it twice. Keeping it as an ordinary Complete prerequisite would
        // make a later Make Amends recovery impossible to represent.
        var remainingRequirements = quest.TaskRequirements
            .Where(requirement =>
                !string.Equals(requirement.RequiredQuestId, unlockQuestId, StringComparison.Ordinal))
            .ToArray();

        return quest with
        {
            TaskRequirements = remainingRequirements,
            SpecialTraderAccessRequirement = new QuestSpecialTraderAccessRequirement(
                traderId,
                unlockQuestId,
                new HashSet<QuestRequiredStatus>(acceptedStatuses),
                AllowManualOverride: true),
        };
    }

    private static IReadOnlyList<QuestDefinition> ApplyUnsupportedAvailabilityRequirements(
        IReadOnlyList<QuestDefinition> quests,
        JsonElement taskData)
    {
        var unsupportedByQuest = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var profileVariablesByQuest =
            new Dictionary<string, IReadOnlyList<QuestProfileVariableRequirement>>(StringComparer.Ordinal);

        foreach (var rawTask in TarkovJsonReader.ReadCollection(taskData, "tasks"))
        {
            var questId = TarkovJsonReader.RequiredString(rawTask, "id", "Quest");
            var types = new HashSet<string>(StringComparer.Ordinal);
            var profileVariables = new List<QuestProfileVariableRequirement>();

            if (rawTask.TryGetProperty("otherRequirements", out var rawRequirements) &&
                rawRequirements.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                foreach (var raw in TarkovJsonReader.ReadCollectionValue(
                             rawRequirements,
                             $"quest {questId} other requirements"))
                {
                    var type = TarkovJsonReader.RequiredString(
                        raw,
                        "type",
                        $"Quest '{questId}' additional requirement");

                    if (string.Equals(type, "globalVariable", StringComparison.Ordinal) &&
                        TryParseProfileVariableRequirement(raw, out var profileVariable))
                    {
                        profileVariables.Add(profileVariable);
                        continue;
                    }

                    // Unknown/malformed future conditions stay fail-closed. In particular,
                    // a changed globalVariable schema is never guessed.
                    types.Add(type);
                }
            }

            var minDelay = TarkovJsonReader.OptionalInt(rawTask, "availableDelaySecondsMin") ?? 0;
            var maxDelay = TarkovJsonReader.OptionalInt(rawTask, "availableDelaySecondsMax") ?? minDelay;
            if (minDelay > 0 || maxDelay > 0)
            {
                types.Add(AvailabilityDelayRequirementType);
            }

            if (types.Count > 0)
            {
                unsupportedByQuest[questId] = types
                    .Order(StringComparer.Ordinal)
                    .ToArray();
            }

            if (profileVariables.Count > 0)
            {
                profileVariablesByQuest[questId] = profileVariables
                    .OrderBy(requirement => requirement.VariableId, StringComparer.Ordinal)
                    .ThenBy(requirement => requirement.RequiredValue)
                    .ToArray();
            }
        }

        return quests
            .Select(quest =>
            {
                unsupportedByQuest.TryGetValue(quest.Id, out var types);
                profileVariablesByQuest.TryGetValue(quest.Id, out var profileVariables);
                return quest with
                {
                    UnsupportedAvailabilityRequirementTypes = types,
                    ProfileVariableRequirementData = profileVariables,
                };
            })
            .ToArray();
    }

    private static bool TryParseProfileVariableRequirement(
        JsonElement raw,
        out QuestProfileVariableRequirement requirement)
    {
        requirement = null!;

        var variableId = TarkovJsonReader.OptionalString(raw, "variableId");
        var compareMethod = TarkovJsonReader.OptionalString(raw, "compareMethod");
        var value = TarkovJsonReader.OptionalInt(raw, "value");

        // Some producers may nest the payload under globalVariable. Accept that exact
        // shape too, but never infer missing fields.
        if (raw.TryGetProperty("globalVariable", out var nested) &&
            nested.ValueKind == JsonValueKind.Object)
        {
            variableId ??= TarkovJsonReader.OptionalString(nested, "id") ??
                           TarkovJsonReader.OptionalString(nested, "variableId");
            compareMethod ??= TarkovJsonReader.OptionalString(nested, "compareMethod");
            value ??= TarkovJsonReader.OptionalInt(nested, "value");
        }

        if (string.IsNullOrWhiteSpace(variableId) ||
            !string.Equals(compareMethod, ">=", StringComparison.Ordinal) ||
            value is null)
        {
            return false;
        }

        requirement = new QuestProfileVariableRequirement(
            variableId.Trim(),
            value.Value,
            ProfileVariableRequirementOperator.AtLeast);
        return true;
    }

}
