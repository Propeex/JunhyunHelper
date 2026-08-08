using JunhyunHelper.Core.Ammo;
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
        var cancellationToken = TestContext.Current.CancellationToken;
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
                new[] { "fixture warning" },
                cancellationToken);

            var loaded = await store.ReadAsync(path, cancellationToken);

            Assert.Equal(ContentSnapshotStore.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal(GameMode.Regular, loaded.GameMode);
            Assert.Equal("item-a", Assert.Single(loaded.Content.Items).Id);

            var quest = Assert.Single(loaded.Content.Quests);
            Assert.Equal("quest-a", quest.Id);
            Assert.Equal("dialogue", Assert.Single(quest.UnsupportedAvailabilityRequirements));

            Assert.Equal("station-a", Assert.Single(loaded.Content.HideoutStations).Id);
            Assert.Equal("fixture warning", Assert.Single(loaded.Warnings));

            var status = Assert.Single(quest.TaskRequirements).AcceptedStatuses;
            Assert.Contains(QuestRequiredStatus.Complete, status);

            var ammo = Assert.Single(loaded.Content.Ammunition);
            Assert.Equal("item-a", ammo.ItemId);
            Assert.Equal(31, ammo.PenetrationPower);
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
                    Array.Empty<QuestTraderLoyaltyRequirement>(),
                    new[] { "dialogue" }),
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
            },
            new[]
            {
                new AmmoDefinition(
                    "item-a",
                    "Caliber556x45NATO",
                    "bullet",
                    1,
                    54,
                    37,
                    31,
                    0.4m,
                    0.2m,
                    0m,
                    -0.05m,
                    922m,
                    0.1m,
                    0.2m,
                    false,
                    "none",
                    Array.Empty<AmmoAcquisition>()),
            });
    }
}
