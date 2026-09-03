using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private enum StoredPackingOutcomeV1170
    {
        Found,
        NoSolution,
        Indeterminate,
    }

    /// <summary>
    /// Packs the complete selected stored-item pool from scratch. Existing current placement
    /// is ignored for every unlocked movable item. Explicit position locks and ancestors that
    /// would indirectly move a locked descendant remain fixed obstacles.
    ///
    /// Storage surfaces are built from the selected pool itself, not only from the current
    /// snapshot. Consequently a selected existing/incoming container contributes its internal
    /// grids in the same solve, while a removed container contributes no capacity and its
    /// former children must find another legal surface.
    /// </summary>
    private StoredPackingOutcomeV1170 TryPackSelectedStoredPoolV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        string incomingInstanceId,
        IReadOnlyCollection<string> removedInstanceIds,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        var removed = removedInstanceIds.Count == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : removedInstanceIds.ToHashSet(StringComparer.Ordinal);
        var selectedExisting = current.StoredItems
            .Where(value => !removed.Contains(value.InstanceId))
            .ToArray();

        var incomingStored = new FarmingGuideStoredItemState(
            incomingInstanceId,
            incomingState,
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false);
        var selectedAll = selectedExisting.Append(incomingStored).ToArray();

        var surfaces = BuildGlobalPackingSurfacesV1170(current, selectedAll).ToArray();
        if (surfaces.Length == 0)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.NoSolution;
        }

        var surfaceById = surfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var protectedIds = selectedExisting
            .Where(value => IsPositionProtectedForGlobalPackingV1170(value.InstanceId, current.StoredItems))
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);

        var coreSurfaces = new List<FarmingGuideRepackingSurface>(surfaces.Length);
        for (var priority = 0; priority < surfaces.Length; priority++)
        {
            var surface = surfaces[priority];
            var fixedObstacles = new List<FarmingGuideGridPlacement>();
            fixedObstacles.AddRange(_reservedCells
                .Where(cell => IsReservedOnSurface(cell, surface))
                .Select((cell, index) => new FarmingGuideGridPlacement(
                    $"__reserved_global_v1170_{priority}_{index}",
                    cell.X,
                    cell.Y,
                    1,
                    1)));

            foreach (var stored in selectedExisting.Where(value =>
                         protectedIds.Contains(value.InstanceId) &&
                         value.GridIndex == surface.GridIndex &&
                         IsOnStorageSurface(value, surface.Kind, surface.ParentInstanceId)))
            {
                var item = ResolveItem(stored.Item);
                if (item is null)
                {
                    proposed = current;
                    return StoredPackingOutcomeV1170.Indeterminate;
                }

                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    stored.Storage,
                    stored.ParentInstanceId,
                    item,
                    stored.Rotated);
                fixedObstacles.Add(new FarmingGuideGridPlacement(
                    stored.InstanceId,
                    stored.X,
                    stored.Y,
                    footprint.Width,
                    footprint.Height));
            }

            coreSurfaces.Add(new FarmingGuideRepackingSurface(
                SurfaceId(surface),
                surface.ParentInstanceId,
                surface.Definition.Width,
                surface.Definition.Height,
                priority,
                fixedObstacles));
        }

        var movable = new List<FarmingGuideGlobalPackingItem>();
        foreach (var stored in selectedExisting)
        {
            if (protectedIds.Contains(stored.InstanceId))
                continue;

            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                proposed = current;
                return StoredPackingOutcomeV1170.Indeterminate;
            }

            var options = BuildTransitionOptionsV1163(item, surfaces).ToArray();
            if (options.Length == 0)
            {
                proposed = current;
                return StoredPackingOutcomeV1170.NoSolution;
            }
            movable.Add(new FarmingGuideGlobalPackingItem(stored.InstanceId, options));
        }

        var incomingOptions = BuildTransitionOptionsV1163(incoming, surfaces).ToArray();
        if (incomingOptions.Length == 0)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.NoSolution;
        }
        movable.Add(new FarmingGuideGlobalPackingItem(incomingInstanceId, incomingOptions));

        var plan = FarmingGuideGlobalPackingPlanner.Plan(coreSurfaces, movable);
        if (plan.Status == FarmingGuideGlobalPackingStatus.BudgetExceeded)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.Indeterminate;
        }
        if (!plan.Found)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.NoSolution;
        }

        var placementById = plan.Placements.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var result = new List<FarmingGuideStoredItemState>(selectedExisting.Length + 1);
        foreach (var stored in selectedExisting)
        {
            if (protectedIds.Contains(stored.InstanceId))
            {
                result.Add(stored);
                continue;
            }

            if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var target))
            {
                proposed = current;
                return StoredPackingOutcomeV1170.Indeterminate;
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

        if (!placementById.TryGetValue(incomingInstanceId, out var incomingPlacement) ||
            !surfaceById.TryGetValue(incomingPlacement.SurfaceId, out var incomingTarget))
        {
            proposed = current;
            return StoredPackingOutcomeV1170.Indeterminate;
        }

        result.Add(incomingStored with
        {
            Storage = incomingTarget.Kind,
            GridIndex = incomingTarget.GridIndex,
            X = incomingPlacement.X,
            Y = incomingPlacement.Y,
            Rotated = incomingPlacement.Rotated,
            ParentInstanceId = incomingTarget.ParentInstanceId,
        });

        if (!TryNormalizeRootStorageKinds(result, out var normalized))
        {
            proposed = current;
            return StoredPackingOutcomeV1170.Indeterminate;
        }

        proposed = current with { StoredItems = normalized };
        if (!PreservesLockedItemPlacementV1164(current, proposed))
        {
            proposed = current;
            return StoredPackingOutcomeV1170.Indeterminate;
        }

        return StoredPackingOutcomeV1170.Found;
    }

    private IEnumerable<RaidSurface> BuildGlobalPackingSurfacesV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<FarmingGuideStoredItemState> selectedStored)
    {
        // Keep the established root surface order only for deterministic placement ordering;
        // it is not a farming priority.
        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.SecureContainer,
                     FarmingGuideStorageKind.Pockets,
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SpecialSlots,
                 })
        {
            var grids = TransitionRootGridsV1163(current, kind);
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

        foreach (var stored in selectedStored.OrderBy(value => value.InstanceId, StringComparer.Ordinal))
        {
            var owner = ResolveItem(stored.Item);
            var grids = owner?.FarmingGuideData?.StorageGrids;
            if (owner is null || grids is null || grids.Count == 0)
                continue;

            for (var index = 0; index < grids.Count; index++)
            {
                yield return new RaidSurface(
                    stored.Storage,
                    stored.InstanceId,
                    index,
                    grids[index],
                    $"{DisplayName(owner)} 내부");
            }
        }
    }

    private bool IsPositionProtectedForGlobalPackingV1170(
        string instanceId,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems) =>
        _lockedItemInstanceIds.Contains(instanceId) ||
        SubtreeContainsLockedItemInSnapshot(instanceId, storedItems);
}
