using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideSnapshotInventoryCounterTests
{
    [Fact]
    public void CountIncludesEquipmentCarriersStoredItemsAndAssemblyChildren()
    {
        var nested = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["mod"] = FarmingGuideItemState.Create("needle"),
            },
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate"] = FarmingGuideItemState.Create("needle"),
            });
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.PrimaryWeapon1] = nested,
                [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create("needle"),
            },
            FarmingGuideItemState.Create("needle"),
            FarmingGuideItemState.Create("bag"),
            FarmingGuideItemState.Create("secure"),
            [new FarmingGuideStoredItemState(
                "stored",
                FarmingGuideItemState.Create("needle"),
                FarmingGuideStorageKind.Backpack,
                0,
                0,
                0,
                false)]);

        Assert.Equal(5, FarmingGuideSnapshotInventoryCounter.Count(snapshot, "needle"));
    }

    [Fact]
    public void StoredStackContributesItsConcreteQuantity()
    {
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create("bag"),
            null,
            [new FarmingGuideStoredItemState(
                "ammo-stack",
                FarmingGuideItemState.Create("ammo"),
                FarmingGuideStorageKind.Backpack,
                0,
                0,
                0,
                false,
                Quantity: 43)]);

        Assert.Equal(43, FarmingGuideSnapshotInventoryCounter.Count(snapshot, "ammo"));
    }

    [Fact]
    public void AcquiredSinceTracksCurrentSnapshotInsteadOfHistoricalAcceptance()
    {
        var baseline = SnapshotWithStored("baseline", "quest-item");
        var afterPickup = new FarmingGuideLoadoutSnapshot(
            baseline.Equipment,
            baseline.Rig,
            baseline.Backpack,
            baseline.SecureContainer,
            baseline.StoredItems.Append(new FarmingGuideStoredItemState(
                "picked-up",
                FarmingGuideItemState.Create("quest-item"),
                FarmingGuideStorageKind.Backpack,
                0,
                1,
                0,
                false)).ToArray());
        var afterDiscard = baseline;

        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.AcquiredSince(baseline, afterPickup, "quest-item"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.AcquiredSince(baseline, afterDiscard, "quest-item"));
    }

    private static FarmingGuideLoadoutSnapshot SnapshotWithStored(string instanceId, string itemId) =>
        new(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create("bag"),
            null,
            [new FarmingGuideStoredItemState(
                instanceId,
                FarmingGuideItemState.Create(itemId),
                FarmingGuideStorageKind.Backpack,
                0,
                0,
                0,
                false)]);
}
