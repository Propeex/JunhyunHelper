using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.17 no longer asks a historical local/tactical planner to choose the Farming Guide
    /// result. The actual decision is produced by ApplyRaidStateTransitionsV1170 from the
    /// unified complete-state candidate pool. This placeholder deliberately carries no
    /// committable advice so an obsolete planner can never leak a hidden priority back in.
    /// </summary>
    private static RaidRecommendation PlanScannedItemRulebookV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        _ = scanned;
        _ = incoming;
        return IndeterminateRaidPlanV1170(current);
    }

    /// <summary>
    /// Authoritative v1.17 raid decision. Stored items, nested containers, top-level
    /// equipment/carriers and the incoming item are evaluated through one complete candidate
    /// pool. Stack quantities are optimized exactly inside each selected physical root set.
    /// A result is committable only when the candidate domain, decision facts, quantity solve
    /// and packing proof are complete; otherwise the non-committing Indeterminate state wins.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        _ = recommendation;

        var currentScore = ScoreRaidStateV1170(current, scanned);
        var found = TryFindBestUnifiedRaidStateWithQuantitiesV1170(
            current,
            scanned,
            incoming,
            currentScore,
            out var candidate,
            out var candidateScore,
            out var proofComplete);

        if (found && FarmingGuideOptimizationPolicy.IsBetter(candidateScore, currentScore))
            return candidate;

        if (!proofComplete || !HasProvableIncomingEconomicValueV1170(scanned))
            return IndeterminateRaidPlanV1170(current);

        return ProvenDiscardV1170(current);
    }

    private bool HasProvableIncomingEconomicValueV1170(ScannerItemSnapshot scanned)
    {
        if (scanned.FleaAveragePrice is not null)
            return true;
        if (_raidFleaAveragePrices.ContainsKey(scanned.ItemId))
            return true;
        return _raidBridge?.ResolveSnapshot(scanned.ItemId)?.FleaAveragePrice is not null;
    }

    private FarmingGuideOptimizationScore ScoreRaidStateV1170(
        FarmingGuideLoadoutSnapshot candidate,
        ScannerItemSnapshot currentScan)
    {
        var baseline = _raidSession?.BaselineSnapshot ?? BuildSnapshot();
        return FarmingGuideOptimizationPolicy.Score(
            baseline,
            candidate,
            itemId =>
            {
                var snapshot = _raidBridge?.ResolveSnapshot(itemId);
                if (snapshot is not null)
                    return Math.Max(0, snapshot.CurrentNeededFir);
                return string.Equals(itemId, currentScan.ItemId, StringComparison.Ordinal)
                    ? Math.Max(0, currentScan.CurrentNeeded)
                    : 0;
            },
            itemId =>
            {
                if (_raidFleaAveragePrices.TryGetValue(itemId, out var remembered))
                    return remembered;
                return _raidBridge?.ResolveSnapshot(itemId)?.FleaAveragePrice;
            });
    }

    private bool IsWeightAdmissibleV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        EnsureWeightSettingsLoadedV1160();
        var currentWeight = CalculateSnapshotWeightKgV1160(current);
        var proposedWeight = CalculateSnapshotWeightKgV1160(proposed);
        var limit = FarmingGuideWeightPolicy.MaximumCarryWeightKg(_weightSettingsV1160);
        if (proposedWeight <= limit)
            return true;
        return currentWeight > limit && proposedWeight <= currentWeight;
    }

    /// <summary>
    /// Incoming provenance is assigned when the unified candidate root is created and is
    /// carried by the exact FarmingGuideItemState through every placement. Do not infer
    /// provenance from a changed slot/location: an existing same-id item can move to a new
    /// slot and must never become falsely FIR-acquired.
    /// </summary>
    private static RaidRecommendation MarkIncomingRaidProvenanceV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        string incomingItemId)
    {
        _ = current;
        _ = incomingItemId;
        return recommendation;
    }
}
