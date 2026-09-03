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
    /// Scores a complete modeled state. FIR satisfaction is derived only from explicit
    /// FoundInRaid provenance carried by the candidate snapshot. RaidAcquired is a separate
    /// acquisition-history fact and must never be promoted to FIR implicitly.
    ///
    /// The baseline parameter remains in this API because callers still use it as raid-session
    /// authority, but it is not used to infer FIR provenance by item-id subtraction.
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
        var firCounts = FarmingGuideSnapshotInventoryCounter.CountFoundInRaidAll(candidate);

        var satisfiedFir = 0;
        long retainedValue = 0;
        foreach (var pair in candidateCounts)
        {
            var quantity = Math.Max(0, pair.Value);
            if (quantity == 0)
                continue;

            var firQuantity = Math.Max(0, firCounts.GetValueOrDefault(pair.Key));
            var need = Math.Max(0, remainingFirNeed(pair.Key));
            satisfiedFir = checked(satisfiedFir + Math.Min(firQuantity, need));

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
