using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Converts one already-proven globally optimal snapshot into the existing compact raid
    /// instruction vocabulary. Root identity matters here: an incoming FIR copy can replace
    /// an otherwise identical raid-start copy, so item-id equality alone is insufficient.
    /// </summary>
    private RaidRecommendation BuildGlobalRecommendationV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GlobalRootV1170 incomingRoot)
    {
        if (proposed.StoredItems.FirstOrDefault(value =>
                string.Equals(value.InstanceId, incomingRoot.InstanceId, StringComparison.Ordinal)) is { } storedIncoming)
        {
            var destructive = HasDiscardedCurrentRootV1170(current, proposed);
            var primary = $"{StorageLocationLabelV1155(proposed, storedIncoming)} 보관";
            return new RaidRecommendation(
                AppendGlobalOperationsV1170(primary, BuildGlobalExistingOperationsV1170(current, proposed)),
                destructive ? FarmingGuideInstructionAction.Replace : FarmingGuideInstructionAction.Store,
                proposed);
        }

        var incomingEquipment = proposed.Equipment.FirstOrDefault(pair =>
            ReferenceEquals(pair.Value, incomingRoot.State));
        if (!incomingEquipment.Equals(default(KeyValuePair<FarmingGuideEquipmentSlot, FarmingGuideItemState>)))
        {
            var occupied = current.Equipment.ContainsKey(incomingEquipment.Key);
            var primary = occupied
                ? incomingEquipment.Key switch
                {
                    FarmingGuideEquipmentSlot.BodyArmor => "방탄복 교체",
                    FarmingGuideEquipmentSlot.Headset => "헤드셋 교체",
                    _ => $"{EquipmentLabel(incomingEquipment.Key)} 교체",
                }
                : $"{EquipmentLabel(incomingEquipment.Key)} 장착";
            return new RaidRecommendation(
                AppendGlobalOperationsV1170(primary, BuildGlobalExistingOperationsV1170(current, proposed)),
                occupied ? FarmingGuideInstructionAction.ReplaceEquip : FarmingGuideInstructionAction.Equip,
                proposed);
        }

        foreach (var kind in V1170CarrierKinds)
        {
            if (!ReferenceEquals(CarrierStateV1170(proposed, kind), incomingRoot.State))
                continue;

            var occupied = CarrierStateV1170(current, kind) is not null;
            var primary = occupied
                ? $"{CarrierLabel(kind)} 교체"
                : $"{CarrierLabel(kind)} 장착";
            return new RaidRecommendation(
                AppendGlobalOperationsV1170(primary, BuildGlobalExistingOperationsV1170(current, proposed)),
                occupied ? FarmingGuideInstructionAction.ReplaceEquip : FarmingGuideInstructionAction.Equip,
                proposed);
        }

        // A better proposed score cannot be produced by removing only current non-negative
        // roots, so reaching this branch means the projection lost the incoming root.
        // Keep current state rather than manufacturing an unrelated instruction.
        return new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
    }

    private bool HasDiscardedCurrentRootV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        foreach (var stored in current.StoredItems)
        {
            if (!IsStoredRootRetainedV1170(stored, proposed))
                return true;
        }

        foreach (var slot in V1170EquipmentSlots)
        {
            if (current.Equipment.TryGetValue(slot, out var state) &&
                !IsTopLevelRootRetainedV1170(
                    state,
                    EquipmentRootIdV1170(slot),
                    proposed))
            {
                return true;
            }
        }

        foreach (var kind in V1170CarrierKinds)
        {
            if (CarrierStateV1170(current, kind) is { } state &&
                !IsTopLevelRootRetainedV1170(
                    state,
                    CarrierRootIdV1170(kind),
                    proposed))
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<string> BuildGlobalExistingOperationsV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        var operations = new List<string>();

        foreach (var slot in V1170EquipmentSlots)
        {
            if (!current.Equipment.TryGetValue(slot, out var state))
                continue;
            AddTopLevelRootOperationV1170(
                state,
                EquipmentRootIdV1170(slot),
                $"equipment:{(int)slot}",
                proposed,
                operations);
        }

        foreach (var kind in V1170CarrierKinds)
        {
            if (CarrierStateV1170(current, kind) is not { } state)
                continue;
            AddTopLevelRootOperationV1170(
                state,
                CarrierRootIdV1170(kind),
                $"carrier:{(int)kind}",
                proposed,
                operations);
        }

        foreach (var original in current.StoredItems)
        {
            var item = ResolveItem(original.Item);
            var name = item is null ? "아이템" : DisplayName(item);
            var afterStored = proposed.StoredItems.FirstOrDefault(value =>
                string.Equals(value.InstanceId, original.InstanceId, StringComparison.Ordinal));
            if (afterStored is not null)
            {
                if (!string.Equals(
                        StorageAreaKeyV1155(original),
                        StorageAreaKeyV1155(afterStored),
                        StringComparison.Ordinal))
                {
                    operations.Add($"{name} 이동 {StorageLocationLabelV1155(proposed, afterStored)}");
                }
                continue;
            }

            if (TryFindTopLevelLocationV1170(proposed, original.Item, out var destination, referenceOnly: true))
            {
                operations.Add($"{name} 이동 {destination}");
                continue;
            }

            operations.Add($"{name} 버리기");
        }

        return operations
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private void AddTopLevelRootOperationV1170(
        FarmingGuideItemState state,
        string syntheticInstanceId,
        string originalLocationKey,
        FarmingGuideLoadoutSnapshot proposed,
        List<string> operations)
    {
        var item = ResolveItem(state);
        var name = item is null ? "아이템" : DisplayName(item);

        var stored = proposed.StoredItems.FirstOrDefault(value =>
            string.Equals(value.InstanceId, syntheticInstanceId, StringComparison.Ordinal));
        if (stored is not null)
        {
            operations.Add($"{name} 이동 {StorageLocationLabelV1155(proposed, stored)}");
            return;
        }

        if (TryFindTopLevelLocationV1170(
                proposed,
                state,
                out var destination,
                referenceOnly: true,
                out var destinationKey))
        {
            if (!string.Equals(originalLocationKey, destinationKey, StringComparison.Ordinal))
                operations.Add($"{name} 이동 {destination}");
            return;
        }

        operations.Add($"{name} 버리기");
    }

    private bool IsStoredRootRetainedV1170(
        FarmingGuideStoredItemState stored,
        FarmingGuideLoadoutSnapshot proposed) =>
        proposed.StoredItems.Any(value =>
            string.Equals(value.InstanceId, stored.InstanceId, StringComparison.Ordinal)) ||
        TryFindTopLevelLocationV1170(proposed, stored.Item, out _, referenceOnly: true);

    private bool IsTopLevelRootRetainedV1170(
        FarmingGuideItemState state,
        string syntheticInstanceId,
        FarmingGuideLoadoutSnapshot proposed) =>
        proposed.StoredItems.Any(value =>
            string.Equals(value.InstanceId, syntheticInstanceId, StringComparison.Ordinal)) ||
        TryFindTopLevelLocationV1170(proposed, state, out _, referenceOnly: true);

    private bool TryFindTopLevelLocationV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState state,
        out string label,
        bool referenceOnly) =>
        TryFindTopLevelLocationV1170(snapshot, state, out label, referenceOnly, out _);

    private bool TryFindTopLevelLocationV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState state,
        out string label,
        bool referenceOnly,
        out string locationKey)
    {
        foreach (var slot in V1170EquipmentSlots)
        {
            if (!snapshot.Equipment.TryGetValue(slot, out var candidate) ||
                !SameGlobalRootIdentityV1170(candidate, state, referenceOnly))
            {
                continue;
            }
            label = EquipmentLabel(slot);
            locationKey = $"equipment:{(int)slot}";
            return true;
        }

        foreach (var kind in V1170CarrierKinds)
        {
            var candidate = CarrierStateV1170(snapshot, kind);
            if (candidate is null || !SameGlobalRootIdentityV1170(candidate, state, referenceOnly))
                continue;
            label = CarrierLabel(kind);
            locationKey = $"carrier:{(int)kind}";
            return true;
        }

        label = string.Empty;
        locationKey = string.Empty;
        return false;
    }

    private static bool SameGlobalRootIdentityV1170(
        FarmingGuideItemState candidate,
        FarmingGuideItemState expected,
        bool referenceOnly) =>
        ReferenceEquals(candidate, expected) ||
        (!referenceOnly && candidate == expected);

    private static string AppendGlobalOperationsV1170(
        string primary,
        IReadOnlyList<string> operations) =>
        operations.Count == 0
            ? primary
            : $"{primary} + {string.Join(", ", operations)}";
}
