using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Storage;

public sealed class ContentSnapshotStoreTests
{
    [Fact]
    public async Task CanonicalContentRoundTripsAsOneVersionedSnapshot()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "JunhyunHelper.Tests",
            Guid.NewGuid().ToString("N"),
            "content.db");

        try
        {
            var content = CreateContent();
            var store = new ContentSnapshotStore();

            await store.WriteNewAsync(
                path,
                GameMode.Regular,
                content,
                new[] { "fixture warning" });

            var loaded = await store.ReadAsync(path);

            Assert.Equal(ContentSnapshotStore.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(GameMode.Regular, loaded.GameMode);
            Assert.Equal("item-a", Assert.Single(loaded.Content.Items).Id);
            Assert.Equal("quest-a", Assert.Single(loaded.Content.Quests).Id);
            Assert.Equal("station-a", Assert.Single(loaded.Content.HideoutStations).Id);
            Assert.Equal("fixture warning", Assert.Single(loaded.Warnings));

            var status = Assert.Single(
                Assert.Single(loaded.Content.Quests).TaskRequirements).AcceptedStatuses;
            Assert.Contains(QuestRequiredStatus.Complete, status);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static GameContentCatalog CreateContent()
    {
        return new GameContentCatalog(
            new[]
            {
                new GameItem(
                    "item-a",
                    "아이템 A",
                    "Item A",
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<string>()),
            },
            new[] { new TraderDefinition("trader-a", "상인 A", "Trader A") },
            new[] { new MapReference("map-a", "맵 A", "Map A", "map-a") },
            new[]
            {
                new QuestDefinition(
                    "quest-a",
                    "퀘스트 A",
                    "Quest A",
                    "trader-a",
                    "map-a",
                    null,
                    null,
                    false,
                    false,
                    false,
                    1,
                    PmcFaction.Usec,
                    null,
                    new[]
                    {
                        new QuestTaskRequirement(
                            "quest-a",
                            new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete }),
                    },
                    Array.Empty<QuestTraderStandingRequirement>(),
                    Array.Empty<QuestTraderLoyaltyRequirement>()),
            },
            Array.Empty<QuestObjective>(),
            new[]
            {
                new QuestItemRequirement(
                    "quest-a",
                    "objective-a",
                    new[] { "item-a" },
                    1,
                    false),
            },
            new[]
            {
                new HideoutStation(
                    "station-a",
                    "시설 A",
                    "Station A",
                    null,
                    new[]
                    {
                        new HideoutLevel(
                            "station-a",
                            1,
                            null,
                            new[]
                            {
                                new HideoutItemRequirement(
                                    "station-a",
                                    1,
                                    "item-a",
                                    2,
                                    false),
                            }),
                    }),
            });
    }
}
