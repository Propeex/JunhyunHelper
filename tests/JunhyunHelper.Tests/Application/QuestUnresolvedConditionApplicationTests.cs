using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class QuestUnresolvedConditionApplicationTests
{
    [Fact]
    public async Task UnsupportedLiveConditionStaysIndeterminateAndCanBeManuallyCompleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile();
            await store.SaveAsync(profile, cancellationToken);

            var quest = CreateOpaqueQuest();
            var content = CreateContent(quest);
            var workspace = await service.LoadAsync(content, profile.ProfileId, cancellationToken);

            var unresolved = Assert.Single(workspace.Quests);
            Assert.Equal(QuestAvailabilityState.Indeterminate, unresolved.Availability.State);
            Assert.Contains(
                unresolved.Availability.Reasons,
                reason => reason.Kind == QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement &&
                          reason.ReferenceId == "globalVariable");

            var problem = Assert.Single(workspace.Problems);
            Assert.Equal(QuestAvailabilityState.Indeterminate, problem.Availability.State);
            Assert.Equal(quest.Id, problem.Quest.Id);

            var completed = await service.CompleteAsync(
                content,
                profile.ProfileId,
                quest.Id,
                cancellationToken);
            Assert.Contains(quest.Id, completed.Profile.CompletedQuestIds);
            Assert.Equal(
                QuestAvailabilityState.Completed,
                Assert.Single(completed.Quests).Availability.State);
            Assert.Empty(completed.Problems);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task IndeterminateQuestWithPermanentFailureConditionCanBeManuallyFailed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile();
            await store.SaveAsync(profile, cancellationToken);

            var quest = CreateOpaqueQuest(unsupportedFailureConditionTypes: ["counterCreator"]);
            Assert.True(quest.RequiresExplicitFailureInput);
            var content = CreateContent(quest);

            var workspace = await service.LoadAsync(content, profile.ProfileId, cancellationToken);
            Assert.Equal(
                QuestAvailabilityState.Indeterminate,
                Assert.Single(workspace.Quests).Availability.State);

            var failed = await service.FailAsync(
                content,
                profile.ProfileId,
                quest.Id,
                cancellationToken);

            Assert.Contains(quest.Id, failed.Profile.FailedQuestIds);
            var entry = Assert.Single(failed.Quests);
            Assert.Equal(QuestAvailabilityState.Unavailable, entry.Availability.State);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static string TempDatabasePath() => Path.Combine(
        Path.GetTempPath(),
        $"JunhyunHelper-UnresolvedQuest-{Guid.NewGuid():N}.db");

    private static GameProfileSnapshot CreateProfile() => new()
    {
        ProfileId = "regular",
        GameMode = GameMode.Regular,
        Level = 50,
        Faction = PmcFaction.Usec,
        PrestigeLevel = 0,
    };

    private static QuestDefinition CreateOpaqueQuest(
        IReadOnlyList<string>? unsupportedFailureConditionTypes = null) => new(
        Id: "opaque-condition",
        NameKo: "조건 퀘스트",
        NameEn: "Condition Quest",
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
        TaskRequirements: [],
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: [],
        UnsupportedAvailabilityRequirementTypes: ["globalVariable"],
        UnsupportedFailureConditionTypes: unsupportedFailureConditionTypes);

    private static GameContentCatalog CreateContent(QuestDefinition quest) => new(
        Items: [],
        Traders: [],
        Maps: [],
        Quests: [quest],
        QuestObjectives: [],
        QuestItemRequirements: [],
        HideoutStations: []);
}
