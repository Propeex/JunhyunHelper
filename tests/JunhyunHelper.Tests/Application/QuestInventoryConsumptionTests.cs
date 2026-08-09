using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class QuestInventoryConsumptionTests
{
    [Fact]
    public async Task CompleteConsumesFixedRequirementsButDoesNotGuessFlexibleCandidate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestConsume-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(path);
            var service = new QuestApplicationService(store);
            var profile = Profile() with
            {
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["fixed-general"] = new(Fir: 3, NonFir: 2),
                    ["fixed-fir"] = new(Fir: 2, NonFir: 5),
                    ["alt-a"] = new(Fir: 4, NonFir: 4),
                },
            };
            var content = Content(
                [
                    new QuestItemRequirement("q", "general", ["fixed-general"], 4, FoundInRaid: false),
                    new QuestItemRequirement("q", "fir", ["fixed-fir"], 2, FoundInRaid: true),
                    new QuestItemRequirement("q", "flex", ["alt-a", "alt-b"], 3, FoundInRaid: false),
                ]);
            await store.SaveAsync(profile, TestContext.Current.CancellationToken);

            var completed = await service.CompleteAsync(
                content,
                profile.ProfileId,
                "q",
                TestContext.Current.CancellationToken);

            Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 0), completed.Profile.Inventory["fixed-general"]);
            Assert.Equal(new InventoryQuantity(Fir: 0, NonFir: 5), completed.Profile.Inventory["fixed-fir"]);
            Assert.Equal(new InventoryQuantity(Fir: 4, NonFir: 4), completed.Profile.Inventory["alt-a"]);
            Assert.True(completed.Profile.QuestConsumptions.ContainsKey("q"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task UndoCanRestoreExactlyWhatCompletionActuallyConsumed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-QuestRestore-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(path);
            var service = new QuestApplicationService(store);
            var profile = Profile() with
            {
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["item"] = new(Fir: 1, NonFir: 1),
                },
            };
            var content = Content([new QuestItemRequirement("q", "submit", ["item"], 5, FoundInRaid: false)]);
            await store.SaveAsync(profile, TestContext.Current.CancellationToken);

            await service.CompleteAsync(content, profile.ProfileId, "q", TestContext.Current.CancellationToken);
            var undone = await service.UndoCompletionAsync(
                content,
                profile.ProfileId,
                "q",
                restoreInventory: true,
                TestContext.Current.CancellationToken);

            Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 1), undone.Profile.Inventory["item"]);
            Assert.False(undone.Profile.QuestConsumptions.ContainsKey("q"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
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

    private static GameContentCatalog Content(IReadOnlyList<QuestItemRequirement> requirements)
    {
        var quest = new QuestDefinition(
            "q",
            NameKo: "q",
            NameEn: "q",
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
            Restartable: false,
            UnsupportedFailureConditionTypes: []);

        return new GameContentCatalog(
            Items: [],
            Traders: [],
            Maps: [],
            Quests: [quest],
            QuestObjectives: [],
            QuestItemRequirements: requirements,
            HideoutStations: []);
    }
}
