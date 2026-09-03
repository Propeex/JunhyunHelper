using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideRaidAcquiredProvenanceTests
{
    [Fact]
    public void AssemblySanitize_PreservesRaidAcquiredOnRoot()
    {
        var catalog = Catalog(Item("loot"));
        var acquired = FarmingGuideItemState.Create("loot", raidAcquired: true);

        var sanitized = FarmingGuideAssemblyPolicy.Sanitize(acquired, catalog);

        Assert.NotNull(sanitized);
        Assert.True(sanitized.RaidAcquired);
    }

    [Fact]
    public void AssemblySanitize_PreservesIndependentChildProvenance()
    {
        var slot = new FarmingGuideAttachmentSlotDefinition(
            "slot",
            "slot",
            "slot",
            false,
            new FarmingGuideItemFilter([], ["child"], [], []));
        var rootItem = Item("root") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [slot],
                [],
                [],
                [],
                false,
                false),
        };
        var catalog = Catalog(rootItem, Item("child"));
        var root = new FarmingGuideItemState(
            "root",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["slot"] = FarmingGuideItemState.Create("child", raidAcquired: true),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        var sanitized = FarmingGuideAssemblyPolicy.Sanitize(root, catalog);

        Assert.NotNull(sanitized);
        Assert.False(sanitized.RaidAcquired);
        Assert.True(Assert.IsType<FarmingGuideItemState>(sanitized.Attachments["slot"]).RaidAcquired);
    }

    [Fact]
    public void LoadoutSanitize_PreservesStoredRaidAcquiredProvenance()
    {
        var catalog = Catalog(Item("loot"));
        var snapshot = FarmingGuideLoadoutSnapshot.Empty with
        {
            StoredItems =
            [
                new FarmingGuideStoredItemState(
                    "scanner-loot",
                    FarmingGuideItemState.Create("loot", raidAcquired: true),
                    FarmingGuideStorageKind.Pockets,
                    0,
                    0,
                    0,
                    false),
            ],
        };

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        var stored = Assert.Single(sanitized.StoredItems);
        Assert.True(stored.Item.RaidAcquired);
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(sanitized, "loot"));
    }

    [Fact]
    public void LoadoutSanitize_PreservesEquippedRaidAcquiredProvenance()
    {
        var helmet = Item("helmet", typeKeys: ["helmet"]);
        var catalog = Catalog(helmet);
        var snapshot = FarmingGuideLoadoutSnapshot.Empty with
        {
            Equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Helmet] =
                    FarmingGuideItemState.Create("helmet", raidAcquired: true),
            },
        };

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        Assert.True(sanitized.Equipment[FarmingGuideEquipmentSlot.Helmet].RaidAcquired);
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(sanitized, "helmet"));
    }

    private static GameItem Item(
        string id,
        IReadOnlyList<string>? typeKeys = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            typeKeys ?? [],
            1,
            1,
            0.1m,
            1_000,
            true);

    private static IReadOnlyDictionary<string, GameItem> Catalog(params GameItem[] items) =>
        items.ToDictionary(static item => item.Id, StringComparer.Ordinal);
}
