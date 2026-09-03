namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Scanner/content facts projected into the Farming Guide decision layer.
/// CurrentNeeded means remaining Found-in-Raid quest/hideout need. Trader price is retained
/// only for source/API compatibility; Farming Guide economic decisions use average Flea
/// Market value exclusively.
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

    // These derived facts remain useful to placement/search and weight validation. They are
    // deliberately not Farming Guide priority tiers: geometry is a system feasibility fact
    // and weight is a final-state constraint.
    public double ValuePerSlot => EffectiveValue / (double)EffectiveSlots;
    public decimal? EffectiveWeightKg => UnitWeightKg is { } weight
        ? Math.Max(0m, weight) * NormalizedQuantity
        : null;
}

/// <summary>
/// v1.17 item-level projection of the Farming Guide rulebook.
///
/// Product priority has exactly two farming dimensions:
/// 1. an item with remaining FIR quest/hideout need outranks ordinary loot;
/// 2. otherwise higher average Flea total value wins.
///
/// Exact inventory optimization must apply FIR priority quantity-wise and compare complete
/// legal final states; this comparator is only for places that still need a deterministic
/// pairwise ordering during migration/search. Weight and footprint are not priority tiers.
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

        return left.EffectiveValue.CompareTo(right.EffectiveValue);
    }

    public static bool ShouldReplace(
        FarmingGuideLootMetrics incoming,
        FarmingGuideLootMetrics existing) => Compare(incoming, existing) > 0;
}
