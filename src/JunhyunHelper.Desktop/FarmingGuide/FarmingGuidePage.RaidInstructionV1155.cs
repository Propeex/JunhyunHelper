using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.15.5 presentation pass. Raid planning remains authoritative; this layer only
    /// compresses the resulting recommendation into the user's glance-readable instruction
    /// vocabulary and reports extra manipulation only when an existing item actually crosses
    /// a storage-area boundary or is removed.
    /// </summary>
    private RaidRecommendation ApplyRaidInstructionPresentationV1155(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        GameItem incoming)
    {
        var instruction = recommendation.Action switch
        {
            FarmingGuideInstructionAction.Equip =>
                FormatEquipInstructionV1155(current, recommendation.ProposedSnapshot, incoming),
            FarmingGuideInstructionAction.ReplaceEquip =>
                FormatEquipmentReplacementInstructionV1155(current, recommendation.ProposedSnapshot, incoming),
            FarmingGuideInstructionAction.Store =>
                FormatStoreInstructionV1155(current, recommendation.ProposedSnapshot, incoming),
            FarmingGuideInstructionAction.Replace =>
                FormatStorageReplacementInstructionV1155(current, recommendation.ProposedSnapshot, incoming),
            FarmingGuideInstructionAction.Discard => "버리기",
            _ => recommendation.Instruction,
        };

        return recommendation with { Instruction = instruction };
    }

    private string FormatEquipInstructionV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming)
    {
        if (TryFindChangedEquipmentSlot(current, proposed, incoming.Id, out var slot))
            return $"{EquipmentLabel(slot)} 장착";
        if (TryFindChangedCarrier(current, proposed, incoming.Id, out var carrier))
            return $"{CarrierLabel(carrier)} 장착";
        return "장비 장착";
    }

    private string FormatEquipmentReplacementInstructionV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming)
    {
        string primary;
        if (IsArmoredRigTransitionV1155(current, proposed, incoming))
        {
            primary = "방탄 리그 전환";
        }
        else if (TryFindChangedEquipmentSlot(current, proposed, incoming.Id, out var slot))
        {
            primary = slot switch
            {
                FarmingGuideEquipmentSlot.BodyArmor => "방탄복 교체",
                FarmingGuideEquipmentSlot.Headset => "헤드셋 교체",
                _ => $"{EquipmentLabel(slot)} 교체",
            };
        }
        else if (TryFindChangedCarrier(current, proposed, incoming.Id, out var carrier))
        {
            primary = $"{CarrierLabel(carrier)} 교체";
        }
        else
        {
            primary = "장비 교체";
        }

        var operations = new List<string>();
        operations.AddRange(BuildDisplacedTopLevelOperationsV1155(current, proposed));
        operations.AddRange(BuildExistingItemOperationsV1155(current, proposed, includeRemoved: true));
        return AppendExistingItemOperationsV1155(primary, operations);
    }

    private string FormatStoreInstructionV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming)
    {
        var placement = FindIncomingStoredPlacementV1155(current, proposed, incoming.Id);
        var location = placement is null
            ? "수납 공간"
            : StorageLocationLabelV1155(proposed, placement);
        var primary = $"{location} 보관";

        return AppendExistingItemOperationsV1155(
            primary,
            BuildExistingItemOperationsV1155(current, proposed, includeRemoved: true));
    }

    private string FormatStorageReplacementInstructionV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming)
    {
        var placement = FindIncomingStoredPlacementV1155(current, proposed, incoming.Id);
        var location = placement is null
            ? "수납 공간"
            : StorageLocationLabelV1155(proposed, placement);

        var proposedIds = proposed.StoredItems
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var removed = current.StoredItems
            .Where(value => !proposedIds.Contains(value.InstanceId))
            .Select(value => ResolveItem(value.Item) is { } item ? DisplayName(item) : "아이템")
            .ToArray();

        var primary = removed.Length == 0
            ? $"{location} 보관"
            : $"{location} {string.Join(", ", removed)} 버리고 보관";

        return AppendExistingItemOperationsV1155(
            primary,
            BuildExistingItemOperationsV1155(current, proposed, includeRemoved: false));
    }

    private IReadOnlyList<string> BuildDisplacedTopLevelOperationsV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        var operations = new List<string>();
        var available = proposed.StoredItems
            .Where(value => value.InstanceId.StartsWith(V1155DisplacedInstancePrefix, StringComparison.Ordinal))
            .ToList();
        var consumed = new HashSet<string>(StringComparer.Ordinal);

        void Add(FarmingGuideItemState? before, FarmingGuideItemState? after)
        {
            if (before is null || SameRootItemV1155(before, after))
                return;
            var item = ResolveItem(before);
            var name = item is null ? "아이템" : DisplayName(item);
            var stored = available.FirstOrDefault(value =>
                !consumed.Contains(value.InstanceId) &&
                string.Equals(value.Item.ItemId, before.ItemId, StringComparison.Ordinal));
            if (stored is null)
            {
                operations.Add($"{name} 버리기");
                return;
            }

            consumed.Add(stored.InstanceId);
            operations.Add($"{name} 이동 {StorageLocationLabelV1155(proposed, stored)}");
        }

        foreach (var slot in new[]
                 {
                     FarmingGuideEquipmentSlot.Headset,
                     FarmingGuideEquipmentSlot.Helmet,
                     FarmingGuideEquipmentSlot.FaceCover,
                     FarmingGuideEquipmentSlot.Armband,
                     FarmingGuideEquipmentSlot.BodyArmor,
                     FarmingGuideEquipmentSlot.Eyewear,
                     FarmingGuideEquipmentSlot.PrimaryWeapon1,
                     FarmingGuideEquipmentSlot.PrimaryWeapon2,
                     FarmingGuideEquipmentSlot.Holster,
                 })
        {
            current.Equipment.TryGetValue(slot, out var before);
            proposed.Equipment.TryGetValue(slot, out var after);
            Add(before, after);
        }

        Add(current.Rig, proposed.Rig);
        Add(current.Backpack, proposed.Backpack);
        return operations;
    }

    private IReadOnlyList<string> BuildExistingItemOperationsV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        bool includeRemoved)
    {
        var afterById = proposed.StoredItems
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var operations = new List<string>();

        foreach (var original in current.StoredItems)
        {
            var item = ResolveItem(original.Item);
            var name = item is null ? "아이템" : DisplayName(item);
            if (!afterById.TryGetValue(original.InstanceId, out var moved))
            {
                if (includeRemoved)
                    operations.Add($"{name} 버리기");
                continue;
            }

            // Contents that remain inside a carrier while that carrier is unequipped did
            // not require an item-by-item user move. The state model re-parents them under
            // the newly stored carrier instance; suppress that representational change.
            if (IsImplicitDisplacedCarrierReparentV1155(current, proposed, original, moved))
                continue;

            // Conversely, root contents that stay "Rig"/"Backpack" while the carrier
            // identity changes do require a real transfer from the old carrier to the new
            // one, even though the coarse storage-kind key is the same.
            if (IsRootCarrierTransferV1155(current, proposed, original, moved))
            {
                operations.Add($"{name} 이동 {StorageLocationLabelV1155(proposed, moved)}");
                continue;
            }

            // Grid index, X/Y and rotation are intentionally ignored here. Repacking inside
            // the same visible storage area is an ordinary Tarkov inventory gesture and does
            // not deserve a separate instruction. Only cross-area moves are surfaced.
            if (string.Equals(
                    StorageAreaKeyV1155(original),
                    StorageAreaKeyV1155(moved),
                    StringComparison.Ordinal))
            {
                continue;
            }

            operations.Add($"{name} 이동 {StorageLocationLabelV1155(proposed, moved)}");
        }

        return operations;
    }

    private bool IsImplicitDisplacedCarrierReparentV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        FarmingGuideStoredItemState original,
        FarmingGuideStoredItemState moved)
    {
        if (original.ParentInstanceId is not null || string.IsNullOrWhiteSpace(moved.ParentInstanceId))
            return false;
        if (original.Storage is not (FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack))
            return false;

        var parent = proposed.StoredItems.FirstOrDefault(value =>
            string.Equals(value.InstanceId, moved.ParentInstanceId, StringComparison.Ordinal));
        if (parent is null || !parent.InstanceId.StartsWith(V1155DisplacedInstancePrefix, StringComparison.Ordinal))
            return false;

        var oldCarrier = original.Storage == FarmingGuideStorageKind.Rig
            ? current.Rig
            : current.Backpack;
        return oldCarrier is not null &&
               string.Equals(parent.Item.ItemId, oldCarrier.ItemId, StringComparison.Ordinal);
    }

    private static bool IsRootCarrierTransferV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        FarmingGuideStoredItemState original,
        FarmingGuideStoredItemState moved)
    {
        if (original.ParentInstanceId is not null || moved.ParentInstanceId is not null ||
            original.Storage != moved.Storage ||
            original.Storage is not (FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack))
        {
            return false;
        }

        var before = original.Storage == FarmingGuideStorageKind.Rig ? current.Rig : current.Backpack;
        var after = moved.Storage == FarmingGuideStorageKind.Rig ? proposed.Rig : proposed.Backpack;
        return before is not null && after is not null &&
               !string.Equals(before.ItemId, after.ItemId, StringComparison.Ordinal);
    }

    private string StorageLocationLabelV1155(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStoredItemState placement)
    {
        if (!string.IsNullOrWhiteSpace(placement.ParentInstanceId))
        {
            var parent = snapshot.StoredItems.FirstOrDefault(value =>
                string.Equals(
                    value.InstanceId,
                    placement.ParentInstanceId,
                    StringComparison.Ordinal));
            var parentItem = parent is null ? null : ResolveItem(parent.Item);
            return parentItem is null ? "내부 공간" : $"{DisplayName(parentItem)} 내부";
        }

        return placement.Storage switch
        {
            FarmingGuideStorageKind.Pockets => "주머니",
            FarmingGuideStorageKind.Rig => "리그",
            FarmingGuideStorageKind.Backpack => "가방",
            FarmingGuideStorageKind.SecureContainer => "컨테이너",
            FarmingGuideStorageKind.SpecialSlots => "특수 슬롯",
            _ => "수납 공간",
        };
    }

    private static string StorageAreaKeyV1155(FarmingGuideStoredItemState placement) =>
        string.IsNullOrWhiteSpace(placement.ParentInstanceId)
            ? $"root:{(int)placement.Storage}"
            : $"nested:{placement.ParentInstanceId}";

    private static string AppendExistingItemOperationsV1155(
        string primary,
        IReadOnlyList<string> operations) =>
        operations.Count == 0
            ? primary
            : $"{primary} + {string.Join(", ", operations)}";

    private static FarmingGuideStoredItemState? FindIncomingStoredPlacementV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        string incomingItemId)
    {
        var currentIds = current.StoredItems
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        return proposed.StoredItems.FirstOrDefault(value =>
            !currentIds.Contains(value.InstanceId) &&
            string.Equals(value.Item.ItemId, incomingItemId, StringComparison.Ordinal));
    }

    private static bool TryFindChangedEquipmentSlot(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        string incomingItemId,
        out FarmingGuideEquipmentSlot slot)
    {
        foreach (var candidate in new[]
                 {
                     FarmingGuideEquipmentSlot.Headset,
                     FarmingGuideEquipmentSlot.Helmet,
                     FarmingGuideEquipmentSlot.FaceCover,
                     FarmingGuideEquipmentSlot.Armband,
                     FarmingGuideEquipmentSlot.BodyArmor,
                     FarmingGuideEquipmentSlot.Eyewear,
                     FarmingGuideEquipmentSlot.PrimaryWeapon1,
                     FarmingGuideEquipmentSlot.PrimaryWeapon2,
                     FarmingGuideEquipmentSlot.Holster,
                 })
        {
            current.Equipment.TryGetValue(candidate, out var before);
            proposed.Equipment.TryGetValue(candidate, out var after);
            if (after is null ||
                !string.Equals(after.ItemId, incomingItemId, StringComparison.Ordinal) ||
                string.Equals(before?.ItemId, after.ItemId, StringComparison.Ordinal))
            {
                continue;
            }

            slot = candidate;
            return true;
        }

        slot = default;
        return false;
    }

    private static bool TryFindChangedCarrier(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        string incomingItemId,
        out FarmingGuideStorageKind kind)
    {
        foreach (var candidate in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            var before = candidate switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                FarmingGuideStorageKind.SecureContainer => current.SecureContainer,
                _ => null,
            };
            var after = candidate switch
            {
                FarmingGuideStorageKind.Rig => proposed.Rig,
                FarmingGuideStorageKind.Backpack => proposed.Backpack,
                FarmingGuideStorageKind.SecureContainer => proposed.SecureContainer,
                _ => null,
            };
            if (after is null ||
                !string.Equals(after.ItemId, incomingItemId, StringComparison.Ordinal) ||
                string.Equals(before?.ItemId, after.ItemId, StringComparison.Ordinal))
            {
                continue;
            }

            kind = candidate;
            return true;
        }

        kind = default;
        return false;
    }

    private static bool IsArmoredRigTransitionV1155(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming) =>
        incoming.FarmingGuideData?.IsArmoredRig == true &&
        current.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor) &&
        !proposed.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor) &&
        proposed.Rig is { } proposedRig &&
        string.Equals(proposedRig.ItemId, incoming.Id, StringComparison.Ordinal) &&
        !string.Equals(current.Rig?.ItemId, incoming.Id, StringComparison.Ordinal);
}
