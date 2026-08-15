using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Storage;

public sealed class UserProfileStoreTests
{
    [Fact]
    public async Task SavesAndLoadsProfileFactsWithoutDerivedState()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            var profile = new GameProfileSnapshot
            {
                ProfileId = "pvp-main",
                GameMode = GameMode.Regular,
                Level = 42,
                Faction = PmcFaction.Usec,
                EditionId = "eod",
                PrestigeLevel = 1,
                Traders = new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
                {
                    ["fence"] = new(2, 3.25m),
                },
                CompletedQuestIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    "quest-a",
                    "quest-b",
                },
                FailedQuestIds = new HashSet<string>(StringComparer.Ordinal)
                {
                    "quest-failed",
                },
                SpecialTraderAccessOverrides = new Dictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["lightkeeper"] = false,
                },
                HideoutLevels = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["workbench"] = 3,
                },
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["item-a"] = new(Fir: 4, NonFir: 7),
                },
            };

            await store.SaveAsync(profile, cancellationToken);
            var loaded = await store.LoadAsync(profile.ProfileId, cancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal(profile.ProfileId, loaded.ProfileId);
            Assert.Equal(profile.GameMode, loaded.GameMode);
            Assert.Equal(profile.Level, loaded.Level);
            Assert.Equal(profile.Faction, loaded.Faction);
            Assert.Equal(profile.EditionId, loaded.EditionId);
            Assert.Equal(profile.PrestigeLevel, loaded.PrestigeLevel);
            Assert.Equal(profile.Traders["fence"], loaded.Traders["fence"]);
            Assert.True(loaded.CompletedQuestIds.SetEquals(profile.CompletedQuestIds));
            Assert.True(loaded.FailedQuestIds.SetEquals(profile.FailedQuestIds));
            Assert.False(loaded.SpecialTraderAccessOverrides["lightkeeper"]);
            Assert.Equal(3, loaded.HideoutLevels["workbench"]);
            Assert.Equal(new InventoryQuantity(4, 7), loaded.Inventory["item-a"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DifferentGameModeProfilesRemainIndependent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile("pvp", GameMode.Regular, level: 20), cancellationToken);
            await store.SaveAsync(Profile("pve", GameMode.Pve, level: 55), cancellationToken);

            var profiles = await store.LoadAllAsync(cancellationToken);

            Assert.Equal(2, profiles.Count);
            Assert.Contains(profiles, profile =>
                profile.ProfileId == "pvp" &&
                profile.GameMode == GameMode.Regular &&
                profile.Level == 20);
            Assert.Contains(profiles, profile =>
                profile.ProfileId == "pve" &&
                profile.GameMode == GameMode.Pve &&
                profile.Level == 55);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavingSameProfileReplacesFactsAtomicallyAtProfileLevel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile("pvp", GameMode.Regular, level: 10), cancellationToken);
            await store.SaveAsync(Profile("pvp", GameMode.Regular, level: 11), cancellationToken);

            var loaded = await store.LoadAsync("pvp", cancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal(11, loaded.Level);
            Assert.Single(await store.LoadAllAsync(cancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteRemovesOnlyRequestedProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            await store.SaveAsync(Profile("pvp", GameMode.Regular, level: 20), cancellationToken);
            await store.SaveAsync(Profile("pve", GameMode.Pve, level: 55), cancellationToken);

            Assert.True(await store.DeleteAsync("pvp", cancellationToken));
            Assert.Null(await store.LoadAsync("pvp", cancellationToken));
            Assert.NotNull(await store.LoadAsync("pve", cancellationToken));
            Assert.False(await store.DeleteAsync("pvp", cancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CompletedAndFailedQuestOverlapIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            var profile = Profile("pvp", GameMode.Regular, level: 1) with
            {
                CompletedQuestIds = new HashSet<string>(["quest-a"], StringComparer.Ordinal),
                FailedQuestIds = new HashSet<string>(["quest-a"], StringComparer.Ordinal),
            };

            await Assert.ThrowsAsync<InvalidDataException>(
                () => store.SaveAsync(profile, cancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidNegativeInventoryIsRejectedInsteadOfNormalizedSilently()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = CreateTempDirectory();
        try
        {
            var store = new UserProfileStore(Path.Combine(directory, "user.db"));
            var profile = Profile("pvp", GameMode.Regular, level: 1) with
            {
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["item-a"] = new(Fir: -1, NonFir: 0),
                },
            };

            await Assert.ThrowsAsync<InvalidDataException>(
                () => store.SaveAsync(profile, cancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static GameProfileSnapshot Profile(string id, GameMode mode, int level) =>
        new()
        {
            ProfileId = id,
            GameMode = mode,
            Level = level,
            Faction = PmcFaction.Usec,
        };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JunhyunHelperTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
