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
    public async Task UnsupportedLiveConditionRemainsIndeterminateAndIsExposedInProblems()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-UnresolvedQuest-{Guid.NewGuid():N}.db");

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = Profile();
            await store.SaveAsync(profile, cancellationToken);
            var quest = OpaqueQuest();
            var content = Content(quest);

            var workspace = await service.LoadAsync(content, profile.ProfileId, cancellationToken);

            var visible = Assert.Single(workspace.Quests);
            Assert.Equal(QuestAvailabilityState.Indeterminate, visible.Availability.State);
            Assert.Contains(
                visible.Availability.Reasons,
                reason => reason.Kind == QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement &&
                          reason.ReferenceId == "globalVariable");

            var problem = Assert.Single(workspace.Problems);
            Assert.Equal(QuestAvailabilityState.Indeterminate, problem.Availability.State);
            Assert.Equal(quest.Id, problem.Quest.Id);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task UserCanCompleteQuestAfterManualInGameConfirmation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-UnresolvedQuestComplete-{Guid.NewGuid():N}.db");

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = Profile();
            await store.SaveAsync(profile, cancellationToken);
            var quest = OpaqueQuest();
            var content = Content(quest);

            var workspace = await service.CompleteAsync(
                content,
                profile.ProfileId,
                quest.Id,
                cancellationToken);

            var completed = Assert.Single(workspace.Quests);
            Assert.Equal(QuestAvailabilityState.Completed, completed.Availability.State);
            Assert.Contains(quest.Id, workspace.Profile.CompletedQuestIds);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static GameProfileSnapshot Profile() => new()
    {
        ProfileId = "regular",
        GameMode = GameMode.Regular,
        Level = 50,
        Faction = PmcFaction.Usec,
        PrestigeLevel = 0,
    };

    private static QuestDefinition OpaqueQuest() => new(
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
        UnsupportedAvailabilityRequirementTypes: ["globalVariable"]);

    private static GameContentCatalog Content(QuestDefinition quest) => new(
        Items: [],
        Traders: [],
        Maps: [],
        Quests: [quest],
        QuestObjectives: [],
        QuestItemRequirements: [],
        HideoutStations: []);
}
