using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.16.4 correction: an explicit stored-item lock is a position lock for automatic
    /// Farming Guide advice. A locked item may still be edited directly by the user, and a
    /// locked carrier root still exposes its legal internal storage, but no automatic plan
    /// may relocate/rotate/re-parent the locked stored instance or indirectly move it by
    /// moving one of its stored ancestors or replacing its root carrier.
    /// </summary>
    private RaidRecommendation PlanScannedItemRulebookV1164(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (TryBuildProtectiveUpgrade(current, incoming, out var protective))
            return protective;
        if (TryBuildHeadsetUpgrade(current, incoming, out var headset))
            return headset;

        if (TryBuildCarrierUpgradeRulebookV1160(current, incoming, out var carrier, out var carrierHandled))
        {
            if (PreservesLockedItemPlacementV1164(current, carrier.ProposedSnapshot))
                return carrier;

            // Carrier migration may have produced a reservation override before the new
            // v1.16.4 position-lock boundary rejected the upgrade. Never leak that rejected
            // plan's lock state into the later ordinary-storage decision.
            _plannedLocksOverrideV1160 = null;
        }
        else if (carrierHandled)
        {
            _plannedLocksOverrideV1160 = null;
        }

        var addedEquipmentLocks = new List<FarmingGuideEquipmentSlot>();
        var addedCarrierLocks = new List<FarmingGuideStorageKind>();
        try
        {
            foreach (var pair in current.Equipment)
            {
                if (_lockedEquipmentSlots.Contains(pair.Key) ||
                    !FarmingGuideCompatibility.IsEquipmentSlotCompatible(pair.Key, incoming))
                {
                    continue;
                }

                _lockedEquipmentSlots.Add(pair.Key);
                addedEquipmentLocks.Add(pair.Key);
            }

            foreach (var pair in new[]
                     {
                         (FarmingGuideStorageKind.Rig, current.Rig),
                         (FarmingGuideStorageKind.Backpack, current.Backpack),
                         (FarmingGuideStorageKind.SecureContainer, current.SecureContainer),
                     })
            {
                if (pair.Item2 is null || _lockedCarriers.Contains(pair.Item1) ||
                    !FarmingGuideCompatibility.IsStorageCarrierCompatible(pair.Item1, incoming))
                {
                    continue;
                }

                _lockedCarriers.Add(pair.Item1);
                addedCarrierLocks.Add(pair.Item1);
            }

            _ = carrierHandled;

            // Keep the useful v1.16.3 secure-promotion behavior only when the complete
            // proposed state leaves every explicit locked item at its exact physical place.
            // If the v1.16.3 optimizer chooses a locked blocker, ignore that promotion and
            // continue through ordinary legal storage instead of issuing a bad move order.
            if (TryBuildSecureProtectionRecommendationV1163(current, scanned, incoming, out var secure) &&
                PreservesLockedItemPlacementV1164(current, secure.ProposedSnapshot))
            {
                return secure;
            }

            return PlanScannedItemHardened(scanned, incoming);
        }
        finally
        {
            foreach (var slot in addedEquipmentLocks)
                _lockedEquipmentSlots.Remove(slot);
            foreach (var kind in addedCarrierLocks)
                _lockedCarriers.Remove(kind);
        }
    }

    private RaidRecommendation ApplyRaidStateTransitionsV1164(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (!PreservesLockedItemPlacementV1164(current, recommendation.ProposedSnapshot))
            return RejectUnsafeRaidPlanV1163(current);

        if (recommendation.Action == FarmingGuideInstructionAction.ReplaceEquip)
            return PreserveDisplacedTopLevelItemsV1164(current, recommendation);

        if (recommendation.Action is not (
                FarmingGuideInstructionAction.Replace or
                FarmingGuideInstructionAction.Discard))
        {
            return recommendation;
        }

        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        if (TryRepackIncomingWithSafeEvictionsV1164(
                current,
                FarmingGuideItemState.Create(incoming.Id),
                incoming,
                incomingMetrics,
                NewDisplacedInstanceIdV1155(),
                out var proposed,
                out var evictedCount))
        {
            return new RaidRecommendation(
                evictedCount == 0 ? "보관" : "교체",
                evictedCount == 0
                    ? FarmingGuideInstructionAction.Store
                    : FarmingGuideInstructionAction.Replace,
                proposed);
        }

        return RejectUnsafeRaidPlanV1163(current);
    }

    private RaidRecommendation PreserveDisplacedTopLevelItemsV1164(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation)
    {
        var originalProposed = recommendation.ProposedSnapshot;
        if (!PreservesLockedItemPlacementV1164(current, originalProposed))
            return RejectUnsafeRaidPlanV1163(current);

        var candidate = originalProposed;
        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            var before = CarrierStateV1155(current, kind);
            var after = CarrierStateV1155(originalProposed, kind);
            if (before is null || SameRootItemV1155(before, after))
                continue;

            var oldItem = ResolveItem(before);
            if (oldItem is null)
                continue;

            if (TryRepackIncomingWithSafeEvictionsV1164(
                    candidate,
                    before,
                    oldItem,
                    MetricsForExisting(oldItem),
                    NewDisplacedInstanceIdV1155(),
                    out var preserved,
                    out _) &&
                PreservesLockedItemPlacementV1164(current, preserved))
            {
                candidate = preserved;
            }
        }

        foreach (var slot in new[]
                 {
                     FarmingGuideEquipmentSlot.Headset,
                     FarmingGuideEquipmentSlot.Helmet,
                     FarmingGuideEquipmentSlot.FaceCover,
                     FarmingGuideEquipmentSlot.Armband,
                     FarmingGuideEquipmentSlot.BodyArmor,
                     FarmingGuideEquipmentSlot.Eyewear,
                     FarmingGuideEquipmentSlot.PrimaryWeapon1,
                     FarmingGuideEquipmentSlot.PrimaryWeapon2,
                     FarmingGuideEquipmentSlot.Holster,
                 })
        {
            current.Equipment.TryGetValue(slot, out var before);
            originalProposed.Equipment.TryGetValue(slot, out var after);
            if (before is null || SameRootItemV1155(before, after))
                continue;

            var oldItem = ResolveItem(before);
            if (oldItem is null)
                continue;

            if (TryRepackIncomingWithSafeEvictionsV1164(
                    candidate,
                    before,
                    oldItem,
                    MetricsForExisting(oldItem),
                    NewDisplacedInstanceIdV1155(),
                    out var preserved,
                    out _) &&
                PreservesLockedItemPlacementV1164(current, preserved))
            {
                candidate = preserved;
            }
        }

        return recommendation with { ProposedSnapshot = candidate };
    }

    private bool TryRepackIncomingWithSafeEvictionsV1164(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        FarmingGuideLootMetrics incomingMetrics,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed,
        out int evictedCount)
    {
        if (TryRepackIncomingStateV1164(
                snapshot,
                incomingState,
                incoming,
                incomingInstanceId,
                out proposed))
        {
            evictedCount = 0;
            return true;
        }

        var candidates = EnumerateSafeEvictionCandidatesV1164(snapshot).ToArray();
        if (candidates.Length == 0)
        {
            proposed = snapshot;
            evictedCount = 0;
            return false;
        }

        var queue = new PriorityQueue<EvictionSubsetV1163, EvictionPriorityV1163>();
        for (var index = 0; index < candidates.Length; index++)
            EnqueueEvictionSubsetV1163(queue, candidates, [index]);

        var planAttempts = 0;
        while (queue.Count > 0 && planAttempts < V1163MaxEvictionPlanAttempts)
        {
            var subset = queue.Dequeue();
            var victimMetrics = subset.Indices
                .Select(index => candidates[index].Metrics)
                .ToArray();
            if (!FarmingGuideLootRetentionPolicy.CanSacrificeFor(incomingMetrics, victimMetrics))
                continue;

            planAttempts++;
            var remove = subset.Indices
                .Select(index => candidates[index].Stored.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var reduced = snapshot with
            {
                StoredItems = snapshot.StoredItems
                    .Where(value => !remove.Contains(value.InstanceId))
                    .ToArray(),
            };

            if (TryRepackIncomingStateV1164(
                    reduced,
                    incomingState,
                    incoming,
                    incomingInstanceId,
                    out proposed) &&
                PreservesLockedItemPlacementV1164(snapshot, proposed))
            {
                evictedCount = subset.Indices.Length;
                return true;
            }

            if (subset.Indices.Length >= V1163MaxEvictionSubsetSize)
                continue;

            var last = subset.Indices[^1];
            for (var next = last + 1; next < candidates.Length; next++)
            {
                var expanded = new int[subset.Indices.Length + 1];
                Array.Copy(subset.Indices, expanded, subset.Indices.Length);
                expanded[^1] = next;
                EnqueueEvictionSubsetV1163(queue, candidates, expanded);
            }
        }

        proposed = snapshot;
        evictedCount = 0;
        return false;
    }

    private IEnumerable<EvictionCandidateV1163> EnumerateSafeEvictionCandidatesV1164(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        var foodCount = CountStoredResourcesV1163(snapshot, FarmingGuideTacticalResourcePolicy.ProvidesFood);
        var drinkCount = CountStoredResourcesV1163(snapshot, FarmingGuideTacticalResourcePolicy.ProvidesDrink);
        var weapons = CurrentRaidWeaponsV1163(snapshot);

        return snapshot.StoredItems
            .Where(stored => !snapshot.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(stored => !_lockedItemInstanceIds.Contains(stored.InstanceId))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Select(stored =>
            {
                var item = ResolveItem(stored.Item);
                if (item is null)
                    return null;

                var metrics = MetricsForStoredV1163(stored, item);
                if (metrics.CurrentNeeded > 0)
                    return null;
                if (FarmingGuideTacticalResourcePolicy.ProvidesFood(item) && foodCount <= 1)
                    return null;
                if (FarmingGuideTacticalResourcePolicy.ProvidesDrink(item) && drinkCount <= 1)
                    return null;
                if (weapons.Count > 0 &&
                    FarmingGuideTacticalResourcePolicy.IsAmmoForAnyWeapon(item, weapons))
                {
                    return null;
                }

                return new EvictionCandidateV1163(stored, metrics);
            })
            .Where(static value => value is not null)
            .Select(static value => value!)
            .OrderBy(value => value.Metrics, LootMetricsComparer.Instance)
            .ThenBy(value => value.Stored.InstanceId, StringComparer.Ordinal);
    }

    private bool TryRepackIncomingStateV1164(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        var surfaces = EnumerateTransitionSurfacesV1163(snapshot, incoming, snapshot.StoredItems)
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var surfaceById = surfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var coreSurfaces = surfaces
            .Select((surface, priority) => ToCoreSurfaceV1163(surface, priority))
            .ToArray();
        if (coreSurfaces.Length == 0)
        {
            proposed = snapshot;
            return false;
        }

        var items = new List<FarmingGuideRepackingItem>(snapshot.StoredItems.Count);
        foreach (var stored in snapshot.StoredItems)
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                proposed = snapshot;
                return false;
            }

            var currentSurfaceId = SurfaceId(stored.Storage, stored.ParentInstanceId, stored.GridIndex);
            if (!surfaceById.ContainsKey(currentSurfaceId))
            {
                proposed = snapshot;
                return false;
            }

            var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                stored.Storage,
                stored.ParentInstanceId,
                item,
                stored.Rotated);
            var options = BuildTransitionOptionsV1163(item, surfaces).ToArray();
            var positionProtected = _lockedItemInstanceIds.Contains(stored.InstanceId) ||
                                    SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems);
            var movable = !positionProtected && options.Length > 0;

            items.Add(new FarmingGuideRepackingItem(
                stored.InstanceId,
                currentSurfaceId,
                stored.X,
                stored.Y,
                footprint.Width,
                footprint.Height,
                stored.Rotated,
                movable,
                options));
        }

        var incomingOptions = BuildTransitionOptionsV1163(incoming, surfaces).ToArray();
        if (incomingOptions.Length == 0)
        {
            proposed = snapshot;
            return false;
        }

        var plan = FarmingGuideRepackingPlanner.TryPlan(
            coreSurfaces,
            items,
            new FarmingGuideRepackingIncoming(incomingInstanceId, incomingOptions));
        if (plan is null || !surfaceById.TryGetValue(plan.Incoming.SurfaceId, out var destination))
        {
            proposed = snapshot;
            return false;
        }

        var placementById = plan.ExistingPlacements
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var result = new List<FarmingGuideStoredItemState>(snapshot.StoredItems.Count + 1);
        foreach (var stored in snapshot.StoredItems)
        {
            if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var target))
            {
                proposed = snapshot;
                return false;
            }

            result.Add(stored with
            {
                Storage = target.Kind,
                GridIndex = target.GridIndex,
                X = placement.X,
                Y = placement.Y,
                Rotated = placement.Rotated,
                ParentInstanceId = target.ParentInstanceId,
            });
        }

        result.Add(new FarmingGuideStoredItemState(
            incomingInstanceId,
            incomingState,
            destination.Kind,
            destination.GridIndex,
            plan.Incoming.X,
            plan.Incoming.Y,
            plan.Incoming.Rotated,
            destination.ParentInstanceId));

        if (!TryNormalizeRootStorageKinds(result, out var normalized))
        {
            proposed = snapshot;
            return false;
        }

        proposed = snapshot with { StoredItems = normalized };
        if (!PreservesLockedItemPlacementV1164(snapshot, proposed))
        {
            proposed = snapshot;
            return false;
        }
        return true;
    }

    private RaidRecommendation ApplyFinalRaidSafetyV1164(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot decisionScan)
    {
        var checkedV1163 = ApplyFinalRaidSafetyV1163(current, recommendation, decisionScan);
        if (checkedV1163.Action != FarmingGuideInstructionAction.Discard &&
            !PreservesLockedItemPlacementV1164(current, checkedV1163.ProposedSnapshot))
        {
            return RejectUnsafeRaidPlanV1163(current);
        }
        return checkedV1163;
    }

    private bool PreservesLockedItemPlacementV1164(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        if (_lockedItemInstanceIds.Count == 0)
            return true;

        var beforeById = current.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var afterById = proposed.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);

        foreach (var lockedId in _lockedItemInstanceIds)
        {
            if (!beforeById.TryGetValue(lockedId, out var before))
                continue;
            if (!afterById.TryGetValue(lockedId, out var after) ||
                !SameStoredPlacementV1164(before, after))
            {
                return false;
            }

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var beforeCurrent = before;
            var afterCurrent = after;
            while (!string.IsNullOrWhiteSpace(beforeCurrent.ParentInstanceId))
            {
                if (string.IsNullOrWhiteSpace(afterCurrent.ParentInstanceId) ||
                    !string.Equals(
                        beforeCurrent.ParentInstanceId,
                        afterCurrent.ParentInstanceId,
                        StringComparison.Ordinal) ||
                    !visited.Add(beforeCurrent.InstanceId) ||
                    !beforeById.TryGetValue(beforeCurrent.ParentInstanceId, out var beforeParent) ||
                    !afterById.TryGetValue(afterCurrent.ParentInstanceId, out var afterParent) ||
                    !SameStoredPlacementV1164(beforeParent, afterParent))
                {
                    return false;
                }

                beforeCurrent = beforeParent;
                afterCurrent = afterParent;
            }

            if (!string.IsNullOrWhiteSpace(afterCurrent.ParentInstanceId) ||
                beforeCurrent.Storage != afterCurrent.Storage)
            {
                return false;
            }

            if (beforeCurrent.Storage is FarmingGuideStorageKind.Rig or
                FarmingGuideStorageKind.Backpack or
                FarmingGuideStorageKind.SecureContainer)
            {
                if (!SameRootItemV1155(
                        CarrierStateV1155(current, beforeCurrent.Storage),
                        CarrierStateV1155(proposed, afterCurrent.Storage)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool SameStoredPlacementV1164(
        FarmingGuideStoredItemState before,
        FarmingGuideStoredItemState after) =>
        string.Equals(before.InstanceId, after.InstanceId, StringComparison.Ordinal) &&
        string.Equals(before.Item.ItemId, after.Item.ItemId, StringComparison.Ordinal) &&
        before.Storage == after.Storage &&
        before.GridIndex == after.GridIndex &&
        before.X == after.X &&
        before.Y == after.Y &&
        before.Rotated == after.Rotated &&
        string.Equals(before.ParentInstanceId, after.ParentInstanceId, StringComparison.Ordinal) &&
        before.NormalizedQuantity == after.NormalizedQuantity;
}
