using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.16.3 protected-storage promotion pass.
    ///
    /// Ordinary free capacity must not end the decision before a secure-container-eligible
    /// incoming item has had a chance to displace strictly lower-priority protected loot
    /// into still-legal ordinary storage. This pass is non-destructive: it only repacks
    /// existing items and requires the incoming item to end inside the secure-container
    /// root. The existing destructive planner remains the later fallback.
    /// </summary>
    private bool TryBuildSecureProtectionRecommendationV1163(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        out RaidRecommendation recommendation)
    {
        recommendation = default!;

        // Preserve the existing rule that a genuinely empty compatible equipment/carrier
        // slot is free carrying capacity and should be used before inventory storage.
        if (EnumerateRaidEquipTargetsHardened(current, incoming)
            .Any(static target => target.ExistingItem is null))
        {
            return false;
        }

        var allSurfaces = EnumerateRaidSurfacesForSnapshot(incoming, current.StoredItems)
            .GroupBy(SurfaceId, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();
        if (allSurfaces.Length == 0)
            return false;

        var surfaceById = allSurfaces.ToDictionary(SurfaceId, StringComparer.Ordinal);
        var secureSurfaceIds = allSurfaces
            .Where(surface => IsSecureProtectedSurfaceV1163(surface, current.StoredItems))
            .Select(SurfaceId)
            .ToHashSet(StringComparer.Ordinal);
        if (secureSurfaceIds.Count == 0)
            return false;

        // Build the incoming options from the normal source-backed storage/filter rules,
        // then deliberately remove every non-secure destination. This is the key boundary
        // that prevents a free pocket from winning merely because it requires zero moves.
        var incomingOptions = BuildRepackingOptions(incoming, current.StoredItems)
            .Where(option => secureSurfaceIds.Contains(option.SurfaceId))
            .ToArray();
        if (incomingOptions.Length == 0)
            return false;

        var coreSurfaces = allSurfaces
            .Select((surface, priority) => new FarmingGuideRepackingSurface(
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
                    .ToArray()))
            .ToArray();

        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true) with
        {
            // decisionScan already carries stack-total Flea value. Keep Quantity=1 here
            // and project total weight explicitly so an equal-value tie remains correct.
            UnitWeightKg = incoming.WeightKg is { } weight
                ? Math.Max(0m, weight) * Math.Max(1, scanned.Quantity)
                : null,
        };

        var coreItems = new List<FarmingGuideRepackingItem>(current.StoredItems.Count);
        foreach (var stored in current.StoredItems)
        {
            var existing = ResolveItem(stored.Item);
            if (existing is null)
                return false;

            var currentSurfaceId = SurfaceId(stored.Storage, stored.ParentInstanceId, stored.GridIndex);
            if (!surfaceById.ContainsKey(currentSurfaceId))
                return false;

            var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                stored.Storage,
                stored.ParentInstanceId,
                existing,
                stored.Rotated);
            var rawOptions = BuildRepackingOptions(existing, current.StoredItems).ToArray();
            var locked = IsInsideLockedItemInSnapshot(stored.InstanceId, current.StoredItems) ||
                         SubtreeContainsLockedItemInSnapshot(stored.InstanceId, current.StoredItems);
            var currentlySecure = IsStoredInSecureRootV1163(stored, current.StoredItems);
            var hasChildren = current.StoredItems.Any(value =>
                string.Equals(value.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal));

            IReadOnlyList<FarmingGuideRepackingOption> options;
            if (currentlySecure)
            {
                var existingMetrics = MetricsForStoredSecurePriorityV1163(stored, existing);
                var mayLoseProtection = !locked &&
                    !hasChildren &&
                    FarmingGuideLootPriorityPolicy.Compare(incomingMetrics, existingMetrics) > 0;

                // Equal/higher-priority secure contents may still slide/repack inside the
                // protected root, but can never be demoted outside it. A populated nested
                // carrier is likewise kept in the secure root because parent-only pricing
                // cannot safely value all descendants.
                options = mayLoseProtection
                    ? rawOptions
                    : rawOptions.Where(option => secureSurfaceIds.Contains(option.SurfaceId)).ToArray();
            }
            else
            {
                // This pass exists to promote the incoming item, not unrelated existing
                // loot. Items already outside secure storage may move as blockers require,
                // but they may not consume newly protected capacity during the promotion.
                options = rawOptions.Where(option => !secureSurfaceIds.Contains(option.SurfaceId)).ToArray();
            }

            var movable = !locked && options.Count > 0;
            coreItems.Add(new FarmingGuideRepackingItem(
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

        const string incomingInstanceId = "__incoming_secure_v1163__";
        var corePlan = FarmingGuideRepackingPlanner.TryPlan(
            coreSurfaces,
            coreItems,
            new FarmingGuideRepackingIncoming(incomingInstanceId, incomingOptions));
        if (corePlan is null || !surfaceById.TryGetValue(corePlan.Incoming.SurfaceId, out var destination))
            return false;

        var placementById = corePlan.ExistingPlacements
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var proposedStored = new List<FarmingGuideStoredItemState>(current.StoredItems.Count + 1);
        foreach (var stored in current.StoredItems)
        {
            if (!placementById.TryGetValue(stored.InstanceId, out var placement) ||
                !surfaceById.TryGetValue(placement.SurfaceId, out var targetSurface))
            {
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

        var incomingState = new FarmingGuideStoredItemState(
            Guid.NewGuid().ToString("N"),
            FarmingGuideItemState.Create(incoming.Id),
            destination.Kind,
            destination.GridIndex,
            corePlan.Incoming.X,
            corePlan.Incoming.Y,
            corePlan.Incoming.Rotated,
            destination.ParentInstanceId);
        proposedStored.Add(incomingState);

        if (!TryNormalizeRootStorageKinds(proposedStored, out var normalized))
            return false;

        var normalizedIncoming = normalized.FirstOrDefault(value =>
            string.Equals(value.InstanceId, incomingState.InstanceId, StringComparison.Ordinal));
        if (normalizedIncoming is null || normalizedIncoming.Storage != FarmingGuideStorageKind.SecureContainer)
            return false;

        var afterById = normalized.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        foreach (var before in current.StoredItems.Where(value =>
                     IsStoredInSecureRootV1163(value, current.StoredItems)))
        {
            if (!afterById.TryGetValue(before.InstanceId, out var after) ||
                after.Storage == FarmingGuideStorageKind.SecureContainer)
            {
                continue;
            }

            // A secure -> ordinary move is permitted only for an unlocked leaf whose
            // deterministic priority is strictly lower than the incoming item.
            if (IsInsideLockedItemInSnapshot(before.InstanceId, current.StoredItems) ||
                SubtreeContainsLockedItemInSnapshot(before.InstanceId, current.StoredItems) ||
                current.StoredItems.Any(value =>
                    string.Equals(value.ParentInstanceId, before.InstanceId, StringComparison.Ordinal)))
            {
                return false;
            }

            var existing = ResolveItem(before.Item);
            if (existing is null ||
                FarmingGuideLootPriorityPolicy.Compare(
                    incomingMetrics,
                    MetricsForStoredSecurePriorityV1163(before, existing)) <= 0)
            {
                return false;
            }
        }

        recommendation = new RaidRecommendation(
            "보안 컨테이너 우선 보관",
            FarmingGuideInstructionAction.Store,
            current with { StoredItems = normalized });
        return true;
    }

    private FarmingGuideLootMetrics MetricsForStoredSecurePriorityV1163(
        FarmingGuideStoredItemState stored,
        GameItem item)
    {
        var snapshot = _raidBridge?.ResolveSnapshot(item.Id);
        var slots = Math.Max(1, snapshot?.Slots ?? ((item.Width ?? 1) * (item.Height ?? 1)));
        var accepted = _acceptedRaidItemCounts.GetValueOrDefault(item.Id);
        var needed = snapshot is null
            ? 0
            : Math.Max(0, snapshot.CurrentNeededFir - accepted);
        int? flea = snapshot?.FleaAveragePrice;
        if (flea is not > 0 && _raidFleaAveragePrices.TryGetValue(item.Id, out var remembered))
            flea = remembered;

        return new FarmingGuideLootMetrics(
            needed,
            snapshot?.TraderSellPrice ?? item.BasePrice,
            flea,
            slots)
        {
            Quantity = stored.NormalizedQuantity,
            UnitWeightKg = item.WeightKg,
        };
    }

    private static bool IsSecureProtectedSurfaceV1163(
        RaidSurface surface,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        if (surface.ParentInstanceId is null)
            return surface.Kind == FarmingGuideStorageKind.SecureContainer;

        var parent = storedItems.FirstOrDefault(value =>
            string.Equals(value.InstanceId, surface.ParentInstanceId, StringComparison.Ordinal));
        return parent is not null && IsStoredInSecureRootV1163(parent, storedItems);
    }

    private static bool IsStoredInSecureRootV1163(
        FarmingGuideStoredItemState stored,
        IReadOnlyList<FarmingGuideStoredItemState> storedItems)
    {
        var current = stored;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (!string.IsNullOrWhiteSpace(current.ParentInstanceId))
        {
            if (!visited.Add(current.InstanceId))
                return false;
            var parent = storedItems.FirstOrDefault(value =>
                string.Equals(value.InstanceId, current.ParentInstanceId, StringComparison.Ordinal));
            if (parent is null)
                return false;
            current = parent;
        }

        return current.Storage == FarmingGuideStorageKind.SecureContainer;
    }
}
