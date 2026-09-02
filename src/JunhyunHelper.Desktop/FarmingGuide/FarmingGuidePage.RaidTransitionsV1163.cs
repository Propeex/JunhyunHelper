using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1163MaxEvictionSubsetSize = 6;
    private const int V1163MaxEvictionPlanAttempts = 256;

    /// <summary>
    /// v1.16.3 correction boundary for all post-rulebook state transitions.
    ///
    /// The historical v1.15.5 transition layer used a prefix-only victim search, valued
    /// quantity-bearing stored stacks as nominal one-unit items in candidate ordering,
    /// hard-coded standard pockets, and treated a locked carrier as if its internal
    /// storage were locked. This path replaces those decisions with a snapshot-aware,
    /// quantity-aware and tactical-safe repacking search while preserving the established
    /// deterministic rulebook and presentation vocabulary.
    /// </summary>
    private RaidRecommendation ApplyRaidStateTransitionsV1163(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot scanned,
        GameItem incoming)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.ReplaceEquip)
            return PreserveDisplacedTopLevelItemsV1163(current, recommendation);

        if (recommendation.Action is not (
                FarmingGuideInstructionAction.Replace or
                FarmingGuideInstructionAction.Discard))
        {
            return recommendation;
        }

        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        if (TryRepackIncomingWithSafeEvictionsV1163(
                current,
                FarmingGuideItemState.Create(incoming.Id),
                incoming,
                incomingMetrics,
                NewDisplacedInstanceIdV1155(),
                out var proposed,
                out var evictedCount))
        {
            return new RaidRecommendation(
                evictedCount == 0 ? "보관" : "교체",
                evictedCount == 0
                    ? FarmingGuideInstructionAction.Store
                    : FarmingGuideInstructionAction.Replace,
                proposed);
        }

        // Do not fall back to a historical destructive result after the corrected search
        // has proved that no safe plan exists. A final Discard recommendation is the
        // fail-closed result and leaves current state unchanged.
        return new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
    }

    /// <summary>
    /// Equipment superiority is allowed to replace the top-level item, but displaced
    /// equipment should remain loot whenever the resulting inventory can retain it safely.
    /// Carrier upgrades already migrate their old contents in the v1.16 rulebook; this
    /// method therefore stores the now-empty displaced carrier first, then ordinary
    /// displaced equipment. A stored old carrier may itself provide legal nested capacity
    /// for the later displaced item.
    /// </summary>
    private RaidRecommendation PreserveDisplacedTopLevelItemsV1163(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation)
    {
        var originalProposed = recommendation.ProposedSnapshot;
        var candidate = originalProposed;

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            var before = CarrierStateV1155(current, kind);
            var after = CarrierStateV1155(originalProposed, kind);
            if (before is null || SameRootItemV1155(before, after))
                continue;

            var oldItem = ResolveItem(before);
            if (oldItem is null)
                continue;

            if (TryRepackIncomingWithSafeEvictionsV1163(
                    candidate,
                    before,
                    oldItem,
                    MetricsForExisting(oldItem),
                    NewDisplacedInstanceIdV1155(),
                    out var preserved,
                    out _))
            {
                candidate = preserved;
            }
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
            originalProposed.Equipment.TryGetValue(slot, out var after);
            if (before is null || SameRootItemV1155(before, after))
                continue;

            var oldItem = ResolveItem(before);
            if (oldItem is null)
                continue;

            if (TryRepackIncomingWithSafeEvictionsV1163(
                    candidate,
                    before,
                    oldItem,
                    MetricsForExisting(oldItem),
                    NewDisplacedInstanceIdV1155(),
                    out var preserved,
                    out _))
            {
                candidate = preserved;
            }
        }

        return recommendation with { ProposedSnapshot = candidate };
    }

    private bool TryRepackIncomingWithSafeEvictionsV1163(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        FarmingGuideLootMetrics incomingMetrics,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed,
        out int evictedCount)
    {
        if (TryRepackIncomingStateV1163(
                snapshot,
                incomingState,
                incoming,
                incomingInstanceId,
                out proposed))
        {
            evictedCount = 0;
            return true;
        }

        var candidates = EnumerateSafeEvictionCandidatesV1163(snapshot).ToArray();
        if (candidates.Length == 0)
        {
            proposed = snapshot;
            evictedCount = 0;
            return false;
        }

        // Best-first subset enumeration. Every single candidate enters the queue, so a
        // geometrically relevant victim can never be hidden merely because several cheaper
        // irrelevant victims sort ahead of it. Supersets are generated only from a popped
        // prefix and priority is monotonic aggregate loss: Flea value, count, footprint,
        // then deterministic instance-id key. The bounded node count keeps raid-time work
        // finite while covering the practical multi-victim cases that the old prefix-only
        // search missed.
        var queue = new PriorityQueue<EvictionSubsetV1163, EvictionPriorityV1163>();
        for (var index = 0; index < candidates.Length; index++)
            EnqueueEvictionSubsetV1163(queue, candidates, [index]);

        var planAttempts = 0;
        while (queue.Count > 0 && planAttempts < V1163MaxEvictionPlanAttempts)
        {
            var subset = queue.Dequeue();
            var victimMetrics = subset.Indices
                .Select(index => candidates[index].Metrics)
                .ToArray();

            // With non-negative victim value, a set that already fails the economic/FIR
            // retention contract cannot become safer by adding more victims.
            if (!FarmingGuideLootRetentionPolicy.CanSacrificeFor(incomingMetrics, victimMetrics))
                continue;

            planAttempts++;
            var remove = subset.Indices
                .Select(index => candidates[index].Stored.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var reduced = snapshot with
            {
                StoredItems = snapshot.StoredItems
                    .Where(value => !remove.Contains(value.InstanceId))
                    .ToArray(),
            };

            if (TryRepackIncomingStateV1163(
                    reduced,
                    incomingState,
                    incoming,
                    incomingInstanceId,
                    out proposed))
            {
                evictedCount = subset.Indices.Length;
                return true;
            }

            if (subset.Indices.Length >= V1163MaxEvictionSubsetSize)
                continue;

            var last = subset.Indices[^1];
            for (var next = last + 1; next < candidates.Length; next++)
            {
                var expanded = new int[subset.Indices.Length + 1];
                Array.Copy(subset.Indices, expanded, subset.Indices.Length);
                expanded[^1] = next;
                EnqueueEvictionSubsetV1163(queue, candidates, expanded);
            }
        }

        proposed = snapshot;
        evictedCount = 0;
        return false;
    }

    private IEnumerable<EvictionCandidateV1163> EnumerateSafeEvictionCandidatesV1163(
        FarmingGuideLoadoutSnapshot snapshot)
    {
        var foodCount = CountStoredResourcesV1163(snapshot, FarmingGuideTacticalResourcePolicy.ProvidesFood);
        var drinkCount = CountStoredResourcesV1163(snapshot, FarmingGuideTacticalResourcePolicy.ProvidesDrink);
        var weapons = CurrentRaidWeaponsV1163(snapshot);

        return snapshot.StoredItems
            .Where(stored => !snapshot.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Where(stored => !IsInsideLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, snapshot.StoredItems))
            .Where(stored => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal)))
            .Select(stored =>
            {
                var item = ResolveItem(stored.Item);
                if (item is null)
                    return null;

                var metrics = MetricsForStoredV1163(stored, item);
                if (metrics.CurrentNeeded > 0)
                    return null;

                // Keep at least one modeled provider of each survival resource. A combined
                // provision can satisfy both contracts and is protected if it is the final
                // provider of either resource.
                if (FarmingGuideTacticalResourcePolicy.ProvidesFood(item) && foodCount <= 1)
                    return null;
                if (FarmingGuideTacticalResourcePolicy.ProvidesDrink(item) && drinkCount <= 1)
                    return null;

                // Current-weapon loose ammunition is a tactical reserve rather than an
                // economic victim pool. Quantity-aware final safety rechecks this invariant;
                // excluding it here lets the planner search the next safe alternative rather
                // than selecting the ammo first and being rejected only at the end.
                if (weapons.Count > 0 &&
                    FarmingGuideTacticalResourcePolicy.IsAmmoForAnyWeapon(item, weapons))
                {
                    return null;
                }

                return new EvictionCandidateV1163(stored, metrics);
            })
            .Where(static value => value is not null)
            .Select(static value => value!)
            .OrderBy(value => value.Metrics, LootMetricsComparer.Instance)
            .ThenBy(value => value.Stored.InstanceId, StringComparer.Ordinal);
    }

    private int CountStoredResourcesV1163(
        FarmingGuideLoadoutSnapshot snapshot,
        Func<GameItem, bool> predicate)
    {
        var count = 0;
        foreach (var stored in snapshot.StoredItems)
        {
            var item = ResolveItem(stored.Item);
            if (item is not null && predicate(item))
                count++;
        }
        return count;
    }

    private IReadOnlyList<GameItem> CurrentRaidWeaponsV1163(FarmingGuideLoadoutSnapshot snapshot)
    {
        var result = new List<GameItem>();
        foreach (var slot in new[]
                 {
                     FarmingGuideEquipmentSlot.PrimaryWeapon1,
                     FarmingGuideEquipmentSlot.PrimaryWeapon2,
                     FarmingGuideEquipmentSlot.Holster,
                 })
        {
            if (snapshot.Equipment.TryGetValue(slot, out var state) && ResolveItem(state) is { } item)
                result.Add(item);
        }
        return result;
    }

    private bool TryRepackIncomingStateV1163(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideItemState incomingState,
        GameItem incoming,
        string incomingInstanceId,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        var surfaces = EnumerateTransitionSurfacesV1163(snapshot, incoming, snapshot.StoredItems)
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        var surfaceById = surfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var coreSurfaces = surfaces
            .Select((surface, priority) => ToCoreSurfaceV1163(surface, priority))
            .ToArray();
        if (coreSurfaces.Length == 0)
        {
            proposed = snapshot;
            return false;
        }

        var items = new List<FarmingGuideRepackingItem>(snapshot.StoredItems.Count);
        foreach (var stored in snapshot.StoredItems)
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                proposed = snapshot;
                return false;
            }

            var currentSurfaceId = SurfaceId(stored.Storage, stored.ParentInstanceId, stored.GridIndex);
            if (!surfaceById.ContainsKey(currentSurfaceId))
            {
                proposed = snapshot;
                return false;
            }

            var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                stored.Storage,
                stored.ParentInstanceId,
                item,
                stored.Rotated);
            var options = BuildTransitionOptionsV1163(item, surfaces).ToArray();

            // Repacking never removes an existing item, so moving a locked exact instance
            // is safe and preserves its lock identity. Likewise, locking an equipped
            // carrier protects that carrier root from replacement but does not freeze its
            // contents or disable its storage grids.
            var movable = options.Length > 0;
            items.Add(new FarmingGuideRepackingItem(
                stored.InstanceId,
                currentSurfaceId,
                stored.X,
                stored.Y,
                footprint.Width,
                footprint.Height,
                stored.Rotated,
                movable,
                options));
        }

        var incomingOptions = BuildTransitionOptionsV1163(incoming, surfaces).ToArray();
        if (incomingOptions.Length == 0)
        {
            proposed = snapshot;
            return false;
        }

        var plan = FarmingGuideRepackingPlanner.TryPlan(
            coreSurfaces,
            items,
            new FarmingGuideRepackingIncoming(incomingInstanceId, incomingOptions));
        if (plan is null || !surfaceById.TryGetValue(plan.Incoming.SurfaceId, out var destination))
        {
            proposed = snapshot;
            return false;
        }

        var placementById = plan.ExistingPlacements
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var result = new List<FarmingGuideStoredItemState>(snapshot.StoredItems.Count + 1);
        foreach (var stored in snapshot.StoredItems)
        {
            if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var target))
            {
                proposed = snapshot;
                return false;
            }

            result.Add(stored with
            {
                Storage = target.Kind,
                GridIndex = target.GridIndex,
                X = placement.X,
                Y = placement.Y,
                Rotated = placement.Rotated,
                ParentInstanceId = target.ParentInstanceId,
            });
        }

        result.Add(new FarmingGuideStoredItemState(
            incomingInstanceId,
            incomingState,
            destination.Kind,
            destination.GridIndex,
            plan.Incoming.X,
            plan.Incoming.Y,
            plan.Incoming.Rotated,
            destination.ParentInstanceId));

        if (!TryNormalizeRootStorageKinds(result, out var normalized))
        {
            proposed = snapshot;
            return false;
        }

        proposed = snapshot with { StoredItems = normalized };
        return true;
    }

    private IEnumerable<RaidSurface> EnumerateTransitionSurfacesV1163(
        FarmingGuideLoadoutSnapshot snapshot,
        GameItem incoming,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var nested = storedItems
            .SelectMany(stored =>
            {
                var owner = ResolveItem(stored.Item);
                var grids = owner?.FarmingGuideData?.StorageGrids;
                if (owner is null || grids is null || grids.Count == 0)
                    return Array.Empty<RaidSurface>();

                return grids
                    .Select((grid, index) => new RaidSurface(
                        stored.Storage,
                        stored.InstanceId,
                        index,
                        grid,
                        $"{DisplayName(owner)} 내부"))
                    .ToArray();
            })
            .ToArray();

        foreach (var surface in nested.Where(surface =>
                     FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                         incoming,
                         surface.Definition.Filters)))
        {
            yield return surface;
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.SecureContainer,
                     FarmingGuideStorageKind.Pockets,
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SpecialSlots,
                 })
        {
            var grids = TransitionRootGridsV1163(snapshot, kind);
            for (var index = 0; index < grids.Count; index++)
            {
                yield return new RaidSurface(
                    kind,
                    null,
                    index,
                    grids[index],
                    TransitionStorageLabelV1155(kind));
            }
        }

        foreach (var surface in nested.Where(surface =>
                     !FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                         incoming,
                         surface.Definition.Filters)))
        {
            yield return surface;
        }
    }

    private IReadOnlyList<FarmingGuideStorageGridDefinition> TransitionRootGridsV1163(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideStorageKind kind)
    {
        if (kind == FarmingGuideStorageKind.Pockets)
            return _pocketGrids;
        if (kind == FarmingGuideStorageKind.SpecialSlots)
        {
            return Enumerable.Range(0, 3)
                .Select(_ => new FarmingGuideStorageGridDefinition(
                    1,
                    1,
                    FarmingGuideItemFilter.Empty))
                .ToArray();
        }

        return ResolveItem(CarrierStateV1155(snapshot, kind))?.FarmingGuideData?.StorageGrids ?? [];
    }

    private FarmingGuideRepackingSurface ToCoreSurfaceV1163(RaidSurface surface, int priority) =>
        new(
            SurfaceId(surface),
            surface.ParentInstanceId,
            surface.Definition.Width,
            surface.Definition.Height,
            priority,
            _reservedCells
                .Where(cell => IsReservedOnSurface(cell, surface))
                .Select((cell, index) => new FarmingGuideGridPlacement(
                    $"__reserved_v1163_{index}",
                    cell.X,
                    cell.Y,
                    1,
                    1))
                .ToArray());

    private static IEnumerable<FarmingGuideRepackingOption> BuildTransitionOptionsV1163(
        GameItem item,
        IReadOnlyList<RaidSurface> surfaces)
    {
        var preference = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (!FarmingGuideStoragePlacementPolicy.CanStore(
                    surface.Kind,
                    surface.ParentInstanceId,
                    item,
                    surface.Definition.Filters))
            {
                preference++;
                continue;
            }

            var rotations = FarmingGuideStoragePlacementPolicy.SupportsRotation(
                surface.Kind,
                surface.ParentInstanceId,
                item)
                ? new[] { false, true }
                : new[] { false };
            foreach (var rotated in rotations)
            {
                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    surface.Kind,
                    surface.ParentInstanceId,
                    item,
                    rotated);
                if (footprint.Width > surface.Definition.Width || footprint.Height > surface.Definition.Height)
                    continue;

                var surfaceId = SurfaceId(surface);
                var key = $"{surfaceId}|{footprint.Width}|{footprint.Height}|{rotated}";
                if (!seen.Add(key))
                    continue;

                yield return new FarmingGuideRepackingOption(
                    surfaceId,
                    footprint.Width,
                    footprint.Height,
                    rotated,
                    preference);
            }
            preference++;
        }
    }

    private static void EnqueueEvictionSubsetV1163(
        PriorityQueue<EvictionSubsetV1163, EvictionPriorityV1163> queue,
        IReadOnlyList<EvictionCandidateV1163> candidates,
        int[] indices)
    {
        long value = 0;
        var slots = 0;
        var ids = new string[indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            var candidate = candidates[indices[i]];
            value += Math.Max(0, candidate.Metrics.EffectiveValue);
            slots += Math.Max(1, candidate.Metrics.EffectiveSlots);
            ids[i] = candidate.Stored.InstanceId;
        }

        queue.Enqueue(
            new EvictionSubsetV1163(indices),
            new EvictionPriorityV1163(
                value,
                indices.Length,
                slots,
                string.Join("|", ids)));
    }

    private sealed record EvictionCandidateV1163(
        FarmingGuideStoredItemState Stored,
        FarmingGuideLootMetrics Metrics);

    private sealed record EvictionSubsetV1163(int[] Indices);

    private readonly record struct EvictionPriorityV1163(
        long Value,
        int Count,
        int Slots,
        string Key) : IComparable<EvictionPriorityV1163>
    {
        public int CompareTo(EvictionPriorityV1163 other)
        {
            var value = Value.CompareTo(other.Value);
            if (value != 0)
                return value;
            var count = Count.CompareTo(other.Count);
            if (count != 0)
                return count;
            var slots = Slots.CompareTo(other.Slots);
            if (slots != 0)
                return slots;
            return string.Compare(Key, other.Key, StringComparison.Ordinal);
        }
    }
}
