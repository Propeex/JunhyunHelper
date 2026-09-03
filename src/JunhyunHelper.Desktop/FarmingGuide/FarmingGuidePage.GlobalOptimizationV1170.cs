using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1170MaxGlobalSubsetAttempts = 2048;

    private RaidRecommendation PlanScannedItemRulebookV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        var planned = PlanScannedItemHardened(scanned, incoming);
        return MarkIncomingRaidProvenanceV1170(current, planned, incoming.Id);
    }

    /// <summary>
    /// v1.17 complete-state decision layer. Stored inventory is packed from scratch through
    /// FarmingGuideGlobalPackingPlanner; current layout is not a farming preference. The
    /// remaining intentionally incomplete domain is top-level equipment/carrier pooling,
    /// which stays fail-closed until unified below the same objective.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (!PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
            recommendation = IndeterminateRaidPlanV1170(current);

        // Keeping every current item plus the incoming item is globally optimal regardless
        // of exact arrangement because both farming objectives are monotone in retained units.
        if (recommendation.Action is FarmingGuideInstructionAction.Store or FarmingGuideInstructionAction.Equip)
        {
            var quantified = ApplyIncomingQuantityV1160(
                current,
                recommendation,
                incoming.Id,
                Math.Max(1, scanned.Quantity));
            quantified = MarkIncomingRaidProvenanceV1170(current, quantified, incoming.Id);
            if (IsWeightAdmissibleV1170(current, quantified.ProposedSnapshot))
                return quantified;
        }

        var incompleteDomain = HasUnmodeledGlobalChoicesV1170(current);
        var currentScore = ScoreRaidStateV1170(current, scanned);
        RaidRecommendation? best = null;
        FarmingGuideOptimizationScore bestScore = currentScore;

        // Historical equipment target discovery is retained temporarily, but a destructive
        // equipment candidate cannot become authoritative while other unlocked top-level
        // equipment/carrier choices remain outside the unified pool.
        if (recommendation.Action == FarmingGuideInstructionAction.ReplaceEquip &&
            PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
        {
            var quantified = ApplyIncomingQuantityV1160(
                current,
                recommendation,
                incoming.Id,
                Math.Max(1, scanned.Quantity));
            quantified = MarkIncomingRaidProvenanceV1170(current, quantified, incoming.Id);
            if (IsWeightAdmissibleV1170(current, quantified.ProposedSnapshot))
            {
                var candidateScore = ScoreRaidStateV1170(quantified.ProposedSnapshot, scanned);
                if (FarmingGuideOptimizationPolicy.IsBetter(candidateScore, bestScore))
                {
                    best = quantified;
                    bestScore = candidateScore;
                }
            }
        }

        var storedFound = TryFindBestStoredStateV1170(
            current,
            scanned,
            incoming,
            currentScore,
            out var stored,
            out var storedScore,
            out var storedProofComplete);
        if (storedFound && FarmingGuideOptimizationPolicy.IsBetter(storedScore, bestScore))
        {
            best = stored;
            bestScore = storedScore;
        }

        if (best is not null)
            return !incompleteDomain && storedProofComplete ? best : IndeterminateRaidPlanV1170(current);

        return !incompleteDomain && storedProofComplete
            ? ProvenDiscardV1170(current)
            : IndeterminateRaidPlanV1170(current);
    }

    private bool TryFindBestStoredStateV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        FarmingGuideOptimizationScore currentScore,
        out RaidRecommendation recommendation,
        out FarmingGuideOptimizationScore score,
        out bool proofComplete)
    {
        var incomingState = FarmingGuideItemState.Create(incoming.Id, raidAcquired: true);
        var incomingInstanceId = $"__incoming_v1170_{Guid.NewGuid():N}";

        if (TryBuildStoredCandidateV1170(
                current,
                scanned,
                incoming,
                incomingState,
                incomingInstanceId,
                [],
                out recommendation,
                out score,
                out var zeroEvictionProof))
        {
            proofComplete = zeroEvictionProof;
            return FarmingGuideOptimizationPolicy.IsBetter(score, currentScore);
        }
        if (!zeroEvictionProof)
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        var victims = EnumerateGlobalVictimsV1170(current).ToArray();
        if (victims.Length == 0)
        {
            recommendation = ProvenDiscardV1170(current);
            score = currentScore;
            proofComplete = true;
            return false;
        }

        var queue = new PriorityQueue<GlobalSubsetV1170, GlobalPriorityV1170>();
        for (var i = 0; i < victims.Length; i++)
            EnqueueGlobalSubsetV1170(queue, current, scanned, incoming, victims, [i]);

        var attempts = 0;
        while (queue.Count > 0 && attempts < V1170MaxGlobalSubsetAttempts)
        {
            var subset = queue.Dequeue();
            attempts++;

            // The queue is ordered by the conceptual complete-state objective. Once the
            // best remaining retained set no longer beats current, all descendants are worse.
            if (!FarmingGuideOptimizationPolicy.IsBetter(subset.Score, currentScore))
            {
                recommendation = ProvenDiscardV1170(current);
                score = currentScore;
                proofComplete = true;
                return false;
            }

            if (TryBuildStoredCandidateV1170(
                    current,
                    scanned,
                    incoming,
                    incomingState,
                    incomingInstanceId,
                    subset.Indices.Select(index => victims[index].InstanceId).ToArray(),
                    out recommendation,
                    out score,
                    out var packingProof))
            {
                proofComplete = packingProof;
                return true;
            }
            if (!packingProof)
            {
                recommendation = IndeterminateRaidPlanV1170(current);
                score = currentScore;
                proofComplete = false;
                return false;
            }

            var last = subset.Indices[^1];
            for (var next = last + 1; next < victims.Length; next++)
            {
                var expanded = new int[subset.Indices.Length + 1];
                Array.Copy(subset.Indices, expanded, subset.Indices.Length);
                expanded[^1] = next;
                EnqueueGlobalSubsetV1170(queue, current, scanned, incoming, victims, expanded);
            }
        }

        recommendation = IndeterminateRaidPlanV1170(current);
        score = currentScore;
        proofComplete = queue.Count == 0;
        return false;
    }

    private bool TryBuildStoredCandidateV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        FarmingGuideItemState incomingState,
        string incomingInstanceId,
        IReadOnlyCollection<string> removedInstanceIds,
        out RaidRecommendation recommendation,
        out FarmingGuideOptimizationScore score,
        out bool proofComplete)
    {
        var packing = TryPackSelectedStoredPoolV1170(
            current,
            incomingState,
            incoming,
            incomingInstanceId,
            removedInstanceIds,
            out var proposed);
        proofComplete = packing != StoredPackingOutcomeV1170.Indeterminate;
        if (packing != StoredPackingOutcomeV1170.Found)
        {
            recommendation = packing == StoredPackingOutcomeV1170.Indeterminate
                ? IndeterminateRaidPlanV1170(current)
                : ProvenDiscardV1170(current);
            score = ScoreRaidStateV1170(current, scanned);
            return false;
        }

        recommendation = new RaidRecommendation(
            removedInstanceIds.Count == 0 ? "보관" : "교체",
            removedInstanceIds.Count == 0
                ? FarmingGuideInstructionAction.Store
                : FarmingGuideInstructionAction.Replace,
            proposed);
        recommendation = ApplyIncomingQuantityV1160(
            current,
            recommendation,
            incoming.Id,
            Math.Max(1, scanned.Quantity));
        recommendation = MarkIncomingRaidProvenanceV1170(current, recommendation, incoming.Id);

        if (!IsWeightAdmissibleV1170(current, recommendation.ProposedSnapshot))
        {
            score = ScoreRaidStateV1170(current, scanned);
            return false;
        }

        score = ScoreRaidStateV1170(recommendation.ProposedSnapshot, scanned);
        return true;
    }

    private IEnumerable<FarmingGuideStoredItemState> EnumerateGlobalVictimsV1170(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        return snapshot.StoredItems
            // Populated containers are valid victims now: the global packing proof retains
            // their children as independent selected items and repacks those children onto
            // the remaining legal surfaces.
            .Where(stored => !_lockedItemInstanceIds.Contains(stored.InstanceId))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            // A reserved cell owned by this exact container is an explicit user constraint;
            // removing the container would remove the reserved surface itself.
            .Where(stored => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(HasProvableVictimFactsV1170)
            .OrderBy(stored => stored.InstanceId, StringComparer.Ordinal);
    }

    private bool HasProvableVictimFactsV1170(FarmingGuideStoredItemState stored)
    {
        var item = ResolveItem(stored.Item);
        if (item is null)
            return false;

        var snapshot = _raidBridge?.ResolveSnapshot(item.Id);
        if (snapshot is null)
            return false;

        return _raidFleaAveragePrices.ContainsKey(item.Id) || snapshot.FleaAveragePrice is not null;
    }

    /// <summary>
    /// Stored/nested/incoming-container choices are now in the complete packing pool. The
    /// remaining missing domain is top-level equipment/carrier pooling.
    /// </summary>
    private bool HasUnmodeledGlobalChoicesV1170(FarmingGuideLoadoutSnapshot current)
    {
        if (current.Equipment.Keys.Any(slot => !_lockedEquipmentSlots.Contains(slot)))
            return true;
        if (current.Rig is not null && !_lockedCarriers.Contains(FarmingGuideStorageKind.Rig))
            return true;
        if (current.Backpack is not null && !_lockedCarriers.Contains(FarmingGuideStorageKind.Backpack))
            return true;
        if (current.SecureContainer is not null && !_lockedCarriers.Contains(FarmingGuideStorageKind.SecureContainer))
            return true;
        return false;
    }

    private void EnqueueGlobalSubsetV1170(
        PriorityQueue<GlobalSubsetV1170, GlobalPriorityV1170> queue,
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        IReadOnlyList<FarmingGuideStoredItemState> victims,
        int[] indices)
    {
        var remove = indices
            .Select(index => victims[index].InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var reduced = current with
        {
            StoredItems = current.StoredItems
                .Where(value => !remove.Contains(value.InstanceId))
                .ToArray(),
        };
        var conceptual = AddIncomingForScoreV1170(reduced, scanned, incoming);
        var objective = ScoreRaidStateV1170(conceptual, scanned);
        var key = string.Join("|", indices.Select(index => victims[index].InstanceId));
        queue.Enqueue(
            new GlobalSubsetV1170(indices, objective),
            new GlobalPriorityV1170(
                -objective.SatisfiedFirUnits,
                -objective.RetainedFleaValue,
                indices.Length,
                key));
    }

    private FarmingGuideLoadoutSnapshot AddIncomingForScoreV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        var placeholder = new FarmingGuideStoredItemState(
            "__score_incoming_v1170__",
            FarmingGuideItemState.Create(incoming.Id, raidAcquired: true),
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false,
            Quantity: Math.Max(1, scanned.Quantity));
        return snapshot with { StoredItems = snapshot.StoredItems.Append(placeholder).ToArray() };
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

    private RaidRecommendation MarkIncomingRaidProvenanceV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        string incomingItemId)
    {
        var proposed = recommendation.ProposedSnapshot;
        var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(proposed.Equipment);
        foreach (var slot in proposed.Equipment.Keys.ToArray())
        {
            current.Equipment.TryGetValue(slot, out var before);
            equipment[slot] = MarkNewRaidStateV1170(before, proposed.Equipment[slot], incomingItemId);
        }

        var currentStored = current.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var stored = proposed.StoredItems
            .Select(value =>
            {
                currentStored.TryGetValue(value.InstanceId, out var before);
                return value with
                {
                    Item = MarkNewRaidStateV1170(before?.Item, value.Item, incomingItemId),
                };
            })
            .ToArray();

        var marked = proposed with
        {
            Equipment = equipment,
            Rig = proposed.Rig is null ? null : MarkNewRaidStateV1170(current.Rig, proposed.Rig, incomingItemId),
            Backpack = proposed.Backpack is null ? null : MarkNewRaidStateV1170(current.Backpack, proposed.Backpack, incomingItemId),
            SecureContainer = proposed.SecureContainer is null
                ? null
                : MarkNewRaidStateV1170(current.SecureContainer, proposed.SecureContainer, incomingItemId),
            StoredItems = stored,
        };
        return recommendation with { ProposedSnapshot = marked };
    }

    private static FarmingGuideItemState MarkNewRaidStateV1170(
        FarmingGuideItemState? before,
        FarmingGuideItemState after,
        string incomingItemId)
    {
        var attachments = new Dictionary<string, FarmingGuideItemState?>(after.Attachments, StringComparer.Ordinal);
        foreach (var pair in after.Attachments)
        {
            if (pair.Value is null)
                continue;
            FarmingGuideItemState? beforeChild = null;
            if (before is not null)
                before.Attachments.TryGetValue(pair.Key, out beforeChild);
            attachments[pair.Key] = MarkNewRaidStateV1170(beforeChild, pair.Value, incomingItemId);
        }

        var plates = new Dictionary<string, FarmingGuideItemState?>(after.ArmorPlates, StringComparer.Ordinal);
        foreach (var pair in after.ArmorPlates)
        {
            if (pair.Value is null)
                continue;
            FarmingGuideItemState? beforeChild = null;
            if (before is not null)
                before.ArmorPlates.TryGetValue(pair.Key, out beforeChild);
            plates[pair.Key] = MarkNewRaidStateV1170(beforeChild, pair.Value, incomingItemId);
        }

        var newlyIntroduced = string.Equals(after.ItemId, incomingItemId, StringComparison.Ordinal) &&
                              (before is null || !string.Equals(before.ItemId, after.ItemId, StringComparison.Ordinal));
        return after with
        {
            RaidAcquired = after.RaidAcquired || newlyIntroduced,
            Attachments = attachments,
            ArmorPlates = plates,
        };
    }

    private sealed record GlobalSubsetV1170(
        int[] Indices,
        FarmingGuideOptimizationScore Score);

    private readonly record struct GlobalPriorityV1170(
        int NegativeFir,
        long NegativeValue,
        int VictimCount,
        string Key) : IComparable<GlobalPriorityV1170>
    {
        public int CompareTo(GlobalPriorityV1170 other)
        {
            var fir = NegativeFir.CompareTo(other.NegativeFir);
            if (fir != 0)
                return fir;
            var value = NegativeValue.CompareTo(other.NegativeValue);
            if (value != 0)
                return value;
            var count = VictimCount.CompareTo(other.VictimCount);
            if (count != 0)
                return count;
            return StringComparer.Ordinal.Compare(Key, other.Key);
        }
    }
}
