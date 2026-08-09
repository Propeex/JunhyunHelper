using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class HideoutInventoryConsumptionTests
{
    [Fact]
    public async Task UpgradeConsumesGeneralFirstAndRollbackCanRestoreExactAmounts()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-HideoutConsume-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(path);
            var service = new HideoutApplicationService(store);
            var profile = Profile();
            await store.SaveAsync(profile, TestContext.Current.CancellationToken);

            var upgraded = await service.SetLevelAsync(
                Content(), profile.ProfileId, "station", 1,
                restoreInventoryOnRollback: false,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, upgraded.Profile.HideoutLevels["station"]);
            Assert.Equal(new InventoryQuantity(Fir: 2, NonFir: 0), upgraded.Profile.Inventory["wire"]);
            Assert.True(upgraded.Profile.HideoutUpgradeConsumptions.ContainsKey("station:1"));

            var rolledBack = await service.SetLevelAsync(
                Content(), profile.ProfileId, "station", 0,
                restoreInventoryOnRollback: true,
                TestContext.Current.CancellationToken);

            Assert.False(rolledBack.Profile.HideoutLevels.ContainsKey("station"));
            Assert.Equal(new InventoryQuantity(Fir: 3, NonFir: 1), rolledBack.Profile.Inventory["wire"]);
            Assert.False(rolledBack.Profile.HideoutUpgradeConsumptions.ContainsKey("station:1"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task RollbackWithoutRestoreKeepsLedgerAndReupgradeDoesNotConsumeTwice()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelper-HideoutNoRestore-{Guid.NewGuid():N}.db");
        try
        {
            var store = new UserProfileStore(path);
            var service = new HideoutApplicationService(store);
            var profile = Profile();
            await store.SaveAsync(profile, TestContext.Current.CancellationToken);

            var upgraded = await service.SetLevelAsync(
                Content(), profile.ProfileId, "station", 1,
                restoreInventoryOnRollback: false,
                TestContext.Current.CancellationToken);
            Assert.Equal(new InventoryQuantity(Fir: 2, NonFir: 0), upgraded.Profile.Inventory["wire"]);

            var rolledBack = await service.SetLevelAsync(
                Content(), profile.ProfileId, "station", 0,
                restoreInventoryOnRollback: false,
                TestContext.Current.CancellationToken);
            Assert.True(rolledBack.Profile.HideoutUpgradeConsumptions.ContainsKey("station:1"));

            var reupgraded = await service.SetLevelAsync(
                Content(), profile.ProfileId, "station", 1,
                restoreInventoryOnRollback: false,
                TestContext.Current.CancellationToken);
            Assert.Equal(new InventoryQuantity(Fir: 2, NonFir: 0), reupgraded.Profile.Inventory["wire"]);
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
        Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["wire"] = new(Fir: 3, NonFir: 1),
        },
    };

    private static GameContentCatalog Content()
    {
        var station = new HideoutStation(
            "station",
            NameKo: "시설",
            NameEn: "Station",
            ImageUrl: null,
            Levels:
            [
                new HideoutLevel(
                    "station",
                    Level: 1,
                    ConstructionTimeSeconds: null,
                    ItemRequirements:
                    [new HideoutItemRequirement("station", 1, "wire", Count: 2, FoundInRaid: false)]),
            ]);

        return new GameContentCatalog(
            Items: [],
            Traders: [],
            Maps: [],
            Quests: [],
            QuestObjectives: [],
            QuestItemRequirements: [],
            HideoutStations: [station]);
    }
}
