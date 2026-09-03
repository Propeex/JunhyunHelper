using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private sealed record GlobalInventoryNodeV1170(
        FarmingGuideItemState State,
        GameItem Item,
        int Quantity);

    private bool TryValidateGlobalRootPhysicalFactsV1170(
        IReadOnlyList<GlobalRootV1170> roots)
    {
        foreach (var root in roots)
        {
            // Root geometry decides which final storage states are physically possible.
            // Unknown dimensions are not a 1x1 item and cannot support destructive advice.
            if (root.Item.Width is not > 0 || root.Item.Height is not > 0)
                return false;
            if (!TryEnumerateGlobalInventoryV1170(root, out var nodes))
                return false;
            if (nodes.Any(node => node.Item.WeightKg is null or < 0m))
                return false;
        }
        return true;
    }

    private bool TryBuildCompleteGlobalFactsV1170(
        IReadOnlyList<GlobalRootV1170> roots,
        ScannerItemSnapshot scanned,
        out GlobalFactsV1170 facts)
    {
        var allNodes = new List<GlobalInventoryNodeV1170>();
        foreach (var root in roots)
        {
            if (!TryEnumerateGlobalInventoryV1170(root, out var nodes))
            {
                facts = default!;
                return false;
            }
            allNodes.AddRange(nodes);
        }

        var flea = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in allNodes.GroupBy(node => node.Item.Id, StringComparer.Ordinal))
        {
            var item = group.First().Item;
            int? value = null;

            if (string.Equals(item.Id, scanned.ItemId, StringComparison.Ordinal) &&
                scanned.FleaAveragePrice is { } incomingFlea)
            {
                value = Math.Max(0, incomingFlea);
            }
            else if (_raidFleaAveragePrices.TryGetValue(item.Id, out var remembered))
            {
                value = Math.Max(0, remembered);
            }
            else if (_raidBridge?.ResolveSnapshot(item.Id)?.FleaAveragePrice is { } resolved)
            {
                value = Math.Max(0, resolved);
            }
            else if (item.FleaTradable == false)
            {
                value = 0;
            }

            // Unknown economic facts are not zero. Without a proven value for every retained
            // modeled item, a lower-value destructive alternative cannot be certified optimal.
            if (value is null)
            {
                facts = default!;
                return false;
            }
            flea[item.Id] = value.Value;
        }

        var remainingFir = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var itemId in allNodes
                     .Where(node => node.State.RaidAcquired)
                     .Select(node => node.Item.Id)
                     .Distinct(StringComparer.Ordinal))
        {
            if (string.Equals(itemId, scanned.ItemId, StringComparison.Ordinal))
            {
                remainingFir[itemId] = Math.Max(0, scanned.CurrentNeededFir);
                continue;
            }

            var snapshot = _raidBridge?.ResolveSnapshot(itemId);
            if (snapshot is null)
            {
                facts = default!;
                return false;
            }
            remainingFir[itemId] = Math.Max(0, snapshot.CurrentNeededFir);
        }

        facts = new GlobalFactsV1170(remainingFir, flea);
        return true;
    }

    private FarmingGuideOptimizationScore ScoreCompleteGlobalRootsV1170(
        IReadOnlyList<GlobalRootV1170> roots,
        GlobalFactsV1170 facts)
    {
        var acquired = new Dictionary<string, int>(StringComparer.Ordinal);
        long retainedValue = 0;

        foreach (var root in roots)
        {
            if (!TryEnumerateGlobalInventoryV1170(root, out var nodes))
                throw new InvalidOperationException("Validated v1.17 root assembly became unresolvable during scoring.");

            foreach (var node in nodes)
            {
                if (node.State.RaidAcquired)
                {
                    acquired[node.Item.Id] = checked(
                        acquired.GetValueOrDefault(node.Item.Id) + Math.Max(1, node.Quantity));
                }
                retainedValue = checked(
                    retainedValue +
                    (long)facts.FleaValue[node.Item.Id] * Math.Max(1, node.Quantity));
            }
        }

        var satisfied = 0;
        foreach (var pair in acquired)
        {
            satisfied = checked(satisfied + Math.Min(
                pair.Value,
                facts.RemainingFirNeed.GetValueOrDefault(pair.Key)));
        }
        return new FarmingGuideOptimizationScore(satisfied, retainedValue);
    }

    private bool TryEnumerateGlobalInventoryV1170(
        GlobalRootV1170 root,
        out IReadOnlyList<GlobalInventoryNodeV1170> nodes) =>
        TryEnumerateStateInventoryV1170(root.State, root.Quantity, out nodes);

    private bool TryEnumerateStateInventoryV1170(
        FarmingGuideItemState root,
        int rootQuantity,
        out IReadOnlyList<GlobalInventoryNodeV1170> nodes)
    {
        var result = new List<GlobalInventoryNodeV1170>();
        var ancestry = new HashSet<FarmingGuideItemState>(ReferenceEqualityComparer.Instance);
        if (!TryAppendStateInventoryV1170(
                root,
                Math.Max(1, rootQuantity),
                result,
                ancestry,
                depth: 0))
        {
            nodes = [];
            return false;
        }

        nodes = result;
        return true;
    }

    private bool TryAppendStateInventoryV1170(
        FarmingGuideItemState state,
        int quantity,
        List<GlobalInventoryNodeV1170> result,
        HashSet<FarmingGuideItemState> ancestry,
        int depth)
    {
        if (depth > FarmingGuideAssemblyPolicy.MaximumAssemblyDepth ||
            !ancestry.Add(state) ||
            !_itemsById.TryGetValue(state.ItemId, out var item))
        {
            return false;
        }

        result.Add(new GlobalInventoryNodeV1170(state, item, Math.Max(1, quantity)));
        foreach (var child in state.Attachments.Values.Concat(state.ArmorPlates.Values))
        {
            if (child is not null &&
                !TryAppendStateInventoryV1170(child, 1, result, ancestry, depth + 1))
            {
                ancestry.Remove(state);
                return false;
            }
        }

        ancestry.Remove(state);
        return true;
    }

    private bool IsSnapshotWithinConfiguredWeightStrictV1170(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        EnsureWeightSettingsLoadedV1160();
        return TryCalculateSnapshotWeightStrictV1170(snapshot, out var totalWeight) &&
               FarmingGuideWeightPolicy.IsWithinLimit(totalWeight, _weightSettingsV1160);
    }

    private bool TryCalculateSnapshotWeightStrictV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        out decimal totalWeight)
    {
        totalWeight = 0m;

        foreach (var pair in snapshot.Equipment)
        {
            if (!FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(pair.Key, _weightSettingsV1160))
                continue;
            if (!TryCalculateStateWeightStrictV1170(pair.Value, 1, out var weight))
                return false;
            totalWeight = checked(totalWeight + weight);
        }

        foreach (var carrier in new[] { snapshot.Rig, snapshot.Backpack, snapshot.SecureContainer })
        {
            if (carrier is null)
                continue;
            if (!TryCalculateStateWeightStrictV1170(carrier, 1, out var weight))
                return false;
            totalWeight = checked(totalWeight + weight);
        }

        foreach (var stored in snapshot.StoredItems)
        {
            if (!TryCalculateStateWeightStrictV1170(
                    stored.Item,
                    stored.NormalizedQuantity,
                    out var weight))
            {
                return false;
            }
            totalWeight = checked(totalWeight + weight);
        }

        return true;
    }

    private bool TryCalculateRootWeightStrictV1170(
        GlobalRootV1170 root,
        out decimal totalWeight) =>
        TryCalculateStateWeightStrictV1170(root.State, root.Quantity, out totalWeight);

    private bool TryCalculateFixedOutsideWeightStrictV1170(
        FarmingGuideLoadoutSnapshot snapshot,
        out decimal totalWeight)
    {
        totalWeight = 0m;
        foreach (var slot in new[] { FarmingGuideEquipmentSlot.Melee, FarmingGuideEquipmentSlot.Dogtag })
        {
            if (!snapshot.Equipment.TryGetValue(slot, out var state))
                continue;
            if (!TryCalculateStateWeightStrictV1170(state, 1, out var weight))
                return false;
            totalWeight = checked(totalWeight + weight);
        }
        return true;
    }

    private bool TryCalculateStateWeightStrictV1170(
        FarmingGuideItemState state,
        int rootQuantity,
        out decimal totalWeight)
    {
        totalWeight = 0m;
        if (!TryEnumerateStateInventoryV1170(state, rootQuantity, out var nodes))
            return false;

        foreach (var node in nodes)
        {
            if (node.Item.WeightKg is not { } unitWeight || unitWeight < 0m)
                return false;
            totalWeight = checked(
                totalWeight + unitWeight * Math.Max(1, node.Quantity));
        }
        return true;
    }
}
