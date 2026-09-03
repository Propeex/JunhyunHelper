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
    /// A result is committable only when the candidate domain, decision facts, quantity solve,
    /// weight facts, FIR provenance and packing proof are complete; otherwise Indeterminate wins.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        _ = recommendation;

        if (!HasProvableRaidWeightDomainV1170(current, incoming) ||
            !HasProvableFirDecisionFactsV1170(current))
        {
            return IndeterminateRaidPlanV1170(current);
        }

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

    /// <summary>
    /// Product rule: once Farming Guide raid mode is active, every newly Scanner-identified
    /// incoming item is modeled as Found-in-Raid. Scanner does not inspect or infer Tarkov's
    /// FIR icon; the active-raid scan event itself is the product authority for new loot.
    ///
    /// This validator therefore only guards already-modeled raid-acquired state. New incoming
    /// roots are created explicitly as FoundInRaid in the unified solver. Any unexpected legacy
    /// raid-acquired Unknown state still fails closed when its FIR requirement could affect the
    /// optimum, so corrupted/incomplete state never becomes destructive advice.
    /// </summary>
    private bool HasProvableFirDecisionFactsV1170(FarmingGuideLoadoutSnapshot current)
    {
        foreach (var state in EnumerateFirDecisionStatesV1170(current))
        {
            if (!state.RaidAcquired)
                continue;

            var requirement = _raidBridge?.ResolveSnapshot(state.ItemId);
            if (state.FirStatus == FarmingGuideFirStatus.Unknown)
            {
                if (requirement is null || Math.Max(0, requirement.CurrentNeededFir) > 0)
                    return false;
                continue;
            }

            if (state.FirStatus == FarmingGuideFirStatus.FoundInRaid && requirement is null)
                return false;
        }

        return true;
    }

    private static IEnumerable<FarmingGuideItemState> EnumerateFirDecisionStatesV1170(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        foreach (var state in snapshot.Equipment.Values)
        {
            foreach (var nested in EnumerateFirDecisionStateTreeV1170(state))
                yield return nested;
        }
        foreach (var state in new[] { snapshot.Rig, snapshot.Backpack, snapshot.SecureContainer })
        {
            if (state is null)
                continue;
            foreach (var nested in EnumerateFirDecisionStateTreeV1170(state))
                yield return nested;
        }
        foreach (var stored in snapshot.StoredItems)
        {
            foreach (var nested in EnumerateFirDecisionStateTreeV1170(stored.Item))
                yield return nested;
        }
    }

    private static IEnumerable<FarmingGuideItemState> EnumerateFirDecisionStateTreeV1170(
        FarmingGuideItemState state)
    {
        yield return state;
        foreach (var child in state.Attachments.Values.Concat(state.ArmorPlates.Values))
        {
            if (child is null)
                continue;
            foreach (var nested in EnumerateFirDecisionStateTreeV1170(child))
                yield return nested;
        }
    }

    private bool HasProvableIncomingEconomicValueV1170(ScannerItemSnapshot scanned) =>
        ResolveUnitEconomicValueV1170(scanned.ItemId) is not null;

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
                    ? Math.Max(0, currentScan.CurrentNeededFir)
                    : 0;
            },
            ResolveUnitEconomicValueV1170);
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
    /// Incoming acquisition/FIR provenance is assigned when the unified candidate root is
    /// created and is carried by the exact FarmingGuideItemState through every placement.
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
