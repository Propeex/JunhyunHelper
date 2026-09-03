using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1170MaxGlobalSubsetAttempts = 2048;

    /// <summary>
    /// v1.17 farming rulebook entry point. Combat-performance upgrade heuristics are no
    /// longer a farming priority. The hardened planner remains useful for legal target
    /// discovery, but every newly introduced scanned item is explicitly marked as raid
    /// acquired so FIR provenance survives later movement and repacking.
    /// </summary>
    private RaidRecommendation PlanScannedItemRulebookV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        var planned = PlanScannedItemHardened(scanned, incoming);
        return MarkIncomingRaidProvenanceV1170(current, planned, incoming.Id);
    }

    /// <summary>
    /// Replaces the historical tactical/victim-first transition with a best-first complete
    /// state search over the currently proven domain. Food/drink/ammunition/medicine receive
    /// no category privilege. Each feasible packed state is scored by (FIR units satisfied,
    /// total retained Flea value), with weight as a hard admissibility constraint.
    ///
    /// The current low-level packing engine is reused only as a system-legality proof. When
    /// the complete v1.17 candidate domain is not representable (for example populated
    /// container decomposition or top-level equipment pooling), destructive advice becomes
    /// Indeterminate rather than pretending the restricted search proved a global optimum.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (!PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
            recommendation = IndeterminateRaidPlanV1170(current);

        // Retaining every current item plus the incoming item is globally optimal for the
        // farming objective: no destructive alternative can improve FIR satisfaction or
        // retained value once all available units are already retained.
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

        var incompleteDomain = HasUnmodeledGlobalChoicesV1170(current, incoming);
        var currentScore = ScoreRaidStateV1170(current, scanned);
        RaidRecommendation? best = null;
        FarmingGuideOptimizationScore bestScore = currentScore;

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

        // A destructive candidate is only globally authoritative when the candidate domain
        // itself is complete and the bounded search proved its frontier. Otherwise retain
        // the current modeled state and surface uncertainty without a hotkey-accept action.
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
                out score))
        {
            proofComplete = true;
            return FarmingGuideOptimizationPolicy.IsBetter(score, currentScore);
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
                    out score))
            {
                proofComplete = true;
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
            recommendation = IndeterminateRaidPlanV1170(current);
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
            .Where(stored => !snapshot.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(stored => !_lockedItemInstanceIds.Contains(stored.InstanceId))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
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

    private bool HasUnmodeledGlobalChoicesV1170(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming)
    {
        // Populated stored containers are not yet decomposed into independent parent/content
        // choices by the low-level removal search.
        if (current.StoredItems.Any(parent =>
                !_lockedItemInstanceIds.Contains(parent.InstanceId) &&
                current.StoredItems.Any(child =>
                    string.Equals(child.ParentInstanceId, parent.InstanceId, StringComparison.Ordinal))))
        {
            return true;
        }

        // A scanned storage item can introduce capacity that the historical surface builder
        // cannot use until the item has already been placed.
        if (incoming.FarmingGuideData?.StorageGrids is { Count: > 0 })
            return true;

        // Unlocked top-level equipment/carriers are still discovered through historical
        // target-specific paths rather than the same all-item candidate pool. Destructive
        // advice therefore remains indeterminate until that representation is unified.
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
            before?.Attachments.TryGetValue(pair.Key, out var beforeChild);
            attachments[pair.Key] = MarkNewRaidStateV1170(beforeChild, pair.Value, incomingItemId);
        }

        var plates = new Dictionary<string, FarmingGuideItemState?>(after.ArmorPlates, StringComparer.Ordinal);
        foreach (var pair in after.ArmorPlates)
        {
            if (pair.Value is null)
                continue;
            before?.ArmorPlates.TryGetValue(pair.Key, out var beforeChild);
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
