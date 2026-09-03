using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1170MaxGlobalSubsetAttempts = 2048;

    /// <summary>
    /// v1.17 farming rulebook entry point. Combat-performance upgrade heuristics are no
    /// longer a farming priority. The hardened planner remains useful for a non-destructive
    /// all-items-retained fast path and legal equipment/storage target discovery; destructive
    /// storage decisions are replaced by the complete-state optimizer below.
    /// </summary>
    private RaidRecommendation PlanScannedItemRulebookV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        _ = current;
        return PlanScannedItemHardened(scanned, incoming);
    }

    /// <summary>
    /// Replaces the historical tactical/victim-first transition with a best-first complete
    /// state search. All unlocked movable stored leaf items are potential economic victims;
    /// food/drink/ammunition/medicine receive no category privilege. Each feasible packed
    /// state is scored by (FIR units satisfied, total retained Flea value), with weight as a
    /// hard admissibility constraint.
    ///
    /// The underlying repacking planner is deliberately reused as the system-mechanics
    /// proof for legal geometry, filters, rotations, nesting, reservations and position
    /// locks. Search ordering does not alter the product objective.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (!PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
            recommendation = RejectUnsafeRaidPlanV1163(current);

        // If every currently retained item plus the incoming item already fits, the primary
        // and secondary objectives cannot be improved by discarding something. Weight still
        // has to be checked with the incoming stack quantity applied.
        if (recommendation.Action is FarmingGuideInstructionAction.Store or FarmingGuideInstructionAction.Equip)
        {
            var quantified = ApplyIncomingQuantityV1160(
                current,
                recommendation,
                incoming.Id,
                Math.Max(1, scanned.Quantity));
            if (IsWeightAdmissibleV1170(current, quantified.ProposedSnapshot))
                return quantified;
        }

        var currentScore = ScoreRaidStateV1170(current, scanned);
        RaidRecommendation? best = null;
        FarmingGuideOptimizationScore bestScore = currentScore;

        // A legal equipment replacement discovered by the non-tactical rulebook is another
        // candidate final state. It is accepted only if its complete-state score actually
        // beats keeping the current state, and only if locks/weight remain valid.
        if (recommendation.Action == FarmingGuideInstructionAction.ReplaceEquip &&
            PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
        {
            var quantified = ApplyIncomingQuantityV1160(
                current,
                recommendation,
                incoming.Id,
                Math.Max(1, scanned.Quantity));
            if (IsWeightAdmissibleV1170(current, quantified.ProposedSnapshot))
            {
                var score = ScoreRaidStateV1170(quantified.ProposedSnapshot, scanned);
                if (FarmingGuideOptimizationPolicy.IsBetter(score, bestScore))
                {
                    best = quantified;
                    bestScore = score;
                }
            }
        }

        if (TryFindBestStoredStateV1170(current, scanned, incoming, currentScore, out var stored, out var storedScore) &&
            FarmingGuideOptimizationPolicy.IsBetter(storedScore, bestScore))
        {
            best = stored;
            bestScore = storedScore;
        }

        // Equal objective => keep the current arrangement and discard the incoming item.
        // Fewer moves is only a deterministic stability tie-break after the farming objective.
        return best ?? RejectUnsafeRaidPlanV1163(current);
    }

    private bool TryFindBestStoredStateV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        FarmingGuideOptimizationScore currentScore,
        out RaidRecommendation recommendation,
        out FarmingGuideOptimizationScore score)
    {
        var incomingState = FarmingGuideItemState.Create(incoming.Id);
        var incomingInstanceId = $"__incoming_v1170_{Guid.NewGuid():N}";

        // First prove the zero-eviction global repack. This keeps every current item and is
        // therefore automatically optimal whenever it is legal and within the weight limit.
        if (TryBuildStoredCandidateV1170(
                current,
                scanned,
                incoming,
                incomingState,
                incomingInstanceId,
                [],
                out recommendation,
                out score))
        {
            return FarmingGuideOptimizationPolicy.IsBetter(score, currentScore);
        }

        var victims = EnumerateGlobalVictimsV1170(current).ToArray();
        if (victims.Length == 0)
        {
            recommendation = RejectUnsafeRaidPlanV1163(current);
            score = currentScore;
            return false;
        }

        // Each node removes a concrete set of unlocked leaf instances. Priority is the
        // objective score of the conceptual retained set plus incoming item, descending.
        // Removing additional items can never improve that score, so the first feasible
        // node popped from this frontier is optimal over this search domain. If the bounded
        // attempt budget is exhausted before such a proof, fail closed rather than claim a
        // heuristic destructive plan is optimal.
        var queue = new PriorityQueue<GlobalSubsetV1170, GlobalPriorityV1170>();
        for (var i = 0; i < victims.Length; i++)
        {
            var indices = new[] { i };
            EnqueueGlobalSubsetV1170(queue, current, scanned, incoming, victims, indices);
        }

        var attempts = 0;
        while (queue.Count > 0 && attempts < V1170MaxGlobalSubsetAttempts)
        {
            var subset = queue.Dequeue();
            attempts++;

            // Queue order is descending objective. Once the best remaining conceptual state
            // does not beat current state, no destructive descendant can become worthwhile.
            if (!FarmingGuideOptimizationPolicy.IsBetter(subset.Score, currentScore))
                break;

            if (TryBuildStoredCandidateV1170(
                    current,
                    scanned,
                    incoming,
                    incomingState,
                    incomingInstanceId,
                    subset.Indices.Select(index => victims[index].InstanceId).ToArray(),
                    out recommendation,
                    out score))
            {
                return true;
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

        recommendation = RejectUnsafeRaidPlanV1163(current);
        score = currentScore;
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
        out FarmingGuideOptimizationScore score)
    {
        var removed = removedInstanceIds.Count == 0
            ? null
            : removedInstanceIds.ToHashSet(StringComparer.Ordinal);
        var reduced = removed is null
            ? current
            : current with
            {
                StoredItems = current.StoredItems
                    .Where(value => !removed.Contains(value.InstanceId))
                    .ToArray(),
            };

        if (!TryRepackIncomingStateV1164(
                reduced,
                incomingState,
                incoming,
                incomingInstanceId,
                out var proposed) ||
            !PreservesLockedItemPlacementV1164(current, proposed))
        {
            recommendation = RejectUnsafeRaidPlanV1163(current);
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
            // The current low-level planner can safely omit only a leaf without destroying
            // descendants. Populated-container decomposition is tracked as remaining v1.17
            // solver work; until then it fails closed rather than silently deleting contents.
            .Where(stored => !snapshot.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(stored => !_lockedItemInstanceIds.Contains(stored.InstanceId))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .OrderBy(stored => stored.InstanceId, StringComparer.Ordinal);
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
            FarmingGuideItemState.Create(incoming.Id),
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
