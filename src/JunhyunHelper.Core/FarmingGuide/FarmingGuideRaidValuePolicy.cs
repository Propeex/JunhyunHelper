namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Calculates the economic value of loot that is still retained relative to the raid-start
/// baseline. Farming Guide economic comparisons use average Flea Market value, so the summary
/// follows the same contract instead of mixing trader/base prices into the raid total.
/// </summary>
public static class FarmingGuideRaidValuePolicy
{
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
}
