using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1170MaximumRetainedSetAttempts = 4096;
    private const string V1170EquipmentPrefix = "__v1170_equipment_";
    private const string V1170CarrierPrefix = "__v1170_carrier_";
    private const string V1170IncomingPrefix = "__v1170_incoming_";

    private static readonly FarmingGuideEquipmentSlot[] V1170EquipmentSlots =
    [
        FarmingGuideEquipmentSlot.Headset,
        FarmingGuideEquipmentSlot.Helmet,
        FarmingGuideEquipmentSlot.FaceCover,
        FarmingGuideEquipmentSlot.Armband,
        FarmingGuideEquipmentSlot.BodyArmor,
        FarmingGuideEquipmentSlot.Eyewear,
        FarmingGuideEquipmentSlot.PrimaryWeapon1,
        FarmingGuideEquipmentSlot.PrimaryWeapon2,
        FarmingGuideEquipmentSlot.Holster,
    ];

    private static readonly FarmingGuideStorageKind[] V1170CarrierKinds =
    [
        FarmingGuideStorageKind.Rig,
        FarmingGuideStorageKind.Backpack,
        FarmingGuideStorageKind.SecureContainer,
    ];

    private enum GlobalRootOriginV1170
    {
        Stored,
        Equipment,
        Carrier,
        Incoming,
    }

    private sealed record GlobalRootV1170(
        string InstanceId,
        FarmingGuideItemState State,
        GameItem Item,
        int Quantity,
        GlobalRootOriginV1170 Origin,
        FarmingGuideStoredItemState? StoredSource = null,
        FarmingGuideEquipmentSlot? EquipmentSlot = null,
        FarmingGuideStorageKind? CarrierKind = null,
        bool Fixed = false);

    private sealed record RetainedSetV1170(
        int[] RemovedOptionalIndices,
        FarmingGuideOptimizationScore Score);

    private readonly record struct RetainedSetPriorityV1170(
        int Fir,
        long Value,
        int RemovedCount,
        string Key) : IComparable<RetainedSetPriorityV1170>
    {
        public int CompareTo(RetainedSetPriorityV1170 other)
        {
            // PriorityQueue dequeues the smallest priority. Reverse the two product score
            // dimensions so the strongest conceptual retained set is tested first.
            var fir = other.Fir.CompareTo(Fir);
            if (fir != 0)
                return fir;
            var value = other.Value.CompareTo(Value);
            if (value != 0)
                return value;
            var count = RemovedCount.CompareTo(other.RemovedCount);
            if (count != 0)
                return count;
            return StringComparer.Ordinal.Compare(Key, other.Key);
        }
    }

    /// <summary>
    /// Authoritative v1.17 Farming Guide decision. All current movable roots and the active
    /// raid Scanner incoming root enter one retained-set search. A retained set is ranked only
    /// by the confirmed product objective; the global packing proof then decides whether that
    /// set is physically legal under Tarkov mechanics, locks and the configured weight rule.
    /// </summary>
    private bool TryPlanScannedItemGlobalV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        out RaidRecommendation recommendation)
    {
        recommendation = new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);

        if (!TryBuildGlobalRootsV1170(current, scanned, incoming, out var roots, out var incomingRoot))
            return false;

        var currentScore = ScoreSnapshotV1170(current, scanned);
        var allScore = ScoreRootsV1170(roots, scanned);

        // If even the conceptual all-retained state cannot improve the current Farming Guide
        // objective, no descendant obtained by removing non-negative-value roots can improve
        // it either. Current state wins the deterministic stability tie: discard incoming.
        if (!FarmingGuideOptimizationPolicy.IsBetter(allScore, currentScore))
            return true;

        var optional = roots
            .Where(static root => !root.Fixed)
            .OrderBy(root => root.InstanceId, StringComparer.Ordinal)
            .ToArray();
        var queue = new PriorityQueue<RetainedSetV1170, RetainedSetPriorityV1170>();
        EnqueueRetainedSetV1170(queue, roots, optional, scanned, []);

        var attempts = 0;
        while (queue.Count > 0 && attempts < V1170MaximumRetainedSetAttempts)
        {
            var candidate = queue.Dequeue();
            attempts++;

            // Search is objective-descending. Once the best remaining conceptual score does
            // not improve current state, no later subset can justify changing the raid state.
            if (!FarmingGuideOptimizationPolicy.IsBetter(candidate.Score, currentScore))
                return true;

            var removedIds = candidate.RemovedOptionalIndices
                .Select(index => optional[index].InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var selected = roots
                .Where(root => !removedIds.Contains(root.InstanceId))
                .ToArray();

            var packing = TryPackGlobalSelectionV1170(
                current,
                selected,
                incomingRoot,
                out var proposed);
            if (packing == FarmingGuideGlobalPackingStatus.BudgetExceeded)
            {
                // This highest-ranked candidate remains unresolved, so a lower-ranked
                // destructive result cannot be claimed globally optimal. Keep the existing
                // product surface unchanged by issuing no pending advice for this scan.
                return false;
            }

            if (packing == FarmingGuideGlobalPackingStatus.Found)
            {
                var proposedScore = ScoreSnapshotV1170(proposed, scanned);
                if (FarmingGuideOptimizationPolicy.IsBetter(proposedScore, currentScore))
                {
                    recommendation = BuildGlobalRecommendationV1170(
                        current,
                        proposed,
                        incomingRoot);
                    return true;
                }
                return true;
            }

            var last = candidate.RemovedOptionalIndices.Length == 0
                ? -1
                : candidate.RemovedOptionalIndices[^1];
            for (var next = last + 1; next < optional.Length; next++)
            {
                var expanded = new int[candidate.RemovedOptionalIndices.Length + 1];
                Array.Copy(candidate.RemovedOptionalIndices, expanded, candidate.RemovedOptionalIndices.Length);
                expanded[^1] = next;
                EnqueueRetainedSetV1170(queue, roots, optional, scanned, expanded);
            }
        }

        // Exhausting a deterministic implementation budget is not proof of a lower-valued
        // alternative. Do not manufacture a discard/replacement result.
        return queue.Count == 0;
    }

    private void EnqueueRetainedSetV1170(
        PriorityQueue<RetainedSetV1170, RetainedSetPriorityV1170> queue,
        IReadOnlyList<GlobalRootV1170> roots,
        IReadOnlyList<GlobalRootV1170> optional,
        ScannerItemSnapshot scanned,
        int[] removedIndices)
    {
        var removedIds = removedIndices
            .Select(index => optional[index].InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var selected = roots.Where(root => !removedIds.Contains(root.InstanceId)).ToArray();
        var score = ScoreRootsV1170(selected, scanned);
        var key = string.Join(',', removedIndices.Select(index => optional[index].InstanceId));
        queue.Enqueue(
            new RetainedSetV1170(removedIndices, score),
            new RetainedSetPriorityV1170(
                score.SatisfiedFirUnits,
                score.RetainedFleaValue,
                removedIndices.Length,
                key));
    }

    private bool TryBuildGlobalRootsV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        out GlobalRootV1170[] roots,
        out GlobalRootV1170 incomingRoot)
    {
        var result = new List<GlobalRootV1170>();
        var effectiveLockedStored = BuildEffectiveLockedStoredIdsV1170(current);
        var effectiveLockedCarriers = new HashSet<FarmingGuideStorageKind>(_lockedCarriers);

        // A fixed cell or fixed stored root inside a root carrier also fixes that carrier;
        // otherwise replacing/moving the carrier would move the user's fixed state indirectly.
        foreach (var stored in current.StoredItems.Where(stored =>
                     effectiveLockedStored.Contains(stored.InstanceId) &&
                     stored.ParentInstanceId is null &&
                     stored.Storage is FarmingGuideStorageKind.Rig or
                         FarmingGuideStorageKind.Backpack or
                         FarmingGuideStorageKind.SecureContainer))
        {
            effectiveLockedCarriers.Add(stored.Storage);
        }
        foreach (var cell in _reservedCells.Where(cell => cell.ParentInstanceId is null &&
                     cell.Storage is FarmingGuideStorageKind.Rig or
                         FarmingGuideStorageKind.Backpack or
                         FarmingGuideStorageKind.SecureContainer))
        {
            effectiveLockedCarriers.Add(cell.Storage);
        }

        foreach (var stored in current.StoredItems)
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                roots = [];
                incomingRoot = default!;
                return false;
            }
            result.Add(new GlobalRootV1170(
                stored.InstanceId,
                stored.Item,
                item,
                stored.NormalizedQuantity,
                GlobalRootOriginV1170.Stored,
                StoredSource: stored,
                Fixed: effectiveLockedStored.Contains(stored.InstanceId)));
        }

        foreach (var slot in V1170EquipmentSlots)
        {
            if (!current.Equipment.TryGetValue(slot, out var state))
                continue;
            var item = ResolveItem(state);
            if (item is null)
            {
                roots = [];
                incomingRoot = default!;
                return false;
            }
            result.Add(new GlobalRootV1170(
                EquipmentRootIdV1170(slot),
                state,
                item,
                1,
                GlobalRootOriginV1170.Equipment,
                EquipmentSlot: slot,
                Fixed: _lockedEquipmentSlots.Contains(slot)));
        }

        foreach (var kind in V1170CarrierKinds)
        {
            var state = CarrierStateV1170(current, kind);
            if (state is null)
                continue;
            var item = ResolveItem(state);
            if (item is null)
            {
                roots = [];
                incomingRoot = default!;
                return false;
            }
            result.Add(new GlobalRootV1170(
                CarrierRootIdV1170(kind),
                state,
                item,
                1,
                GlobalRootOriginV1170.Carrier,
                CarrierKind: kind,
                Fixed: effectiveLockedCarriers.Contains(kind)));
        }

        incomingRoot = new GlobalRootV1170(
            $"{V1170IncomingPrefix}{Guid.NewGuid():N}",
            FarmingGuideItemState.Create(incoming.Id, raidAcquired: true),
            incoming,
            Math.Max(1, scanned.Quantity),
            GlobalRootOriginV1170.Incoming);
        result.Add(incomingRoot);
        roots = result.ToArray();
        return true;
    }

    private HashSet<string> BuildEffectiveLockedStoredIdsV1170(FarmingGuideLoadoutSnapshot snapshot)
    {
        var byId = snapshot.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var fixedIds = new HashSet<string>(_lockedItemInstanceIds, StringComparer.Ordinal);
        foreach (var cell in _reservedCells)
        {
            if (!string.IsNullOrWhiteSpace(cell.ParentInstanceId))
                fixedIds.Add(cell.ParentInstanceId);
        }

        var pending = new Stack<string>(fixedIds);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!byId.TryGetValue(current, out var stored) ||
                string.IsNullOrWhiteSpace(stored.ParentInstanceId) ||
                !fixedIds.Add(stored.ParentInstanceId))
            {
                continue;
            }
            pending.Push(stored.ParentInstanceId);
        }
        return fixedIds;
    }

    private FarmingGuideOptimizationScore ScoreSnapshotV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        ScannerItemSnapshot scanned) =>
        FarmingGuideOptimizationPolicy.Score(
            snapshot,
            itemId => RemainingFirNeedV1170(itemId, scanned),
            ResolveUnitFleaValueV1170);

    private FarmingGuideOptimizationScore ScoreRootsV1170(
        IReadOnlyList<GlobalRootV1170> roots,
        ScannerItemSnapshot scanned)
    {
        var acquired = new Dictionary<string, int>(StringComparer.Ordinal);
        long retainedValue = 0;
        foreach (var root in roots)
        {
            if (root.State.RaidAcquired)
            {
                acquired[root.Item.Id] = checked(
                    acquired.GetValueOrDefault(root.Item.Id) + Math.Max(1, root.Quantity));
            }
            retainedValue = checked(
                retainedValue + (long)Math.Max(0, ResolveUnitFleaValueV1170(root.Item.Id) ?? 0) *
                Math.Max(1, root.Quantity));
        }

        var satisfied = 0;
        foreach (var pair in acquired)
        {
            satisfied = checked(satisfied + Math.Min(
                pair.Value,
                RemainingFirNeedV1170(pair.Key, scanned)));
        }
        return new FarmingGuideOptimizationScore(satisfied, retainedValue);
    }

    private int RemainingFirNeedV1170(string itemId, ScannerItemSnapshot scanned)
    {
        if (string.Equals(itemId, scanned.ItemId, StringComparison.Ordinal))
            return Math.Max(0, scanned.CurrentNeededFir);
        return Math.Max(0, _raidBridge?.ResolveSnapshot(itemId)?.CurrentNeededFir ?? 0);
    }

    private int? ResolveUnitFleaValueV1170(string itemId)
    {
        if (_raidFleaAveragePrices.TryGetValue(itemId, out var remembered))
            return Math.Max(0, remembered);
        return _raidBridge?.ResolveSnapshot(itemId)?.FleaAveragePrice is { } flea
            ? Math.Max(0, flea)
            : 0;
    }

    private static FarmingGuideItemState? CarrierStateV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStorageKind kind) => kind switch
        {
            FarmingGuideStorageKind.Rig => snapshot.Rig,
            FarmingGuideStorageKind.Backpack => snapshot.Backpack,
            FarmingGuideStorageKind.SecureContainer => snapshot.SecureContainer,
            _ => null,
        };

    private static string EquipmentRootIdV1170(FarmingGuideEquipmentSlot slot) =>
        $"{V1170EquipmentPrefix}{(int)slot}";

    private static string CarrierRootIdV1170(FarmingGuideStorageKind kind) =>
        $"{V1170CarrierPrefix}{(int)kind}";
}
