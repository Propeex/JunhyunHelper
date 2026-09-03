using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1170GlobalOptimizerSmoke()
    {
        const string ordinaryId = "__v1170_smoke_ordinary";
        const string incomingId = "__v1170_smoke_incoming";
        const string foodId = "__v1170_smoke_food";
        const string containerId = "__v1170_smoke_container";
        const string helmetOldId = "__v1170_smoke_helmet_old";
        const string helmetNewId = "__v1170_smoke_helmet_new";
        const string unknownValueId = "__v1170_smoke_unknown_value";
        const string assemblyRootId = "__v1170_smoke_assembly_root";
        const string assemblyChildId = "__v1170_smoke_assembly_child";
        const string heavyRootId = "__v1170_smoke_heavy_root";
        const string heavyChildId = "__v1170_smoke_heavy_child";
        const string unknownWeightId = "__v1170_smoke_unknown_weight";
        const string unknownSizeId = "__v1170_smoke_unknown_size";
        const string wideIncomingId = "__v1170_smoke_wide_incoming";
        var ids = new[]
        {
            ordinaryId,
            incomingId,
            foodId,
            containerId,
            helmetOldId,
            helmetNewId,
            unknownValueId,
            assemblyRootId,
            assemblyChildId,
            heavyRootId,
            heavyChildId,
            unknownWeightId,
            unknownSizeId,
            wideIncomingId,
        };

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

        var ordinary = V1170SmokeItem(ordinaryId, weightKg: 1m);
        var incoming = V1170SmokeItem(incomingId, weightKg: 1m);
        var food = V1170SmokeItem(foodId, weightKg: 1m) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesFoodDrink",
                [], [], [], [], [], false, false)
            {
                Energy = 40,
                Hydration = 20,
            },
        };
        var container = V1170SmokeItem(containerId, weightKg: 1m, typeKeys: ["container"]) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty)],
                [], [], [], [], false, false),
        };
        var helmetOld = V1170SmokeItem(helmetOldId, weightKg: 1m, typeKeys: ["helmet"]);
        var helmetNew = V1170SmokeItem(helmetNewId, weightKg: 1m, typeKeys: ["helmet"]);
        var unknownValue = V1170SmokeItem(unknownValueId, weightKg: 1m);
        var assemblyRoot = V1170SmokeItem(assemblyRootId, weightKg: 1m);
        var assemblyChild = V1170SmokeItem(assemblyChildId, weightKg: 1m);
        var heavyRoot = V1170SmokeItem(heavyRootId, weightKg: 1m);
        var heavyChild = V1170SmokeItem(heavyChildId, weightKg: 77m);
        var unknownWeight = V1170SmokeItem(unknownWeightId, weightKg: null);
        var unknownSize = V1170SmokeItem(unknownSizeId, weightKg: 1m, width: null, height: null);
        var wideIncoming = V1170SmokeItem(wideIncomingId, weightKg: 1m, width: 2, height: 1);

        foreach (var item in new[]
                 {
                     ordinary,
                     incoming,
                     food,
                     container,
                     helmetOld,
                     helmetNew,
                     unknownValue,
                     assemblyRoot,
                     assemblyChild,
                     heavyRoot,
                     heavyChild,
                     unknownWeight,
                     unknownSize,
                     wideIncoming,
                 })
        {
            _itemsById[item.Id] = item;
        }

        var snapshots = new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
        {
            [ordinaryId] = V1170SmokeSnapshot(ordinaryId, flea: 500_000, firNeed: 0),
            [incomingId] = V1170SmokeSnapshot(incomingId, flea: 1_000, firNeed: 1),
            [foodId] = V1170SmokeSnapshot(foodId, flea: 10_000, firNeed: 0),
            [containerId] = V1170SmokeSnapshot(containerId, flea: 20_000, firNeed: 0),
            [helmetOldId] = V1170SmokeSnapshot(helmetOldId, flea: 20_000, firNeed: 0),
            [helmetNewId] = V1170SmokeSnapshot(helmetNewId, flea: 80_000, firNeed: 0),
            [unknownValueId] = V1170SmokeSnapshot(unknownValueId, flea: null, firNeed: 0),
            [assemblyRootId] = V1170SmokeSnapshot(assemblyRootId, flea: 1_000, firNeed: 0),
            [assemblyChildId] = V1170SmokeSnapshot(assemblyChildId, flea: 200_000, firNeed: 0),
            [heavyRootId] = V1170SmokeSnapshot(heavyRootId, flea: 1_000, firNeed: 0),
            [heavyChildId] = V1170SmokeSnapshot(heavyChildId, flea: 1_000, firNeed: 0),
            [unknownWeightId] = V1170SmokeSnapshot(unknownWeightId, flea: 100_000, firNeed: 0),
            [unknownSizeId] = V1170SmokeSnapshot(unknownSizeId, flea: 100_000, firNeed: 0),
            [wideIncomingId] = V1170SmokeSnapshot(wideIncomingId, flea: 10_000, firNeed: 0),
        };
        var bridge = new FarmingGuideRaidBridge();
        bridge.SetScannerSnapshotResolver(itemId => snapshots.GetValueOrDefault(itemId));
        _raidBridge = bridge;
        _weightSettingsV1160 = new FarmingGuideWeightSettings(0);
        _weightSettingsProfileIdV1160 = _profileId;

        try
        {
            ClearV1170SmokeLocks();

            // Absolute FIR priority: one 1x1 slot is full of a much more valuable ordinary
            // item, but a needed Scanner-acquired item must replace it.
            _pocketGrids = [new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty)];
            var current = V1170SmokeStoredSnapshot(ordinaryId, "ordinary-instance");
            if (!TryPlanScannedItemGlobalV1170(
                    current,
                    snapshots[incomingId],
                    incoming,
                    out var firRecommendation) ||
                firRecommendation.Action != FarmingGuideInstructionAction.Replace ||
                firRecommendation.ProposedSnapshot.StoredItems.Any(value => value.Item.ItemId == ordinaryId) ||
                firRecommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    value.Item.ItemId == incomingId) is not { Item.RaidAcquired: true })
            {
                throw new InvalidOperationException("v1.17 global optimizer did not enforce needed-FIR priority/provenance.");
            }

            // FIR quota is quantity-capped. With one already retained Scanner-acquired copy
            // satisfying the one-unit need, an equal-value second copy cannot displace it.
            var firstAcquired = new FarmingGuideLoadoutSnapshot(
                new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
                null, null, null,
                [
                    new FarmingGuideStoredItemState(
                        "first-acquired",
                        FarmingGuideItemState.Create(incomingId, raidAcquired: true),
                        FarmingGuideStorageKind.Pockets,
                        0, 0, 0, false),
                ]);
            if (!TryPlanScannedItemGlobalV1170(
                    firstAcquired,
                    snapshots[incomingId],
                    incoming,
                    out var quotaRecommendation) ||
                quotaRecommendation.Action != FarmingGuideInstructionAction.Discard ||
                !ReferenceEquals(quotaRecommendation.ProposedSnapshot, firstAcquired))
            {
                throw new InvalidOperationException("v1.17 FIR quota cap or stable objective tie is incorrect.");
            }

            // No tactical category privilege: ordinary economic value may replace the last
            // food/drink provider when no FIR need exists.
            snapshots[incomingId] = V1170SmokeSnapshot(incomingId, flea: 50_000, firNeed: 0);
            var foodCurrent = V1170SmokeStoredSnapshot(foodId, "food-instance");
            if (!TryPlanScannedItemGlobalV1170(
                    foodCurrent,
                    snapshots[incomingId],
                    incoming,
                    out var economicRecommendation) ||
                economicRecommendation.Action != FarmingGuideInstructionAction.Replace ||
                economicRecommendation.ProposedSnapshot.StoredItems.Any(value => value.Item.ItemId == foodId))
            {
                throw new InvalidOperationException("v1.17 incorrectly restored tactical food/drink protection.");
            }

            // Complete retained value includes modeled assembly descendants. A cheap root
            // carrying a valuable attachment must not be replaced by a lower total-value item.
            var assembledState = new FarmingGuideItemState(
                assemblyRootId,
                new Dictionary<string, FarmingGuideItemState?>
                {
                    ["attachment"] = FarmingGuideItemState.Create(assemblyChildId),
                },
                new Dictionary<string, FarmingGuideItemState?>());
            var assembledCurrent = FarmingGuideLoadoutSnapshot.Empty with
            {
                StoredItems =
                [
                    new FarmingGuideStoredItemState(
                        "assembled-instance",
                        assembledState,
                        FarmingGuideStorageKind.Pockets,
                        0, 0, 0, false),
                ],
            };
            snapshots[incomingId] = V1170SmokeSnapshot(incomingId, flea: 100_000, firNeed: 0);
            if (!TryPlanScannedItemGlobalV1170(
                    assembledCurrent,
                    snapshots[incomingId],
                    incoming,
                    out var assembledValueRecommendation) ||
                assembledValueRecommendation.Action != FarmingGuideInstructionAction.Discard)
            {
                throw new InvalidOperationException("v1.17 complete-state value omitted an assembly descendant.");
            }

            // An incoming retained container contributes its internal capacity in the same
            // scan: keep the existing loot by moving it inside the newly retained container.
            snapshots[ordinaryId] = V1170SmokeSnapshot(ordinaryId, flea: 30_000, firNeed: 0);
            snapshots[containerId] = V1170SmokeSnapshot(containerId, flea: 20_000, firNeed: 0);
            var containerCurrent = V1170SmokeStoredSnapshot(ordinaryId, "ordinary-for-container");
            if (!TryPlanScannedItemGlobalV1170(
                    containerCurrent,
                    snapshots[containerId],
                    container,
                    out var containerRecommendation) ||
                containerRecommendation.Action != FarmingGuideInstructionAction.Store ||
                containerRecommendation.ProposedSnapshot.StoredItems.Count != 2 ||
                containerRecommendation.ProposedSnapshot.StoredItems.Single(value =>
                    value.Item.ItemId == ordinaryId).ParentInstanceId is null)
            {
                throw new InvalidOperationException("v1.17 did not use incoming-container capacity in the same global solve.");
            }

            // Exact stored-item lock: the fixed root remains at the exact physical cell and
            // the incoming item uses the other cell.
            _pocketGrids = [new FarmingGuideStorageGridDefinition(2, 1, FarmingGuideItemFilter.Empty)];
            _lockedItemInstanceIds.Add("locked-instance");
            var lockedCurrent = V1170SmokeStoredSnapshot(ordinaryId, "locked-instance");
            snapshots[incomingId] = V1170SmokeSnapshot(incomingId, flea: 50_000, firNeed: 0);
            if (!TryPlanScannedItemGlobalV1170(
                    lockedCurrent,
                    snapshots[incomingId],
                    incoming,
                    out var lockedRecommendation) ||
                lockedRecommendation.ProposedSnapshot.StoredItems.Single(value =>
                    value.InstanceId == "locked-instance") is not { X: 0, Y: 0 })
            {
                throw new InvalidOperationException("v1.17 moved an explicitly fixed stored item.");
            }
            _lockedItemInstanceIds.Clear();

            // Equipment is in the same value pool. With no legal storage, a more valuable
            // helmet replaces the lower-valued unlocked helmet through the helmet slot.
            _pocketGrids = [];
            var equippedCurrent = FarmingGuideLoadoutSnapshot.Empty with
            {
                Equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
                {
                    [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create(helmetOldId),
                },
            };
            if (!TryPlanScannedItemGlobalV1170(
                    equippedCurrent,
                    snapshots[helmetNewId],
                    helmetNew,
                    out var equipmentRecommendation) ||
                equipmentRecommendation.Action != FarmingGuideInstructionAction.ReplaceEquip ||
                equipmentRecommendation.ProposedSnapshot.Equipment[FarmingGuideEquipmentSlot.Helmet].ItemId != helmetNewId)
            {
                throw new InvalidOperationException("v1.17 equipment root is not unified with the economic candidate pool.");
            }

            // A retained old top-level root may move into storage while the incoming root
            // occupies its former slot. Restrict this synthetic pocket to the old root so
            // the legal optimum must perform the transfer instead of using the equally valid
            // stable tie (keep old helmet equipped + store the incoming helmet).
            _pocketGrids =
            [
                new FarmingGuideStorageGridDefinition(
                    1,
                    1,
                    new FarmingGuideItemFilter([], [helmetOldId], [], [])),
            ];
            if (!TryPlanScannedItemGlobalV1170(
                    equippedCurrent,
                    snapshots[helmetNewId],
                    helmetNew,
                    out var retainedEquipmentRecommendation) ||
                retainedEquipmentRecommendation.Action != FarmingGuideInstructionAction.ReplaceEquip ||
                retainedEquipmentRecommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    value.Item.ItemId == helmetOldId) is not { } movedOldHelmet ||
                string.Equals(
                    movedOldHelmet.InstanceId,
                    EquipmentRootIdV1170(FarmingGuideEquipmentSlot.Helmet),
                    StringComparison.Ordinal) ||
                !retainedEquipmentRecommendation.Instruction.Contains("이동", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("v1.17 did not persist a relocated equipment root safely.");
            }

            snapshots[foodId] = V1170SmokeSnapshot(foodId, flea: 10_000, firNeed: 0);
            if (!TryPlanScannedItemGlobalV1170(
                    retainedEquipmentRecommendation.ProposedSnapshot,
                    snapshots[foodId],
                    food,
                    out var consecutiveRecommendation) ||
                consecutiveRecommendation.Action != FarmingGuideInstructionAction.Discard)
            {
                throw new InvalidOperationException("v1.17 consecutive scan failed after retaining relocated equipment.");
            }

            // Global packing can require movement inside the same storage area. The text
            // instruction must not suppress that current -> final physical delta.
            _pocketGrids = [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)];
            snapshots[ordinaryId] = V1170SmokeSnapshot(ordinaryId, flea: 30_000, firNeed: 0);
            var diagonalCurrent = FarmingGuideLoadoutSnapshot.Empty with
            {
                StoredItems =
                [
                    new FarmingGuideStoredItemState(
                        "diagonal-a",
                        FarmingGuideItemState.Create(ordinaryId),
                        FarmingGuideStorageKind.Pockets,
                        0, 0, 0, false),
                    new FarmingGuideStoredItemState(
                        "diagonal-b",
                        FarmingGuideItemState.Create(ordinaryId),
                        FarmingGuideStorageKind.Pockets,
                        0, 1, 1, false),
                ],
            };
            if (!TryPlanScannedItemGlobalV1170(
                    diagonalCurrent,
                    snapshots[wideIncomingId],
                    wideIncoming,
                    out var repackRecommendation) ||
                repackRecommendation.Action != FarmingGuideInstructionAction.Store ||
                !repackRecommendation.Instruction.Contains("내부 재배치", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("v1.17 hid a required same-area global repack operation.");
            }

            // Strict final weight: a single incoming root heavier than the configured limit
            // is never an admissible retained state, regardless of its value.
            _pocketGrids = [new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty)];
            var overweightIncoming = V1170SmokeItem("__v1170_smoke_overweight", weightKg: 80m);
            _itemsById[overweightIncoming.Id] = overweightIncoming;
            var overweightSnapshot = V1170SmokeSnapshot(overweightIncoming.Id, flea: 9_000_000, firNeed: 0);
            snapshots[overweightIncoming.Id] = overweightSnapshot;
            if (!TryPlanScannedItemGlobalV1170(
                    FarmingGuideLoadoutSnapshot.Empty,
                    overweightSnapshot,
                    overweightIncoming,
                    out var overweightRecommendation) ||
                overweightRecommendation.Action != FarmingGuideInstructionAction.Discard)
            {
                throw new InvalidOperationException("v1.17 accepted a final state above the configured weight limit.");
            }
            _itemsById.Remove(overweightIncoming.Id);
            snapshots.Remove(overweightIncoming.Id);

            // Assembly descendants contribute to carry weight. A current modeled assembly
            // that is already over the configured limit cannot be treated as a legal baseline.
            var heavyState = new FarmingGuideItemState(
                heavyRootId,
                new Dictionary<string, FarmingGuideItemState?>
                {
                    ["attachment"] = FarmingGuideItemState.Create(heavyChildId),
                },
                new Dictionary<string, FarmingGuideItemState?>());
            var heavyCurrent = FarmingGuideLoadoutSnapshot.Empty with
            {
                StoredItems =
                [
                    new FarmingGuideStoredItemState(
                        "heavy-assembled-instance",
                        heavyState,
                        FarmingGuideStorageKind.Pockets,
                        0, 0, 0, false),
                ],
            };
            if (TryPlanScannedItemGlobalV1170(
                    heavyCurrent,
                    snapshots[incomingId],
                    incoming,
                    out _))
            {
                throw new InvalidOperationException("v1.17 omitted assembly descendants from current/final weight proof.");
            }

            // Unknown physical facts are uncertainty, not zero weight or 1x1 geometry.
            if (TryPlanScannedItemGlobalV1170(
                    FarmingGuideLoadoutSnapshot.Empty,
                    snapshots[unknownWeightId],
                    unknownWeight,
                    out _))
            {
                throw new InvalidOperationException("v1.17 treated unknown item weight as a proven zero.");
            }
            if (TryPlanScannedItemGlobalV1170(
                    FarmingGuideLoadoutSnapshot.Empty,
                    snapshots[unknownSizeId],
                    unknownSize,
                    out _))
            {
                throw new InvalidOperationException("v1.17 treated unknown item dimensions as a proven 1x1 footprint.");
            }

            // Missing price for a Flea-tradable current root is uncertainty, not zero value.
            // The optimizer must refuse to prove a destructive recommendation.
            snapshots[incomingId] = V1170SmokeSnapshot(incomingId, flea: 100_000, firNeed: 0);
            var unknownCurrent = V1170SmokeStoredSnapshot(unknownValueId, "unknown-value-instance");
            if (TryPlanScannedItemGlobalV1170(
                    unknownCurrent,
                    snapshots[incomingId],
                    incoming,
                    out _))
            {
                throw new InvalidOperationException("v1.17 treated an unknown tradable Flea price as a proven zero value.");
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

    private void ClearV1170SmokeLocks()
    {
        _lockedEquipmentSlots.Clear();
        _lockedCarriers.Clear();
        _lockedItemInstanceIds.Clear();
        _reservedCells.Clear();
    }

    private static FarmingGuideLoadoutSnapshot V1170SmokeStoredSnapshot(
        string itemId,
        string instanceId) =>
        FarmingGuideLoadoutSnapshot.Empty with
        {
            StoredItems =
            [
                new FarmingGuideStoredItemState(
                    instanceId,
                    FarmingGuideItemState.Create(itemId),
                    FarmingGuideStorageKind.Pockets,
                    0, 0, 0, false),
            ],
        };

    private static ScannerItemSnapshot V1170SmokeSnapshot(
        string itemId,
        int? flea,
        int firNeed) =>
        new(
            itemId,
            itemId,
            null,
            null,
            flea,
            null,
            null,
            1,
            firNeed,
            null)
        {
            CurrentNeededFir = firNeed,
        };

    private static GameItem V1170SmokeItem(
        string id,
        decimal? weightKg,
        IReadOnlyList<string>? typeKeys = null,
        int? width = 1,
        int? height = 1) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            typeKeys ?? [],
            width,
            height,
            weightKg,
            1_000,
            true);
}
