using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.17 presentation is derived from the complete optimized state rather than the
    /// historical local-replacement vocabulary. Every current global root is tracked by the
    /// exact identity already used by the unified solver, so an equipped/carrier item that was
    /// retained elsewhere is reported as a move and a root that actually disappeared is the
    /// only thing reported as a discard.
    /// </summary>
    private RaidRecommendation ApplyRaidInstructionPresentationV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        GameItem incoming)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.Indeterminate)
            return recommendation;
        if (recommendation.Action == FarmingGuideInstructionAction.Discard)
            return recommendation with { Instruction = "버리기" };

        var proposed = recommendation.ProposedSnapshot;
        var primary = BuildIncomingInstructionV1170(current, proposed, incoming);
        var operations = BuildExistingRootOperationsV1170(current, proposed)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return recommendation with
        {
            Instruction = AppendExistingItemOperationsV1155(primary, operations),
        };
    }

    private string BuildIncomingInstructionV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GameItem incoming)
    {
        var incomingStored = proposed.StoredItems.FirstOrDefault(value =>
            value.InstanceId.StartsWith(V1170IncomingInstancePrefix, StringComparison.Ordinal) &&
            value.Item.RaidAcquired &&
            string.Equals(value.Item.ItemId, incoming.Id, StringComparison.Ordinal));
        if (incomingStored is not null)
            return $"{StorageLocationLabelV1155(proposed, incomingStored)} 보관";

        var currentStates = EnumerateRootStatesV1170(current)
            .ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var slot in V1170GlobalEquipmentSlots)
        {
            if (!proposed.Equipment.TryGetValue(slot, out var after) ||
                currentStates.Contains(after) ||
                !after.RaidAcquired ||
                !string.Equals(after.ItemId, incoming.Id, StringComparison.Ordinal))
            {
                continue;
            }

            current.Equipment.TryGetValue(slot, out var before);
            return before is null
                ? $"{EquipmentLabel(slot)} 장착"
                : $"{EquipmentLabel(slot)} 교체";
        }

        foreach (var kind in V1170GlobalCarrierSlots)
        {
            var after = CarrierStateV1155(proposed, kind);
            if (after is null ||
                currentStates.Contains(after) ||
                !after.RaidAcquired ||
                !string.Equals(after.ItemId, incoming.Id, StringComparison.Ordinal))
            {
                continue;
            }

            var before = CarrierStateV1155(current, kind);
            return before is null
                ? $"{CarrierLabel(kind)} 장착"
                : $"{CarrierLabel(kind)} 교체";
        }

        return recommendationFallbackV1170(incoming);
    }

    private static string recommendationFallbackV1170(GameItem incoming)
    {
        _ = incoming;
        return "수납 공간 보관";
    }

    private IReadOnlyList<string> BuildExistingRootOperationsV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        if (!TryBuildCurrentOwnedRootsV1170(current, out var roots))
            return [];

        var rootIdByState = new Dictionary<FarmingGuideItemState, string>(ReferenceEqualityComparer.Instance);
        foreach (var root in roots)
            rootIdByState[root.State] = root.InstanceId;

        var operations = new List<string>();
        foreach (var root in roots.OrderBy(value => value.InstanceId, StringComparer.Ordinal))
        {
            var beforeKey = CurrentRootLocationKeyV1170(root, current, rootIdByState);
            var after = FindProposedRootLocationV1170(root, proposed, rootIdByState);
            var name = DisplayName(root.Item);

            if (after is null)
            {
                operations.Add(root.Quantity > 1
                    ? $"{name} {root.Quantity}개 버리기"
                    : $"{name} 버리기");
                continue;
            }

            if (string.Equals(beforeKey, after.Value.Key, StringComparison.Ordinal))
                continue;

            operations.Add($"{name} 이동 {after.Value.Label}");
        }

        return operations;
    }

    private static IEnumerable<FarmingGuideItemState> EnumerateRootStatesV1170(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        foreach (var state in snapshot.Equipment.Values)
            yield return state;
        if (snapshot.Rig is not null)
            yield return snapshot.Rig;
        if (snapshot.Backpack is not null)
            yield return snapshot.Backpack;
        if (snapshot.SecureContainer is not null)
            yield return snapshot.SecureContainer;
        foreach (var stored in snapshot.StoredItems)
            yield return stored.Item;
    }

    private static string CurrentRootLocationKeyV1170(
        GlobalOwnedRootV1170 root,
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyDictionary<FarmingGuideItemState, string> rootIdByState)
    {
        if (root.EquipmentSlot is { } equipmentSlot)
            return $"equipment:{(int)equipmentSlot}";
        if (root.CarrierKind is { } carrierKind)
            return $"carrier:{(int)carrierKind}";
        return root.StoredSource is { } stored
            ? PhysicalStorageAreaKeyV1170(current, stored, rootIdByState)
            : $"unknown:{root.InstanceId}";
    }

    private RootLocationV1170? FindProposedRootLocationV1170(
        GlobalOwnedRootV1170 root,
        FarmingGuideLoadoutSnapshot proposed,
        IReadOnlyDictionary<FarmingGuideItemState, string> rootIdByState)
    {
        var stored = proposed.StoredItems.FirstOrDefault(value =>
            string.Equals(value.InstanceId, root.InstanceId, StringComparison.Ordinal));
        if (stored is not null)
        {
            return new RootLocationV1170(
                PhysicalStorageAreaKeyV1170(proposed, stored, rootIdByState),
                StorageLocationLabelV1155(proposed, stored));
        }

        foreach (var slot in V1170GlobalEquipmentSlots)
        {
            if (proposed.Equipment.TryGetValue(slot, out var state) && ReferenceEquals(state, root.State))
                return new RootLocationV1170($"equipment:{(int)slot}", EquipmentLabel(slot));
        }

        foreach (var kind in V1170GlobalCarrierSlots)
        {
            if (ReferenceEquals(CarrierStateV1155(proposed, kind), root.State))
                return new RootLocationV1170($"carrier:{(int)kind}", CarrierLabel(kind));
        }

        return null;
    }

    private static string PhysicalStorageAreaKeyV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStoredItemState placement,
        IReadOnlyDictionary<FarmingGuideItemState, string> rootIdByState)
    {
        if (!string.IsNullOrWhiteSpace(placement.ParentInstanceId))
            return $"inside:{placement.ParentInstanceId}";

        if (placement.Storage is FarmingGuideStorageKind.Rig or
            FarmingGuideStorageKind.Backpack or
            FarmingGuideStorageKind.SecureContainer)
        {
            var carrier = CarrierStateV1155(snapshot, placement.Storage);
            if (carrier is not null && rootIdByState.TryGetValue(carrier, out var ownerId))
                return $"inside:{ownerId}";
            if (carrier is not null)
                return $"inside-state:{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(carrier)}";
        }

        return $"root-storage:{(int)placement.Storage}";
    }

    private readonly record struct RootLocationV1170(string Key, string Label);
}
