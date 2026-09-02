using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Published-EXE regression coverage for the v1.16.3 raid-decision safety pass.
    /// Each scenario owns a synthetic inventory and restores the real page state before
    /// returning so the smoke itself cannot alter the user's working Farming Guide state.
    /// </summary>
    private void VerifyV1163RaidDecisionSafetySmoke()
    {
        VerifyV1163SecurePromotionBeforeFreePocketSmoke();
        VerifyV1163LockedCarrierStorageSmoke();
        VerifyV1163ExpandedPocketTransitionSmoke();
        VerifyV1163StackTotalValueSmoke();
        VerifyV1163NonPrefixVictimSmoke();
        VerifyV1163SurvivalReserveSmoke();
        VerifyV1163CurrentWeaponAmmoReserveSmoke();
        VerifyV1163LockedItemMayMoveSmoke();
        VerifyV1163FirOnlyNeedSmoke();
    }

    private void VerifyV1163SecurePromotionBeforeFreePocketSmoke()
    {
        const string secureId = "__junhyun_smoke_v1163_secure";
        const string lowId = "__junhyun_smoke_v1163_secure_low";
        const string incomingId = "__junhyun_smoke_v1163_secure_high";
        const string lowInstanceId = "__junhyun_smoke_v1163_secure_low_instance";
        var ids = new[] { secureId, lowId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[secureId] = V1163Carrier(secureId, "ItemPropertiesContainer", 1, 1);
            _itemsById[lowId] = SmokeItem(lowId);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                _pocketGrids = [V1163Grid(1, 1)];
                SetCarrier(FarmingGuideStorageKind.SecureContainer, FarmingGuideItemState.Create(secureId));
                StoredItems.Add(V1163Stored(
                    lowInstanceId,
                    lowId,
                    FarmingGuideStorageKind.SecureContainer,
                    x: 0,
                    y: 0));
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [lowId] = V1163Fact(lowId, flea: 1_000),
                });

                var incoming = _itemsById[incomingId];
                var recommendation = PlanV1163DecisionSmoke(incoming, V1163Fact(incomingId, flea: 100_000));
                if (recommendation.Action != FarmingGuideInstructionAction.Store)
                    throw new InvalidOperationException($"v1.16.3 secure promotion returned {recommendation.Action}.");

                var promoted = FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId)
                    ?? throw new InvalidOperationException("High-priority incoming loot disappeared during secure promotion.");
                if (promoted.Storage != FarmingGuideStorageKind.SecureContainer || promoted.ParentInstanceId is not null)
                    throw new InvalidOperationException("High-priority incoming loot was not promoted into the secure-container root.");

                var demoted = recommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    string.Equals(value.InstanceId, lowInstanceId, StringComparison.Ordinal));
                if (demoted is null || demoted.Storage != FarmingGuideStorageKind.Pockets)
                    throw new InvalidOperationException("Lower-priority secure loot was not preserved in the free pocket.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163LockedCarrierStorageSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_locked_backpack";
        const string incomingId = "__junhyun_smoke_v1163_locked_backpack_loot";
        var ids = new[] { backpackId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                _lockedCarriers.Add(FarmingGuideStorageKind.Backpack);
                var incoming = _itemsById[incomingId];
                var recommendation = TransitionFromDiscardV1163(
                    incoming,
                    V1163Fact(incomingId, flea: 10_000));

                if (recommendation.Action != FarmingGuideInstructionAction.Store)
                    throw new InvalidOperationException("A locked backpack incorrectly disabled its free internal storage.");
                var placed = FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId);
                if (placed is null || placed.Storage != FarmingGuideStorageKind.Backpack)
                    throw new InvalidOperationException("Incoming loot was not stored inside the locked backpack.");
                if (!SameRootItemV1155(
                        BuildSnapshot().Backpack,
                        recommendation.ProposedSnapshot.Backpack))
                {
                    throw new InvalidOperationException("Using locked-carrier storage replaced the locked carrier root.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163ExpandedPocketTransitionSmoke()
    {
        const string incomingId = "__junhyun_smoke_v1163_expanded_pocket_loot";
        var previousItems = CaptureV1163SmokeItems([incomingId]);

        try
        {
            _itemsById[incomingId] = SmokeItem(incomingId) with { Width = 2, Height = 1 };
            RunV1163DecisionScenario(() =>
            {
                _pocketGrids = [V1163Grid(2, 1)];
                var incoming = _itemsById[incomingId];
                var recommendation = TransitionFromDiscardV1163(
                    incoming,
                    V1163Fact(incomingId, flea: 20_000, slots: 2));

                if (recommendation.Action != FarmingGuideInstructionAction.Store)
                    throw new InvalidOperationException("The v1.16.3 transition path ignored expanded-pocket geometry.");
                var placed = FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId);
                if (placed is null || placed.Storage != FarmingGuideStorageKind.Pockets)
                    throw new InvalidOperationException("A 2x1 item was not placed into the synthetic expanded pocket.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163StackTotalValueSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_stack_backpack";
        const string stackId = "__junhyun_smoke_v1163_stack_item";
        const string incomingId = "__junhyun_smoke_v1163_stack_incoming";
        const string stackInstanceId = "__junhyun_smoke_v1163_stack_instance";
        var ids = new[] { backpackId, stackId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[stackId] = SmokeItem(stackId);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                StoredItems.Add(V1163Stored(
                    stackInstanceId,
                    stackId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0,
                    quantity: 60));
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [stackId] = V1163Fact(stackId, flea: 1_000),
                });

                var recommendation = TransitionFromDiscardV1163(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 10_000));
                if (recommendation.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("A ₽60,000 stored stack was valued as one ₽1,000 unit during eviction.");
                if (!recommendation.ProposedSnapshot.StoredItems.Any(value =>
                        string.Equals(value.InstanceId, stackInstanceId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Quantity-aware stack protection lost the existing stack.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163NonPrefixVictimSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_subset_backpack";
        const string cheapId = "__junhyun_smoke_v1163_subset_cheap";
        const string blockerId = "__junhyun_smoke_v1163_subset_blocker";
        const string incomingId = "__junhyun_smoke_v1163_subset_incoming";
        const string cheapInstanceId = "__junhyun_smoke_v1163_subset_cheap_instance";
        const string blockerInstanceId = "__junhyun_smoke_v1163_subset_blocker_instance";
        var ids = new[] { backpackId, cheapId, blockerId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 2, 2);
            _itemsById[cheapId] = SmokeItem(cheapId);
            _itemsById[blockerId] = SmokeItem(blockerId) with { Width = 2, Height = 1 };
            _itemsById[incomingId] = SmokeItem(incomingId) with { Width = 2, Height = 2 };

            RunV1163DecisionScenario(() =>
            {
                _pocketGrids = [V1163Grid(1, 1)];
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                StoredItems.Add(V1163Stored(
                    cheapInstanceId,
                    cheapId,
                    FarmingGuideStorageKind.Pockets,
                    x: 0,
                    y: 0));
                StoredItems.Add(V1163Stored(
                    blockerInstanceId,
                    blockerId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0));
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [cheapId] = V1163Fact(cheapId, flea: 100),
                    [blockerId] = V1163Fact(blockerId, flea: 1_000, slots: 2),
                });

                var recommendation = TransitionFromDiscardV1163(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 5_000, slots: 4));
                if (recommendation.Action != FarmingGuideInstructionAction.Replace)
                    throw new InvalidOperationException("Bounded subset eviction did not find the geometrically relevant victim.");

                var retainedIds = recommendation.ProposedSnapshot.StoredItems
                    .Select(value => value.InstanceId)
                    .ToHashSet(StringComparer.Ordinal);
                if (!retainedIds.Contains(cheapInstanceId))
                    throw new InvalidOperationException("Subset eviction still discarded the irrelevant cheaper pocket item.");
                if (retainedIds.Contains(blockerInstanceId))
                    throw new InvalidOperationException("Subset eviction retained the actual 2x1 geometric blocker.");
                if (FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId) is null)
                    throw new InvalidOperationException("Subset eviction did not preserve the incoming 2x2 item.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163SurvivalReserveSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_survival_backpack";
        const string rationId = "__junhyun_smoke_v1163_survival_ration";
        const string incomingId = "__junhyun_smoke_v1163_survival_incoming";
        const string rationInstanceId = "__junhyun_smoke_v1163_survival_instance";
        var ids = new[] { backpackId, rationId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[rationId] = SmokeItem(rationId) with
            {
                FarmingGuideData = V1163Layout("ItemPropertiesFoodDrink") with
                {
                    Energy = 25,
                    Hydration = 25,
                },
            };
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                StoredItems.Add(V1163Stored(
                    rationInstanceId,
                    rationId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0));
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [rationId] = V1163Fact(rationId, flea: 100),
                });

                var recommendation = TransitionFromDiscardV1163(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 100_000));
                if (recommendation.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("Automatic eviction consumed the final modeled food/drink reserve.");
                if (!recommendation.ProposedSnapshot.StoredItems.Any(value =>
                        string.Equals(value.InstanceId, rationInstanceId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException("Final food/drink reserve disappeared from the proposed snapshot.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163CurrentWeaponAmmoReserveSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_ammo_backpack";
        const string weaponId = "__junhyun_smoke_v1163_weapon";
        const string ammoId = "__junhyun_smoke_v1163_weapon_ammo";
        const string incomingId = "__junhyun_smoke_v1163_ammo_incoming";
        const string ammoInstanceId = "__junhyun_smoke_v1163_ammo_instance";
        var ids = new[] { backpackId, weaponId, ammoId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[weaponId] = SmokeItem(weaponId) with
            {
                FarmingGuideData = V1163Layout("ItemPropertiesWeapon") with
                {
                    WeaponCaliber = "CaliberSmoke",
                    AllowedAmmoItemIds = [ammoId],
                },
            };
            _itemsById[ammoId] = SmokeItem(ammoId) with
            {
                FarmingGuideData = V1163Layout("ItemPropertiesAmmo") with
                {
                    AmmoCaliber = "CaliberSmoke",
                },
            };
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                Equipment[FarmingGuideEquipmentSlot.PrimaryWeapon1] = FarmingGuideItemState.Create(weaponId);
                StoredItems.Add(V1163Stored(
                    ammoInstanceId,
                    ammoId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0,
                    quantity: 30));
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [ammoId] = V1163Fact(ammoId, flea: 50),
                });

                var recommendation = TransitionFromDiscardV1163(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 100_000));
                if (recommendation.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("Automatic eviction consumed ammunition for the currently carried weapon.");
                if (!recommendation.ProposedSnapshot.StoredItems.Any(value =>
                        string.Equals(value.InstanceId, ammoInstanceId, StringComparison.Ordinal) &&
                        value.NormalizedQuantity == 30))
                {
                    throw new InvalidOperationException("Current-weapon ammunition reserve was not preserved exactly.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163LockedItemMayMoveSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_locked_move_backpack";
        const string lockedId = "__junhyun_smoke_v1163_locked_move_item";
        const string incomingId = "__junhyun_smoke_v1163_locked_move_incoming";
        const string lockedInstanceId = "__junhyun_smoke_v1163_locked_move_instance";
        var ids = new[] { backpackId, lockedId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 2, 1);
            _itemsById[lockedId] = SmokeItem(lockedId);
            _itemsById[incomingId] = SmokeItem(incomingId) with { Width = 2, Height = 1 };

            RunV1163DecisionScenario(() =>
            {
                _pocketGrids = [V1163Grid(1, 1)];
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                StoredItems.Add(V1163Stored(
                    lockedInstanceId,
                    lockedId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0));
                _lockedItemInstanceIds.Add(lockedInstanceId);
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [lockedId] = V1163Fact(lockedId, flea: 500),
                });

                var recommendation = TransitionFromDiscardV1163(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 10_000, slots: 2));
                if (recommendation.Action != FarmingGuideInstructionAction.Store)
                    throw new InvalidOperationException("A locked exact item was incorrectly treated as position-frozen.");

                var moved = recommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    string.Equals(value.InstanceId, lockedInstanceId, StringComparison.Ordinal));
                if (moved is null || moved.Storage != FarmingGuideStorageKind.Pockets)
                    throw new InvalidOperationException("Locked item did not move intact to the free pocket during safe repacking.");
                if (FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId)?.Storage !=
                    FarmingGuideStorageKind.Backpack)
                {
                    throw new InvalidOperationException("Incoming 2x1 loot did not occupy the repacked backpack.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1163FirOnlyNeedSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1163_fir_backpack";
        const string existingId = "__junhyun_smoke_v1163_fir_existing";
        const string incomingId = "__junhyun_smoke_v1163_fir_incoming";
        const string existingInstanceId = "__junhyun_smoke_v1163_fir_existing_instance";
        var ids = new[] { backpackId, existingId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[existingId] = SmokeItem(existingId);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(backpackId));
                StoredItems.Add(V1163Stored(
                    existingInstanceId,
                    existingId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0));

                var facts = new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [existingId] = V1163Fact(existingId, flea: 1_000, currentNeeded: 5, firNeeded: 0),
                };
                SetV1163SmokeFacts(facts);
                var incoming = _itemsById[incomingId];
                var incomingFact = V1163Fact(incomingId, flea: 2_000);
                var economic = TransitionFromDiscardV1163(incoming, incomingFact);
                if (economic.Action != FarmingGuideInstructionAction.Replace)
                    throw new InvalidOperationException("A non-FIR general need still received protected FIR priority.");

                facts[existingId] = V1163Fact(existingId, flea: 1_000, currentNeeded: 5, firNeeded: 1);
                var protectedFir = TransitionFromDiscardV1163(incoming, incomingFact);
                if (protectedFir.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("An actually FIR-needed existing item was allowed into the victim pool.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private RaidRecommendation PlanV1163DecisionSmoke(GameItem incoming, ScannerItemSnapshot decisionScan)
    {
        var current = BuildSnapshot();
        var planned = PlanScannedItemRulebookV1160(current, decisionScan, incoming);
        var transitioned = ApplyRaidStateTransitionsV1163(current, planned, decisionScan, incoming);
        var optimized = OptimizeDestructiveRaidPlanV1155(current, transitioned, decisionScan, incoming);
        var quantityApplied = ApplyIncomingQuantityV1160(
            current,
            optimized,
            incoming.Id,
            Math.Max(1, decisionScan.Quantity));
        return ApplyFinalRaidSafetyV1163(current, quantityApplied, decisionScan);
    }

    private RaidRecommendation TransitionFromDiscardV1163(GameItem incoming, ScannerItemSnapshot decisionScan)
    {
        var current = BuildSnapshot();
        var transitioned = ApplyRaidStateTransitionsV1163(
            current,
            new RaidRecommendation("legacy discard", FarmingGuideInstructionAction.Discard, current),
            decisionScan,
            incoming);
        var optimized = OptimizeDestructiveRaidPlanV1155(current, transitioned, decisionScan, incoming);
        var quantityApplied = ApplyIncomingQuantityV1160(
            current,
            optimized,
            incoming.Id,
            Math.Max(1, decisionScan.Quantity));
        return ApplyFinalRaidSafetyV1163(current, quantityApplied, decisionScan);
    }

    private void RunV1163DecisionScenario(Action scenario)
    {
        var previousSnapshot = BuildSnapshot();
        var previousLocks = BuildLockState();
        var previousPockets = _pocketGrids;
        var previousBridge = _raidBridge;
        var previousSession = _raidSession;
        var previousPlannedLocks = _plannedLocksOverrideV1160;
        var previousAccepted = _acceptedRaidItemCounts.ToArray();
        var previousPrices = _raidFleaAveragePrices.ToArray();

        try
        {
            _raidSession = null;
            _plannedLocksOverrideV1160 = null;
            _raidBridge = new FarmingGuideRaidBridge();
            _acceptedRaidItemCounts.Clear();
            _raidFleaAveragePrices.Clear();
            _pocketGrids = [];
            Equipment.Clear();
            SetCarrier(FarmingGuideStorageKind.Rig, null);
            SetCarrier(FarmingGuideStorageKind.Backpack, null);
            SetCarrier(FarmingGuideStorageKind.SecureContainer, null);
            StoredItems.Clear();
            ApplyLockState(FarmingGuideLockState.Empty);
            scenario();
        }
        finally
        {
            _pocketGrids = previousPockets;
            _raidBridge = previousBridge;
            _raidSession = previousSession;
            _plannedLocksOverrideV1160 = previousPlannedLocks;
            _acceptedRaidItemCounts.Clear();
            foreach (var pair in previousAccepted)
                _acceptedRaidItemCounts[pair.Key] = pair.Value;
            _raidFleaAveragePrices.Clear();
            foreach (var pair in previousPrices)
                _raidFleaAveragePrices[pair.Key] = pair.Value;
            ApplySnapshot(previousSnapshot);
            ApplyLockState(previousLocks);
        }
    }

    private void SetV1163SmokeFacts(IReadOnlyDictionary<string, ScannerItemSnapshot> facts)
    {
        var bridge = new FarmingGuideRaidBridge();
        bridge.SetScannerSnapshotResolver(itemId => facts.TryGetValue(itemId, out var value) ? value : null);
        _raidBridge = bridge;
    }

    private Dictionary<string, GameItem?> CaptureV1163SmokeItems(IEnumerable<string> ids) =>
        ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var value) ? value : null,
            StringComparer.Ordinal);

    private void RestoreV1163SmokeItems(IReadOnlyDictionary<string, GameItem?> previous)
    {
        foreach (var pair in previous)
        {
            if (pair.Value is not null)
                _itemsById[pair.Key] = pair.Value;
            else
                _itemsById.Remove(pair.Key);
        }
    }

    private static FarmingGuideStoredItemState V1163Stored(
        string instanceId,
        string itemId,
        FarmingGuideStorageKind storage,
        int x,
        int y,
        int quantity = 1) =>
        new(
            instanceId,
            FarmingGuideItemState.Create(itemId),
            storage,
            GridIndex: 0,
            X: x,
            Y: y,
            Rotated: false,
            ParentInstanceId: null,
            Quantity: quantity);

    private static FarmingGuideStoredItemState? FindV1163StoredByItem(
        FarmingGuideLoadoutSnapshot snapshot,
        string itemId) =>
        snapshot.StoredItems.FirstOrDefault(value =>
            string.Equals(value.Item.ItemId, itemId, StringComparison.Ordinal));

    private static GameItem V1163Carrier(
        string id,
        string propertiesType,
        int width,
        int height) =>
        SmokeItem(id) with
        {
            FarmingGuideData = V1163Layout(propertiesType) with
            {
                StorageGrids = [V1163Grid(width, height)],
            },
        };

    private static FarmingGuideItemLayout V1163Layout(string propertiesType) =>
        new(
            propertiesType,
            [],
            [],
            [],
            [],
            [],
            false,
            false);

    private static FarmingGuideStorageGridDefinition V1163Grid(int width, int height) =>
        new(width, height, FarmingGuideItemFilter.Empty);

    private static ScannerItemSnapshot V1163Fact(
        string itemId,
        int flea,
        int slots = 1,
        int currentNeeded = 0,
        int firNeeded = 0,
        int quantity = 1) =>
        new(
            itemId,
            itemId,
            null,
            TraderSellPrice: flea,
            FleaAveragePrice: flea,
            TraderPricePerSlot: slots <= 0 ? flea : flea / Math.Max(1, slots),
            FleaPricePerSlot: slots <= 0 ? flea : flea / Math.Max(1, slots),
            Slots: Math.Max(1, slots),
            CurrentNeeded: currentNeeded)
        {
            CurrentNeededFir = firNeeded,
            Quantity = Math.Max(1, quantity),
        };
}
