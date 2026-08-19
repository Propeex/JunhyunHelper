using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class FutureNeededItemsPlannerTests
{
    [Fact]
    public void FutureLevelLockedQuest_IsIncluded()
    {
        var content = Content(
            quests: [Quest("future", minimumLevel: 40)],
            questRequirements: [Requirement("future", "wire", 3)]);
        var plan = FutureNeededItemsPlanner.Calculate(content, Profile(level: 10));

        var wire = Assert.Single(plan.NeededItems);
        Assert.Equal("wire", wire.ItemId);
        Assert.Equal(3, wire.RequiredTotal);
    }

    [Fact]
    public void PermanentlyUnavailableQuest_IsExcludedAndOwnedItemBecomesCleanup()
    {
        var content = Content(
            quests: [Quest("bear", requiredFaction: PmcFaction.Bear)],
            questRequirements: [Requirement("bear", "wire", 3)]);
        var profile = Profile(
            faction: PmcFaction.Usec,
            inventory: Inventory(("wire", new InventoryQuantity(0, 5))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.Empty(plan.NeededItems);
        var cleanup = Assert.Single(plan.CleanupItems);
        Assert.Equal("wire", cleanup.ItemId);
        Assert.Equal(5, cleanup.SurplusNonFir);
        Assert.Equal(0, cleanup.RequiredTotal);
    }

    [Fact]
    public void CompletedQuestRequirement_DisappearsButInventoryRemainsCleanup()
    {
        var content = Content(
            quests: [Quest("done")],
            questRequirements: [Requirement("done", "wire", 8)]);
        var profile = Profile(
            completedQuestIds: new HashSet<string>(["done"], StringComparer.Ordinal),
            inventory: Inventory(("wire", new InventoryQuantity(0, 8))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.Empty(plan.NeededItems);
        var cleanup = Assert.Single(plan.CleanupItems);
        Assert.Equal(8, cleanup.SurplusTotal);
    }

    [Fact]
    public void UnsupportedQuestCondition_IsConservativelyIncluded()
    {
        var content = Content(
            quests: [Quest("unknown", unsupported: ["dialogue"])],
            questRequirements: [Requirement("unknown", "wire", 3)]);
        var profile = Profile(inventory: Inventory(("wire", new InventoryQuantity(0, 3))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.Single(plan.NeededItems);
        Assert.Empty(plan.CleanupItems);
        Assert.Equal(
            QuestFutureReachabilityState.IndeterminatePotential,
            plan.QuestReachability["unknown"].State);
    }

    [Fact]
    public void ExplicitFailedQuestRequirement_DisappearsAndFailedOnlyRecoveryRequirementRemains()
    {
        var source = Quest("source", unsupportedFailure: ["shoot"]);
        var recovery = Quest(
            "recovery",
            taskRequirements: [FailedRequirement("source")]);
        var content = Content(
            quests: [source, recovery],
            questRequirements:
            [
                Requirement("source", "old-item", 3),
                Requirement("recovery", "recovery-item", 2),
            ]);
        var profile = Profile(
            failedQuestIds: new HashSet<string>(["source"], StringComparer.Ordinal),
            inventory: Inventory(("old-item", new InventoryQuantity(0, 3))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.DoesNotContain(plan.NeededItems, item => item.ItemId == "old-item");
        Assert.Equal(2, plan.NeededItems.Single(item => item.ItemId == "recovery-item").RequiredTotal);
        Assert.Equal(3, plan.CleanupItems.Single(item => item.ItemId == "old-item").SurplusTotal);
    }

    [Fact]
    public void CompletedBranch_AutomaticallyRemovesFailedSiblingItemsAndKeepsRecoveryItems()
    {
        var chosen = Quest("chosen");
        var sibling = Quest(
            "sibling",
            completionFailureConditions: [new QuestCompletionFailureCondition("chosen")]);
        var recovery = Quest("recovery", taskRequirements: [FailedRequirement("sibling")]);
        var content = Content(
            quests: [chosen, sibling, recovery],
            questRequirements:
            [
                Requirement("sibling", "sibling-item", 4),
                Requirement("recovery", "recovery-item", 1),
            ]);
        var profile = Profile(
            completedQuestIds: new HashSet<string>(["chosen"], StringComparer.Ordinal),
            inventory: Inventory(("sibling-item", new InventoryQuantity(0, 4))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.DoesNotContain(plan.NeededItems, item => item.ItemId == "sibling-item");
        Assert.Single(plan.NeededItems, item => item.ItemId == "recovery-item");
        Assert.Equal(4, plan.CleanupItems.Single(item => item.ItemId == "sibling-item").SurplusTotal);
        Assert.Equal(QuestFutureReachabilityState.Unavailable, plan.QuestReachability["sibling"].State);
        Assert.Equal(QuestFutureReachabilityState.Potential, plan.QuestReachability["recovery"].State);
    }

    [Fact]
    public void KnownHideoutLevel_IncludesEveryLaterLevel()
    {
        var station = new HideoutStation(
            "workbench",
            "작업대",
            "Workbench",
            null,
            [
                Level("workbench", 1, ("wire", 2)),
                Level("workbench", 2, ("wire", 3)),
                Level("workbench", 3, ("bolt", 4)),
            ]);
        var content = Content(hideoutStations: [station]);
        var profile = Profile(hideoutLevels: new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["workbench"] = 1,
        });

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.Equal(2, plan.NeededItems.Count);
        Assert.Equal(3, plan.NeededItems.Single(item => item.ItemId == "wire").RequiredTotal);
        Assert.Equal(4, plan.NeededItems.Single(item => item.ItemId == "bolt").RequiredTotal);
    }

    [Fact]
    public void MissingHideoutStationProgress_IsLevelZeroAndIncludesFutureMaterials()
    {
        var station = new HideoutStation(
            "workbench",
            "작업대",
            "Workbench",
            null,
            [Level("workbench", 1, ("wire", 2))]);
        var content = Content(hideoutStations: [station]);
        var profile = Profile(inventory: Inventory(("wire", new InventoryQuantity(0, 8))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        var wire = Assert.Single(plan.NeededItems);
        Assert.Equal(2, wire.RequiredTotal);
        var cleanup = Assert.Single(plan.CleanupItems);
        Assert.Equal("wire", cleanup.ItemId);
        Assert.Equal(6, cleanup.SurplusNonFir);
    }

    [Fact]
    public void AlternativeQuestItems_AreNeverArbitrarilyMarkedForCleanup()
    {
        var content = Content(
            quests: [Quest("choice")],
            questRequirements:
            [
                new QuestItemRequirement("choice", "obj", ["a", "b"], 2, false),
            ]);
        var profile = Profile(inventory: Inventory(("a", new InventoryQuantity(0, 5))));

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        Assert.Single(plan.AlternativeQuestRequirements);
        Assert.Empty(plan.CleanupItems);
        Assert.Contains(plan.CleanupProtections, protection =>
            protection.ItemId == "a" &&
            protection.Kind == CleanupProtectionKind.AlternativeQuestRequirement);
    }

    [Fact]
    public void FirCleanup_PreservesRequiredFirAndOnlyUsefulUnrestrictedQuantity()
    {
        var requirements = new[]
        {
            new ItemRequirement(
                "salewa",
                5,
                3,
                new ItemRequirementSource(ItemRequirementSourceKind.Quest, "q")),
        };
        var inventory = Inventory(("salewa", new InventoryQuantity(4, 10)));

        var cleanup = Assert.Single(InventorySurplusCalculator.Calculate(requirements, inventory));

        Assert.Equal(1, cleanup.SurplusFir);
        Assert.Equal(8, cleanup.SurplusNonFir);
        Assert.Equal(9, cleanup.SurplusTotal);
    }

    private static GameContentCatalog Content(
        IReadOnlyList<QuestDefinition>? quests = null,
        IReadOnlyList<QuestItemRequirement>? questRequirements = null,
        IReadOnlyList<HideoutStation>? hideoutStations = null) =>
        new(
            Array.Empty<GameItem>(),
            Array.Empty<JunhyunHelper.Core.Reference.TraderDefinition>(),
            Array.Empty<JunhyunHelper.Core.Reference.MapReference>(),
            quests ?? Array.Empty<QuestDefinition>(),
            Array.Empty<QuestObjective>(),
            questRequirements ?? Array.Empty<QuestItemRequirement>(),
            hideoutStations ?? Array.Empty<HideoutStation>());

    private static QuestDefinition Quest(
        string id,
        int minimumLevel = 1,
        PmcFaction? requiredFaction = null,
        IReadOnlyList<string>? unsupported = null,
        IReadOnlyList<QuestTaskRequirement>? taskRequirements = null,
        IReadOnlyList<QuestCompletionFailureCondition>? completionFailureConditions = null,
        IReadOnlyList<string>? unsupportedFailure = null) =>
        new(
            id,
            id,
            id,
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            minimumLevel,
            requiredFaction,
            null,
            taskRequirements ?? [],
            [],
            [],
            unsupported,
            completionFailureConditions,
            false,
            unsupportedFailure);

    private static QuestTaskRequirement FailedRequirement(string questId) =>
        new(questId, new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Failed]));

    private static QuestItemRequirement Requirement(string questId, string itemId, int count) =>
        new(questId, $"{questId}-obj", [itemId], count, false);

    private static HideoutLevel Level(
        string stationId,
        int level,
        params (string ItemId, int Count)[] items) =>
        new(
            stationId,
            level,
            null,
            items.Select(item => new HideoutItemRequirement(
                stationId,
                level,
                item.ItemId,
                item.Count,
                false)).ToArray());

    private static GameProfileSnapshot Profile(
        int level = 1,
        PmcFaction faction = PmcFaction.Usec,
        IReadOnlySet<string>? completedQuestIds = null,
        IReadOnlySet<string>? failedQuestIds = null,
        IReadOnlyDictionary<string, int>? hideoutLevels = null,
        IReadOnlyDictionary<string, InventoryQuantity>? inventory = null) =>
        new()
        {
            ProfileId = "test",
            GameMode = GameMode.Regular,
            Level = level,
            Faction = faction,
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
            FailedQuestIds = failedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
            HideoutLevels = hideoutLevels ?? new Dictionary<string, int>(StringComparer.Ordinal),
            Inventory = inventory ?? new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal),
        };

    private static IReadOnlyDictionary<string, InventoryQuantity> Inventory(
        params (string ItemId, InventoryQuantity Quantity)[] items) =>
        items.ToDictionary(item => item.ItemId, item => item.Quantity, StringComparer.Ordinal);
}
