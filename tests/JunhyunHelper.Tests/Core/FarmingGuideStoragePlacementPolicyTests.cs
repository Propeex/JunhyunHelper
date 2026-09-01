using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideStoragePlacementPolicyTests
{
    [Fact]
    public void SpecialSlotCompatibilityUsesSourceTypeClassification()
    {
        var allowed = Item("surv12", width: 1, height: 3, typeKeys: ["specialSlot"]);
        var normalized = Item("tool", width: 2, height: 1, typeKeys: ["special_slot"]);
        var denied = Item("loot", width: 1, height: 1);

        Assert.True(FarmingGuideStoragePlacementPolicy.IsSpecialSlotCompatible(allowed));
        Assert.True(FarmingGuideStoragePlacementPolicy.IsSpecialSlotCompatible(normalized));
        Assert.False(FarmingGuideStoragePlacementPolicy.IsSpecialSlotCompatible(denied));
    }

    [Fact]
    public void RootSpecialSlotUsesOneCellRegardlessOfOrdinaryFootprint()
    {
        var item = Item("surv12", width: 1, height: 3, typeKeys: ["specialSlot"]);

        var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
            FarmingGuideStorageKind.SpecialSlots,
            parentInstanceId: null,
            item,
            rotated: false);

        Assert.Equal((1, 1), footprint);
        Assert.False(FarmingGuideStoragePlacementPolicy.SupportsRotation(
            FarmingGuideStorageKind.SpecialSlots,
            parentInstanceId: null,
            item));
    }

    [Fact]
    public void NestedStorageInsideSpecialSlotItemKeepsOrdinaryFootprint()
    {
        var item = Item("nested", width: 1, height: 3, typeKeys: ["specialSlot"]);

        var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
            FarmingGuideStorageKind.SpecialSlots,
            parentInstanceId: "parent",
            item,
            rotated: true);

        Assert.Equal((3, 1), footprint);
    }

    [Fact]
    public void SanitizeSnapshotAcceptsOnlyCompatibleItemsInSpecialSlotsAndCompressesTheirFootprint()
    {
        var surv12 = Item("surv12", width: 1, height: 3, typeKeys: ["specialSlot"]);
        var ordinary = Item("ordinary", width: 1, height: 1);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [surv12.Id] = surv12,
            [ordinary.Id] = ordinary,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            null,
            null,
            [
                new FarmingGuideStoredItemState(
                    "surv",
                    FarmingGuideItemState.Create(surv12.Id),
                    FarmingGuideStorageKind.SpecialSlots,
                    0,
                    0,
                    0,
                    false),
                new FarmingGuideStoredItemState(
                    "ordinary",
                    FarmingGuideItemState.Create(ordinary.Id),
                    FarmingGuideStorageKind.SpecialSlots,
                    1,
                    0,
                    0,
                    false),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        var kept = Assert.Single(sanitized.StoredItems);
        Assert.Equal("surv", kept.InstanceId);
    }

    private static GameItem Item(
        string id,
        int width,
        int height,
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
            width,
            height);
}
