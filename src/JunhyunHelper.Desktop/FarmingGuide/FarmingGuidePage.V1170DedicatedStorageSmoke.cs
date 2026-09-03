using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1170DedicatedStorageSmoke()
    {
        const string secureId = "__v1170_dedicated_secure";
        const string caseId = "__v1170_dedicated_case";
        const string keyId = "__v1170_dedicated_key";
        const string keyCategoryId = "__v1170_dedicated_key_category";
        const string caseInstanceId = "__v1170_dedicated_case_instance";

        var ids = new[] { secureId, caseId, keyId };
        var previousItems = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);
        var previousBridge = _raidBridge;
        var previousPockets = _pocketGrids;
        var previousWeight = _weightSettingsV1160;
        var previousWeightProfile = _weightSettingsProfileIdV1160;
        var previousEquipmentLocks = _lockedEquipmentSlots.ToArray();
        var previousCarrierLocks = _lockedCarriers.ToArray();
        var previousItemLocks = _lockedItemInstanceIds.ToArray();
        var previousCells = _reservedCells.ToArray();

        var secure = V1170SmokeItem(secureId, weightKg: 1m) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(2, 1, FarmingGuideItemFilter.Empty)],
                [], [], [], [], false, false),
        };
        var dedicatedCase = V1170SmokeItem(caseId, weightKg: 1m, typeKeys: ["container"]) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [
                    new FarmingGuideStorageGridDefinition(
                        1,
                        1,
                        new FarmingGuideItemFilter([keyCategoryId], [], [], [])),
                ],
                [], [], [], [], false, false),
        };
        var key = V1170SmokeItem(keyId, weightKg: 1m) with
        {
            CategoryIds = [keyCategoryId],
        };

        _itemsById[secureId] = secure;
        _itemsById[caseId] = dedicatedCase;
        _itemsById[keyId] = key;

        var snapshots = new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
        {
            [secureId] = V1170SmokeSnapshot(secureId, flea: 100_000, firNeed: 0),
            [caseId] = V1170SmokeSnapshot(caseId, flea: 50_000, firNeed: 0),
            [keyId] = V1170SmokeSnapshot(keyId, flea: 10_000, firNeed: 0),
        };
        var bridge = new FarmingGuideRaidBridge();
        bridge.SetScannerSnapshotResolver(itemId => snapshots.GetValueOrDefault(itemId));

        try
        {
            _raidBridge = bridge;
            _pocketGrids = [];
            _weightSettingsV1160 = new FarmingGuideWeightSettings(0);
            _weightSettingsProfileIdV1160 = _profileId;
            ClearV1170SmokeLocks();

            var current = FarmingGuideLoadoutSnapshot.Empty with
            {
                SecureContainer = FarmingGuideItemState.Create(secureId),
                StoredItems =
                [
                    new FarmingGuideStoredItemState(
                        caseInstanceId,
                        FarmingGuideItemState.Create(caseId),
                        FarmingGuideStorageKind.SecureContainer,
                        0,
                        0,
                        0,
                        false),
                ],
            };

            if (!TryPlanScannedItemGlobalV1170(
                    current,
                    snapshots[keyId],
                    key,
                    out var recommendation) ||
                recommendation.Action != FarmingGuideInstructionAction.Store)
            {
                throw new InvalidOperationException("v1.17 global solver could not retain the dedicated-storage-compatible incoming item.");
            }

            var retainedKey = recommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                string.Equals(value.Item.ItemId, keyId, StringComparison.Ordinal));
            if (retainedKey is null ||
                !string.Equals(retainedKey.ParentInstanceId, caseInstanceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "v1.17 global solver stopped using compatible dedicated nested storage for final placement.");
            }
        }
        finally
        {
            _raidBridge = previousBridge;
            _pocketGrids = previousPockets;
            _weightSettingsV1160 = previousWeight;
            _weightSettingsProfileIdV1160 = previousWeightProfile;

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
}
