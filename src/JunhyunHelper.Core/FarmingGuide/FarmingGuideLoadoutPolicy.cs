using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Product-owned safety policy for persisted and edited Farming Guide state.
/// Saved state is advisory user input: when current Tarkov structure no longer proves
/// that a placement is valid, discard that placement instead of rendering or persisting
/// an impossible inventory state.
/// </summary>
public static class FarmingGuideLoadoutPolicy
{
    public static bool CanReplaceCarrier(bool movingSameCarrier, bool targetContainsItems) =>
        movingSameCarrier || !targetContainsItems;

    public static FarmingGuideLoadoutSnapshot SanitizeSnapshot(
        FarmingGuideLoadoutSnapshot snapshot,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        IReadOnlyList<FarmingGuideStorageGridDefinition>? pocketGrids = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(itemCatalog);
        pocketGrids ??= FarmingGuidePocketLayoutPolicy.StandardGrids;

        var equipment = snapshot.Equipment
            .Where(entry =>
                entry.Key is not (FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag) &&
                itemCatalog.TryGetValue(entry.Value.ItemId, out var item) &&
                FarmingGuideCompatibility.IsEquipmentSlotCompatible(entry.Key, item))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        var rig = ValidCarrier(snapshot.Rig, FarmingGuideStorageKind.Rig, itemCatalog);
        var backpack = ValidCarrier(snapshot.Backpack, FarmingGuideStorageKind.Backpack, itemCatalog);
        var secureContainer = ValidCarrier(snapshot.SecureContainer, FarmingGuideStorageKind.SecureContainer, itemCatalog);

        // Preserve the same mutual-exclusion contract as interactive placement.
        if (rig is not null &&
            itemCatalog.TryGetValue(rig.ItemId, out var rigItem) &&
            rigItem.FarmingGuideData?.IsArmoredRig == true)
        {
            equipment.Remove(FarmingGuideEquipmentSlot.BodyArmor);
        }

        if (equipment.TryGetValue(FarmingGuideEquipmentSlot.Helmet, out var helmetState) &&
            itemCatalog.TryGetValue(helmetState.ItemId, out var helmet) &&
            helmet.FarmingGuideData?.BlocksHeadphones == true)
        {
            equipment.Remove(FarmingGuideEquipmentSlot.Headset);
        }

        var carriers = new Dictionary<FarmingGuideStorageKind, FarmingGuideItemState?>
        {
            [FarmingGuideStorageKind.Rig] = rig,
            [FarmingGuideStorageKind.Backpack] = backpack,
            [FarmingGuideStorageKind.SecureContainer] = secureContainer,
        };
        var accepted = new List<FarmingGuideStoredItemState>();
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var stored in snapshot.StoredItems)
        {
            if (string.IsNullOrWhiteSpace(stored.InstanceId) ||
                !instanceIds.Add(stored.InstanceId) ||
                !itemCatalog.TryGetValue(stored.Item.ItemId, out var item))
            {
                continue;
            }

            var grids = ResolveGrids(stored.Storage, carriers, itemCatalog, pocketGrids);
            if (stored.GridIndex < 0 || stored.GridIndex >= grids.Count)
                continue;

            var grid = grids[stored.GridIndex];
            if (!FarmingGuideCompatibility.FilterAllows(item, grid.Filters))
                continue;

            var existing = accepted
                .Where(value => value.Storage == stored.Storage && value.GridIndex == stored.GridIndex)
                .Select(value =>
                {
                    var existingItem = itemCatalog[value.Item.ItemId];
                    var footprint = FarmingGuidePlacementEngine.Footprint(
                        existingItem.Width ?? 1,
                        existingItem.Height ?? 1,
                        value.Rotated);
                    return new FarmingGuideGridPlacement(
                        value.InstanceId,
                        value.X,
                        value.Y,
                        footprint.Width,
                        footprint.Height);
                });

            if (!FarmingGuidePlacementEngine.CanPlace(
                    grid.Width,
                    grid.Height,
                    stored.X,
                    stored.Y,
                    item.Width ?? 1,
                    item.Height ?? 1,
                    stored.Rotated,
                    existing))
            {
                continue;
            }

            accepted.Add(stored);
        }

        return new FarmingGuideLoadoutSnapshot(
            equipment,
            rig,
            backpack,
            secureContainer,
            accepted);
    }

    private static FarmingGuideItemState? ValidCarrier(
        FarmingGuideItemState? state,
        FarmingGuideStorageKind kind,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        if (state is null || !itemCatalog.TryGetValue(state.ItemId, out var item))
            return null;
        return FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item) ? state : null;
    }

    private static IReadOnlyList<FarmingGuideStorageGridDefinition> ResolveGrids(
        FarmingGuideStorageKind kind,
        IReadOnlyDictionary<FarmingGuideStorageKind, FarmingGuideItemState?> carriers,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        IReadOnlyList<FarmingGuideStorageGridDefinition> pocketGrids)
    {
        static FarmingGuideStorageGridDefinition[] FixedGrids(int count) =>
            Enumerable.Range(0, count)
                .Select(_ => new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty))
                .ToArray();

        if (kind == FarmingGuideStorageKind.Pockets)
            return pocketGrids;
        if (kind == FarmingGuideStorageKind.SpecialSlots)
            return FixedGrids(3);
        if (!carriers.TryGetValue(kind, out var carrier) ||
            carrier is null ||
            !itemCatalog.TryGetValue(carrier.ItemId, out var carrierItem))
        {
            return [];
        }

        return carrierItem.FarmingGuideData?.StorageGrids ?? [];
    }
}
