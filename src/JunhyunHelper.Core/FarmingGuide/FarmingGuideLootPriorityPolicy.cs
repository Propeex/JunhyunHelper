namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Scanner-owned facts projected into the Farming Guide decision layer. Keeping this
/// value object in Core prevents the placement algorithm from depending on Scanner UI.
/// </summary>
public sealed record FarmingGuideLootMetrics(
    int CurrentNeeded,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int Slots)
{
    public int EffectiveValue => Math.Max(TraderSellPrice ?? 0, FleaAveragePrice ?? 0);
    public int EffectiveSlots => Math.Max(1, Slots);
    public double ValuePerSlot => EffectiveValue / (double)EffectiveSlots;
}

/// <summary>
/// One deliberately small policy boundary for loot priority. Placement mechanics do not
/// know why one item outranks another, so this policy can be replaced without rewriting
/// raid/session/grid code.
/// </summary>
public static class FarmingGuideLootPriorityPolicy
{
    public static int Compare(FarmingGuideLootMetrics left, FarmingGuideLootMetrics right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var needed = (left.CurrentNeeded > 0).CompareTo(right.CurrentNeeded > 0);
        if (needed != 0)
            return needed;

        var perSlot = left.ValuePerSlot.CompareTo(right.ValuePerSlot);
        if (perSlot != 0)
            return perSlot;

        var total = left.EffectiveValue.CompareTo(right.EffectiveValue);
        if (total != 0)
            return total;

        return left.EffectiveSlots.CompareTo(right.EffectiveSlots) * -1;
    }

    public static bool ShouldReplace(
        FarmingGuideLootMetrics incoming,
        FarmingGuideLootMetrics existing) => Compare(incoming, existing) > 0;
}
