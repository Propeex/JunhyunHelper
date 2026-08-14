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

        var refUnlockQuestId = gameMode == GameMode.Pve
            ? RefPveUnlockQuestId
            : RefRegularUnlockQuestId;

        return quests
            .Select(quest => quest.TraderId switch
            {
                LightkeeperTraderId => StrengthenTraderGate(quest, LightkeeperUnlockQuestId),
                BtrDriverTraderId => StrengthenTraderGate(quest, BtrDriverUnlockQuestId),
                RefTraderId => StrengthenTraderGate(quest, refUnlockQuestId),
                _ => quest,
            })
            .ToArray();
    }

    private static QuestDefinition StrengthenTraderGate(
        QuestDefinition quest,
        string unlockQuestId)
    {
        // The unlock quest itself must never acquire a self-dependency. This also keeps
        // the policy safe if the upstream source later assigns an unlock quest directly
        // to the trader it unlocks.
        if (string.Equals(quest.Id, unlockQuestId, StringComparison.Ordinal))
            return quest;

        // json.tarkov.dev currently does not repeat trader-access gates on most quests
        // offered by Lightkeeper, BTR Driver or Ref. Treat trader access as a canonical
        // prerequisite so availability, future reachability and Needed Items all share
        // the same truth. If upstream already references the unlock quest with a weaker
        // Active-compatible condition, strengthen it to Complete rather than rendering
        // duplicate prerequisite rows.
        var found = false;
        var requirements = quest.TaskRequirements
            .Select(requirement =>
            {
                if (!string.Equals(requirement.RequiredQuestId, unlockQuestId, StringComparison.Ordinal))
                    return requirement;

                found = true;
                return new QuestTaskRequirement(
                    unlockQuestId,
                    new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete });
            })
            .ToList();

        if (!found)
        {
            requirements.Add(new QuestTaskRequirement(
                unlockQuestId,
                new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete }));
        }

        return quest with { TaskRequirements = requirements.ToArray() };
    }

    private static IReadOnlyList<QuestDefinition> ApplyUnsupportedAvailabilityRequirements(
        IReadOnlyList<QuestDefinition> quests,
        JsonElement taskData)
    {
        var unsupportedByQuest = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var rawTask in TarkovJsonReader.ReadCollection(taskData, "tasks"))
        {
            var questId = TarkovJsonReader.RequiredString(rawTask, "id", "Quest");
            if (!rawTask.TryGetProperty("otherRequirements", out var rawRequirements) ||
                rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var types = TarkovJsonReader.ReadCollectionValue(
                    rawRequirements,
                    $"quest {questId} other requirements")
                .Select(raw => TarkovJsonReader.RequiredString(
                    raw,
                    "type",
                    $"Quest '{questId}' additional requirement"))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            if (types.Length > 0)
                unsupportedByQuest[questId] = types;
        }

        return quests
            .Select(quest => unsupportedByQuest.TryGetValue(quest.Id, out var types)
                ? quest with { UnsupportedAvailabilityRequirementTypes = types }
                : quest)
            .ToArray();
    }
}
