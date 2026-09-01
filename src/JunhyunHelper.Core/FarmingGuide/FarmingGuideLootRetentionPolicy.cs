namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Decides whether an already-legal destructive plan may sacrifice its victim set.
/// Geometry stays in the repacking planner; this policy is the deterministic economic
/// manual for the resulting state.
/// </summary>
public static class FarmingGuideLootRetentionPolicy
{
    public static bool CanSacrificeFor(
        FarmingGuideLootMetrics preserved,
        IReadOnlyList<FarmingGuideLootMetrics> victims)
    {
        ArgumentNullException.ThrowIfNull(preserved);
        ArgumentNullException.ThrowIfNull(victims);
        if (victims.Count == 0)
            return true;

        // CurrentNeeded is remaining FIR need in v1.16.0. A destructive automatic action
        // never throws away an FIR-needed item. FIR-needed incoming loot may displace only
        // ordinary, unlocked loot.
        if (victims.Any(static value => value.CurrentNeeded > 0))
            return false;
        if (preserved.CurrentNeeded > 0)
            return true;

        long victimValue = 0;
        foreach (var victim in victims)
            victimValue += Math.Max(0, victim.EffectiveValue);

        // Compare the actual total sacrifice with the actual incoming stack value. This
        // fixes the old ₽/slot error where a large valuable item could be rejected merely
        // because one blocker happened to have a higher per-cell value.
        return preserved.EffectiveValue > victimValue;
    }

    /// <summary>
    /// Chooses between two legal victim sets. Less important loss is better: FIR-needed
    /// victims, aggregate Flea value, victim count, then occupied footprint.
    /// </summary>
    public static bool IsPreferredVictimSet(
        IReadOnlyList<FarmingGuideLootMetrics> candidate,
        IReadOnlyList<FarmingGuideLootMetrics> incumbent)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(incumbent);

        static (int FirNeeded, long Value, int Count, int Slots) Score(
            IReadOnlyList<FarmingGuideLootMetrics> values)
        {
            var firNeeded = 0;
            long value = 0;
            var slots = 0;
            foreach (var item in values)
            {
                if (item.CurrentNeeded > 0)
                    firNeeded++;
                value += Math.Max(0, item.EffectiveValue);
                slots += Math.Max(1, item.EffectiveSlots);
            }
            return (firNeeded, value, values.Count, slots);
        }

        var left = Score(candidate);
        var right = Score(incumbent);
        if (left.FirNeeded != right.FirNeeded)
            return left.FirNeeded < right.FirNeeded;
        if (left.Value != right.Value)
            return left.Value < right.Value;
        if (left.Count != right.Count)
            return left.Count < right.Count;
        return left.Slots < right.Slots;
    }
}
