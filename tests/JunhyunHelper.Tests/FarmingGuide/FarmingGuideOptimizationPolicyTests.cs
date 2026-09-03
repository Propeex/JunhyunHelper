using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideOptimizationPolicyTests
{
    [Fact]
    public void NeededRaidAcquiredUnitOutranksMuchHigherOrdinaryValue()
    {
        var snapshot = Snapshot(
            Stored("needed", "quest", raidAcquired: true),
            Stored("valuable", "valuable", raidAcquired: false));

        var score = FarmingGuideOptimizationPolicy.Score(
            snapshot,
            id => id == "quest" ? 1 : 0,
            id => id == "quest" ? 1 : 1_000_000);

        Assert.Equal(1, score.SatisfiedFirUnits);
        Assert.Equal(1_000_001, score.RetainedFleaValue);
    }

    [Fact]
    public void FirBenefitIsCappedByRemainingRequiredQuantity()
    {
        var snapshot = Snapshot(
            Stored("stack", "quest", raidAcquired: true, quantity: 8));

        var score = FarmingGuideOptimizationPolicy.Score(
            snapshot,
            _ => 3,
            _ => 100);

        Assert.Equal(3, score.SatisfiedFirUnits);
        Assert.Equal(800, score.RetainedFleaValue);
    }

    [Fact]
    public void RaidAcquiredProvenanceDoesNotDependOnNetItemIdDelta()
    {
        var snapshot = Snapshot(
            Stored("replacement", "same-id", raidAcquired: true));

        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(snapshot, "same-id"));
    }

    [Fact]
    public void TotalRetainedFleaValueBreaksFirTie()
    {
        var lower = FarmingGuideOptimizationPolicy.Score(
            Snapshot(Stored("a", "a", raidAcquired: false)),
            _ => 0,
            _ => 10_000);
        var higher = FarmingGuideOptimizationPolicy.Score(
            Snapshot(Stored("b", "b", raidAcquired: false)),
            _ => 0,
            _ => 20_000);

        Assert.True(FarmingGuideOptimizationPolicy.IsBetter(higher, lower));
    }

    private static FarmingGuideLoadoutSnapshot Snapshot(params FarmingGuideStoredItemState[] stored) =>
        FarmingGuideLoadoutSnapshot.Empty with { StoredItems = stored };

    private static FarmingGuideStoredItemState Stored(
        string instanceId,
        string itemId,
        bool raidAcquired,
        int quantity = 1) =>
        new(
            instanceId,
            FarmingGuideItemState.Create(itemId, raidAcquired),
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false,
            Quantity: quantity);
}
