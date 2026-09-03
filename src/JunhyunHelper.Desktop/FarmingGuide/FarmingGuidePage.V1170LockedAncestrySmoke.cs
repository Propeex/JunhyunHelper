using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1170LockedAncestrySmoke()
    {
        const string secureId = "__v1170_lock_secure";
        const string grandId = "__v1170_lock_grand";
        const string parentId = "__v1170_lock_parent";
        const string childId = "__v1170_lock_child";
        const string incomingId = "__v1170_lock_incoming";
        const string grandInstance = "__v1170_lock_grand_instance";
        const string parentInstance = "__v1170_lock_parent_instance";
        const string childInstance = "__v1170_lock_child_instance";

        var ids = new[] { secureId, grandId, parentId, childId, incomingId };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousEquipmentLocks = _lockedEquipmentSlots.ToArray();
        var previousCarrierLocks = _lockedCarriers.ToArray();
        var previousItemLocks = _lockedItemInstanceIds.ToArray();
        var previousCells = _reservedCells.ToArray();

        foreach (var item in ids.Select(id => V1170SmokeItem(id, weightKg: 1m)))
            _itemsById[item.Id] = item;

        try
        {
            var current = FarmingGuideLoadoutSnapshot.Empty with
            {
                SecureContainer = FarmingGuideItemState.Create(secureId),
                StoredItems =
                [
                    new FarmingGuideStoredItemState(
                        grandInstance,
                        FarmingGuideItemState.Create(grandId),
                        FarmingGuideStorageKind.SecureContainer,
                        0, 0, 0, false),
                    new FarmingGuideStoredItemState(
                        parentInstance,
                        FarmingGuideItemState.Create(parentId),
                        FarmingGuideStorageKind.SecureContainer,
                        0, 0, 0, false,
                        grandInstance),
                    new FarmingGuideStoredItemState(
                        childInstance,
                        FarmingGuideItemState.Create(childId),
                        FarmingGuideStorageKind.SecureContainer,
                        0, 0, 0, false,
                        parentInstance),
                ],
            };
            var incoming = _itemsById[incomingId];
            var scanned = V1170SmokeSnapshot(incomingId, flea: 1_000, firNeed: 0);

            ClearV1170SmokeLocks();
            _lockedItemInstanceIds.Add(childInstance);
            if (!TryBuildGlobalRootsV1170(current, scanned, incoming, out var itemLockedRoots, out _))
                throw new InvalidOperationException("v1.17 could not build roots for nested item-lock ancestry smoke.");

            AssertV1170FixedRoot(itemLockedRoots, childInstance, "locked nested child");
            AssertV1170FixedRoot(itemLockedRoots, parentInstance, "parent of locked nested child");
            AssertV1170FixedRoot(itemLockedRoots, grandInstance, "ancestor of locked nested child");
            AssertV1170FixedRoot(
                itemLockedRoots,
                CarrierRootIdV1170(FarmingGuideStorageKind.SecureContainer),
                "root carrier of locked nested child");

            ClearV1170SmokeLocks();
            _reservedCells.Add(new FarmingGuideLockedCell(
                FarmingGuideStorageKind.SecureContainer,
                0,
                0,
                0,
                parentInstance));
            if (!TryBuildGlobalRootsV1170(current, scanned, incoming, out var cellLockedRoots, out _))
                throw new InvalidOperationException("v1.17 could not build roots for nested fixed-cell ancestry smoke.");

            AssertV1170FixedRoot(cellLockedRoots, parentInstance, "owner of nested fixed cell");
            AssertV1170FixedRoot(cellLockedRoots, grandInstance, "ancestor of nested fixed cell");
            AssertV1170FixedRoot(
                cellLockedRoots,
                CarrierRootIdV1170(FarmingGuideStorageKind.SecureContainer),
                "root carrier of nested fixed cell");
        }
        finally
        {
            _lockedEquipmentSlots.Clear();
            _lockedEquipmentSlots.UnionWith(previousEquipmentLocks);
            _lockedCarriers.Clear();
            _lockedCarriers.UnionWith(previousCarrierLocks);
            _lockedItemInstanceIds.Clear();
            _lockedItemInstanceIds.UnionWith(previousItemLocks);
            _reservedCells.Clear();
            _reservedCells.UnionWith(previousCells);

            foreach (var id in ids)
            {
                if (previousItems[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }
    }

    private static void AssertV1170FixedRoot(
        IReadOnlyList<GlobalRootV1170> roots,
        string instanceId,
        string description)
    {
        var root = roots.SingleOrDefault(value =>
            string.Equals(value.InstanceId, instanceId, StringComparison.Ordinal));
        if (root is null || !root.Fixed)
            throw new InvalidOperationException($"v1.17 did not fix the {description}.");
    }
}
