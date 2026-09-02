using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class ContentSnapshotEquipmentFactsSchemaTests
{
    [Fact]
    public void V12MarksOlderReadableSnapshotsForRefresh()
    {
        Assert.Equal(12, ContentSnapshotStore.CurrentSchemaVersion);
        Assert.True(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 10, EmptyCatalog())));
        Assert.True(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 11, EmptyCatalog())));
        Assert.False(ContentSnapshotStore.RequiresCurrentSchemaRefresh(
            Snapshot(schemaVersion: 12, EmptyCatalog())));
    }

    [Fact]
    public async Task CurrentSnapshotRoundTripPreservesEquipmentAndTacticalComparisonFacts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var root = Path.Combine(
            Path.GetTempPath(),
            "JunhyunHelper-SchemaV12Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "content.db");

        try
        {
            var item = new GameItem(
                "headset",
                "헤드셋",
                "Headset",
                "헤드셋",
                "Headset",
                null,
                null,
                [],
                [],
                [],
                1,
                1) with
            {
                FarmingGuideData = new FarmingGuideItemLayout(
                    "ItemPropertiesWeapon",
                    [],
                    [],
                    [],
                    [],
                    [],
                    false,
                    false)
                {
                    ArmorClass = 5,
                    HeadsetDistanceModifier = 1.22m,
                    HeadsetDistortion = 0.16m,
                    Energy = 25,
                    Hydration = 15,
                    WeaponCaliber = "Caliber545x39",
                    AllowedAmmoItemIds = ["ammo-a", "ammo-b"],
                },
            };
            var content = EmptyCatalog() with { Items = [item] };
            var store = new ContentSnapshotStore();

            await store.WriteNewAsync(path, GameMode.Regular, content, cancellationToken: cancellationToken);
            var snapshot = await store.ReadAsync(path, cancellationToken);

            Assert.Equal(12, snapshot.SchemaVersion);
            var restored = Assert.Single(snapshot.Content.Items);
            Assert.Equal(5, restored.FarmingGuideData?.ArmorClass);
            Assert.Equal(1.22m, restored.FarmingGuideData?.HeadsetDistanceModifier);
            Assert.Equal(0.16m, restored.FarmingGuideData?.HeadsetDistortion);
            Assert.Equal(25, restored.FarmingGuideData?.Energy);
            Assert.Equal(15, restored.FarmingGuideData?.Hydration);
            Assert.Equal("Caliber545x39", restored.FarmingGuideData?.WeaponCaliber);
            Assert.Equal(["ammo-a", "ammo-b"], restored.FarmingGuideData?.AllowedAmmoItemIds);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static StoredContentSnapshot Snapshot(int schemaVersion, GameContentCatalog content) =>
        new(schemaVersion, GameMode.Regular, DateTimeOffset.UtcNow, content, []);

    private static GameContentCatalog EmptyCatalog() =>
        new([], [], [], [], [], [], []);
}
