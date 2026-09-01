using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1155MaxEvictionVictims = 6;
    private const string V1155DisplacedInstancePrefix = "__raid_displaced_v1155__";
    private const string V1155HandoffSurfaceId = "__raid_transition_handoff_v1155__";
    private const string V1155HandoffIncomingId = "__raid_transition_trigger_v1155__";

    /// <summary>
    /// v1.15.5 state-transition layer. Historical planners may still express an equipment
    /// replacement as a destructive snapshot. This layer treats the displaced top-level
    /// item as loot again, then asks the same legality/repacking machinery to preserve the
    /// best complete state before presentation is generated.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1155(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        var result = recommendation;
        if (recommendation.Action == FarmingGuideInstructionAction.ReplaceEquip)
        {
            result = PreserveDisplacedTopLevelItemsV1155(current, recommendation);
        }

        // The hardened v1.15.4 path already tries one destructive victim. If it reaches
        // Discard, allow a bounded aggregate-value search for cases where two or more cheap
        // leaf items must be removed to fit one clearly more important incoming item.
        if (result.Action == FarmingGuideInstructionAction.Discard)
        {
            var metrics = ToMetrics(scanned, adjustAcceptedCount: true);
            var incomingState = FarmingGuideItemState.Create(incoming.Id);
            if (TryRepackIncomingWithBoundedEvictionsV1155(
                    current,
                    incomingState,
                    incoming,
                    metrics,
                    NewDisplacedInstanceIdV1155(),
                    out var stored,
                    out var evictedCount))
            {
                result = new RaidRecommendation(
                    evictedCount == 0 ? "보관" : "교체",
                    evictedCount == 0
                        ? FarmingGuideInstructionAction.Store
                        : FarmingGuideInstructionAction.Replace,
                    stored);
            }
        }

        return result;
    }

    private RaidRecommendation PreserveDisplacedTopLevelItemsV1155(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation)
    {
        var originalProposed = recommendation.ProposedSnapshot;
        var candidate = originalProposed;

        // Carriers are handled first because their old contents belong to the displaced
        // carrier. Preserving that carrier may itself create nested storage that a later
        // displaced armor/helmet/etc. can use.
        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                 })
        {
            var before = CarrierStateV1155(current, kind);
            var after = CarrierStateV1155(originalProposed, kind);
            if (before is null || SameRootItemV1155(before, after))
                continue;

            var oldItem = ResolveItem(before);
            if (oldItem is null)
                continue;

            if (TryPreserveDisplacedCarrierWithEvictionsV1155(
                    current,
                    candidate,
                    kind,
                    before,
                    oldItem,
                    out var preserved))
            {
                candidate = preserved;
            }
        }

        // Any changed/removed ordinary top-level equipment is now an inventory candidate.
        // This also handles the body armor removed by the atomic ordinary-rig -> armored-
        // rig transition after the old rig has had a chance to become nested storage.
        foreach (var pair in current.Equipment)
        {
            if (pair.Key is FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag)
                continue;
            var after = originalProposed.Equipment.GetValueOrDefault(pair.Key);
            if (SameRootItemV1155(pair.Value, after))
                continue;

            var oldItem = ResolveItem(pair.Value);
            if (oldItem is null)
                continue;
            var metrics = MetricsForExisting(oldItem);
            if (TryRepackIncomingWithBoundedEvictionsV1155(
                    candidate,
                    pair.Value,
                    oldItem,
                    metrics,
                    NewDisplacedInstanceIdV1155(),
                    out var preserved,
                    out _))
            {
                candidate = preserved;
            }
        }

        return recommendation with { ProposedSnapshot = candidate };
    }

    private bool TryPreserveDisplacedCarrierWithEvictionsV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposedTopLevel,
        FarmingGuideStorageKind kind,
        FarmingGuideItemState oldCarrierState,
        GameItem oldCarrier,
        out FarmingGuideLoadoutSnapshot preserved)
    {
        if (TryPreserveDisplacedCarrierV1155(
                current,
                proposedTopLevel,
                kind,
                oldCarrierState,
                oldCarrier,
                out preserved))
        {
            return true;
        }

        var carrierMetrics = MetricsForExisting(oldCarrier);
        var victims = EnumerateEvictionCandidatesV1155(current)
            .Take(V1155MaxEvictionVictims)
            .ToArray();
        for (var count = 1; count <= victims.Length; count++)
        {
            var prefix = victims.Take(count).ToArray();
            if (!FarmingGuideLootRetentionPolicy.CanSacrificeFor(
                    carrierMetrics,
                    prefix.Select(value => value.Metrics).ToArray()))
            {
                continue;
            }

            var remove = prefix
                .Select(value => value.Stored.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var reduced = current with
            {
                StoredItems = current.StoredItems
                    .Where(value => !remove.Contains(value.InstanceId))
                    .ToArray(),
            };
            if (TryPreserveDisplacedCarrierV1155(
                    reduced,
                    proposedTopLevel,
                    kind,
                    oldCarrierState,
                    oldCarrier,
                    out preserved))
            {
                return true;
            }
        }

        preserved = proposedTopLevel;
        return false;
    }

    /// <summary>
    /// Forces the old carrier out of a synthetic handoff surface. The carrier is an
    /// existing movable item to the Core repacker, so its own internal grids are legal
    /// parent surfaces during the same search. A backpack blocker can therefore move into
    /// the old rig while the old rig itself moves into that backpack, without teaching the
    /// geometry solver anything about "equipment replacement".
    /// </summary>
    private bool TryPreserveDisplacedCarrierV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposedTopLevel,
        FarmingGuideStorageKind kind,
        FarmingGuideItemState oldCarrierState,
        GameItem oldCarrier,
        out FarmingGuideLoadoutSnapshot preserved)
    {
        var displacedId = NewDisplacedInstanceIdV1155();
        var workspaceStored = current.StoredItems
            .Select(value => value.ParentInstanceId is null && value.Storage == kind
                ? value with { ParentInstanceId = displacedId }
                : value)
            .ToList();
        var displacedState = new FarmingGuideStoredItemState(
            displacedId,
            oldCarrierState,
            kind,
            0,
            0,
            0,
            false);
        workspaceStored.Add(displacedState);

        var workspaceSnapshot = proposedTopLevel with { StoredItems = workspaceStored };
        var realSurfaces = EnumerateTransitionSurfacesV1155(
                workspaceSnapshot,
                oldCarrier,
                workspaceStored)
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var surfaceById = realSurfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var coreSurfaces = realSurfaces
            .Select((surface, priority) => ToCoreSurfaceV1155(surface, priority))
            .Append(new FarmingGuideRepackingSurface(
                V1155HandoffSurfaceId,
                null,
                1,
                1,
                int.MaxValue,
                []))
            .ToArray();

        var items = new List<FarmingGuideRepackingItem>(workspaceStored.Count);
        foreach (var stored in workspaceStored)
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                preserved = proposedTopLevel;
                return false;
            }

            var currentSurfaceId = string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal)
                ? V1155HandoffSurfaceId
                : SurfaceId(stored.Storage, stored.ParentInstanceId, stored.GridIndex);
            if (!coreSurfaces.Any(surface => string.Equals(surface.Id, currentSurfaceId, StringComparison.Ordinal)))
            {
                preserved = proposedTopLevel;
                return false;
            }

            var currentFootprint = string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal)
                ? (Width: 1, Height: 1)
                : FarmingGuideStoragePlacementPolicy.Footprint(
                    stored.Storage,
                    stored.ParentInstanceId,
                    item,
                    stored.Rotated);
            var movable = string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal) ||
                          IsTransitionItemMovableV1155(stored, workspaceStored);
            var options = BuildTransitionOptionsV1155(item, realSurfaces, workspaceStored).ToArray();
            if (!string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal) && options.Length == 0)
                movable = false;

            items.Add(new FarmingGuideRepackingItem(
                stored.InstanceId,
                currentSurfaceId,
                string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal) ? 0 : stored.X,
                string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal) ? 0 : stored.Y,
                currentFootprint.Width,
                currentFootprint.Height,
                string.Equals(stored.InstanceId, displacedId, StringComparison.Ordinal) ? false : stored.Rotated,
                movable,
                options));
        }

        var plan = FarmingGuideRepackingPlanner.TryPlan(
            coreSurfaces,
            items,
            new FarmingGuideRepackingIncoming(
                V1155HandoffIncomingId,
                [new FarmingGuideRepackingOption(V1155HandoffSurfaceId, 1, 1, false, 0)]));
        if (plan is null)
        {
            preserved = proposedTopLevel;
            return false;
        }

        var placements = plan.ExistingPlacements
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var result = new List<FarmingGuideStoredItemState>(workspaceStored.Count);
        foreach (var stored in workspaceStored)
        {
            if (!placements.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var surface))
            {
                preserved = proposedTopLevel;
                return false;
            }

            result.Add(stored with
            {
                Storage = surface.Kind,
                GridIndex = surface.GridIndex,
                X = placement.X,
                Y = placement.Y,
                Rotated = placement.Rotated,
                ParentInstanceId = surface.ParentInstanceId,
            });
        }

        if (!TryNormalizeRootStorageKinds(result, out var normalized))
        {
            preserved = proposedTopLevel;
            return false;
        }

        preserved = proposedTopLevel with { StoredItems = normalized };
        return true;
    }

    private bool TryRepackIncomingWithBoundedEvictionsV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        FarmingGuideLootMetrics incomingMetrics,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed,
        out int evictedCount)
    {
        if (TryRepackIncomingStateV1155(
                snapshot,
                incomingState,
                incoming,
                incomingInstanceId,
                out proposed))
        {
            evictedCount = 0;
            return true;
        }

        var victims = EnumerateEvictionCandidatesV1155(snapshot)
            .Take(V1155MaxEvictionVictims)
            .ToArray();
        for (var count = 1; count <= victims.Length; count++)
        {
            var prefix = victims.Take(count).ToArray();
            if (!FarmingGuideLootRetentionPolicy.CanSacrificeFor(
                    incomingMetrics,
                    prefix.Select(value => value.Metrics).ToArray()))
            {
                continue;
            }

            var remove = prefix
                .Select(value => value.Stored.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var reduced = snapshot with
            {
                StoredItems = snapshot.StoredItems
                    .Where(value => !remove.Contains(value.InstanceId))
                    .ToArray(),
            };
            if (TryRepackIncomingStateV1155(
                    reduced,
                    incomingState,
                    incoming,
                    incomingInstanceId,
                    out proposed))
            {
                evictedCount = count;
                return true;
            }
        }

        proposed = snapshot;
        evictedCount = 0;
        return false;
    }

    private bool TryRepackIncomingStateV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        var surfaces = EnumerateTransitionSurfacesV1155(snapshot, incoming, snapshot.StoredItems)
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var surfaceById = surfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var coreSurfaces = surfaces
            .Select((surface, priority) => ToCoreSurfaceV1155(surface, priority))
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
            var movable = IsTransitionItemMovableV1155(stored, snapshot.StoredItems);
            var options = BuildTransitionOptionsV1155(item, surfaces, snapshot.StoredItems).ToArray();
            if (options.Length == 0)
                movable = false;

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

        var incomingOptions = BuildTransitionOptionsV1155(
                incoming,
                surfaces,
                snapshot.StoredItems)
            .ToArray();
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
        return true;
    }

    private IEnumerable<TransitionEvictionCandidateV1155> EnumerateEvictionCandidatesV1155(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        return snapshot.StoredItems
            .Where(stored => !snapshot.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(stored => !IsInsideLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Select(stored =>
            {
                var item = ResolveItem(stored.Item);
                return item is null
                    ? null
                    : new TransitionEvictionCandidateV1155(stored, MetricsForExisting(item));
            })
            .Where(static value => value is not null)
            .Select(static value => value!)
            .OrderBy(value => value.Metrics, LootMetricsComparer.Instance)
            .ThenBy(value => value.Stored.InstanceId, StringComparer.Ordinal);
    }

    private IEnumerable<RaidSurface> EnumerateTransitionSurfacesV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        GameItem incoming,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var nested = storedItems
            .SelectMany(stored =>
            {
                var owner = ResolveItem(stored.Item);
                var grids = owner?.FarmingGuideData?.StorageGrids;
                if (owner is null || grids is null || grids.Count == 0)
                    return Array.Empty<RaidSurface>();
                return grids
                    .Select((grid, index) => new RaidSurface(
                        stored.Storage,
                        stored.InstanceId,
                        index,
                        grid,
                        $"{DisplayName(owner)} 내부"))
                    .ToArray();
            })
            .ToArray();

        foreach (var surface in nested.Where(surface =>
                     FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                         incoming,
                         surface.Definition.Filters)))
        {
            yield return surface;
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.SecureContainer,
                     FarmingGuideStorageKind.Pockets,
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SpecialSlots,
                 })
        {
            var grids = TransitionRootGridsV1155(snapshot, kind);
            for (var index = 0; index < grids.Count; index++)
            {
                yield return new RaidSurface(
                    kind,
                    null,
                    index,
                    grids[index],
                    TransitionStorageLabelV1155(kind));
            }
        }

        foreach (var surface in nested.Where(surface =>
                     !FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                         incoming,
                         surface.Definition.Filters)))
        {
            yield return surface;
        }
    }

    private FarmingGuideRepackingSurface ToCoreSurfaceV1155(RaidSurface surface, int priority) =>
        new(
            SurfaceId(surface),
            surface.ParentInstanceId,
            surface.Definition.Width,
            surface.Definition.Height,
            priority,
            _reservedCells
                .Where(cell => IsReservedOnSurface(cell, surface))
                .Select((cell, index) => new FarmingGuideGridPlacement(
                    $"__reserved_v1155_{index}",
                    cell.X,
                    cell.Y,
                    1,
                    1))
                .ToArray());

    private IEnumerable<FarmingGuideRepackingOption> BuildTransitionOptionsV1155(
        GameItem item,
        IReadOnlyList<RaidSurface> surfaces,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var preference = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (IsTransitionSurfaceLockedV1155(surface, storedItems) ||
                !FarmingGuideStoragePlacementPolicy.CanStore(
                    surface.Kind,
                    surface.ParentInstanceId,
                    item,
                    surface.Definition.Filters))
            {
                preference++;
                continue;
            }

            var rotations = FarmingGuideStoragePlacementPolicy.SupportsRotation(
                surface.Kind,
                surface.ParentInstanceId,
                item)
                ? new[] { false, true }
                : new[] { false };
            foreach (var rotated in rotations)
            {
                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    surface.Kind,
                    surface.ParentInstanceId,
                    item,
                    rotated);
                if (footprint.Width > surface.Definition.Width || footprint.Height > surface.Definition.Height)
                    continue;
                var id = SurfaceId(surface);
                var key = $"{id}|{footprint.Width}|{footprint.Height}|{rotated}";
                if (!seen.Add(key))
                    continue;
                yield return new FarmingGuideRepackingOption(
                    id,
                    footprint.Width,
                    footprint.Height,
                    rotated,
                    preference);
            }
            preference++;
        }
    }

    private bool IsTransitionItemMovableV1155(
        FarmingGuideStoredItemState stored,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems) =>
        !_lockedCarriers.Contains(stored.Storage) &&
        !IsInsideLockedItemInSnapshot(stored.InstanceId, storedItems) &&
        !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, storedItems);

    private bool IsTransitionSurfaceLockedV1155(
        RaidSurface surface,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        if (surface.ParentInstanceId is null)
            return _lockedCarriers.Contains(surface.Kind);

        var parent = storedItems.FirstOrDefault(value =>
            string.Equals(value.InstanceId, surface.ParentInstanceId, StringComparison.Ordinal));
        return parent is null ||
               _lockedCarriers.Contains(parent.Storage) ||
               IsInsideLockedItemInSnapshot(parent.InstanceId, storedItems);
    }

    private IReadOnlyList<FarmingGuideStorageGridDefinition> TransitionRootGridsV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStorageKind kind)
    {
        if (kind == FarmingGuideStorageKind.Pockets)
            return FarmingGuidePocketLayoutPolicy.StandardGrids;
        if (kind == FarmingGuideStorageKind.SpecialSlots)
        {
            return Enumerable.Range(0, 3)
                .Select(_ => new FarmingGuideStorageGridDefinition(
                    1,
                    1,
                    FarmingGuideItemFilter.Empty))
                .ToArray();
        }

        var state = CarrierStateV1155(snapshot, kind);
        return ResolveItem(state)?.FarmingGuideData?.StorageGrids ?? [];
    }

    private static FarmingGuideItemState? CarrierStateV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStorageKind kind) => kind switch
        {
            FarmingGuideStorageKind.Rig => snapshot.Rig,
            FarmingGuideStorageKind.Backpack => snapshot.Backpack,
            FarmingGuideStorageKind.SecureContainer => snapshot.SecureContainer,
            _ => null,
        };

    private static bool SameRootItemV1155(
        FarmingGuideItemState? left,
        FarmingGuideItemState? right) =>
        left is null
            ? right is null
            : right is not null && string.Equals(left.ItemId, right.ItemId, StringComparison.Ordinal);

    private static string TransitionStorageLabelV1155(FarmingGuideStorageKind kind) => kind switch
    {
        FarmingGuideStorageKind.SecureContainer => "컨테이너",
        FarmingGuideStorageKind.Pockets => "주머니",
        FarmingGuideStorageKind.Rig => "리그",
        FarmingGuideStorageKind.Backpack => "가방",
        FarmingGuideStorageKind.SpecialSlots => "특수 슬롯",
        _ => "보관함",
    };

    private static string NewDisplacedInstanceIdV1155() =>
        $"{V1155DisplacedInstancePrefix}{Guid.NewGuid():N}";

    private sealed record TransitionEvictionCandidateV1155(
        FarmingGuideStoredItemState Stored,
        FarmingGuideLootMetrics Metrics);
}
