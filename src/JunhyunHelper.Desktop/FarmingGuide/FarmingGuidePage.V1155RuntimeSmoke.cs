using System.Windows.Controls;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1155FarmingGuideSmoke()
    {
        VerifyV1155NestedStorageViewportSmoke();
        VerifyV1155RaidInstructionPresentationSmoke();
        VerifyV1155RaidStateTransitionSmoke();
    }

    private void VerifyV1155NestedStorageViewportSmoke()
    {
        const string caseId = "__junhyun_smoke_v1155_key_tool";
        const string instanceId = "__junhyun_smoke_v1155_key_tool_instance";

        _itemsById.TryGetValue(caseId, out var previousItem);
        var previousSnapshot = BuildSnapshot();
        var previousLocks = BuildLockState();
        var item = NamedSmokeItem(caseId, "Key tool") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesContainer",
                [new FarmingGuideStorageGridDefinition(4, 4, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var placement = new FarmingGuideStoredItemState(
            instanceId,
            FarmingGuideItemState.Create(caseId),
            FarmingGuideStorageKind.SecureContainer,
            0,
            0,
            0,
            false);

        _itemsById[caseId] = item;
        try
        {
            Equipment.Clear();
            SetCarrier(FarmingGuideStorageKind.Rig, null);
            SetCarrier(FarmingGuideStorageKind.Backpack, null);
            SetCarrier(FarmingGuideStorageKind.SecureContainer, null);
            StoredItems.Clear();
            StoredItems.Add(placement);
            ApplyLockState(FarmingGuideLockState.Empty);

            OpenStoredWorkbench(new PlacedItemSource(placement));
            WorkbenchHost.UpdateLayout();

            var dock = WorkbenchHost.Child as DockPanel
                ?? throw new InvalidOperationException("Nested storage workbench lost its DockPanel host.");
            var scrollViewer = dock.Children.OfType<ScrollViewer>().FirstOrDefault()
                ?? throw new InvalidOperationException("Nested storage workbench lost its ScrollViewer.");
            scrollViewer.UpdateLayout();

            if (scrollViewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                throw new InvalidOperationException(
                    "A fitting 4x4 nested container still enables vertical scrolling.");
            }
            if (scrollViewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                throw new InvalidOperationException(
                    "A fitting 4x4 nested container still enables horizontal scrolling.");
            }
            if (scrollViewer.ScrollableHeight > 0.5d || scrollViewer.ScrollableWidth > 0.5d)
            {
                throw new InvalidOperationException(
                    $"A fitting 4x4 nested container is still clipped: scrollable=" +
                    $"{scrollViewer.ScrollableWidth:0.#}x{scrollViewer.ScrollableHeight:0.#}.");
            }
        }
        finally
        {
            CloseWorkbench();
            ApplySnapshot(previousSnapshot);
            ApplyLockState(previousLocks);
            if (previousItem is not null)
                _itemsById[caseId] = previousItem;
            else
                _itemsById.Remove(caseId);
        }
    }

    private void VerifyV1155RaidInstructionPresentationSmoke()
    {
        const string incomingId = "__junhyun_smoke_v1155_incoming";
        const string armoredRigId = "__junhyun_smoke_v1155_armored_rig";
        const string oldEquipmentId = "__junhyun_smoke_v1155_old_equipment";
        const string oldCarrierId = "__junhyun_smoke_v1155_old_carrier";
        const string moveAId = "__junhyun_smoke_v1155_move_a";
        const string moveBId = "__junhyun_smoke_v1155_move_b";
        const string discardId = "__junhyun_smoke_v1155_discard";

        var ids = new[]
        {
            incomingId,
            armoredRigId,
            oldEquipmentId,
            oldCarrierId,
            moveAId,
            moveBId,
            discardId,
        };
        var previous = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var item) ? item : null,
            StringComparer.Ordinal);

        var incoming = NamedSmokeItem(incomingId, "신규");
        var armoredRig = NamedSmokeItem(armoredRigId, "신규 방탄리그") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesChestRig",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                true),
        };
        _itemsById[incomingId] = incoming;
        _itemsById[armoredRigId] = armoredRig;
        _itemsById[oldEquipmentId] = NamedSmokeItem(oldEquipmentId, "기존 장비");
        _itemsById[oldCarrierId] = NamedSmokeItem(oldCarrierId, "기존 가방");
        _itemsById[moveAId] = NamedSmokeItem(moveAId, "이동A");
        _itemsById[moveBId] = NamedSmokeItem(moveBId, "이동B");
        _itemsById[discardId] = NamedSmokeItem(discardId, "버릴것");

        try
        {
            var empty = Snapshot();
            var bodyEquip = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.BodyArmor] = FarmingGuideItemState.Create(incomingId),
            });
            AssertInstruction(
                "방탄복 장착",
                empty,
                FarmingGuideInstructionAction.Equip,
                bodyEquip,
                incoming);

            var oldBody = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.BodyArmor] = FarmingGuideItemState.Create(oldEquipmentId),
            });
            AssertInstruction(
                "방탄복 교체 + 기존 장비 버리기",
                oldBody,
                FarmingGuideInstructionAction.ReplaceEquip,
                bodyEquip,
                incoming);

            var oldHeadset = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Headset] = FarmingGuideItemState.Create(oldEquipmentId),
            });
            var newHeadset = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Headset] = FarmingGuideItemState.Create(incomingId),
            });
            AssertInstruction(
                "헤드셋 교체 + 기존 장비 버리기",
                oldHeadset,
                FarmingGuideInstructionAction.ReplaceEquip,
                newHeadset,
                incoming);

            var oldHelmet = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create(oldEquipmentId),
            });
            var newHelmet = Snapshot(equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.Helmet] = FarmingGuideItemState.Create(incomingId),
            });
            AssertInstruction(
                "헬멧 교체 + 기존 장비 버리기",
                oldHelmet,
                FarmingGuideInstructionAction.ReplaceEquip,
                newHelmet,
                incoming);

            var carrierItem = Stored("carrier-item", moveAId, FarmingGuideStorageKind.Backpack, 0, 0, 0);
            var carrierItemRepacked = carrierItem with { X = 1 };
            var oldBackpack = Snapshot(
                backpack: FarmingGuideItemState.Create(oldCarrierId),
                stored: [carrierItem]);
            var newBackpack = Snapshot(
                backpack: FarmingGuideItemState.Create(incomingId),
                stored: [carrierItemRepacked]);
            AssertInstruction(
                "가방 교체 + 기존 가방 버리기, 이동A 이동 가방",
                oldBackpack,
                FarmingGuideInstructionAction.ReplaceEquip,
                newBackpack,
                incoming);

            var rigItem = Stored("rig-item", moveAId, FarmingGuideStorageKind.Rig, 0, 1, 0);
            var rigItemRepacked = rigItem with { X = 0, Y = 1 };
            var armorAndRig = Snapshot(
                equipment: new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
                {
                    [FarmingGuideEquipmentSlot.BodyArmor] = FarmingGuideItemState.Create(oldEquipmentId),
                },
                rig: FarmingGuideItemState.Create(oldCarrierId),
                stored: [rigItem]);
            var armoredRigTransition = Snapshot(
                rig: FarmingGuideItemState.Create(armoredRigId),
                stored: [rigItemRepacked]);
            AssertInstruction(
                "방탄 리그 전환 + 기존 장비 버리기, 기존 가방 버리기, 이동A 이동 리그",
                armorAndRig,
                FarmingGuideInstructionAction.ReplaceEquip,
                armoredRigTransition,
                armoredRig);

            var directStored = Stored("incoming-direct", incomingId, FarmingGuideStorageKind.SecureContainer, 0, 0, 0);
            AssertInstruction(
                "컨테이너 보관",
                empty,
                FarmingGuideInstructionAction.Store,
                Snapshot(stored: [directStored]),
                incoming);

            var sameArea = Stored("same-area", moveAId, FarmingGuideStorageKind.Backpack, 0, 0, 0);
            var sameAreaMoved = sameArea with { GridIndex = 1, X = 2, Y = 1, Rotated = true };
            var incomingStored = Stored("incoming-repacked", incomingId, FarmingGuideStorageKind.Backpack, 0, 0, 0);
            AssertInstruction(
                "가방 보관",
                Snapshot(stored: [sameArea]),
                FarmingGuideInstructionAction.Store,
                Snapshot(stored: [sameAreaMoved, incomingStored]),
                incoming);

            var moveA = Stored("move-a", moveAId, FarmingGuideStorageKind.Rig, 0, 0, 0);
            var moveB = Stored("move-b", moveBId, FarmingGuideStorageKind.SecureContainer, 0, 0, 0);
            var discard = Stored("discard", discardId, FarmingGuideStorageKind.Backpack, 0, 0, 0);
            var moveATarget = moveA with { Storage = FarmingGuideStorageKind.Backpack, X = 1 };
            var moveBTarget = moveB with { Storage = FarmingGuideStorageKind.Rig, X = 1 };
            var replacementIncoming = Stored("incoming-replace", incomingId, FarmingGuideStorageKind.Backpack, 0, 0, 0);
            AssertInstruction(
                "가방 버릴것 버리고 보관 + 이동A 이동 가방, 이동B 이동 리그",
                Snapshot(stored: [moveA, moveB, discard]),
                FarmingGuideInstructionAction.Replace,
                Snapshot(stored: [moveATarget, moveBTarget, replacementIncoming]),
                incoming);

            AssertInstruction(
                "버리기",
                empty,
                FarmingGuideInstructionAction.Discard,
                empty,
                incoming);
        }
        finally
        {
            foreach (var id in ids)
            {
                if (previous[id] is { } original)
                    _itemsById[id] = original;
                else
                    _itemsById.Remove(id);
            }
        }

        void AssertInstruction(
            string expected,
            FarmingGuideLoadoutSnapshot current,
            FarmingGuideInstructionAction action,
            FarmingGuideLoadoutSnapshot proposed,
            GameItem scannedItem)
        {
            var formatted = ApplyRaidInstructionPresentationV1155(
                current,
                new RaidRecommendation("legacy verbose instruction", action, proposed),
                scannedItem);
            if (!string.Equals(formatted.Instruction, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Compact Farming Guide instruction mismatch. Expected '{expected}', got '{formatted.Instruction}'.");
            }
        }

        static FarmingGuideStoredItemState Stored(
            string instanceId,
            string itemId,
            FarmingGuideStorageKind storage,
            int grid,
            int x,
            int y) =>
            new(instanceId, FarmingGuideItemState.Create(itemId), storage, grid, x, y, false);

        static FarmingGuideLoadoutSnapshot Snapshot(
            IReadOnlyDictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>? equipment = null,
            FarmingGuideItemState? rig = null,
            FarmingGuideItemState? backpack = null,
            FarmingGuideItemState? secure = null,
            IReadOnlyList<FarmingGuideStoredItemState>? stored = null) =>
            new(
                equipment ?? new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
                rig,
                backpack,
                secure,
                stored ?? []);
    }

    private static GameItem NamedSmokeItem(string id, string name) =>
        new(
            id,
            name,
            name,
            name,
            name,
            null,
            null,
            [],
            [],
            [],
            1,
            1);
}
