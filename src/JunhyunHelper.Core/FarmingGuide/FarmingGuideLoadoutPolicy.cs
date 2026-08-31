using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Product-owned safety policy for persisted and edited Farming Guide state.
/// Saved state is advisory user input: when current Tarkov structure no longer proves
/// that a placement or nested assembly is valid, discard that impossible state instead
/// of rendering or persisting it.
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

        var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>();
        foreach (var entry in snapshot.Equipment)
        {
            if (entry.Key is FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag ||
                !itemCatalog.TryGetValue(entry.Value.ItemId, out var item) ||
                !FarmingGuideCompatibility.IsEquipmentSlotCompatible(entry.Key, item))
            {
                continue;
            }

            var sanitized = FarmingGuideAssemblyPolicy.Sanitize(entry.Value, itemCatalog);
            if (sanitized is not null)
                equipment[entry.Key] = sanitized;
        }

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
        var acceptedByInstance = new Dictionary<string, FarmingGuideStoredItemState>(StringComparer.Ordinal);
        var duplicateInstanceIds = snapshot.StoredItems
            .Where(static stored => !string.IsNullOrWhiteSpace(stored.InstanceId))
            .GroupBy(static stored => stored.InstanceId, StringComparer.Ordinal)
            .Where(static group => group.Skip(1).Any())
            .Select(static group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        // Root placements must be accepted before their nested children. This also keeps
        // older schema-v1 files (which have no ParentInstanceId field) fully compatible.
        foreach (var stored in snapshot.StoredItems.Where(static value => value.ParentInstanceId is null))
        {
            TryAcceptStored(
                stored,
                carriers,
                accepted,
                acceptedByInstance,
                duplicateInstanceIds,
                itemCatalog,
                pocketGrids);
        }

        // Nested containers can themselves contain nested containers. Iterate until no
        // additional parent can be proven. Missing parents and cycles therefore fail closed.
        var pending = snapshot.StoredItems
            .Where(static value => value.ParentInstanceId is not null)
            .ToList();
        while (pending.Count > 0)
        {
            var progressed = false;
            for (var index = pending.Count - 1; index >= 0; index--)
            {
                var stored = pending[index];
                if (string.IsNullOrWhiteSpace(stored.ParentInstanceId) ||
                    duplicateInstanceIds.Contains(stored.InstanceId))
                {
                    pending.RemoveAt(index);
                    continue;
                }

                if (!acceptedByInstance.ContainsKey(stored.ParentInstanceId))
                    continue;

                TryAcceptStored(
                    stored,
                    carriers,
                    accepted,
                    acceptedByInstance,
                    duplicateInstanceIds,
                    itemCatalog,
                    pocketGrids);
                pending.RemoveAt(index);
                progressed = true;
            }

            if (!progressed)
                break;
        }

        return new FarmingGuideLoadoutSnapshot(
            equipment,
            rig,
            backpack,
            secureContainer,
            accepted);
    }

    private static bool TryAcceptStored(
        FarmingGuideStoredItemState stored,
        IReadOnlyDictionary<FarmingGuideStorageKind, FarmingGuideItemState?> carriers,
        List<FarmingGuideStoredItemState> accepted,
        Dictionary<string, FarmingGuideStoredItemState> acceptedByInstance,
        IReadOnlySet<string> duplicateInstanceIds,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        IReadOnlyList<FarmingGuideStorageGridDefinition> pocketGrids)
    {
        if (string.IsNullOrWhiteSpace(stored.InstanceId) ||
            duplicateInstanceIds.Contains(stored.InstanceId) ||
            acceptedByInstance.ContainsKey(stored.InstanceId) ||
            string.Equals(stored.InstanceId, stored.ParentInstanceId, StringComparison.Ordinal) ||
            !itemCatalog.TryGetValue(stored.Item.ItemId, out var item))
        {
            return false;
        }

        var sanitizedAssembly = FarmingGuideAssemblyPolicy.Sanitize(stored.Item, itemCatalog);
        if (sanitizedAssembly is null)
            return false;
        stored = stored with { Item = sanitizedAssembly };
        item = itemCatalog[sanitizedAssembly.ItemId];

        var grids = ResolveGrids(
            stored.Storage,
            stored.ParentInstanceId,
            carriers,
            acceptedByInstance,
            itemCatalog,
            pocketGrids);
        if (stored.GridIndex < 0 || stored.GridIndex >= grids.Count)
            return false;

        var grid = grids[stored.GridIndex];
        if (!FarmingGuideCompatibility.FilterAllows(item, grid.Filters))
            return false;

        var existing = accepted
            .Where(value =>
                value.GridIndex == stored.GridIndex &&
                SameStorageSurface(value, stored))
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
            return false;
        }

        accepted.Add(stored);
        acceptedByInstance[stored.InstanceId] = stored;
        return true;
    }

    private static bool SameStorageSurface(
        FarmingGuideStoredItemState left,
        FarmingGuideStoredItemState right)
    {
        if (left.ParentInstanceId is not null || right.ParentInstanceId is not null)
        {
            return string.Equals(
                left.ParentInstanceId,
                right.ParentInstanceId,
                StringComparison.Ordinal);
        }

        return left.Storage == right.Storage;
    }

    private static FarmingGuideItemState? ValidCarrier(
        FarmingGuideItemState? state,
        FarmingGuideStorageKind kind,
        IReadOnlyDictionary<string, GameItem> itemCatalog)
    {
        if (state is null || !itemCatalog.TryGetValue(state.ItemId, out var item) ||
            !FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item))
        {
            return null;
        }

        return FarmingGuideAssemblyPolicy.Sanitize(state, itemCatalog);
    }

    private static IReadOnlyList<FarmingGuideStorageGridDefinition> ResolveGrids(
        FarmingGuideStorageKind kind,
        string? parentInstanceId,
        IReadOnlyDictionary<FarmingGuideStorageKind, FarmingGuideItemState?> carriers,
        IReadOnlyDictionary<string, FarmingGuideStoredItemState> acceptedByInstance,
        IReadOnlyDictionary<string, GameItem> itemCatalog,
        IReadOnlyList<FarmingGuideStorageGridDefinition> pocketGrids)
    {
        static FarmingGuideStorageGridDefinition[] FixedGrids(int count) =>
            Enumerable.Range(0, count)
                .Select(_ => new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty))
                .ToArray();

        if (parentInstanceId is not null)
        {
            if (!acceptedByInstance.TryGetValue(parentInstanceId, out var parent) ||
                !itemCatalog.TryGetValue(parent.Item.ItemId, out var parentItem))
            {
                return [];
            }

            return parentItem.FarmingGuideData?.StorageGrids ?? [];
        }

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
