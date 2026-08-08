using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
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
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");

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
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task CannotCompleteLockedQuest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");

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
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task IndeterminateQuestsAreReturnedAsProblemsNotNormalCurrentQuests()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"JunhyunHelper-QuestApp-{Guid.NewGuid():N}.db");

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new QuestApplicationService(store);
            var profile = CreateProfile() with { PrestigeLevel = null };
            var quest = CreateQuest("prestige", requiredPrestigeLevel: 1);
            var content = EmptyContent([quest]);
            await store.SaveAsync(profile, cancellationToken);

            var workspace = await service.LoadAsync(content, profile.ProfileId, cancellationToken);

            var problem = Assert.Single(workspace.Problems);
            Assert.Equal("prestige", problem.Quest.Id);
            Assert.Equal(QuestAvailabilityState.Indeterminate, problem.Availability.State);
            Assert.DoesNotContain(
                workspace.Quests,
                entry => entry.Quest.Id == "prestige" &&
                         entry.Availability.State == QuestAvailabilityState.Current);
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
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
            [
                new QuestTaskRequirement(
                    "a",
                    new[] { QuestRequiredStatus.Complete }),
            ]);

        return EmptyContent([questA, questB]);
    }

    private static QuestDefinition CreateQuest(
        string id,
        int? requiredPrestigeLevel = null,
        IReadOnlyList<QuestTaskRequirement>? taskRequirements = null) =>
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
            TraderLoyaltyRequirements: []);

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
