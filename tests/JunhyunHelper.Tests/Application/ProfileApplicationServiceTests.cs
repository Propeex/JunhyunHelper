using JunhyunHelper.Application.Profiles;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class ProfileApplicationServiceTests
{
    [Fact]
    public async Task CreateStoresOneExplicitProfilePerGameMode()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new ProfileApplicationService(store);

            var created = await service.CreateAsync(
                GameMode.Regular,
                level: 15,
                PmcFaction.Usec,
                editionId: "eod",
                prestigeLevel: 0,
                traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal),
                cancellationToken);

            Assert.Equal("regular", created.ProfileId);
            Assert.Equal(GameMode.Regular, created.GameMode);
            Assert.Equal(15, created.Level);
            Assert.Equal("eod", created.EditionId);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(
                    GameMode.Regular,
                    level: 20,
                    PmcFaction.Bear,
                    editionId: null,
                    prestigeLevel: 0,
                    traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal),
                    cancellationToken));
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task UpdatingSettingsPreservesQuestHideoutAndInventoryFacts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new ProfileApplicationService(store);
            var original = new GameProfileSnapshot
            {
                ProfileId = "regular",
                GameMode = GameMode.Regular,
                Level = 10,
                Faction = PmcFaction.Usec,
                CompletedQuestIds = new HashSet<string>(["quest-a"], StringComparer.Ordinal),
                HideoutLevels = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["station-a"] = 2,
                },
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["item-a"] = new InventoryQuantity(3, 4),
                },
            };
            await store.SaveAsync(original, cancellationToken);

            var updated = await service.UpdateSettingsAsync(
                original.ProfileId,
                level: 25,
                PmcFaction.Bear,
                editionId: "unheard",
                prestigeLevel: 1,
                traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
                {
                    ["trader-a"] = new TraderProgress(3, 1.25m),
                },
                cancellationToken);

            Assert.Equal(25, updated.Level);
            Assert.Equal(PmcFaction.Bear, updated.Faction);
            Assert.Equal("unheard", updated.EditionId);
            Assert.Equal(1, updated.PrestigeLevel);
            Assert.Contains("quest-a", updated.CompletedQuestIds);
            Assert.Equal(2, updated.HideoutLevels["station-a"]);
            Assert.Equal(new InventoryQuantity(3, 4), updated.Inventory["item-a"]);
            Assert.Equal(new TraderProgress(3, 1.25m), updated.Traders["trader-a"]);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    private static string TempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"JunhyunHelper-ProfileApp-{Guid.NewGuid():N}.db");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
