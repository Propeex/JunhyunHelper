using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private void VerifyV1155RaidStateTransitionSmoke()
    {
        const string oldRigId = "__junhyun_smoke_v1155_transition_old_rig";
        const string newRigId = "__junhyun_smoke_v1155_transition_new_rig";
        const string bagId = "__junhyun_smoke_v1155_transition_bag";
        const string blockerId = "__junhyun_smoke_v1155_transition_blocker";
        const string blockerInstanceId = "__junhyun_smoke_v1155_transition_blocker_instance";

        var ids = new[] { oldRigId, newRigId, bagId, blockerId };
        var previous = ids.ToDictionary(
            id => id,
            id => _itemsById.TryGetValue(id, out var value) ? value : null,
            StringComparer.Ordinal);
        var previousLocks = BuildLockState();

        var oldRig = NamedSmokeItem(oldRigId, "기존 리그") with
        {
            Width = 2,
            Height = 2,
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesChestRig",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var newRigOnlyGrid = new FarmingGuideItemFilter(
            ["__junhyun_smoke_v1155_transition_never_category"],
            [],
            [],
            []);
        var newRig = NamedSmokeItem(newRigId, "신규 리그") with
        {
            Width = 2,
            Height = 2,
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesChestRig",
                [new FarmingGuideStorageGridDefinition(1, 1, newRigOnlyGrid)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var bag = NamedSmokeItem(bagId, "가방") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesBackpack",
                [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                [],
                false,
                false),
        };
        var blocker = NamedSmokeItem(blockerId, "블로커") with { Width = 2, Height = 2 };

        _itemsById[oldRigId] = oldRig;
        _itemsById[newRigId] = newRig;
        _itemsById[bagId] = bag;
        _itemsById[blockerId] = blocker;

        try
        {
            ApplyLockState(FarmingGuideLockState.Empty);
            var blockerState = new FarmingGuideStoredItemState(
                blockerInstanceId,
                FarmingGuideItemState.Create(blockerId),
                FarmingGuideStorageKind.Backpack,
                0,
                0,
                0,
                false);
            var current = new FarmingGuideLoadoutSnapshot(
                new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
                FarmingGuideItemState.Create(oldRigId),
                FarmingGuideItemState.Create(bagId),
                null,
                [blockerState]);
            var rawProposed = current with { Rig = FarmingGuideItemState.Create(newRigId) };
            var transitioned = PreserveDisplacedTopLevelItemsV1155(
                current,
                new RaidRecommendation(
                    "legacy destructive rig replacement",
                    FarmingGuideInstructionAction.ReplaceEquip,
                    rawProposed));

            var displacedRig = transitioned.ProposedSnapshot.StoredItems.SingleOrDefault(value =>
                value.InstanceId.StartsWith(V1155DisplacedInstancePrefix, StringComparison.Ordinal) &&
                string.Equals(value.Item.ItemId, oldRigId, StringComparison.Ordinal));
            if (displacedRig is null ||
                displacedRig.ParentInstanceId is not null ||
                displacedRig.Storage != FarmingGuideStorageKind.Backpack)
            {
                throw new InvalidOperationException(
                    "Displaced rig was not preserved as loot inside the equipped backpack.");
            }

            var movedBlocker = transitioned.ProposedSnapshot.StoredItems.Single(value =>
                string.Equals(value.InstanceId, blockerInstanceId, StringComparison.Ordinal));
            if (!string.Equals(movedBlocker.ParentInstanceId, displacedRig.InstanceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Backpack blocker was not allowed to move into the displaced rig's newly available storage.");
            }

            var formatted = ApplyRaidInstructionPresentationV1155(
                current,
                transitioned,
                newRig);
            var expected = "리그 교체 + 기존 리그 이동 가방, 블로커 이동 기존 리그 내부";
            if (!string.Equals(formatted.Instruction, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"State-transition instruction mismatch. Expected '{expected}', got '{formatted.Instruction}'.");
            }
        }
        finally
        {
            ApplyLockState(previousLocks);
            foreach (var id in ids)
            {
                if (previous[id] is { } value)
                    _itemsById[id] = value;
                else
                    _itemsById.Remove(id);
            }
        }
    }
}
