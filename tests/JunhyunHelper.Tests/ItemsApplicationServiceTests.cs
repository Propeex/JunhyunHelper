using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class ItemsApplicationServiceTests
{
    [Fact]
    public async Task InventoryUpdate_PreservesOtherProgressFacts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = TempDbPath();
        try
        {
            var store = new UserProfileStore(path);
            var original = new GameProfileSnapshot
            {
                ProfileId = "regular",
                GameMode = GameMode.Regular,
                Level = 20,
                Faction = PmcFaction.Usec,
                CompletedQuestIds = new HashSet<string>(["quest"], StringComparer.Ordinal),
                HideoutLevels = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["workbench"] = 2,
                },
            };
            await store.SaveAsync(original, cancellationToken);

            var service = new ItemsApplicationService(store);
            var workspace = await service.SetInventoryAsync(
                EmptyContent(),
                original.ProfileId,
                "wire",
                2,
                7,
                cancellationToken);

            Assert.Contains("quest", workspace.Profile.CompletedQuestIds);
            Assert.Equal(2, workspace.Profile.HideoutLevels["workbench"]);
            Assert.Equal(new InventoryQuantity(2, 7), workspace.Profile.Inventory["wire"]);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task SettingInventoryToZero_RemovesStoredEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var path = TempDbPath();
        try
        {
            var store = new UserProfileStore(path);
            var original = new GameProfileSnapshot
            {
                ProfileId = "regular",
                GameMode = GameMode.Regular,
                Level = 1,
                Faction = PmcFaction.Usec,
                Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
                {
                    ["wire"] = new InventoryQuantity(0, 4),
                },
            };
            await store.SaveAsync(original, cancellationToken);

            var service = new ItemsApplicationService(store);
            var workspace = await service.SetInventoryAsync(
                EmptyContent(),
                original.ProfileId,
                "wire",
                0,
                0,
                cancellationToken);

            Assert.False(workspace.Profile.Inventory.ContainsKey("wire"));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static GameContentCatalog EmptyContent() =>
        new(
            Array.Empty<GameItem>(),
            Array.Empty<JunhyunHelper.Core.Reference.TraderDefinition>(),
            Array.Empty<JunhyunHelper.Core.Reference.MapReference>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestDefinition>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestObjective>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestItemRequirement>(),
            Array.Empty<JunhyunHelper.Core.Hideout.HideoutStation>());

    private static string TempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"JunhyunHelper.Tests.{Guid.NewGuid():N}.db");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
