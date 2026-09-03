using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideOptimizationPolicyTests
{
    [Fact]
    public void Score_PrioritizesNeededFirUnitsAheadOfEconomicValue()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var needed = Snapshot(Stored("needed", "quest-food", 1));
        var expensive = Snapshot(Stored("expensive", "valuable", 1));
        var needs = new Dictionary<string, int> { ["quest-food"] = 1 };
        var prices = new Dictionary<string, int?>
        {
            ["quest-food"] = 5_000,
            ["valuable"] = 500_000,
        };

        var neededScore = FarmingGuideOptimizationPolicy.Score(
            baseline,
            needed,
            id => needs.GetValueOrDefault(id),
            id => prices.GetValueOrDefault(id));
        var expensiveScore = FarmingGuideOptimizationPolicy.Score(
            baseline,
            expensive,
            id => needs.GetValueOrDefault(id),
            id => prices.GetValueOrDefault(id));

        Assert.True(FarmingGuideOptimizationPolicy.IsBetter(neededScore, expensiveScore));
    }

    [Fact]
    public void Score_CapsFirBenefitAtRemainingRequiredQuantity()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var one = Snapshot(Stored("one", "quest-item", 1));
        var three = Snapshot(Stored("three", "quest-item", 3));

        var oneScore = FarmingGuideOptimizationPolicy.Score(baseline, one, _ => 1, _ => 10_000);
        var threeScore = FarmingGuideOptimizationPolicy.Score(baseline, three, _ => 1, _ => 10_000);

        Assert.Equal(1, oneScore.SatisfiedFirUnits);
        Assert.Equal(1, threeScore.SatisfiedFirUnits);
        Assert.True(threeScore.RetainedFleaValue > oneScore.RetainedFleaValue);
    }

    [Fact]
    public void Score_DoesNotTreatBaselineCopyAsNewFirAcquisition()
    {
        var baseline = Snapshot(Stored("baseline", "quest-item", 1));
        var unchanged = Snapshot(Stored("baseline", "quest-item", 1));
        var acquiredSecond = Snapshot(
            Stored("baseline", "quest-item", 1),
            Stored("loot", "quest-item", 1));

        var unchangedScore = FarmingGuideOptimizationPolicy.Score(baseline, unchanged, _ => 2, _ => 10_000);
        var acquiredScore = FarmingGuideOptimizationPolicy.Score(baseline, acquiredSecond, _ => 2, _ => 10_000);

        Assert.Equal(0, unchangedScore.SatisfiedFirUnits);
        Assert.Equal(1, acquiredScore.SatisfiedFirUnits);
    }

    [Fact]
    public void Score_UsesTotalRetainedValueAfterFirTie()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var left = Snapshot(
            Stored("needed-left", "quest-item", 1),
            Stored("left-value", "left", 1));
        var right = Snapshot(
            Stored("needed-right", "quest-item", 1),
            Stored("right-value", "right", 1));
        var prices = new Dictionary<string, int?>
        {
            ["quest-item"] = 1_000,
            ["left"] = 20_000,
            ["right"] = 30_000,
        };

        var leftScore = FarmingGuideOptimizationPolicy.Score(
            baseline, left, id => id == "quest-item" ? 1 : 0, id => prices.GetValueOrDefault(id));
        var rightScore = FarmingGuideOptimizationPolicy.Score(
            baseline, right, id => id == "quest-item" ? 1 : 0, id => prices.GetValueOrDefault(id));

        Assert.True(FarmingGuideOptimizationPolicy.IsBetter(rightScore, leftScore));
    }

    private static FarmingGuideLoadoutSnapshot Snapshot(params FarmingGuideStoredItemState[] stored) =>
        FarmingGuideLoadoutSnapshot.Empty with { StoredItems = stored };

    private static FarmingGuideStoredItemState Stored(string instanceId, string itemId, int quantity) =>
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
