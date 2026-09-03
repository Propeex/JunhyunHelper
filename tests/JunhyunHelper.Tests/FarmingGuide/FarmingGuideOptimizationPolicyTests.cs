using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideOptimizationPolicyTests
{
    [Fact]
    public void Score_PrioritizesNeededFirUnitsAheadOfEconomicValue()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var needed = Snapshot(Stored(
            "needed",
            "quest-food",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid));
        var expensive = Snapshot(Stored("expensive", "valuable", 1, acquired: true));
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
        var one = Snapshot(Stored(
            "one",
            "quest-item",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid));
        var three = Snapshot(Stored(
            "three",
            "quest-item",
            3,
            acquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid));

        var oneScore = FarmingGuideOptimizationPolicy.Score(baseline, one, _ => 1, _ => 10_000);
        var threeScore = FarmingGuideOptimizationPolicy.Score(baseline, three, _ => 1, _ => 10_000);

        Assert.Equal(1, oneScore.SatisfiedFirUnits);
        Assert.Equal(1, threeScore.SatisfiedFirUnits);
        Assert.True(threeScore.RetainedFleaValue > oneScore.RetainedFleaValue);
    }

    [Fact]
    public void Score_RaidAcquiredUnknownDoesNotSatisfyFirNeed()
    {
        var candidate = Snapshot(Stored(
            "loot",
            "quest-item",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.Unknown));

        var score = FarmingGuideOptimizationPolicy.Score(
            FarmingGuideLoadoutSnapshot.Empty,
            candidate,
            _ => 1,
            _ => 10_000);

        Assert.Equal(0, score.SatisfiedFirUnits);
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(candidate, "quest-item"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(candidate, "quest-item"));
    }

    [Fact]
    public void Score_RaidAcquiredNotFoundInRaidDoesNotSatisfyFirNeed()
    {
        var candidate = Snapshot(Stored(
            "loot",
            "quest-item",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.NotFoundInRaid));

        var score = FarmingGuideOptimizationPolicy.Score(
            FarmingGuideLoadoutSnapshot.Empty,
            candidate,
            _ => 1,
            _ => 10_000);

        Assert.Equal(0, score.SatisfiedFirUnits);
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(candidate, "quest-item"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(candidate, "quest-item"));
    }

    [Fact]
    public void Score_ExplicitFoundInRaidSatisfiesFirNeed()
    {
        var candidate = Snapshot(Stored(
            "loot",
            "quest-item",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid));

        var score = FarmingGuideOptimizationPolicy.Score(
            FarmingGuideLoadoutSnapshot.Empty,
            candidate,
            _ => 1,
            _ => 10_000);

        Assert.Equal(1, score.SatisfiedFirUnits);
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(candidate, "quest-item"));
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(candidate, "quest-item"));
    }

    [Fact]
    public void Score_PreservesFirProvenanceWhenIdenticalBaselineCopyWasDiscarded()
    {
        var baseline = Snapshot(Stored("baseline", "quest-item", 1));
        var current = Snapshot(Stored(
            "loot",
            "quest-item",
            1,
            acquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid));

        var score = FarmingGuideOptimizationPolicy.Score(baseline, current, _ => 1, _ => 10_000);

        Assert.Equal(1, score.SatisfiedFirUnits);
        Assert.Equal(10_000, score.RetainedFleaValue);
    }

    [Fact]
    public void CompleteEquipmentNormalizationPreservesAcquisitionAndFirProvenance()
    {
        var original = FarmingGuideItemState.Create(
            "quest-item",
            raidAcquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid) with
        {
            Attachments = new Dictionary<string, FarmingGuideItemState?>
            {
                ["scope"] = FarmingGuideItemState.Create("optic"),
            },
        };

        var normalized = FarmingGuideCompleteEquipmentPolicy.NormalizeState(original);

        Assert.True(normalized.RaidAcquired);
        Assert.Equal(FarmingGuideFirStatus.FoundInRaid, normalized.FirStatus);
        Assert.True(normalized.IsFirQualified);
        Assert.Empty(normalized.Attachments);
    }

    [Fact]
    public void Score_UsesTotalRetainedValueAfterFirTie()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var left = Snapshot(
            Stored(
                "needed-left",
                "quest-item",
                1,
                acquired: true,
                firStatus: FarmingGuideFirStatus.FoundInRaid),
            Stored("left-value", "left", 1, acquired: true));
        var right = Snapshot(
            Stored(
                "needed-right",
                "quest-item",
                1,
                acquired: true,
                firStatus: FarmingGuideFirStatus.FoundInRaid),
            Stored("right-value", "right", 1, acquired: true));
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

    [Fact]
    public void ProvenanceCountersTrackNestedAssemblyIndependently()
    {
        var acquiredAttachment = FarmingGuideItemState.Create(
            "optic",
            raidAcquired: true,
            firStatus: FarmingGuideFirStatus.FoundInRaid);
        var acquiredButNotFirPlate = FarmingGuideItemState.Create(
            "plate",
            raidAcquired: true,
            firStatus: FarmingGuideFirStatus.NotFoundInRaid);
        var weapon = FarmingGuideItemState.Create("weapon") with
        {
            Attachments = new Dictionary<string, FarmingGuideItemState?>
            {
                ["scope"] = acquiredAttachment,
            },
            ArmorPlates = new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate"] = acquiredButNotFirPlate,
            },
        };
        var snapshot = FarmingGuideLoadoutSnapshot.Empty with
        {
            Equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.PrimaryWeapon1] = weapon,
            },
        };

        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(snapshot, "optic"));
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(snapshot, "optic"));
        Assert.Equal(1, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(snapshot, "plate"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(snapshot, "plate"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.CountRaidAcquired(snapshot, "weapon"));
        Assert.Equal(0, FarmingGuideSnapshotInventoryCounter.CountFoundInRaid(snapshot, "weapon"));
    }

    private static FarmingGuideLoadoutSnapshot Snapshot(params FarmingGuideStoredItemState[] stored) =>
        FarmingGuideLoadoutSnapshot.Empty with { StoredItems = stored };

    private static FarmingGuideStoredItemState Stored(
        string instanceId,
        string itemId,
        int quantity,
        bool acquired = false,
        FarmingGuideFirStatus firStatus = FarmingGuideFirStatus.Unknown) =>
        new(
            instanceId,
            FarmingGuideItemState.Create(itemId, raidAcquired: acquired, firStatus: firStatus),
            FarmingGuideStorageKind.Backpack,
            0,
            0,
            0,
            false,
            Quantity: quantity);
}
