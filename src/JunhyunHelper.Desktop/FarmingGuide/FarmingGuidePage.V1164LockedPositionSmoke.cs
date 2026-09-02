using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.16.4 supersedes the v1.16.3 exact-item-lock movement scenario. All other
    /// v1.16.3 decision boundaries remain part of the published EXE smoke, while the new
    /// regressions prove that an explicit stored-item lock freezes physical placement.
    /// </summary>
    private void VerifyV1164RaidDecisionSafetySmoke()
    {
        VerifyV1163SecurePromotionBeforeFreePocketSmoke();
        VerifyV1163LockedCarrierStorageSmoke();
        VerifyV1163ExpandedPocketTransitionSmoke();
        VerifyV1163StackTotalValueSmoke();
        VerifyV1163NonPrefixVictimSmoke();
        VerifyV1163SurvivalReserveSmoke();
        VerifyV1163CurrentWeaponAmmoReserveSmoke();
        VerifyV1163FirOnlyNeedSmoke();

        VerifyV1164LockedSecureItemStaysPutSmoke();
        VerifyV1164LockedItemBlocksAutomaticRepackingSmoke();
        VerifyV1164FinalSafetyRejectsMovedLockSmoke();
        VerifyV1164LockedItemBlocksCarrierReplacementSmoke();
    }

    /// <summary>
    /// Direct regression for the user-observed failure shape: a secure-container item is
    /// explicitly locked, ordinary free storage exists, and a higher-value secure-eligible
    /// scan arrives. The incoming item may use ordinary storage, but the locked secure item
    /// must not be demoted merely to optimize protection.
    /// </summary>
    private void VerifyV1164LockedSecureItemStaysPutSmoke()
    {
        const string secureId = "__junhyun_smoke_v1164_locked_secure";
        const string lockedId = "__junhyun_smoke_v1164_locked_secure_item";
        const string incomingId = "__junhyun_smoke_v1164_locked_secure_incoming";
        const string lockedInstanceId = "__junhyun_smoke_v1164_locked_secure_instance";
        var ids = new[] { secureId, lockedId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[secureId] = V1163Carrier(secureId, "ItemPropertiesContainer", 1, 1);
            _itemsById[lockedId] = SmokeItem(lockedId);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                _pocketGrids = [V1163Grid(1, 1)];
                SetCarrier(FarmingGuideStorageKind.SecureContainer, FarmingGuideItemState.Create(secureId));
                StoredItems.Add(V1163Stored(
                    lockedInstanceId,
                    lockedId,
                    FarmingGuideStorageKind.SecureContainer,
                    x: 0,
                    y: 0));
                _lockedItemInstanceIds.Add(lockedInstanceId);
                SetV1163SmokeFacts(new Dictionary<string, ScannerItemSnapshot>(StringComparer.Ordinal)
                {
                    [lockedId] = V1163Fact(lockedId, flea: 1_000),
                });

                var recommendation = PlanV1164DecisionSmoke(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 100_000));
                if (recommendation.Action != FarmingGuideInstructionAction.Store)
                    throw new InvalidOperationException("A locked secure item prevented use of unrelated free ordinary storage.");

                var locked = recommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    string.Equals(value.InstanceId, lockedInstanceId, StringComparison.Ordinal));
                if (locked is null ||
                    locked.Storage != FarmingGuideStorageKind.SecureContainer ||
                    locked.GridIndex != 0 || locked.X != 0 || locked.Y != 0 || locked.Rotated ||
                    locked.ParentInstanceId is not null)
                {
                    throw new InvalidOperationException("Automatic secure promotion moved the explicitly locked item.");
                }

                var incoming = FindV1163StoredByItem(recommendation.ProposedSnapshot, incomingId);
                if (incoming is null || incoming.Storage != FarmingGuideStorageKind.Pockets)
                    throw new InvalidOperationException("Incoming loot did not fall back to the free ordinary pocket.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1164LockedItemBlocksAutomaticRepackingSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1164_locked_repack_backpack";
        const string lockedId = "__junhyun_smoke_v1164_locked_repack_item";
        const string incomingId = "__junhyun_smoke_v1164_locked_repack_incoming";
        const string lockedInstanceId = "__junhyun_smoke_v1164_locked_repack_instance";
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
                    [lockedId] = V1163Fact(lockedId, flea: 100),
                });

                var recommendation = PlanV1164DecisionSmoke(
                    _itemsById[incomingId],
                    V1163Fact(incomingId, flea: 100_000, slots: 2));
                if (recommendation.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("Automatic repacking moved a locked blocker to make room for incoming loot.");

                var locked = recommendation.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                    string.Equals(value.InstanceId, lockedInstanceId, StringComparison.Ordinal));
                if (locked is null || locked.Storage != FarmingGuideStorageKind.Backpack || locked.X != 0 || locked.Y != 0)
                    throw new InvalidOperationException("Locked repacking blocker did not remain at its exact placement.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1164FinalSafetyRejectsMovedLockSmoke()
    {
        const string backpackId = "__junhyun_smoke_v1164_final_backpack";
        const string lockedId = "__junhyun_smoke_v1164_final_locked";
        const string incomingId = "__junhyun_smoke_v1164_final_incoming";
        const string lockedInstanceId = "__junhyun_smoke_v1164_final_locked_instance";
        var ids = new[] { backpackId, lockedId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[backpackId] = V1163Carrier(backpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[lockedId] = SmokeItem(lockedId);
            _itemsById[incomingId] = SmokeItem(incomingId);

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

                var current = BuildSnapshot();
                var moved = current.StoredItems
                    .Select(value => string.Equals(value.InstanceId, lockedInstanceId, StringComparison.Ordinal)
                        ? value with
                        {
                            Storage = FarmingGuideStorageKind.Pockets,
                            GridIndex = 0,
                            X = 0,
                            Y = 0,
                            ParentInstanceId = null,
                        }
                        : value)
                    .ToArray();
                var unsafeRecommendation = new RaidRecommendation(
                    "unsafe locked move",
                    FarmingGuideInstructionAction.Store,
                    current with { StoredItems = moved });
                var checkedRecommendation = ApplyFinalRaidSafetyV1164(
                    current,
                    unsafeRecommendation,
                    V1163Fact(incomingId, flea: 10_000));

                if (checkedRecommendation.Action != FarmingGuideInstructionAction.Discard ||
                    !PreservesLockedItemPlacementV1164(current, checkedRecommendation.ProposedSnapshot))
                {
                    throw new InvalidOperationException("Final safety did not fail closed on a moved locked-item proposal.");
                }
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private void VerifyV1164LockedItemBlocksCarrierReplacementSmoke()
    {
        const string oldBackpackId = "__junhyun_smoke_v1164_root_old_backpack";
        const string newBackpackId = "__junhyun_smoke_v1164_root_new_backpack";
        const string lockedId = "__junhyun_smoke_v1164_root_locked";
        const string incomingId = "__junhyun_smoke_v1164_root_incoming";
        const string lockedInstanceId = "__junhyun_smoke_v1164_root_locked_instance";
        var ids = new[] { oldBackpackId, newBackpackId, lockedId, incomingId };
        var previousItems = CaptureV1163SmokeItems(ids);

        try
        {
            _itemsById[oldBackpackId] = V1163Carrier(oldBackpackId, "ItemPropertiesBackpack", 1, 1);
            _itemsById[newBackpackId] = V1163Carrier(newBackpackId, "ItemPropertiesBackpack", 2, 1);
            _itemsById[lockedId] = SmokeItem(lockedId);
            _itemsById[incomingId] = SmokeItem(incomingId);

            RunV1163DecisionScenario(() =>
            {
                SetCarrier(FarmingGuideStorageKind.Backpack, FarmingGuideItemState.Create(oldBackpackId));
                StoredItems.Add(V1163Stored(
                    lockedInstanceId,
                    lockedId,
                    FarmingGuideStorageKind.Backpack,
                    x: 0,
                    y: 0));
                _lockedItemInstanceIds.Add(lockedInstanceId);

                var current = BuildSnapshot();
                var replacedCarrier = current with
                {
                    Backpack = FarmingGuideItemState.Create(newBackpackId),
                };
                var unsafeRecommendation = new RaidRecommendation(
                    "unsafe carrier replacement",
                    FarmingGuideInstructionAction.ReplaceEquip,
                    replacedCarrier);
                var checkedRecommendation = ApplyFinalRaidSafetyV1164(
                    current,
                    unsafeRecommendation,
                    V1163Fact(incomingId, flea: 10_000));

                if (checkedRecommendation.Action != FarmingGuideInstructionAction.Discard)
                    throw new InvalidOperationException("Replacing the root carrier indirectly moved an explicitly locked item.");
            });
        }
        finally
        {
            RestoreV1163SmokeItems(previousItems);
        }
    }

    private RaidRecommendation PlanV1164DecisionSmoke(GameItem incoming, ScannerItemSnapshot decisionScan)
    {
        var current = BuildSnapshot();
        var planned = PlanScannedItemRulebookV1164(current, decisionScan, incoming);
        var transitioned = ApplyRaidStateTransitionsV1164(current, planned, decisionScan, incoming);
        var quantityApplied = ApplyIncomingQuantityV1160(
            current,
            transitioned,
            incoming.Id,
            Math.Max(1, decisionScan.Quantity));
        return ApplyFinalRaidSafetyV1164(current, quantityApplied, decisionScan);
    }
}
