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
}
