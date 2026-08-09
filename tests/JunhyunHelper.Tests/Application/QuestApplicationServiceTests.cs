using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class QuestApplicationServiceTests
{
    [Fact]
    public async Task CompleteAndUndoOnlyChangeCompletedQuestFactAndRecalculate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile();
            var content = CreateQuestChain();
            await store.SaveAsync(profile, cancellationToken);

            var initial = await service.LoadAsync(content, profile.ProfileId, cancellationToken);
            Assert.Equal(QuestAvailabilityState.Current, Find(initial, "a").Availability.State);
            Assert.Equal(QuestAvailabilityState.Locked, Find(initial, "b").Availability.State);

            var completed = await service.CompleteAsync(content, profile.ProfileId, "a", cancellationToken);
            Assert.Contains("a", completed.Profile.CompletedQuestIds);
            Assert.Equal(QuestAvailabilityState.Completed, Find(completed, "a").Availability.State);
            Assert.Equal(QuestAvailabilityState.Current, Find(completed, "b").Availability.State);

            var undone = await service.UndoCompletionAsync(content, profile.ProfileId, "a", cancellationToken);
            Assert.DoesNotContain("a", undone.Profile.CompletedQuestIds);
            Assert.Equal(QuestAvailabilityState.Current, Find(undone, "a").Availability.State);
            Assert.Equal(QuestAvailabilityState.Locked, Find(undone, "b").Availability.State);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ManualPermanentFailureAndUndoOnlyChangeFailureFactAndRecalculate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestFail-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile() with
            {
                HideoutLevels = new Dictionary<string, int>(StringComparer.Ordinal) { ["workbench"] = 2 },
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal) { ["wire"] = new(1, 3) },
            };
            var source = CreateQuest("source", unsupportedFailureConditions: ["shoot"]);
            var recovery = CreateQuest(
                "recovery",
                taskRequirements:
                [new QuestTaskRequirement("source", new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Failed]))]);
            var content = EmptyContent([source, recovery]);
            await store.SaveAsync(profile, cancellationToken);

            var failed = await service.FailAsync(content, profile.ProfileId, "source", cancellationToken);
            Assert.Contains("source", failed.Profile.FailedQuestIds);
            Assert.Equal(QuestAvailabilityState.Unavailable, Find(failed, "source").Availability.State);
            Assert.Equal(QuestAvailabilityState.Current, Find(failed, "recovery").Availability.State);
            Assert.Equal(2, failed.Profile.HideoutLevels["workbench"]);
            Assert.Equal(new InventoryQuantity(1, 3), failed.Profile.Inventory["wire"]);

            var undone = await service.UndoFailureAsync(content, profile.ProfileId, "source", cancellationToken);
            Assert.DoesNotContain("source", undone.Profile.FailedQuestIds);
            Assert.Equal(QuestAvailabilityState.Current, Find(undone, "source").Availability.State);
            Assert.Equal(QuestAvailabilityState.Locked, Find(undone, "recovery").Availability.State);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ManualFailureIsRejectedForRestartableOrAutomaticallyObservableQuest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestFailReject-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile();
            await store.SaveAsync(profile, cancellationToken);

            var normal = CreateQuest("normal");
            var restartable = CreateQuest("restartable", restartable: true, unsupportedFailureConditions: ["shoot"]);
            var content = EmptyContent([normal, restartable]);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.FailAsync(content, profile.ProfileId, "normal", cancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.FailAsync(content, profile.ProfileId, "restartable", cancellationToken));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task CompletingQuestAfterContentChangeRemovesStaleExplicitFailureFact()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestStaleFail-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile() with
            {
                FailedQuestIds = new HashSet<string>(["source"], StringComparer.Ordinal),
            };
            await store.SaveAsync(profile, cancellationToken);

            var content = EmptyContent([CreateQuest("source")]);
            var initial = await service.LoadAsync(content, profile.ProfileId, cancellationToken);
            Assert.Equal(QuestAvailabilityState.Current, Find(initial, "source").Availability.State);

            var completed = await service.CompleteAsync(content, profile.ProfileId, "source", cancellationToken);
            Assert.Contains("source", completed.Profile.CompletedQuestIds);
            Assert.DoesNotContain("source", completed.Profile.FailedQuestIds);

            var reloaded = await store.LoadAsync(profile.ProfileId, cancellationToken);
            Assert.NotNull(reloaded);
            Assert.Contains("source", reloaded.CompletedQuestIds);
            Assert.DoesNotContain("source", reloaded.FailedQuestIds);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task CannotCompleteLockedQuest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile();
            await store.SaveAsync(profile, cancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CompleteAsync(CreateQuestChain(), profile.ProfileId, "b", cancellationToken));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task UnsetPrestigeIsNormalizedToZeroAndPrestigeQuestLocks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile() with { PrestigeLevel = null };
            var quest = CreateQuest("prestige", requiredPrestigeLevel: 1);
            var content = EmptyContent([quest]);
            await store.SaveAsync(profile, cancellationToken);

            var workspace = await service.LoadAsync(content, profile.ProfileId, cancellationToken);

            Assert.Equal(0, workspace.Profile.PrestigeLevel);
            Assert.Empty(workspace.Problems);
            var entry = Find(workspace, "prestige");
            Assert.Equal(QuestAvailabilityState.Locked, entry.Availability.State);
            Assert.Contains(
                entry.Availability.Reasons,
                reason => reason.Kind == QuestAvailabilityReasonKind.Prestige);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private static QuestCatalogEntry Find(QuestWorkspace workspace, string questId) =>
        Assert.Single(workspace.Quests, entry => entry.Quest.Id == questId);

    private static GameProfileSnapshot CreateProfile() =>
        new()
        {
            ProfileId = "regular",
            GameMode = GameMode.Regular,
            Level = 50,
            Faction = PmcFaction.Usec,
            PrestigeLevel = 0,
        };

    private static GameContentCatalog CreateQuestChain()
    {
        var questA = CreateQuest("a");
        var questB = CreateQuest(
            "b",
            taskRequirements:
            [new QuestTaskRequirement("a", new[] { QuestRequiredStatus.Complete })]);
        return EmptyContent([questA, questB]);
    }

    private static QuestDefinition CreateQuest(
        string id,
        int? requiredPrestigeLevel = null,
        IReadOnlyList<QuestTaskRequirement>? taskRequirements = null,
        bool restartable = false,
        IReadOnlyList<string>? unsupportedFailureConditions = null) =>
        new(
            id,
            NameKo: id,
            NameEn: id,
            TraderId: null,
            MapId: null,
            WikiUrl: null,
            Experience: null,
            KappaRequired: false,
            LightkeeperRequired: false,
            Disabled: false,
            MinimumPlayerLevel: 1,
            RequiredFaction: null,
            RequiredPrestigeLevel: requiredPrestigeLevel,
            TaskRequirements: taskRequirements ?? [],
            TraderStandingRequirements: [],
            TraderLoyaltyRequirements: [],
            Restartable: restartable,
            UnsupportedFailureConditionTypes: unsupportedFailureConditions);

    private static GameContentCatalog EmptyContent(IReadOnlyList<QuestDefinition> quests) =>
        new(
            Items: [],
            Traders: [],
            Maps: [],
            Quests: quests,
            QuestObjectives: [],
            QuestItemRequirements: [],
            HideoutStations: []);
}
