namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Complete-state farming objective introduced in v1.17.
///
/// The score is lexicographic: satisfied Found-in-Raid need first, retained economic value
/// second. Geometry and weight are feasibility constraints outside this score.
/// </summary>
public readonly record struct FarmingGuideOptimizationScore(
    int SatisfiedFirUnits,
    long RetainedFleaValue)
    : IComparable<FarmingGuideOptimizationScore>
{
    public int CompareTo(FarmingGuideOptimizationScore other)
    {
        var fir = SatisfiedFirUnits.CompareTo(other.SatisfiedFirUnits);
        return fir != 0 ? fir : RetainedFleaValue.CompareTo(other.RetainedFleaValue);
    }
}

public static class FarmingGuideOptimizationPolicy
{
    /// <summary>
    /// Scores a complete modeled state. FIR satisfaction is derived from explicit per-item
    /// raid provenance carried by the candidate snapshot. The baseline parameter remains in
    /// this API because callers still use it as the raid-session authority, but it is no
    /// longer used to infer provenance by item-id subtraction.
    /// </summary>
    public static FarmingGuideOptimizationScore Score(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot candidate,
        Func<string, int> remainingFirNeed,
        Func<string, int?> fleaUnitValue)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(remainingFirNeed);
        ArgumentNullException.ThrowIfNull(fleaUnitValue);

        var candidateCounts = FarmingGuideSnapshotInventoryCounter.CountAll(candidate);
        var acquiredCounts = FarmingGuideSnapshotInventoryCounter.CountRaidAcquiredAll(candidate);

        var satisfiedFir = 0;
        long retainedValue = 0;
        foreach (var pair in candidateCounts)
        {
            var quantity = Math.Max(0, pair.Value);
            if (quantity == 0)
                continue;

            var acquired = Math.Max(0, acquiredCounts.GetValueOrDefault(pair.Key));
            var need = Math.Max(0, remainingFirNeed(pair.Key));
            satisfiedFir = checked(satisfiedFir + Math.Min(acquired, need));

            var unitValue = Math.Max(0, fleaUnitValue(pair.Key) ?? 0);
            retainedValue = checked(retainedValue + (long)unitValue * quantity);
        }

        return new FarmingGuideOptimizationScore(satisfiedFir, retainedValue);
    }

    public static bool IsBetter(
        FarmingGuideOptimizationScore candidate,
        FarmingGuideOptimizationScore incumbent) =>
        candidate.CompareTo(incumbent) > 0;
}
