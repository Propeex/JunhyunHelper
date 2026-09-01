namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Decides whether a bounded destructive plan is economically/semantically preferable.
/// Geometry deliberately stays in the repacking planner; this policy only answers whether
/// the already-identified victim set may be sacrificed for the item being preserved.
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

        var victimNeeds = victims.Any(static value => value.CurrentNeeded > 0);
        if (victimNeeds)
        {
            // Do not automatically trade one required item for another required item.
            // The current facts do not prove which quest/hideout obligation is safer to
            // sacrifice, so destructive planning fails conservatively.
            return false;
        }

        // Existing loot policy makes a still-needed item categorically more important
        // than ordinary market loot. Preserve that contract for victim sets as well.
        if (preserved.CurrentNeeded > 0)
            return true;

        long victimValue = 0;
        foreach (var victim in victims)
            victimValue += Math.Max(0, victim.EffectiveValue);

        // For non-needed loot, never destroy equal-or-greater total known value merely to
        // improve geometry. Strict inequality also keeps ties deterministic and stable.
        return preserved.EffectiveValue > victimValue;
    }

    /// <summary>
    /// Chooses between two already-legal destructive plans for the same preserved item.
    /// Lower sacrifice is better: required victims first, then aggregate known value,
    /// victim count and finally occupied slots. Placement/search details stay out of this
    /// policy so the ranking can evolve independently.
    /// </summary>
    public static bool IsPreferredVictimSet(
        IReadOnlyList<FarmingGuideLootMetrics> candidate,
        IReadOnlyList<FarmingGuideLootMetrics> incumbent)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(incumbent);

        static (int Needed, long Value, int Count, int Slots) Score(
            IReadOnlyList<FarmingGuideLootMetrics> values)
        {
            var needed = 0;
            long value = 0;
            var slots = 0;
            foreach (var item in values)
            {
                if (item.CurrentNeeded > 0)
                    needed++;
                value += Math.Max(0, item.EffectiveValue);
                slots += Math.Max(1, item.EffectiveSlots);
            }
            return (needed, value, values.Count, slots);
        }

        var left = Score(candidate);
        var right = Score(incumbent);
        if (left.Needed != right.Needed)
            return left.Needed < right.Needed;
        if (left.Value != right.Value)
            return left.Value < right.Value;
        if (left.Count != right.Count)
            return left.Count < right.Count;
        return left.Slots < right.Slots;
    }
}
