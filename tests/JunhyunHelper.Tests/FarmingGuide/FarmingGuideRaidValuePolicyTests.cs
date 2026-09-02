using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideRaidValuePolicyTests
{
    [Fact]
    public void CalculateAcquiredFleaValue_CountsOnlyNetAcquiredQuantity()
    {
        var baseline = Snapshot(
            Stored("baseline-a", "item-a", 1),
            Stored("baseline-stack", "ammo", 30));
        var current = Snapshot(
            Stored("baseline-a", "item-a", 1),
            Stored("baseline-stack", "ammo", 50),
            Stored("loot-b", "item-b", 1));

        var prices = new Dictionary<string, int?>
        {
            ["item-a"] = 999_999,
            ["ammo"] = 1_000,
            ["item-b"] = 25_000,
        };

        var value = FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(
            baseline,
            current,
            itemId => prices.GetValueOrDefault(itemId));

        Assert.Equal(45_000L, value);
    }

    [Fact]
    public void CalculateAcquiredFleaValue_DoesNotBecomeNegativeWhenBaselineItemsAreLost()
    {
        var baseline = Snapshot(Stored("baseline", "item-a", 3));
        var current = FarmingGuideLoadoutSnapshot.Empty;

        var value = FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(
            baseline,
            current,
            _ => 100_000);

        Assert.Equal(0L, value);
    }

    [Fact]
    public void CalculateAcquiredFleaValue_ReflectsLootDiscardedAfterPickup()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var current = Snapshot(Stored("loot", "item-a", 1));

        Assert.Equal(
            40_000L,
            FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(baseline, current, _ => 40_000));
        Assert.Equal(
            0L,
            FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(
                baseline,
                FarmingGuideLoadoutSnapshot.Empty,
                _ => 40_000));
    }

    [Fact]
    public void CalculateAcquiredFleaValue_CountsNestedAssemblyItemsAndIgnoresUnknownPrices()
    {
        var attachment = FarmingGuideItemState.Create("attachment");
        var root = FarmingGuideItemState.Create("weapon") with
        {
            Attachments = new Dictionary<string, FarmingGuideItemState?>
            {
                ["scope"] = attachment,
            },
        };
        var current = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.PrimaryWeapon1] = root,
            },
            null,
            null,
            null,
            []);

        var value = FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(
            FarmingGuideLoadoutSnapshot.Empty,
            current,
            itemId => itemId == "attachment" ? 15_000 : null);

        Assert.Equal(15_000L, value);
    }

    [Fact]
    public void CalculateAcquiredFleaValue_IgnoresNonPositivePrices()
    {
        var current = Snapshot(
            Stored("zero", "zero", 1),
            Stored("negative", "negative", 1));

        var value = FarmingGuideRaidValuePolicy.CalculateAcquiredFleaValue(
            FarmingGuideLoadoutSnapshot.Empty,
            current,
            itemId => itemId == "zero" ? 0 : -100);

        Assert.Equal(0L, value);
    }

    private static FarmingGuideLoadoutSnapshot Snapshot(params FarmingGuideStoredItemState[] stored) =>
        FarmingGuideLoadoutSnapshot.Empty with { StoredItems = stored };

    private static FarmingGuideStoredItemState Stored(
        string instanceId,
        string itemId,
        int quantity) =>
        new(
            instanceId,
            FarmingGuideItemState.Create(itemId),
            FarmingGuideStorageKind.Backpack,
            0,
            0,
            0,
            false,
            Quantity: quantity);
}
