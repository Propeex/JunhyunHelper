using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Compares the historical one-victim destructive result with the bounded v1.15.5
    /// victim-set search. This lets two cheap blockers beat one more valuable blocker when
    /// both plans can legally retain the same scanned item.
    /// </summary>
    private RaidRecommendation OptimizeDestructiveRaidPlanV1155(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (recommendation.Action != FarmingGuideInstructionAction.Replace)
            return recommendation;

        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        if (!TryRepackIncomingWithBoundedEvictionsV1155(
                current,
                FarmingGuideItemState.Create(incoming.Id),
                incoming,
                incomingMetrics,
                NewDisplacedInstanceIdV1155(),
                out var alternative,
                out _))
        {
            return recommendation;
        }

        var incumbentVictims = RemovedStoredMetricsV1155(current, recommendation.ProposedSnapshot);
        var alternativeVictims = RemovedStoredMetricsV1155(current, alternative);
        if (!FarmingGuideLootRetentionPolicy.IsPreferredVictimSet(
                alternativeVictims,
                incumbentVictims))
        {
            return recommendation;
        }

        return recommendation with { ProposedSnapshot = alternative };
    }

    private IReadOnlyList<FarmingGuideLootMetrics> RemovedStoredMetricsV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        var retained = proposed.StoredItems
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var result = new List<FarmingGuideLootMetrics>();
        foreach (var stored in current.StoredItems)
        {
            if (retained.Contains(stored.InstanceId))
                continue;
            var item = ResolveItem(stored.Item);
            if (item is not null)
                result.Add(MetricsForExisting(item));
        }
        return result;
    }
}
