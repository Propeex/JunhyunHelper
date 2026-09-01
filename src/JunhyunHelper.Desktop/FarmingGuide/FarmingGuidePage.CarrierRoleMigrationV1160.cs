using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int ReservationSearchLimitV1160 = 4096;

    /// <summary>
    /// Carrier replacement preserves protected meaning rather than old coordinates. Exact
    /// locked item instances stay in the modeled inventory and may move inside the new
    /// carrier. Root reserved cells are grouped into connected shapes and translated to any
    /// legal grid position that lets all existing contents fit. If no such layout exists,
    /// the carrier upgrade is forbidden.
    /// </summary>
    private bool TryBuildCarrierUpgradeRulebookV1160(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming,
        out RaidRecommendation recommendation,
        out bool handled)
    {
        handled = false;

        var incomingIsRig = FarmingGuideCompatibility.IsStorageCarrierCompatible(
            FarmingGuideStorageKind.Rig,
            incoming);
        var incomingIsArmoredRig = incomingIsRig && incoming.FarmingGuideData?.IsArmoredRig == true;

        // Body armor + ordinary rig -> superior armored rig remains one atomic transition.
        if (incomingIsArmoredRig &&
            current.Equipment.TryGetValue(FarmingGuideEquipmentSlot.BodyArmor, out var bodyArmorState) &&
            current.Rig is { } currentRigState)
        {
            var currentRig = ResolveItem(currentRigState);
            var bodyArmor = ResolveItem(bodyArmorState);
            if (currentRig is not null && bodyArmor is not null &&
                currentRig.FarmingGuideData?.IsArmoredRig != true &&
                FarmingGuideEquipmentUpgradePolicy.IsBodyArmorToArmoredRigUpgrade(incoming, bodyArmor))
            {
                handled = true;
                if (_lockedCarriers.Contains(FarmingGuideStorageKind.Rig) ||
                    _lockedEquipmentSlots.Contains(FarmingGuideEquipmentSlot.BodyArmor) ||
                    !IsCarrierConflictFree(
                        FarmingGuideStorageKind.Rig,
                        incoming,
                        current,
                        removingEquipment: FarmingGuideEquipmentSlot.BodyArmor) ||
                    !TryPackCarrierContentsAndRolesV1160(
                        FarmingGuideStorageKind.Rig,
                        incoming,
                        current,
                        out var packedStored,
                        out var migratedReserved,
                        out var movedCount))
                {
                    recommendation = default!;
                    return false;
                }

                var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(current.Equipment);
                equipment.Remove(FarmingGuideEquipmentSlot.BodyArmor);
                var proposed = current with
                {
                    Equipment = equipment,
                    Rig = FarmingGuideItemState.Create(incoming.Id),
                    StoredItems = packedStored,
                };
                _plannedLocksOverrideV1160 = LocksWithMigratedRootReservationV1160(
                    FarmingGuideStorageKind.Rig,
                    migratedReserved);
                recommendation = new RaidRecommendation(
                    movedCount > 0 ? "방탄 리그로 교체 · 내부 재배치" : "방탄 리그로 교체",
                    FarmingGuideInstructionAction.ReplaceEquip,
                    proposed);
                return true;
            }
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            var existingState = kind switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                FarmingGuideStorageKind.SecureContainer => current.SecureContainer,
                _ => null,
            };
            if (existingState is null ||
                !FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, incoming))
            {
                continue;
            }

            var existing = ResolveItem(existingState);
            if (existing is null || !FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(kind, incoming, existing))
                continue;

            handled = true;
            if (_lockedCarriers.Contains(kind) ||
                !IsCarrierConflictFree(kind, incoming, current, removingEquipment: null) ||
                !TryPackCarrierContentsAndRolesV1160(
                    kind,
                    incoming,
                    current,
                    out var packedStored,
                    out var migratedReserved,
                    out var movedCount))
            {
                recommendation = default!;
                return false;
            }

            var proposed = kind switch
            {
                FarmingGuideStorageKind.Rig => current with
                {
                    Rig = FarmingGuideItemState.Create(incoming.Id),
                    StoredItems = packedStored,
                },
                FarmingGuideStorageKind.Backpack => current with
                {
                    Backpack = FarmingGuideItemState.Create(incoming.Id),
                    StoredItems = packedStored,
                },
                FarmingGuideStorageKind.SecureContainer => current with
                {
                    SecureContainer = FarmingGuideItemState.Create(incoming.Id),
                    StoredItems = packedStored,
                },
                _ => current,
            };
            _plannedLocksOverrideV1160 = LocksWithMigratedRootReservationV1160(kind, migratedReserved);
            recommendation = new RaidRecommendation(
                movedCount > 0
                    ? $"{CarrierLabel(kind)} 업그레이드 · 내부 재배치"
                    : $"{CarrierLabel(kind)} 업그레이드",
                FarmingGuideInstructionAction.ReplaceEquip,
                proposed);
            return true;
        }

        recommendation = default!;
        return false;
    }

    private FarmingGuideLockState LocksWithMigratedRootReservationV1160(
        FarmingGuideStorageKind kind,
        IReadOnlyList<FarmingGuideLockedCell> migrated)
    {
        var current = BuildLockState();
        return current with
        {
            ReservedCells = current.ReservedCells
                .Where(value => value.Storage != kind || value.ParentInstanceId is not null)
                .Concat(migrated)
                .Distinct()
                .OrderBy(value => value.Storage)
                .ThenBy(value => value.GridIndex)
                .ThenBy(value => value.Y)
                .ThenBy(value => value.X)
                .ToArray(),
        };
    }

    private bool TryPackCarrierContentsAndRolesV1160(
        FarmingGuideStorageKind kind,
        GameItem incomingCarrier,
        FarmingGuideLoadoutSnapshot current,
        out IReadOnlyList<FarmingGuideStoredItemState> packedStored,
        out IReadOnlyList<FarmingGuideLockedCell> migratedReserved,
        out int movedCount)
    {
        var grids = incomingCarrier.FarmingGuideData?.StorageGrids ?? [];
        if (grids.Count == 0)
        {
            packedStored = current.StoredItems;
            migratedReserved = [];
            movedCount = 0;
            return !current.StoredItems.Any(value => value.ParentInstanceId is null && value.Storage == kind) &&
                   !_reservedCells.Any(value => value.Storage == kind && value.ParentInstanceId is null);
        }

        var roots = current.StoredItems
            .Where(value => value.ParentInstanceId is null && value.Storage == kind)
            .OrderBy(value => value.InstanceId, StringComparer.Ordinal)
            .ToArray();
        var components = BuildReservedComponentsV1160(kind);
        var selected = new List<FarmingGuideLockedCell>();
        var occupied = new HashSet<(int Grid, int X, int Y)>();
        var attempts = 0;
        IReadOnlyList<FarmingGuideStoredItemState>? foundPacked = null;
        IReadOnlyList<FarmingGuideLockedCell>? foundReserved = null;
        var foundMovedCount = 0;

        bool Search(int componentIndex)
        {
            if (++attempts > ReservationSearchLimitV1160)
                return false;

            if (componentIndex >= components.Count)
            {
                if (!TryPackRootsAroundReservationV1160(
                        kind,
                        grids,
                        roots,
                        current,
                        selected,
                        out var candidatePacked,
                        out var candidateMovedCount))
                {
                    return false;
                }

                foundPacked = candidatePacked;
                foundReserved = selected.ToArray();
                foundMovedCount = candidateMovedCount;
                return true;
            }

            var component = components[componentIndex];
            for (var gridIndex = 0; gridIndex < grids.Count; gridIndex++)
            {
                var grid = grids[gridIndex];
                for (var y = 0; y <= grid.Height - component.Height; y++)
                {
                    for (var x = 0; x <= grid.Width - component.Width; x++)
                    {
                        var cells = component.Cells
                            .Select(cell => (Grid: gridIndex, X: x + cell.X, Y: y + cell.Y))
                            .ToArray();
                        if (cells.Any(occupied.Contains))
                            continue;

                        foreach (var cell in cells)
                        {
                            occupied.Add(cell);
                            selected.Add(new FarmingGuideLockedCell(kind, cell.Grid, cell.X, cell.Y));
                        }
                        if (Search(componentIndex + 1))
                            return true;
                        selected.RemoveRange(selected.Count - cells.Length, cells.Length);
                        foreach (var cell in cells)
                            occupied.Remove(cell);
                    }
                }
            }
            return false;
        }

        var success = Search(0);
        packedStored = success && foundPacked is not null ? foundPacked : current.StoredItems;
        migratedReserved = success && foundReserved is not null ? foundReserved : [];
        movedCount = success ? foundMovedCount : 0;
        return success;
    }

    private bool TryPackRootsAroundReservationV1160(
        FarmingGuideStorageKind kind,
        IReadOnlyList<FarmingGuideStorageGridDefinition> grids,
        IReadOnlyList<FarmingGuideStoredItemState> roots,
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<FarmingGuideLockedCell> reserved,
        out IReadOnlyList<FarmingGuideStoredItemState> packedStored,
        out int movedCount)
    {
        static string SurfaceId(int gridIndex) => $"carrier-grid:{gridIndex}";
        var surfaces = grids
            .Select((grid, index) => new FarmingGuideCarrierPackingSurface(
                SurfaceId(index),
                grid.Width,
                grid.Height,
                index,
                reserved
                    .Where(cell => cell.GridIndex == index)
                    .Select((cell, obstacleIndex) => new FarmingGuideGridPlacement(
                        $"__reserved__{obstacleIndex}", cell.X, cell.Y, 1, 1))
                    .ToArray()))
            .ToArray();

        var items = new List<FarmingGuideCarrierPackingItem>(roots.Count);
        foreach (var root in roots)
        {
            var item = ResolveItem(root.Item);
            if (item is null)
            {
                packedStored = current.StoredItems;
                movedCount = 0;
                return false;
            }

            var options = new List<FarmingGuideCarrierPackingOption>();
            for (var gridIndex = 0; gridIndex < grids.Count; gridIndex++)
            {
                var grid = grids[gridIndex];
                if (!FarmingGuideStoragePlacementPolicy.CanStore(kind, null, item, grid.Filters))
                    continue;
                var rotations = FarmingGuideStoragePlacementPolicy.SupportsRotation(kind, null, item)
                    ? new[] { false, true }
                    : new[] { false };
                foreach (var rotated in rotations)
                {
                    var footprint = FarmingGuideStoragePlacementPolicy.Footprint(kind, null, item, rotated);
                    if (footprint.Width > grid.Width || footprint.Height > grid.Height)
                        continue;
                    options.Add(new FarmingGuideCarrierPackingOption(
                        SurfaceId(gridIndex),
                        footprint.Width,
                        footprint.Height,
                        rotated,
                        gridIndex * 2 + (rotated ? 1 : 0)));
                }
            }

            if (options.Count == 0)
            {
                packedStored = current.StoredItems;
                movedCount = 0;
                return false;
            }

            // A lock protects the instance from removal, not its old X/Y coordinate. Every
            // existing root is included in the pack, so no locked item can disappear.
            items.Add(new FarmingGuideCarrierPackingItem(
                root.InstanceId,
                root.GridIndex >= 0 && root.GridIndex < grids.Count ? SurfaceId(root.GridIndex) : null,
                root.X,
                root.Y,
                root.Rotated,
                Fixed: false,
                options));
        }

        var plan = FarmingGuideCarrierPackingPlanner.TryPack(surfaces, items);
        if (plan is null)
        {
            packedStored = current.StoredItems;
            movedCount = 0;
            return false;
        }

        var placementById = plan.Placements.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var result = new List<FarmingGuideStoredItemState>(current.StoredItems.Count);
        foreach (var stored in current.StoredItems)
        {
            if (stored.ParentInstanceId is null && stored.Storage == kind)
            {
                if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                    !int.TryParse(placement.SurfaceId.AsSpan("carrier-grid:".Length), out var gridIndex))
                {
                    packedStored = current.StoredItems;
                    movedCount = 0;
                    return false;
                }
                result.Add(stored with
                {
                    GridIndex = gridIndex,
                    X = placement.X,
                    Y = placement.Y,
                    Rotated = placement.Rotated,
                });
            }
            else
            {
                result.Add(stored);
            }
        }

        packedStored = result;
        movedCount = plan.MovedCount;
        return true;
    }

    private IReadOnlyList<ReservedShapeV1160> BuildReservedComponentsV1160(FarmingGuideStorageKind kind)
    {
        var remaining = _reservedCells
            .Where(value => value.Storage == kind && value.ParentInstanceId is null)
            .OrderBy(value => value.GridIndex)
            .ThenBy(value => value.Y)
            .ThenBy(value => value.X)
            .ToHashSet();
        var result = new List<ReservedShapeV1160>();

        while (remaining.Count > 0)
        {
            var first = remaining.OrderBy(value => value.GridIndex)
                .ThenBy(value => value.Y)
                .ThenBy(value => value.X)
                .First();
            var gridIndex = first.GridIndex;
            var queue = new Queue<FarmingGuideLockedCell>();
            var component = new List<FarmingGuideLockedCell>();
            remaining.Remove(first);
            queue.Enqueue(first);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                component.Add(cell);
                foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    var adjacent = new FarmingGuideLockedCell(kind, gridIndex, cell.X + dx, cell.Y + dy);
                    if (remaining.Remove(adjacent))
                        queue.Enqueue(adjacent);
                }
            }

            var minX = component.Min(value => value.X);
            var minY = component.Min(value => value.Y);
            var normalized = component
                .Select(value => (X: value.X - minX, Y: value.Y - minY))
                .OrderBy(value => value.Y)
                .ThenBy(value => value.X)
                .ToArray();
            result.Add(new ReservedShapeV1160(
                normalized,
                normalized.Max(value => value.X) + 1,
                normalized.Max(value => value.Y) + 1));
        }

        return result;
    }

    private sealed record ReservedShapeV1160(
        IReadOnlyList<(int X, int Y)> Cells,
        int Width,
        int Height);
}
