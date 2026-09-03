namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Calculates retained raid-loot economic value using average Flea Market value.
/// </summary>
public static class FarmingGuideRaidValuePolicy
{
    /// <summary>
    /// Legacy baseline-delta calculation retained for compatibility with pre-v1.17 tests
    /// and maintenance code. New v1.17 live raid UI uses explicit Scanner-acquired provenance.
    /// </summary>
    public static long CalculateAcquiredFleaValue(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot current,
        Func<string, int?> fleaAveragePriceResolver)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(fleaAveragePriceResolver);

        long total = 0;
        foreach (var pair in FarmingGuideSnapshotInventoryCounter.AcquiredSinceAll(baseline, current))
        {
            var price = fleaAveragePriceResolver(pair.Key);
            if (price is null or <= 0)
                continue;

            total = checked(total + ((long)price.Value * pair.Value));
        }

        return total;
    }

    /// <summary>
    /// v1.17 authoritative active-raid value. Counts exactly the retained roots that entered
    /// the modeled raid through Scanner, so replacing an identical raid-start item does not
    /// erase the newly acquired loot from the value summary.
    /// </summary>
    public static long CalculateRaidAcquiredFleaValue(
        FarmingGuideLoadoutSnapshot current,
        Func<string, int?> fleaAveragePriceResolver)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(fleaAveragePriceResolver);

        long total = 0;
        foreach (var pair in FarmingGuideSnapshotInventoryCounter.CountRaidAcquiredAll(current))
        {
            var price = fleaAveragePriceResolver(pair.Key);
            if (price is null or <= 0)
                continue;

            total = checked(total + ((long)price.Value * pair.Value));
        }

        return total;
    }
}
