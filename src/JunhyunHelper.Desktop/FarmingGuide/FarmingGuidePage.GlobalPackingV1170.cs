using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private enum GlobalSurfaceRoleV1170
    {
        Storage,
        Equipment,
        Carrier,
    }

    private enum GlobalStorageSurfaceModeV1170
    {
        Root,
        Nested,
        EquippedCarrierGrid,
    }

    private sealed record GlobalSurfaceV1170(
        string Id,
        GlobalSurfaceRoleV1170 Role,
        int Priority,
        FarmingGuideStorageKind StorageKind = FarmingGuideStorageKind.Pockets,
        int GridIndex = 0,
        FarmingGuideStorageGridDefinition? Definition = null,
        string? OwnerInstanceId = null,
        string? StateParentInstanceId = null,
        GlobalStorageSurfaceModeV1170 StorageMode = GlobalStorageSurfaceModeV1170.Root,
        FarmingGuideEquipmentSlot? EquipmentSlot = null,
        FarmingGuideStorageKind? CarrierKind = null);

    private FarmingGuideGlobalPackingStatus TryPackGlobalSelectionV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalRootV1170> selected,
        GlobalRootV1170 incomingRoot,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        proposed = current;
        if (selected.Count == 0)
            return FarmingGuideGlobalPackingStatus.NoSolution;

        var rootsById = selected.ToDictionary(root => root.InstanceId, StringComparer.Ordinal);
        var surfaces = BuildGlobalSurfacesV1170(selected);
        var surfaceById = surfaces.ToDictionary(surface => surface.Id, StringComparer.Ordinal);
        if (surfaces.Count == 0)
            return FarmingGuideGlobalPackingStatus.NoSolution;

        var packingSurfaces = surfaces.Select(surface =>
            new FarmingGuideGlobalPackingSurface(
                surface.Id,
                surface.OwnerInstanceId,
                surface.Role == GlobalSurfaceRoleV1170.Storage
                    ? Math.Max(1, surface.Definition?.Width ?? 1)
                    : 1,
                surface.Role == GlobalSurfaceRoleV1170.Storage
                    ? Math.Max(1, surface.Definition?.Height ?? 1)
                    : 1,
                surface.Priority,
                BuildReservedObstaclesV1170(surface))).ToArray();

        var packingItems = new List<FarmingGuideGlobalPackingItem>(selected.Count);
        foreach (var root in selected)
        {
            var options = BuildGlobalPackingOptionsV1170(root, surfaces).ToArray();
            if (options.Length == 0)
                return FarmingGuideGlobalPackingStatus.NoSolution;

            var currentPlacement = BuildCurrentGlobalPlacementV1170(current, root, surfaceById);
            packingItems.Add(new FarmingGuideGlobalPackingItem(
                root.InstanceId,
                root.Fixed,
                currentPlacement,
                options));
        }

        EnsureWeightSettingsLoadedV1160();
        var maximumAdmissibleWeight = FarmingGuideWeightPolicy.MaximumCarryWeightKg(_weightSettingsV1160);

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(
            packingSurfaces,
            packingItems,
            placements => ValidateGlobalFinalPlacementV1170(
                placements,
                rootsById,
                surfaceById,
                maximumAdmissibleWeight));
        if (!result.Found)
            return result.Status;

        if (!TryBuildGlobalSnapshotV1170(
                current,
                selected,
                result.Placements,
                surfaceById,
                out proposed))
        {
            return FarmingGuideGlobalPackingStatus.NoSolution;
        }

        return FarmingGuideGlobalPackingStatus.Found;
    }

    private IReadOnlyList<GlobalSurfaceV1170> BuildGlobalSurfacesV1170(
        IReadOnlyList<GlobalRootV1170> selected)
    {
        var surfaces = new List<GlobalSurfaceV1170>();

        for (var index = 0; index < _pocketGrids.Count; index++)
        {
            surfaces.Add(new GlobalSurfaceV1170(
                RootStorageSurfaceIdV1170(FarmingGuideStorageKind.Pockets, index),
                GlobalSurfaceRoleV1170.Storage,
                Priority: 30 + index,
                StorageKind: FarmingGuideStorageKind.Pockets,
                GridIndex: index,
                Definition: _pocketGrids[index]));
        }

        for (var index = 0; index < 3; index++)
        {
            surfaces.Add(new GlobalSurfaceV1170(
                RootStorageSurfaceIdV1170(FarmingGuideStorageKind.SpecialSlots, index),
                GlobalSurfaceRoleV1170.Storage,
                Priority: 70 + index,
                StorageKind: FarmingGuideStorageKind.SpecialSlots,
                GridIndex: index,
                Definition: new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty)));
        }

        foreach (var slot in V1170EquipmentSlots)
        {
            surfaces.Add(new GlobalSurfaceV1170(
                EquipmentSurfaceIdV1170(slot),
                GlobalSurfaceRoleV1170.Equipment,
                Priority: 10 + (int)slot,
                EquipmentSlot: slot));
        }

        foreach (var kind in V1170CarrierKinds)
        {
            surfaces.Add(new GlobalSurfaceV1170(
                CarrierSurfaceIdV1170(kind),
                GlobalSurfaceRoleV1170.Carrier,
                Priority: 10 + (int)kind,
                CarrierKind: kind));
        }

        foreach (var root in selected)
        {
            var grids = root.Item.FarmingGuideData?.StorageGrids;
            if (grids is null || grids.Count == 0)
                continue;

            // Any retained storage item may expose its grids while stored inside another
            // legal storage surface.
            for (var index = 0; index < grids.Count; index++)
            {
                surfaces.Add(new GlobalSurfaceV1170(
                    NestedSurfaceIdV1170(root.InstanceId, index),
                    GlobalSurfaceRoleV1170.Storage,
                    Priority: 80 + index,
                    StorageKind: FarmingGuideStorageKind.Pockets,
                    GridIndex: index,
                    Definition: grids[index],
                    OwnerInstanceId: root.InstanceId,
                    StateParentInstanceId: root.InstanceId,
                    StorageMode: GlobalStorageSurfaceModeV1170.Nested));
            }

            // Carrier grids use the existing root-state representation (ParentInstanceId null)
            // only when this exact root is equipped in the matching carrier slot.
            foreach (var kind in V1170CarrierKinds.Where(kind =>
                         FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, root.Item)))
            {
                for (var index = 0; index < grids.Count; index++)
                {
                    surfaces.Add(new GlobalSurfaceV1170(
                        CarrierGridSurfaceIdV1170(root.InstanceId, kind, index),
                        GlobalSurfaceRoleV1170.Storage,
                        Priority: RootStoragePreferenceV1170(kind) + index,
                        StorageKind: kind,
                        GridIndex: index,
                        Definition: grids[index],
                        OwnerInstanceId: root.InstanceId,
                        StateParentInstanceId: null,
                        StorageMode: GlobalStorageSurfaceModeV1170.EquippedCarrierGrid,
                        CarrierKind: kind));
                }
            }
        }

        return surfaces;
    }

    private IReadOnlyList<FarmingGuideGridPlacement> BuildReservedObstaclesV1170(
        GlobalSurfaceV1170 surface)
    {
        if (surface.Role != GlobalSurfaceRoleV1170.Storage)
            return [];

        IEnumerable<FarmingGuideLockedCell> matching = surface.StorageMode switch
        {
            GlobalStorageSurfaceModeV1170.Nested => _reservedCells.Where(cell =>
                cell.GridIndex == surface.GridIndex &&
                string.Equals(cell.ParentInstanceId, surface.StateParentInstanceId, StringComparison.Ordinal)),
            _ => _reservedCells.Where(cell =>
                cell.ParentInstanceId is null &&
                cell.Storage == surface.StorageKind &&
                cell.GridIndex == surface.GridIndex),
        };

        return matching.Select((cell, index) => new FarmingGuideGridPlacement(
            $"__v1170_reserved_{surface.Id}_{index}",
            cell.X,
            cell.Y,
            1,
            1)).ToArray();
    }

    private IEnumerable<FarmingGuideGlobalPackingOption> BuildGlobalPackingOptionsV1170(
        GlobalRootV1170 root,
        IReadOnlyList<GlobalSurfaceV1170> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface.Role == GlobalSurfaceRoleV1170.Equipment)
            {
                if (surface.EquipmentSlot is { } slot &&
                    FarmingGuideCompatibility.IsEquipmentSlotCompatible(slot, root.Item))
                {
                    yield return new FarmingGuideGlobalPackingOption(
                        surface.Id, 1, 1, false, surface.Priority);
                }
                continue;
            }

            if (surface.Role == GlobalSurfaceRoleV1170.Carrier)
            {
                if (surface.CarrierKind is { } kind &&
                    FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, root.Item))
                {
                    yield return new FarmingGuideGlobalPackingOption(
                        surface.Id, 1, 1, false, surface.Priority);
                }
                continue;
            }

            var parentId = surface.StateParentInstanceId;
            if (surface.Definition is not { } definition ||
                !FarmingGuideStoragePlacementPolicy.CanStore(
                    surface.StorageKind,
                    parentId,
                    root.Item,
                    definition.Filters))
            {
                continue;
            }

            var preference = surface.StorageMode == GlobalStorageSurfaceModeV1170.Nested &&
                             FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                                 root.Item,
                                 definition.Filters)
                ? 0
                : surface.Priority;
            var rotations = FarmingGuideStoragePlacementPolicy.SupportsRotation(
                surface.StorageKind,
                parentId,
                root.Item)
                ? new[] { false, true }
                : new[] { false };
            foreach (var rotated in rotations)
            {
                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    surface.StorageKind,
                    parentId,
                    root.Item,
                    rotated);
                yield return new FarmingGuideGlobalPackingOption(
                    surface.Id,
                    footprint.Width,
                    footprint.Height,
                    rotated,
                    preference);
            }
        }
    }

    private FarmingGuideGlobalPackingPlacement? BuildCurrentGlobalPlacementV1170(
        FarmingGuideLoadoutSnapshot current,
        GlobalRootV1170 root,
        IReadOnlyDictionary<string, GlobalSurfaceV1170> surfaces)
    {
        string? surfaceId = null;
        int x = 0;
        int y = 0;
        var width = 1;
        var height = 1;
        var rotated = false;

        switch (root.Origin)
        {
            case GlobalRootOriginV1170.Stored when root.StoredSource is { } stored:
                x = stored.X;
                y = stored.Y;
                rotated = stored.Rotated;
                if (!string.IsNullOrWhiteSpace(stored.ParentInstanceId))
                {
                    surfaceId = NestedSurfaceIdV1170(stored.ParentInstanceId, stored.GridIndex);
                }
                else if (stored.Storage is FarmingGuideStorageKind.Pockets or FarmingGuideStorageKind.SpecialSlots)
                {
                    surfaceId = RootStorageSurfaceIdV1170(stored.Storage, stored.GridIndex);
                }
                else if (stored.Storage is FarmingGuideStorageKind.Rig or
                         FarmingGuideStorageKind.Backpack or
                         FarmingGuideStorageKind.SecureContainer)
                {
                    var ownerId = CarrierRootIdV1170(stored.Storage);
                    surfaceId = CarrierGridSurfaceIdV1170(ownerId, stored.Storage, stored.GridIndex);
                }

                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    stored.Storage,
                    stored.ParentInstanceId,
                    root.Item,
                    stored.Rotated);
                width = footprint.Width;
                height = footprint.Height;
                break;

            case GlobalRootOriginV1170.Equipment when root.EquipmentSlot is { } slot:
                surfaceId = EquipmentSurfaceIdV1170(slot);
                break;

            case GlobalRootOriginV1170.Carrier when root.CarrierKind is { } kind:
                surfaceId = CarrierSurfaceIdV1170(kind);
                break;
        }

        return surfaceId is not null && surfaces.ContainsKey(surfaceId)
            ? new FarmingGuideGlobalPackingPlacement(
                root.InstanceId,
                surfaceId,
                x,
                y,
                width,
                height,
                rotated)
            : null;
    }

    private bool ValidateGlobalFinalPlacementV1170(
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement> placements,
        IReadOnlyDictionary<string, GlobalRootV1170> roots,
        IReadOnlyDictionary<string, GlobalSurfaceV1170> surfaces,
        decimal maximumAdmissibleWeight)
    {
        // Container capacity is conditional on how the owner itself is retained. A nested
        // surface is real only while its owner is stored; a root carrier-grid surface is real
        // only while that exact owner occupies the matching carrier slot.
        foreach (var placement in placements.Values)
        {
            if (!surfaces.TryGetValue(placement.SurfaceId, out var surface) ||
                surface.Role != GlobalSurfaceRoleV1170.Storage ||
                string.IsNullOrWhiteSpace(surface.OwnerInstanceId))
            {
                continue;
            }
            if (!placements.TryGetValue(surface.OwnerInstanceId, out var ownerPlacement) ||
                !surfaces.TryGetValue(ownerPlacement.SurfaceId, out var ownerSurface))
            {
                return false;
            }

            if (surface.StorageMode == GlobalStorageSurfaceModeV1170.Nested &&
                ownerSurface.Role != GlobalSurfaceRoleV1170.Storage)
            {
                return false;
            }
            if (surface.StorageMode == GlobalStorageSurfaceModeV1170.EquippedCarrierGrid &&
                (ownerSurface.Role != GlobalSurfaceRoleV1170.Carrier ||
                 ownerSurface.CarrierKind != surface.CarrierKind))
            {
                return false;
            }
        }

        var topLevel = placements.Values
            .Select(placement =>
            {
                if (!surfaces.TryGetValue(placement.SurfaceId, out var surface) ||
                    surface.Role == GlobalSurfaceRoleV1170.Storage ||
                    !roots.TryGetValue(placement.InstanceId, out var root))
                {
                    return (Root: (GlobalRootV1170?)null, Surface: (GlobalSurfaceV1170?)null);
                }
                return (Root: root, Surface: surface);
            })
            .Where(value => value.Root is not null && value.Surface is not null)
            .Select(value => (Root: value.Root!, Surface: value.Surface!))
            .ToArray();

        for (var left = 0; left < topLevel.Length; left++)
        {
            for (var right = left + 1; right < topLevel.Length; right++)
            {
                if (FarmingGuideCompatibility.ItemsConflict(
                        topLevel[left].Root.Item,
                        topLevel[right].Root.Item))
                {
                    return false;
                }
            }
        }

        var bodyArmorOccupied = topLevel.Any(value =>
            value.Surface.EquipmentSlot == FarmingGuideEquipmentSlot.BodyArmor);
        var equippedRig = topLevel.FirstOrDefault(value =>
            value.Surface.CarrierKind == FarmingGuideStorageKind.Rig).Root;
        if (bodyArmorOccupied && equippedRig?.Item.FarmingGuideData?.IsArmoredRig == true)
            return false;

        var headsetOccupied = topLevel.Any(value =>
            value.Surface.EquipmentSlot == FarmingGuideEquipmentSlot.Headset);
        var equippedHelmet = topLevel.FirstOrDefault(value =>
            value.Surface.EquipmentSlot == FarmingGuideEquipmentSlot.Helmet).Root;
        if (headsetOccupied && equippedHelmet?.Item.FarmingGuideData?.BlocksHeadphones == true)
            return false;

        decimal totalWeight = 0m;
        foreach (var placement in placements.Values)
        {
            if (!roots.TryGetValue(placement.InstanceId, out var root) ||
                !surfaces.TryGetValue(placement.SurfaceId, out var surface))
            {
                return false;
            }

            if (surface.Role == GlobalSurfaceRoleV1170.Equipment &&
                surface.EquipmentSlot is { } slot &&
                !FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(slot, _weightSettingsV1160))
            {
                continue;
            }
            totalWeight += FarmingGuideWeightPolicy.ItemWeightKg(root.Item, root.Quantity);
        }

        return totalWeight <= maximumAdmissibleWeight;
    }

    private bool TryBuildGlobalSnapshotV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalRootV1170> selected,
        IReadOnlyList<FarmingGuideGlobalPackingPlacement> placements,
        IReadOnlyDictionary<string, GlobalSurfaceV1170> surfaces,
        out FarmingGuideLoadoutSnapshot snapshot)
    {
        var roots = selected.ToDictionary(root => root.InstanceId, StringComparer.Ordinal);
        var placementById = placements.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var equipment = current.Equipment
            .Where(pair => pair.Key is FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        FarmingGuideItemState? rig = null;
        FarmingGuideItemState? backpack = null;
        FarmingGuideItemState? secure = null;
        var stored = new List<FarmingGuideStoredItemState>();

        foreach (var root in selected)
        {
            if (!placementById.TryGetValue(root.InstanceId, out var placement) ||
                !surfaces.TryGetValue(placement.SurfaceId, out var surface))
            {
                snapshot = current;
                return false;
            }

            switch (surface.Role)
            {
                case GlobalSurfaceRoleV1170.Equipment when surface.EquipmentSlot is { } slot:
                    equipment[slot] = root.State;
                    break;

                case GlobalSurfaceRoleV1170.Carrier when surface.CarrierKind is { } kind:
                    switch (kind)
                    {
                        case FarmingGuideStorageKind.Rig:
                            rig = root.State;
                            break;
                        case FarmingGuideStorageKind.Backpack:
                            backpack = root.State;
                            break;
                        case FarmingGuideStorageKind.SecureContainer:
                            secure = root.State;
                            break;
                    }
                    break;

                case GlobalSurfaceRoleV1170.Storage:
                    if (!TryResolveStoredKindV1170(
                            root.InstanceId,
                            placementById,
                            surfaces,
                            out var storageKind))
                    {
                        snapshot = current;
                        return false;
                    }
                    stored.Add(new FarmingGuideStoredItemState(
                        root.InstanceId,
                        root.State,
                        storageKind,
                        surface.GridIndex,
                        placement.X,
                        placement.Y,
                        placement.Rotated,
                        surface.StateParentInstanceId,
                        root.Quantity));
                    break;
            }
        }

        snapshot = new FarmingGuideLoadoutSnapshot(
            equipment,
            rig,
            backpack,
            secure,
            stored);
        return true;
    }

    private static bool TryResolveStoredKindV1170(
        string instanceId,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement> placements,
        IReadOnlyDictionary<string, GlobalSurfaceV1170> surfaces,
        out FarmingGuideStorageKind kind)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = instanceId;
        while (seen.Add(current) &&
               placements.TryGetValue(current, out var placement) &&
               surfaces.TryGetValue(placement.SurfaceId, out var surface))
        {
            if (surface.Role == GlobalSurfaceRoleV1170.Carrier && surface.CarrierKind is { } carrier)
            {
                kind = carrier;
                return true;
            }
            if (surface.Role != GlobalSurfaceRoleV1170.Storage)
                break;
            if (surface.StorageMode != GlobalStorageSurfaceModeV1170.Nested ||
                string.IsNullOrWhiteSpace(surface.OwnerInstanceId))
            {
                kind = surface.StorageKind;
                return true;
            }
            current = surface.OwnerInstanceId;
        }

        kind = default;
        return false;
    }

    private static int RootStoragePreferenceV1170(FarmingGuideStorageKind kind) => kind switch
    {
        FarmingGuideStorageKind.SecureContainer => 20,
        FarmingGuideStorageKind.Pockets => 30,
        FarmingGuideStorageKind.Rig => 40,
        FarmingGuideStorageKind.Backpack => 50,
        FarmingGuideStorageKind.SpecialSlots => 70,
        _ => 80,
    };

    private static string RootStorageSurfaceIdV1170(FarmingGuideStorageKind kind, int gridIndex) =>
        $"root:{(int)kind}:{gridIndex}";

    private static string NestedSurfaceIdV1170(string ownerInstanceId, int gridIndex) =>
        $"nested:{ownerInstanceId}:{gridIndex}";

    private static string CarrierGridSurfaceIdV1170(
        string ownerInstanceId,
        FarmingGuideStorageKind kind,
        int gridIndex) =>
        $"carrier-grid:{ownerInstanceId}:{(int)kind}:{gridIndex}";

    private static string EquipmentSurfaceIdV1170(FarmingGuideEquipmentSlot slot) =>
        $"equipment:{(int)slot}";

    private static string CarrierSurfaceIdV1170(FarmingGuideStorageKind kind) =>
        $"carrier:{(int)kind}";
}
