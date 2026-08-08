using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Resilience;

public sealed class MajorUpdateResilienceTests
{
    [Fact]
    public async Task QuestRequirementReplacementPreservesInventoryAndSurfacesOldStockAsCleanup()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var oldContent = Catalog(
                items: [Item("wire"), Item("bolts")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "wire", 8)]);
            var newContent = Catalog(
                items: [Item("bolts")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "bolts", 8)]);
            var profile = Profile(
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["wire"] = new InventoryQuantity(Fir: 0, NonFir: 8),
                });

            var run = await RunPatchAsync(root, oldContent, newContent, profile, cancellationToken);

            Assert.Equal(new InventoryQuantity(0, 8), run.UserAfter.Inventory["wire"]);
            Assert.DoesNotContain(run.After.Content.Items, item => item.Id == "wire");
            Assert.Equal(8, run.AfterPlan.NeededItems.Single(item => item.ItemId == "bolts").RemainingTotal);

            var cleanup = run.AfterPlan.CleanupItems.Single(item => item.ItemId == "wire");
            Assert.Equal(8, cleanup.SurplusTotal);

            var change = Assert.Single(InventoryCleanupChangeDetector.FindIncreases(run.BeforePlan, run.AfterPlan));
            Assert.Equal("wire", change.ItemId);
            Assert.Equal(8, change.IncreasedBy);

            var previous = await run.SnapshotStore.ReadAsync(run.Paths.PreviousPath, cancellationToken);
            Assert.Equal("wire", previous.Content.QuestItemRequirements.Single().AcceptedItemIds.Single());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RequirementQuantityDropCreatesOnlyTheNewSafeSurplus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var oldContent = Catalog(
                items: [Item("wire")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "wire", 10)]);
            var newContent = Catalog(
                items: [Item("wire")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "wire", 4)]);
            var profile = Profile(
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["wire"] = new InventoryQuantity(Fir: 2, NonFir: 8),
                });

            var run = await RunPatchAsync(root, oldContent, newContent, profile, cancellationToken);

            Assert.Equal(new InventoryQuantity(2, 8), run.UserAfter.Inventory["wire"]);
            var cleanup = run.AfterPlan.CleanupItems.Single(item => item.ItemId == "wire");
            Assert.Equal(2, cleanup.SurplusFir);
            Assert.Equal(4, cleanup.SurplusNonFir);
            Assert.Equal(6, cleanup.SurplusTotal);

            var change = Assert.Single(InventoryCleanupChangeDetector.FindIncreases(run.BeforePlan, run.AfterPlan));
            Assert.Equal(6, change.IncreasedBy);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task NewEditionExclusionRemovesFutureRequirementWithoutTouchingUserFacts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var oldContent = Catalog(
                items: [Item("rare")],
                quests: [Quest("quest-edition")],
                questRequirements: [Requirement("quest-edition", "rare", 1)],
                editions:
                [
                    new EditionDefinition(
                        "standard",
                        "Standard",
                        new HashSet<string>(StringComparer.Ordinal),
                        new HashSet<string>(StringComparer.Ordinal)),
                ]);
            var newContent = Catalog(
                items: [Item("rare")],
                quests: [Quest("quest-edition")],
                questRequirements: [Requirement("quest-edition", "rare", 1)],
                editions:
                [
                    new EditionDefinition(
                        "standard",
                        "Standard",
                        new HashSet<string>(StringComparer.Ordinal),
                        new HashSet<string>(["quest-edition"], StringComparer.Ordinal)),
                ]);
            var profile = Profile(
                editionId: "standard",
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["rare"] = new InventoryQuantity(Fir: 1, NonFir: 0),
                });

            var run = await RunPatchAsync(root, oldContent, newContent, profile, cancellationToken);

            Assert.Equal("standard", run.UserAfter.EditionId);
            Assert.Equal(new InventoryQuantity(1, 0), run.UserAfter.Inventory["rare"]);
            Assert.Equal(
                QuestFutureReachabilityState.Unavailable,
                run.AfterPlan.QuestReachability["quest-edition"].State);
            Assert.DoesNotContain(run.AfterPlan.NeededItems, item => item.ItemId == "rare");
            Assert.Equal(1, run.AfterPlan.CleanupItems.Single(item => item.ItemId == "rare").SurplusTotal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task HideoutMaterialReplacementReplansAllFutureLevelsFromSameStoredProgress()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var oldStation = Station("workbench", level: 2, itemId: "wire", count: 5);
            var newStation = Station("workbench", level: 2, itemId: "bolts", count: 5);
            var oldContent = Catalog(
                items: [Item("wire"), Item("bolts")],
                hideoutStations: [oldStation]);
            var newContent = Catalog(
                items: [Item("wire"), Item("bolts")],
                hideoutStations: [newStation]);
            var profile = Profile(
                hideoutLevels: new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["workbench"] = 1,
                },
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["wire"] = new InventoryQuantity(Fir: 0, NonFir: 5),
                });

            var run = await RunPatchAsync(root, oldContent, newContent, profile, cancellationToken);

            Assert.Equal(1, run.UserAfter.HideoutLevels["workbench"]);
            Assert.Equal(5, run.AfterPlan.NeededItems.Single(item => item.ItemId == "bolts").RemainingTotal);
            Assert.Equal(5, run.AfterPlan.CleanupItems.Single(item => item.ItemId == "wire").SurplusTotal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FlexibleCandidateListChangeNeedsNoUserChoiceMigration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var oldRequirement = FlexibleRequirement("quest-flex", ["a", "b"], 5);
            var newRequirement = FlexibleRequirement("quest-flex", ["b", "c"], 5);
            var oldContent = Catalog(
                items: [Item("a"), Item("b"), Item("c")],
                quests: [Quest("quest-flex")],
                questRequirements: [oldRequirement]);
            var newContent = Catalog(
                items: [Item("a"), Item("b"), Item("c")],
                quests: [Quest("quest-flex")],
                questRequirements: [newRequirement]);
            var profile = Profile(
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["a"] = new InventoryQuantity(Fir: 0, NonFir: 5),
                });

            var run = await RunPatchAsync(root, oldContent, newContent, profile, cancellationToken);

            var oldProgress = FlexibleQuestItemRequirementCalculator.Calculate(
                run.BeforePlan.AlternativeQuestRequirements.Single(),
                run.UserAfter.Inventory);
            var newProgress = FlexibleQuestItemRequirementCalculator.Calculate(
                run.AfterPlan.AlternativeQuestRequirements.Single(),
                run.UserAfter.Inventory);

            Assert.True(oldProgress.IsFulfilled);
            Assert.False(newProgress.IsFulfilled);
            Assert.Equal(5, newProgress.RemainingTotal);
            Assert.Equal(new InventoryQuantity(0, 5), run.UserAfter.Inventory["a"]);
            Assert.Equal(5, run.AfterPlan.CleanupItems.Single(item => item.ItemId == "a").SurplusTotal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidCandidateLeavesActiveContentAndUserProgressUntouched()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = CreateTempDirectory();
        try
        {
            var mode = GameMode.Regular;
            var snapshotStore = new ContentSnapshotStore();
            var activation = new ContentActivationService(Path.Combine(root, "content"), snapshotStore);
            var paths = activation.GetPaths(mode);
            var userStore = new UserProfileStore(Path.Combine(root, "user.db"));

            var validContent = Catalog(
                items: [Item("wire")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "wire", 8)]);
            var invalidCandidate = Catalog(
                items: [Item("wire")],
                quests: [Quest("quest-a")],
                questRequirements: [Requirement("quest-a", "missing-item", 8)]);
            var profile = Profile(
                inventory: new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["wire"] = new InventoryQuantity(Fir: 0, NonFir: 8),
                });

            await snapshotStore.WriteNewAsync(paths.ActivePath, mode, validContent, cancellationToken: cancellationToken);
            await userStore.SaveAsync(profile, cancellationToken);
            await snapshotStore.WriteNewAsync(paths.CandidatePath, mode, invalidCandidate, cancellationToken: cancellationToken);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                activation.ActivateCandidateAsync(mode, cancellationToken));

            var active = await activation.ReadActiveOrRecoverAsync(mode, cancellationToken);
            var userAfter = await userStore.LoadAsync(profile.ProfileId, cancellationToken);

            Assert.NotNull(userAfter);
            Assert.Equal("wire", active.Content.QuestItemRequirements.Single().AcceptedItemIds.Single());
            Assert.Equal(new InventoryQuantity(0, 8), userAfter.Inventory["wire"]);
            Assert.False(File.Exists(paths.PreviousPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<PatchRun> RunPatchAsync(
        string root,
        GameContentCatalog oldContent,
        GameContentCatalog newContent,
        GameProfileSnapshot profile,
        CancellationToken cancellationToken)
    {
        var mode = profile.GameMode;
        var snapshotStore = new ContentSnapshotStore();
        var activation = new ContentActivationService(Path.Combine(root, "content"), snapshotStore);
        var paths = activation.GetPaths(mode);
        var userStore = new UserProfileStore(Path.Combine(root, "user.db"));

        await snapshotStore.WriteNewAsync(paths.ActivePath, mode, oldContent, cancellationToken: cancellationToken);
        await userStore.SaveAsync(profile, cancellationToken);

        var before = await activation.ReadActiveOrRecoverAsync(mode, cancellationToken);
        var userBefore = await userStore.LoadAsync(profile.ProfileId, cancellationToken)
            ?? throw new InvalidDataException("Test user profile was not persisted.");
        var beforePlan = FutureNeededItemsPlanner.Calculate(before.Content, userBefore);

        await snapshotStore.WriteNewAsync(paths.CandidatePath, mode, newContent, cancellationToken: cancellationToken);
        await activation.ActivateCandidateAsync(mode, cancellationToken);

        var after = await activation.ReadActiveOrRecoverAsync(mode, cancellationToken);
        var userAfter = await userStore.LoadAsync(profile.ProfileId, cancellationToken)
            ?? throw new InvalidDataException("User profile disappeared during content activation.");
        var afterPlan = FutureNeededItemsPlanner.Calculate(after.Content, userAfter);

        return new PatchRun(
            snapshotStore,
            paths,
            before,
            after,
            userAfter,
            beforePlan,
            afterPlan);
    }

    private static GameContentCatalog Catalog(
        IReadOnlyList<GameItem>? items = null,
        IReadOnlyList<QuestDefinition>? quests = null,
        IReadOnlyList<QuestItemRequirement>? questRequirements = null,
        IReadOnlyList<HideoutStation>? hideoutStations = null,
        IReadOnlyList<EditionDefinition>? editions = null) =>
        new(
            items ?? Array.Empty<GameItem>(),
            Array.Empty<JunhyunHelper.Core.Reference.TraderDefinition>(),
            Array.Empty<JunhyunHelper.Core.Reference.MapReference>(),
            quests ?? Array.Empty<QuestDefinition>(),
            Array.Empty<QuestObjective>(),
            questRequirements ?? Array.Empty<QuestItemRequirement>(),
            hideoutStations ?? Array.Empty<HideoutStation>(),
            Ammo: null,
            EditionData: editions);

    private static GameItem Item(string id) =>
        new(id, id, id, id, id, null, null, Array.Empty<string>());

    private static QuestDefinition Quest(string id) =>
        new(
            id,
            id,
            id,
            TraderId: null,
            MapId: null,
            WikiUrl: null,
            Experience: null,
            KappaRequired: false,
            LightkeeperRequired: false,
            Disabled: false,
            MinimumPlayerLevel: 1,
            RequiredFaction: null,
            RequiredPrestigeLevel: null,
            TaskRequirements: Array.Empty<QuestTaskRequirement>(),
            TraderStandingRequirements: Array.Empty<QuestTraderStandingRequirement>(),
            TraderLoyaltyRequirements: Array.Empty<QuestTraderLoyaltyRequirement>());

    private static QuestItemRequirement Requirement(
        string questId,
        string itemId,
        int count,
        bool foundInRaid = false) =>
        new(questId, $"{questId}-objective", [itemId], count, foundInRaid);

    private static QuestItemRequirement FlexibleRequirement(
        string questId,
        IReadOnlyList<string> itemIds,
        int count,
        bool foundInRaid = false) =>
        new(questId, $"{questId}-objective", itemIds, count, foundInRaid);

    private static HideoutStation Station(
        string stationId,
        int level,
        string itemId,
        int count) =>
        new(
            stationId,
            stationId,
            stationId,
            ImageUrl: null,
            Levels:
            [
                new HideoutLevel(
                    stationId,
                    level,
                    ConstructionTimeSeconds: null,
                    ItemRequirements:
                    [
                        new HideoutItemRequirement(
                            stationId,
                            level,
                            itemId,
                            count,
                            FoundInRaid: false),
                    ]),
            ]);

    private static GameProfileSnapshot Profile(
        string? editionId = null,
        IReadOnlyDictionary<string, int>? hideoutLevels = null,
        IReadOnlyDictionary<string, InventoryQuantity>? inventory = null) =>
        new()
        {
            ProfileId = "regular",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            EditionId = editionId,
            HideoutLevels = hideoutLevels is null
                ? new Dictionary<string, int>(StringComparer.Ordinal)
                : new Dictionary<string, int>(hideoutLevels, StringComparer.Ordinal),
            Inventory = inventory is null
                ? new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                : new Dictionary<string, InventoryQuantity>(inventory, StringComparer.Ordinal),
        };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-MajorPatch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record PatchRun(
        ContentSnapshotStore SnapshotStore,
        ContentModePaths Paths,
        StoredContentSnapshot Before,
        StoredContentSnapshot After,
        GameProfileSnapshot UserAfter,
        FutureNeededItemsPlan BeforePlan,
        FutureNeededItemsPlan AfterPlan);
}
