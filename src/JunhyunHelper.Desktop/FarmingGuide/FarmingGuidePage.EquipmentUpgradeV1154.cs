using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Objective equipment superiority is evaluated before ordinary storage. A clearly
    /// better armor/carrier should be worn now instead of being put in a free backpack cell.
    /// Equipment whose source data has no single defensible ordering (notably headphones)
    /// falls through to the existing value/need policy rather than receiving a guessed rank.
    /// </summary>
    private RaidRecommendation PlanScannedItemEquipmentAware(ScannerItemSnapshot scanned, GameItem incoming)
    {
        var current = BuildSnapshot();
        if (TryBuildProtectiveUpgrade(current, incoming, out var protective))
            return protective;
        if (TryBuildCarrierUpgrade(current, incoming, out var carrier))
            return carrier;
        return PlanScannedItemHardened(scanned, incoming);
    }

    private bool TryBuildProtectiveUpgrade(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming,
        out RaidRecommendation recommendation)
    {
        foreach (var slot in new[]
                 {
                     FarmingGuideEquipmentSlot.BodyArmor,
                     FarmingGuideEquipmentSlot.Helmet,
                     FarmingGuideEquipmentSlot.FaceCover,
                     FarmingGuideEquipmentSlot.Eyewear,
                 })
        {
            if (!current.Equipment.TryGetValue(slot, out var existingState) ||
                _lockedEquipmentSlots.Contains(slot) ||
                !FarmingGuideCompatibility.IsEquipmentSlotCompatible(slot, incoming))
            {
                continue;
            }

            var existing = ResolveItem(existingState);
            if (existing is null ||
                !FarmingGuideEquipmentUpgradePolicy.IsProtectiveUpgrade(incoming, existing) ||
                !CanEquipInSnapshot(slot, incoming, current))
            {
                continue;
            }

            var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(current.Equipment)
            {
                [slot] = FarmingGuideItemState.Create(incoming.Id),
            };
            var oldClass = FarmingGuideEquipmentUpgradePolicy.ArmorClass(existing);
            var newClass = FarmingGuideEquipmentUpgradePolicy.ArmorClass(incoming);
            recommendation = new RaidRecommendation(
                $"{EquipmentLabel(slot)}의 {DisplayName(existing)}을 {DisplayName(incoming)}으로 교체 · 방어 등급 {oldClass}→{newClass}",
                FarmingGuideInstructionAction.ReplaceEquip,
                current with { Equipment = equipment });
            return true;
        }

        recommendation = default!;
        return false;
    }

    private bool TryBuildCarrierUpgrade(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming,
        out RaidRecommendation recommendation)
    {
        // The user explicitly allows ordinary body armor + ordinary rig -> armored rig
        // when the incoming armored rig is a protection upgrade and every current rig item
        // can be preserved inside it. The reverse transition is intentionally not inferred:
        // scanning body armor cannot create a missing ordinary rig.
        if (FarmingGuideCompatibility.IsStorageCarrierCompatible(FarmingGuideStorageKind.Rig, incoming) &&
            incoming.FarmingGuideData?.IsArmoredRig == true &&
            current.Rig is { } currentRigState &&
            current.Equipment.TryGetValue(FarmingGuideEquipmentSlot.BodyArmor, out var bodyArmorState) &&
            !_lockedCarriers.Contains(FarmingGuideStorageKind.Rig) &&
            !_lockedEquipmentSlots.Contains(FarmingGuideEquipmentSlot.BodyArmor))
        {
            var currentRig = ResolveItem(currentRigState);
            var bodyArmor = ResolveItem(bodyArmorState);
            if (currentRig is not null &&
                bodyArmor is not null &&
                currentRig.FarmingGuideData?.IsArmoredRig != true &&
                FarmingGuideEquipmentUpgradePolicy.IsBodyArmorToArmoredRigUpgrade(incoming, bodyArmor) &&
                IsCarrierConflictFree(
                    FarmingGuideStorageKind.Rig,
                    incoming,
                    current,
                    removingEquipment: FarmingGuideEquipmentSlot.BodyArmor) &&
                TryPackCarrierContentsForUpgrade(
                    FarmingGuideStorageKind.Rig,
                    incoming,
                    current,
                    out var packedStored,
                    out var movedCount))
            {
                var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(current.Equipment);
                equipment.Remove(FarmingGuideEquipmentSlot.BodyArmor);
                var proposed = current with
                {
                    Equipment = equipment,
                    Rig = FarmingGuideItemState.Create(incoming.Id),
                    StoredItems = packedStored,
                };
                var movement = movedCount > 0 ? $" · 리그 내부 {movedCount}개 재배치" : string.Empty;
                recommendation = new RaidRecommendation(
                    $"{DisplayName(bodyArmor)} + {DisplayName(currentRig)}을 {DisplayName(incoming)}으로 교체{movement}",
                    FarmingGuideInstructionAction.ReplaceEquip,
                    proposed);
                return true;
            }

            // An armored rig conflicts with the existing body armor. If the explicit
            // combined transition is not safe, do not reinterpret it as an ordinary rig
            // upgrade and silently delete or invalidate the body armor.
            recommendation = default!;
            return false;
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                 })
        {
            var existingState = kind switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                _ => null,
            };
            if (existingState is null ||
                _lockedCarriers.Contains(kind) ||
                !FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, incoming))
            {
                continue;
            }

            var existing = ResolveItem(existingState);
            if (existing is null ||
                !FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(kind, incoming, existing) ||
                !IsCarrierConflictFree(kind, incoming, current, removingEquipment: null) ||
                !TryPackCarrierContentsForUpgrade(kind, incoming, current, out var packedStored, out var movedCount))
            {
                continue;
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
                _ => current,
            };
            var movement = movedCount > 0 ? $" · 내부 {movedCount}개 재배치" : string.Empty;
            recommendation = new RaidRecommendation(
                $"{CarrierLabel(kind)}의 {DisplayName(existing)}을 {DisplayName(incoming)}으로 업그레이드{movement}",
                FarmingGuideInstructionAction.ReplaceEquip,
                proposed);
            return true;
        }

        recommendation = default!;
        return false;
    }

    private bool IsCarrierConflictFree(
        FarmingGuideStorageKind kind,
        GameItem incoming,
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideEquipmentSlot? removingEquipment) =>
        EnumerateSnapshotEquippedItems(current, removingEquipment, kind)
            .All(other => !FarmingGuideCompatibility.ItemsConflict(incoming, other));

    private bool TryPackCarrierContentsForUpgrade(
        FarmingGuideStorageKind kind,
        GameItem incomingCarrier,
        FarmingGuideLoadoutSnapshot current,
        out IReadOnlyList<FarmingGuideStoredItemState> packedStored,
        out int movedCount)
    {
        var grids = incomingCarrier.FarmingGuideData?.StorageGrids ?? [];
        var roots = current.StoredItems
            .Where(item => item.ParentInstanceId is null && item.Storage == kind)
            .OrderBy(item => item.InstanceId, StringComparer.Ordinal)
            .ToArray();

        if (roots.Length == 0)
        {
            packedStored = current.StoredItems;
            movedCount = 0;
            return true;
        }
        if (grids.Count == 0)
        {
            packedStored = current.StoredItems;
            movedCount = 0;
            return false;
        }

        var reserved = _reservedCells
            .Where(cell => cell.Storage == kind && cell.ParentInstanceId is null)
            .ToArray();
        foreach (var cell in reserved)
        {
            if (cell.GridIndex < 0 || cell.GridIndex >= grids.Count ||
                cell.X < 0 || cell.Y < 0 ||
                cell.X >= grids[cell.GridIndex].Width || cell.Y >= grids[cell.GridIndex].Height)
            {
                packedStored = current.StoredItems;
                movedCount = 0;
                return false;
            }
        }

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
                        $"__reserved__{obstacleIndex}",
                        cell.X,
                        cell.Y,
                        1,
                        1))
                    .ToArray()))
            .ToArray();

        var items = new List<FarmingGuideCarrierPackingItem>(roots.Length);
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

            var fixedRoot = IsInsideLockedItemInSnapshot(root.InstanceId, current.StoredItems) ||
                            SubtreeContainsLockedItemInSnapshot(root.InstanceId, current.StoredItems);
            items.Add(new FarmingGuideCarrierPackingItem(
                root.InstanceId,
                root.GridIndex >= 0 && root.GridIndex < grids.Count ? SurfaceId(root.GridIndex) : null,
                root.X,
                root.Y,
                root.Rotated,
                fixedRoot,
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
}
