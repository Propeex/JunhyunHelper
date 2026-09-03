namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Scanner/content facts projected into the Farming Guide decision layer.
/// CurrentNeeded means remaining Found-in-Raid Quest/Hideout need.
/// Trader price is retained only for source/API compatibility; Farming Guide economic
/// decisions use average Flea Market value.
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

    // These remain useful system facts for placement/search/weight calculations. They are
    // deliberately not Farming Guide priority tiers.
    public double ValuePerSlot => EffectiveValue / (double)EffectiveSlots;
    public decimal? EffectiveWeightKg => UnitWeightKg is { } weight
        ? Math.Max(0m, weight) * NormalizedQuantity
        : null;
}

/// <summary>
/// v1.17 pairwise projection of the confirmed Farming Guide objective.
///
/// Product priority has exactly two farming dimensions:
/// 1. an item with remaining FIR Quest/Hideout need outranks ordinary loot;
/// 2. otherwise higher total average-Flea value wins.
///
/// Complete inventory optimization applies FIR priority quantity-wise and compares complete
/// legal final states. Weight and footprint are system constraints/mechanics, not item
/// priority tie-breakers.
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
