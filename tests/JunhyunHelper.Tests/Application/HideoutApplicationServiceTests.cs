using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class HideoutApplicationServiceTests
{
    [Fact]
    public async Task UnenteredLevelStaysUnknownUntilUserSetsIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new HideoutApplicationService(store);
            var profile = CreateProfile();
            var content = CreateContent();
            await store.SaveAsync(profile, cancellationToken);

            var initial = await service.LoadAsync(content, profile.ProfileId, cancellationToken);
            var station = Assert.Single(initial.Stations);
            Assert.Null(station.CurrentLevel);
            Assert.Null(station.NextLevel);

            var entered = await service.SetLevelAsync(
                content,
                profile.ProfileId,
                "station-a",
                0,
                cancellationToken);
            var enteredStation = Assert.Single(entered.Stations);
            Assert.Equal(0, enteredStation.CurrentLevel);
            Assert.Equal(1, enteredStation.NextLevel?.Level);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task ChangingHideoutLevelPreservesOtherProfileFactsAndCanBeCleared()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new HideoutApplicationService(store);
            var profile = CreateProfile() with
            {
                CompletedQuestIds = new HashSet<string>(["quest-a"], StringComparer.Ordinal),
            };
            var content = CreateContent();
            await store.SaveAsync(profile, cancellationToken);

            var set = await service.SetLevelAsync(
                content,
                profile.ProfileId,
                "station-a",
                2,
                cancellationToken);
            Assert.Equal(2, set.Profile.HideoutLevels["station-a"]);
            Assert.Contains("quest-a", set.Profile.CompletedQuestIds);
            Assert.Null(Assert.Single(set.Stations).NextLevel);

            var cleared = await service.SetLevelAsync(
                content,
                profile.ProfileId,
                "station-a",
                null,
                cancellationToken);
            Assert.DoesNotContain("station-a", cleared.Profile.HideoutLevels.Keys);
            Assert.Null(Assert.Single(cleared.Stations).CurrentLevel);
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    [Fact]
    public async Task RejectsLevelOutsideStationRange()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var databasePath = TempDatabasePath();

        try
        {
            var store = new UserProfileStore(databasePath);
            var service = new HideoutApplicationService(store);
            var profile = CreateProfile();
            await store.SaveAsync(profile, cancellationToken);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.SetLevelAsync(
                    CreateContent(),
                    profile.ProfileId,
                    "station-a",
                    3,
                    cancellationToken));
        }
        finally
        {
            DeleteIfExists(databasePath);
        }
    }

    private static GameProfileSnapshot CreateProfile() => new()
    {
        ProfileId = "regular",
        GameMode = GameMode.Regular,
        Level = 10,
        Faction = PmcFaction.Usec,
    };

    private static GameContentCatalog CreateContent()
    {
        var station = new HideoutStation(
            "station-a",
            "작업대",
            "Workbench",
            null,
            [
                new HideoutLevel(
                    "station-a",
                    1,
                    60,
                    [new HideoutItemRequirement("station-a", 1, "item-a", 2, false)]),
                new HideoutLevel(
                    "station-a",
                    2,
                    120,
                    [new HideoutItemRequirement("station-a", 2, "item-a", 3, false)]),
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

    private static string TempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"JunhyunHelper-HideoutApp-{Guid.NewGuid():N}.db");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
