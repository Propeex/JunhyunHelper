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
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-UnresolvedQuest-{Guid.NewGuid():N}.db");

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = new GameProfileSnapshot
            {
                ProfileId = "regular",
                GameMode = GameMode.Regular,
                Level = 50,
                Faction = PmcFaction.Usec,
                PrestigeLevel = 0,
            };
            await store.SaveAsync(profile, cancellationToken);

            var quest = new QuestDefinition(
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
            var content = new GameContentCatalog(
                Items: [],
                Traders: [],
                Maps: [],
                Quests: [quest],
                QuestObjectives: [],
                QuestItemRequirements: [],
                HideoutStations: []);

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
}
