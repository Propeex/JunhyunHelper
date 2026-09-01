using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.15.4 raid-planning path. The v1.15.3 method remains private historical code in
    /// its original file so the correction is reviewable as a bounded maintenance change;
    /// runtime Scanner input is routed here from FarmingGuidePage.Raid.cs.
    /// </summary>
    private RaidRecommendation PlanScannedItemHardened(ScannerItemSnapshot scanned, GameItem item)
    {
        var current = BuildSnapshot();
        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        var equipTargets = EnumerateRaidEquipTargetsHardened(current, item).ToArray();

        // Empty equipment capacity is free carrying capacity. Preserve both items instead
        // of consuming storage or replacing something when a legal empty slot exists.
        var emptyEquip = equipTargets.FirstOrDefault(static target => target.ExistingItem is null);
        if (emptyEquip is not null)
        {
            return new RaidRecommendation(
                $"{emptyEquip.Label}에 장착",
                FarmingGuideInstructionAction.Equip,
                emptyEquip.ProposedSnapshot);
        }

        var surfaces = EnumerateRaidSurfacesForSnapshot(item, current.StoredItems).ToArray();
        foreach (var surface in surfaces)
        {
            if (!TryFindFitHardened(surface, item, current.StoredItems, ignoredInstanceId: null, out var fit))
                continue;

            return CreateDirectStoreRecommendation(current, item, surface, fit);
        }

        // Fragmented free capacity must be exhausted before any destructive decision.
        if (TryBuildRepackingStorePlan(current, item, out var repacked))
        {
            return new RaidRecommendation(
                FormatRepackingStoreInstruction(current, repacked),
                FarmingGuideInstructionAction.Store,
                repacked.ProposedSnapshot);
        }

        // Equipment replacement is destructive (the old equipped item leaves the modeled
        // raid state), so it intentionally runs only after direct/repacked non-destructive
        // storage has failed.
        var incomingEquipMetrics = AsSingleSlot(incomingMetrics);
        RaidEquipCandidate? bestEquipReplacement = null;
        foreach (var target in equipTargets.Where(static target => target.ExistingItem is not null))
        {
            var metrics = AsSingleSlot(MetricsForExisting(target.ExistingItem!));
            if (!FarmingGuideLootPriorityPolicy.ShouldReplace(incomingEquipMetrics, metrics))
                continue;
            if (bestEquipReplacement is null ||
                FarmingGuideLootPriorityPolicy.Compare(metrics, bestEquipReplacement.Metrics) < 0)
            {
                bestEquipReplacement = new RaidEquipCandidate(target, metrics);
            }
        }

        if (bestEquipReplacement is not null)
        {
            return new RaidRecommendation(
                $"{bestEquipReplacement.Target.Label}의 {DisplayName(bestEquipReplacement.Target.ExistingItem!)}과 교체",
                FarmingGuideInstructionAction.ReplaceEquip,
                bestEquipReplacement.Target.ProposedSnapshot);
        }

        // Destructive storage replacement remains a last resort. Never auto-delete a
        // populated container: valuing only the parent would silently destroy unmodeled
        // aggregate value in its descendants. A locked ancestor/reserved internal cell also
        // protects the candidate from automated removal.
        var replacements = surfaces
            .SelectMany(surface => current.StoredItems
                .Where(stored => stored.GridIndex == surface.GridIndex &&
                                 IsOnStorageSurface(stored, surface.Kind, surface.ParentInstanceId))
                .Select(stored => (Surface: surface, Stored: stored)))
            .Where(candidate => !IsInsideLockedItemInSnapshot(candidate.Stored.InstanceId, current.StoredItems))
            .Where(candidate => !SubtreeContainsLockedItemInSnapshot(candidate.Stored.InstanceId, current.StoredItems))
            .Where(candidate => !current.StoredItems.Any(child =>
                string.Equals(child.ParentInstanceId, candidate.Stored.InstanceId, StringComparison.Ordinal)))
            .Where(candidate => !_reservedCells.Any(cell =>
                string.Equals(cell.ParentInstanceId, candidate.Stored.InstanceId, StringComparison.Ordinal)))
            .Select(candidate =>
            {
                var existingItem = ResolveItem(candidate.Stored.Item);
                var metrics = existingItem is null
                    ? null
                    : MetricsForStorageSurface(existingItem, candidate.Surface);
                var incoming = MetricsForStorageSurface(incomingMetrics, candidate.Surface);
                return (candidate.Surface, candidate.Stored, ExistingItem: existingItem, Metrics: metrics, Incoming: incoming);
            })
            .Where(candidate => candidate.ExistingItem is not null && candidate.Metrics is not null)
            .Where(candidate => FarmingGuideLootPriorityPolicy.ShouldReplace(candidate.Incoming, candidate.Metrics!))
            .OrderBy(candidate => candidate.Metrics!, LootMetricsComparer.Instance)
            .ThenBy(candidate => candidate.Stored.InstanceId, StringComparer.Ordinal)
            .ToArray();

        foreach (var candidate in replacements)
        {
            var remaining = current.StoredItems
                .Where(value => !string.Equals(value.InstanceId, candidate.Stored.InstanceId, StringComparison.Ordinal))
                .ToArray();
            var reduced = current with { StoredItems = remaining };

            // Removing one genuinely lower-priority leaf may create either an immediate
            // opening or enough slack to relocate the actual geometric blocker. Re-run the
            // same non-destructive planner for the remaining items instead of requiring the
            // incoming item to occupy the removed item's exact cells.
            if (!TryBuildRepackingStorePlan(reduced, item, out var replacementPlan))
                continue;

            var moveText = FormatMoveSummary(reduced, replacementPlan);
            var instruction = string.IsNullOrWhiteSpace(moveText)
                ? $"{candidate.Surface.Label}의 {DisplayName(candidate.ExistingItem!)}과 교체"
                : $"{DisplayName(candidate.ExistingItem!)} 버리고 · {moveText} 후 {replacementPlan.Destination.Label}에 보관";
            return new RaidRecommendation(
                instruction,
                FarmingGuideInstructionAction.Replace,
                replacementPlan.ProposedSnapshot);
        }

        return new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
    }

    private RaidRecommendation CreateDirectStoreRecommendation(
        FarmingGuideLoadoutSnapshot current,
        GameItem item,
        RaidSurface surface,
        RaidFit fit)
    {
        var added = new FarmingGuideStoredItemState(
            Guid.NewGuid().ToString("N"),
            FarmingGuideItemState.Create(item.Id),
            surface.Kind,
            surface.GridIndex,
            fit.X,
            fit.Y,
            fit.Rotated,
            surface.ParentInstanceId);
        var proposed = current with { StoredItems = current.StoredItems.Append(added).ToArray() };
        return new RaidRecommendation(
            $"{surface.Label}에 보관",
            FarmingGuideInstructionAction.Store,
            proposed);
    }

    private bool TryBuildRepackingStorePlan(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming,
        out RaidRepackingStorePlan plan)
    {
        var orderedSurfaces = EnumerateRaidSurfacesForSnapshot(incoming, current.StoredItems).ToArray();
        if (orderedSurfaces.Length == 0)
        {
            plan = default!;
            return false;
        }

        var surfaceById = orderedSurfaces
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var coreSurfaces = orderedSurfaces
            .Select((surface, priority) => new FarmingGuideRepackingSurface(
                SurfaceId(surface),
                surface.ParentInstanceId,
                surface.Definition.Width,
                surface.Definition.Height,
                priority,
                _reservedCells
                    .Where(cell => IsReservedOnSurface(cell, surface))
                    .Select((cell, index) => new FarmingGuideGridPlacement(
                        $"__reserved_{index}",
                        cell.X,
                        cell.Y,
                        1,
                        1))
                    .ToArray()))
            .GroupBy(surface => surface.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

        var coreItems = new List<FarmingGuideRepackingItem>();
        foreach (var stored in current.StoredItems)
        {
            var existingItem = ResolveItem(stored.Item);
            if (existingItem is null)
            {
                plan = default!;
                return false;
            }

            var currentSurfaceId = SurfaceId(stored.Storage, stored.ParentInstanceId, stored.GridIndex);
            if (!surfaceById.ContainsKey(currentSurfaceId))
            {
                plan = default!;
                return false;
            }

            var currentFootprint = FarmingGuideStoragePlacementPolicy.Footprint(
                stored.Storage,
                stored.ParentInstanceId,
                existingItem,
                stored.Rotated);
            var movable = !IsInsideLockedItemInSnapshot(stored.InstanceId, current.StoredItems) &&
                          !SubtreeContainsLockedItemInSnapshot(stored.InstanceId, current.StoredItems);
            var options = BuildRepackingOptions(existingItem, current.StoredItems).ToArray();
            if (options.Length == 0)
                movable = false;

            coreItems.Add(new FarmingGuideRepackingItem(
                stored.InstanceId,
                currentSurfaceId,
                stored.X,
                stored.Y,
                currentFootprint.Width,
                currentFootprint.Height,
                stored.Rotated,
                movable,
                options));
        }

        var incomingOptions = BuildRepackingOptions(incoming, current.StoredItems).ToArray();
        if (incomingOptions.Length == 0)
        {
            plan = default!;
            return false;
        }

        const string incomingInstanceId = "__incoming__";
        var corePlan = FarmingGuideRepackingPlanner.TryPlan(
            coreSurfaces,
            coreItems,
            new FarmingGuideRepackingIncoming(incomingInstanceId, incomingOptions));
        if (corePlan is null || !surfaceById.TryGetValue(corePlan.Incoming.SurfaceId, out var destination))
        {
            plan = default!;
            return false;
        }

        var placementById = corePlan.ExistingPlacements
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var proposedStored = new List<FarmingGuideStoredItemState>(current.StoredItems.Count + 1);
        foreach (var stored in current.StoredItems)
        {
            if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var targetSurface))
            {
                plan = default!;
                return false;
            }

            proposedStored.Add(stored with
            {
                Storage = targetSurface.Kind,
                GridIndex = targetSurface.GridIndex,
                X = placement.X,
                Y = placement.Y,
                Rotated = placement.Rotated,
                ParentInstanceId = targetSurface.ParentInstanceId,
            });
        }

        proposedStored.Add(new FarmingGuideStoredItemState(
            Guid.NewGuid().ToString("N"),
            FarmingGuideItemState.Create(incoming.Id),
            destination.Kind,
            destination.GridIndex,
            corePlan.Incoming.X,
            corePlan.Incoming.Y,
            corePlan.Incoming.Rotated,
            destination.ParentInstanceId));

        if (!TryNormalizeRootStorageKinds(proposedStored, out var normalized))
        {
            plan = default!;
            return false;
        }

        plan = new RaidRepackingStorePlan(
            current with { StoredItems = normalized },
            destination,
            corePlan.MovedInstanceIds,
            corePlan.SearchNodes);
        return true;
    }

    private IEnumerable<FarmingGuideRepackingOption> BuildRepackingOptions(
        GameItem item,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var preference = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var surface in EnumerateRaidSurfacesForSnapshot(item, storedItems))
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

                var key = $"{SurfaceId(surface)}|{footprint.Width}|{footprint.Height}|{rotated}";
                if (!seen.Add(key))
                    continue;
                yield return new FarmingGuideRepackingOption(
                    SurfaceId(surface),
                    footprint.Width,
                    footprint.Height,
                    rotated,
                    preference);
            }
            preference++;
        }
    }

    private string FormatRepackingStoreInstruction(
        FarmingGuideLoadoutSnapshot current,
        RaidRepackingStorePlan plan)
    {
        var moveText = FormatMoveSummary(current, plan);
        return string.IsNullOrWhiteSpace(moveText)
            ? $"{plan.Destination.Label}에 보관"
            : $"{moveText} 후 {plan.Destination.Label}에 보관";
    }

    private string FormatMoveSummary(
        FarmingGuideLoadoutSnapshot current,
        RaidRepackingStorePlan plan)
    {
        if (plan.MovedInstanceIds.Count == 0)
            return string.Empty;

        var before = current.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var after = plan.ProposedSnapshot.StoredItems.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var summaries = new List<string>();
        foreach (var id in plan.MovedInstanceIds)
        {
            if (!before.TryGetValue(id, out var original) || !after.TryGetValue(id, out var moved))
                continue;
            var item = ResolveItem(original.Item);
            var name = item is null ? "아이템" : DisplayName(item);
            var originalSurface = SurfaceId(original.Storage, original.ParentInstanceId, original.GridIndex);
            var movedSurface = SurfaceId(moved.Storage, moved.ParentInstanceId, moved.GridIndex);
            if (string.Equals(originalSurface, movedSurface, StringComparison.Ordinal))
            {
                summaries.Add($"{name} 위치 이동");
                continue;
            }

            var target = EnumerateRaidSurfacesForSnapshot(item ?? ResolveItem(moved.Item)!, plan.ProposedSnapshot.StoredItems)
                .FirstOrDefault(surface => string.Equals(SurfaceId(surface), movedSurface, StringComparison.Ordinal));
            summaries.Add(target is null ? $"{name} 이동" : $"{name}→{target.Label}");
        }

        if (summaries.Count <= 3)
            return string.Join(" · ", summaries);
        return $"{string.Join(" · ", summaries.Take(2))} 외 {summaries.Count - 2}개 이동";
    }

    private IEnumerable<RaidEquipTarget> EnumerateRaidEquipTargetsHardened(
        FarmingGuideLoadoutSnapshot current,
        GameItem incoming)
    {
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
            var existingState = current.Equipment.GetValueOrDefault(slot);
            if (existingState is not null && _lockedEquipmentSlots.Contains(slot))
                continue;
            if (!CanEquipInSnapshot(slot, incoming, current))
                continue;

            var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(current.Equipment)
            {
                [slot] = FarmingGuideItemState.Create(incoming.Id),
            };
            yield return new RaidEquipTarget(
                EquipmentLabel(slot),
                ResolveItem(existingState),
                current with { Equipment = equipment });
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            var existingState = kind switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                FarmingGuideStorageKind.SecureContainer => current.SecureContainer,
                _ => null,
            };
            if (existingState is not null && _lockedCarriers.Contains(kind))
                continue;
            if (!CanSetCarrierInSnapshot(kind, incoming, current))
                continue;

            var proposed = kind switch
            {
                FarmingGuideStorageKind.Rig => current with { Rig = FarmingGuideItemState.Create(incoming.Id) },
                FarmingGuideStorageKind.Backpack => current with { Backpack = FarmingGuideItemState.Create(incoming.Id) },
                FarmingGuideStorageKind.SecureContainer => current with { SecureContainer = FarmingGuideItemState.Create(incoming.Id) },
                _ => current,
            };
            yield return new RaidEquipTarget(
                CarrierLabel(kind),
                ResolveItem(existingState),
                proposed);
        }
    }

    private IEnumerable<RaidSurface> EnumerateRaidSurfacesForSnapshot(
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

        var root = StorageDefinitions().ToDictionary(value => value.Kind);
        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.SecureContainer,
                     FarmingGuideStorageKind.Pockets,
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SpecialSlots,
                 })
        {
            if (!root.TryGetValue(kind, out var storage))
                continue;
            for (var index = 0; index < storage.Grids.Count; index++)
                yield return new RaidSurface(kind, null, index, storage.Grids[index], storage.Label);
        }

        foreach (var surface in nested.Where(surface =>
                     !FarmingGuideStoragePlacementPolicy.IsDedicatedStorageFor(
                         incoming,
                         surface.Definition.Filters)))
        {
            yield return surface;
        }
    }

    private bool TryFindFitHardened(
        RaidSurface surface,
        GameItem item,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems,
        string? ignoredInstanceId,
        out RaidFit fit)
    {
        if (!FarmingGuideStoragePlacementPolicy.CanStore(
                surface.Kind,
                surface.ParentInstanceId,
                item,
                surface.Definition.Filters))
        {
            fit = default;
            return false;
        }

        var existing = storedItems
            .Where(stored => stored.GridIndex == surface.GridIndex &&
                             IsOnStorageSurface(stored, surface.Kind, surface.ParentInstanceId))
            .Select(stored =>
            {
                var existingItem = ResolveItem(stored.Item);
                var footprint = existingItem is null
                    ? (Width: 1, Height: 1)
                    : FarmingGuideStoragePlacementPolicy.Footprint(
                        stored.Storage,
                        stored.ParentInstanceId,
                        existingItem,
                        stored.Rotated);
                return new FarmingGuideGridPlacement(
                    stored.InstanceId,
                    stored.X,
                    stored.Y,
                    footprint.Width,
                    footprint.Height);
            })
            .Concat(_reservedCells
                .Where(cell => IsReservedOnSurface(cell, surface))
                .Select((cell, index) => new FarmingGuideGridPlacement(
                    $"__locked_{index}", cell.X, cell.Y, 1, 1)))
            .ToArray();

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
            var found = FarmingGuidePlacementEngine.FindFirstFit(
                surface.Definition.Width,
                surface.Definition.Height,
                footprint.Width,
                footprint.Height,
                rotated: false,
                existing,
                ignoredInstanceId);
            if (found is { } point)
            {
                fit = new RaidFit(point.X, point.Y, rotated);
                return true;
            }
        }

        fit = default;
        return false;
    }

    private bool IsInsideLockedItemInSnapshot(
        string instanceId,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        string? current = instanceId;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
        {
            if (_lockedItemInstanceIds.Contains(current))
                return true;
            current = storedItems.FirstOrDefault(value =>
                string.Equals(value.InstanceId, current, StringComparison.Ordinal))?.ParentInstanceId;
        }
        return false;
    }

    private bool SubtreeContainsLockedItemInSnapshot(
        string instanceId,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var pending = new Stack<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        pending.Push(instanceId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
                continue;
            if (_lockedItemInstanceIds.Contains(current))
                return true;
            foreach (var child in storedItems.Where(value =>
                         string.Equals(value.ParentInstanceId, current, StringComparison.Ordinal)))
                pending.Push(child.InstanceId);
        }
        return false;
    }

    private static bool IsReservedOnSurface(FarmingGuideLockedCell cell, RaidSurface surface)
    {
        if (cell.GridIndex != surface.GridIndex)
            return false;
        if (surface.ParentInstanceId is not null || cell.ParentInstanceId is not null)
        {
            return string.Equals(
                cell.ParentInstanceId,
                surface.ParentInstanceId,
                StringComparison.Ordinal);
        }
        return cell.Storage == surface.Kind;
    }

    private static string SurfaceId(RaidSurface surface) =>
        SurfaceId(surface.Kind, surface.ParentInstanceId, surface.GridIndex);

    private static string SurfaceId(
        FarmingGuideStorageKind kind,
        string? parentInstanceId,
        int gridIndex) =>
        parentInstanceId is null
            ? $"R|{(int)kind}|{gridIndex}"
            : $"N|{parentInstanceId}|{gridIndex}";

    private static bool TryNormalizeRootStorageKinds(
        IReadOnlyList<FarmingGuideStoredItemState> source,
        out IReadOnlyList<FarmingGuideStoredItemState> normalized)
    {
        var byId = source.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var rootKindById = new Dictionary<string, FarmingGuideStorageKind>(StringComparer.Ordinal);

        bool TryResolve(string instanceId, HashSet<string> visiting, out FarmingGuideStorageKind kind)
        {
            if (rootKindById.TryGetValue(instanceId, out kind))
                return true;
            if (!byId.TryGetValue(instanceId, out var item) || !visiting.Add(instanceId))
            {
                kind = default;
                return false;
            }

            if (item.ParentInstanceId is null)
            {
                kind = item.Storage;
            }
            else if (!TryResolve(item.ParentInstanceId, visiting, out kind))
            {
                visiting.Remove(instanceId);
                return false;
            }

            visiting.Remove(instanceId);
            rootKindById[instanceId] = kind;
            return true;
        }

        var result = new List<FarmingGuideStoredItemState>(source.Count);
        foreach (var item in source)
        {
            if (!TryResolve(item.InstanceId, new HashSet<string>(StringComparer.Ordinal), out var rootKind))
            {
                normalized = [];
                return false;
            }
            result.Add(item with { Storage = rootKind });
        }

        normalized = result;
        return true;
    }

    private sealed record RaidRepackingStorePlan(
        FarmingGuideLoadoutSnapshot ProposedSnapshot,
        RaidSurface Destination,
        IReadOnlyList<string> MovedInstanceIds,
        int SearchNodes);
}
