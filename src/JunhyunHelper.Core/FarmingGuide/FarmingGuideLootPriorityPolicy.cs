namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Scanner/content facts projected into the Farming Guide decision layer.
/// CurrentNeeded intentionally means remaining Found-in-Raid need in v1.16.0.
/// Trader price is retained only for source/API compatibility; Farming Guide economic
/// decisions use average Flea Market value exclusively.
/// </summary>
public sealed record FarmingGuideLootMetrics(
    int CurrentNeeded,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int Slots)
{
    public int Quantity { get; init; } = 1;
    public decimal? UnitWeightKg { get; init; }

    public int NormalizedQuantity => Math.Max(1, Quantity);
    public int UnitFleaValue => Math.Max(0, FleaAveragePrice ?? 0);
    public int EffectiveValue => checked(UnitFleaValue * NormalizedQuantity);
    public int EffectiveSlots => Math.Max(1, Slots);
    public double ValuePerSlot => EffectiveValue / (double)EffectiveSlots;
    public decimal? EffectiveWeightKg => UnitWeightKg is { } weight
        ? Math.Max(0m, weight) * NormalizedQuantity
        : null;
}

/// <summary>
/// Deterministic Farming Guide priority manual. This is deliberately lexicographic rather
/// than a weighted score: classify the item, compare one rule, and only on a tie proceed
/// to the next rule.
///
/// 1. Remaining Found-in-Raid need outranks ordinary economic loot.
/// 2. Average Flea Market total value decides ordinary economic priority.
/// 3. Equal-value items prefer the lighter known total weight.
/// 4. A final tie prefers the smaller ordinary footprint.
///
/// Geometry-specific destructive decisions compare the complete victim set in
/// FarmingGuideLootRetentionPolicy instead of treating ₽/slot as an absolute item score.
/// </summary>
public static class FarmingGuideLootPriorityPolicy
{
    public static int Compare(FarmingGuideLootMetrics left, FarmingGuideLootMetrics right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var firNeeded = (left.CurrentNeeded > 0).CompareTo(right.CurrentNeeded > 0);
        if (firNeeded != 0)
            return firNeeded;

        var totalValue = left.EffectiveValue.CompareTo(right.EffectiveValue);
        if (totalValue != 0)
            return totalValue;

        // Unknown weight must never be guessed. Weight breaks a tie only when both facts
        // are present, otherwise the decision falls through to footprint/stability.
        if (left.EffectiveWeightKg is { } leftWeight && right.EffectiveWeightKg is { } rightWeight)
        {
            var lighter = rightWeight.CompareTo(leftWeight);
            if (lighter != 0)
                return lighter;
        }

        return right.EffectiveSlots.CompareTo(left.EffectiveSlots);
    }

    public static bool ShouldReplace(
        FarmingGuideLootMetrics incoming,
        FarmingGuideLootMetrics existing) => Compare(incoming, existing) > 0;
}
