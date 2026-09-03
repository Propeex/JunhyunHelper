namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// The complete-state Farming Guide objective confirmed for v1.17.
/// Comparison is lexicographic: satisfied currently-needed FIR units first, then total
/// retained average-Flea value. Geometry and weight are feasibility constraints outside this
/// score and must never be smuggled in as additional priority dimensions.
/// </summary>
public readonly record struct FarmingGuideOptimizationScore(
    int SatisfiedFirUnits,
    long RetainedFleaValue) : IComparable<FarmingGuideOptimizationScore>
{
    public int CompareTo(FarmingGuideOptimizationScore other)
    {
        var fir = SatisfiedFirUnits.CompareTo(other.SatisfiedFirUnits);
        return fir != 0 ? fir : RetainedFleaValue.CompareTo(other.RetainedFleaValue);
    }
}

public static class FarmingGuideOptimizationPolicy
{
    public static FarmingGuideOptimizationScore Score(
        FarmingGuideLoadoutSnapshot snapshot,
        Func<string, int> remainingFirNeed,
        Func<string, int?> fleaAverageValue)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(remainingFirNeed);
        ArgumentNullException.ThrowIfNull(fleaAverageValue);

        // Melee and dogtag are fixed setup state by product contract. They never enter the
        // v1.17 candidate pool, so they must also stay outside both sides of the objective
        // comparison. Leaving those constants in snapshot scoring can still distort a real
        // decision when an incoming candidate happens to share the same item id.
        var scoringSnapshot = snapshot with
        {
            Equipment = snapshot.Equipment
                .Where(pair => pair.Key is not FarmingGuideEquipmentSlot.Melee and not FarmingGuideEquipmentSlot.Dogtag)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
        };

        var acquired = FarmingGuideSnapshotInventoryCounter.CountRaidAcquiredAll(scoringSnapshot);
        var satisfiedFir = 0;
        foreach (var pair in acquired)
        {
            satisfiedFir = checked(satisfiedFir + Math.Min(
                Math.Max(0, pair.Value),
                Math.Max(0, remainingFirNeed(pair.Key))));
        }

        long retainedValue = 0;
        foreach (var pair in FarmingGuideSnapshotInventoryCounter.CountAll(scoringSnapshot))
        {
            var unitValue = Math.Max(0, fleaAverageValue(pair.Key) ?? 0);
            retainedValue = checked(retainedValue + (long)unitValue * Math.Max(0, pair.Value));
        }

        return new FarmingGuideOptimizationScore(satisfiedFir, retainedValue);
    }

    public static bool IsBetter(
        FarmingGuideOptimizationScore candidate,
        FarmingGuideOptimizationScore current) => candidate.CompareTo(current) > 0;
}
