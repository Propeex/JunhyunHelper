using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private const int V1170MaxUnifiedSubsetAttempts = 4096;
    private const string V1170EquipmentInstancePrefix = "__equipment_v1170_";
    private const string V1170CarrierInstancePrefix = "__carrier_v1170_";
    private const string V1170IncomingInstancePrefix = "__incoming_v1170_";

    private static readonly FarmingGuideEquipmentSlot[] V1170GlobalEquipmentSlots =
    [
        FarmingGuideEquipmentSlot.Headset,
        FarmingGuideEquipmentSlot.Helmet,
        FarmingGuideEquipmentSlot.FaceCover,
        FarmingGuideEquipmentSlot.Armband,
        FarmingGuideEquipmentSlot.BodyArmor,
        FarmingGuideEquipmentSlot.Eyewear,
        FarmingGuideEquipmentSlot.PrimaryWeapon1,
        FarmingGuideEquipmentSlot.PrimaryWeapon2,
        FarmingGuideEquipmentSlot.Holster,
    ];

    private static readonly FarmingGuideStorageKind[] V1170GlobalCarrierSlots =
    [
        FarmingGuideStorageKind.Rig,
        FarmingGuideStorageKind.Backpack,
        FarmingGuideStorageKind.SecureContainer,
    ];

    private enum GlobalRootOriginV1170
    {
        Stored,
        Equipment,
        Carrier,
        Incoming,
    }

    private enum GlobalRaidSurfaceRoleV1170
    {
        Storage,
        Equipment,
        Carrier,
    }

    private sealed record GlobalOwnedRootV1170(
        string InstanceId,
        FarmingGuideItemState State,
        GameItem Item,
        int Quantity,
        GlobalRootOriginV1170 Origin,
        FarmingGuideStoredItemState? StoredSource = null,
        FarmingGuideEquipmentSlot? EquipmentSlot = null,
        FarmingGuideStorageKind? CarrierKind = null,
        bool Fixed = false);

    private sealed record GlobalRaidSurfaceV1170(
        string Id,
        GlobalRaidSurfaceRoleV1170 Role,
        FarmingGuideStorageKind StorageKind,
        int GridIndex,
        FarmingGuideStorageGridDefinition? Definition,
        string? OwnerInstanceId = null,
        FarmingGuideEquipmentSlot? EquipmentSlot = null,
        FarmingGuideStorageKind? CarrierKind = null);

    private sealed record UnifiedSubsetV1170(
        int[] VictimIndices,
        FarmingGuideOptimizationScore Score);

    private readonly record struct UnifiedSubsetPriorityV1170(
        int NegativeFir,
        long NegativeValue,
        int RemovedCount,
        string Key) : IComparable<UnifiedSubsetPriorityV1170>
    {
        public int CompareTo(UnifiedSubsetPriorityV1170 other)
        {
            var fir = NegativeFir.CompareTo(other.NegativeFir);
            if (fir != 0)
                return fir;
            var value = NegativeValue.CompareTo(other.NegativeValue);
            if (value != 0)
                return value;
            var count = RemovedCount.CompareTo(other.RemovedCount);
            if (count != 0)
                return count;
            return StringComparer.Ordinal.Compare(Key, other.Key);
        }
    }

    /// <summary>
    /// Complete root-item candidate pool for v1.17. Unlocked stored roots, equipment roots,
    /// carrier roots and the incoming item share the same packing search. Equipment/carrier
    /// slots are virtual one-cell surfaces; container grids are owned surfaces whose capacity
    /// exists only while that exact owner item is retained.
    ///
    /// Assemblies are atomic at this boundary because the current complete-equipment runtime
    /// model exposes complete roots. If a non-empty assembly state nevertheless reaches this
    /// path, destructive advice fails closed rather than silently assuming detachable parts.
    /// Stack splitting is likewise detected as an incomplete physical domain until modeled
    /// explicitly; all-retained plans remain safe and can still be issued.
    /// </summary>
    private bool TryFindBestUnifiedRaidStateV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        FarmingGuideOptimizationScore currentScore,
        out RaidRecommendation recommendation,
        out FarmingGuideOptimizationScore score,
        out bool proofComplete)
    {
        if (!TryBuildCurrentOwnedRootsV1170(current, out var currentRoots))
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        var incomingRoot = new GlobalOwnedRootV1170(
            $"{V1170IncomingInstancePrefix}{Guid.NewGuid():N}",
            FarmingGuideItemState.Create(incoming.Id, raidAcquired: true),
            incoming,
            Math.Max(1, scanned.Quantity),
            GlobalRootOriginV1170.Incoming);

        var incompletePhysicalDomain =
            HasUnmodeledAssemblyChoicesV1170(currentRoots) ||
            HasUnmodeledStackSplitChoicesV1170(currentRoots, incomingRoot);
        var hasUnknownDiscardChoice = currentRoots.Any(root =>
            !root.Fixed && !HasProvableOwnedRootFactsV1170(root));

        var allSelected = currentRoots.Append(incomingRoot).ToArray();
        var allPacking = TryPackUnifiedSelectionV1170(current, allSelected, out var allProposed);
        if (allPacking == StoredPackingOutcomeV1170.Found)
        {
            recommendation = BuildUnifiedRecommendationV1170(
                current,
                allProposed,
                incomingRoot,
                removedCount: 0);
            recommendation = MarkIncomingRaidProvenanceV1170(current, recommendation, incoming.Id);
            if (IsWeightAdmissibleV1170(current, recommendation.ProposedSnapshot))
            {
                score = ScoreRaidStateV1170(recommendation.ProposedSnapshot, scanned);
                proofComplete = true;
                return FarmingGuideOptimizationPolicy.IsBetter(score, currentScore);
            }
        }
        else if (allPacking == StoredPackingOutcomeV1170.Indeterminate)
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        // Once a destructive choice is necessary, any unmodeled detach/split operation or
        // unknown-value removable item can change the true optimum. Do not continue under a
        // knowingly incomplete candidate set.
        if (incompletePhysicalDomain || hasUnknownDiscardChoice)
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        var victims = currentRoots
            .Where(root => !root.Fixed)
            .Where(HasProvableOwnedRootFactsV1170)
            .OrderBy(root => root.InstanceId, StringComparer.Ordinal)
            .ToArray();
        if (victims.Length == 0)
        {
            recommendation = ProvenDiscardV1170(current);
            score = currentScore;
            proofComplete = true;
            return false;
        }

        var queue = new PriorityQueue<UnifiedSubsetV1170, UnifiedSubsetPriorityV1170>();
        for (var index = 0; index < victims.Length; index++)
            EnqueueUnifiedSubsetV1170(queue, currentRoots, incomingRoot, scanned, victims, [index]);

        var attempts = 0;
        while (queue.Count > 0 && attempts < V1170MaxUnifiedSubsetAttempts)
        {
            var subset = queue.Dequeue();
            attempts++;
            if (!FarmingGuideOptimizationPolicy.IsBetter(subset.Score, currentScore))
            {
                recommendation = ProvenDiscardV1170(current);
                score = currentScore;
                proofComplete = true;
                return false;
            }

            var removed = subset.VictimIndices
                .Select(index => victims[index].InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var selected = currentRoots
                .Where(root => !removed.Contains(root.InstanceId))
                .Append(incomingRoot)
                .ToArray();
            var packing = TryPackUnifiedSelectionV1170(current, selected, out var proposed);
            if (packing == StoredPackingOutcomeV1170.Indeterminate)
            {
                recommendation = IndeterminateRaidPlanV1170(current);
                score = currentScore;
                proofComplete = false;
                return false;
            }
            if (packing == StoredPackingOutcomeV1170.Found)
            {
                recommendation = BuildUnifiedRecommendationV1170(
                    current,
                    proposed,
                    incomingRoot,
                    removed.Count);
                recommendation = MarkIncomingRaidProvenanceV1170(current, recommendation, incoming.Id);
                if (IsWeightAdmissibleV1170(current, recommendation.ProposedSnapshot))
                {
                    score = ScoreRaidStateV1170(recommendation.ProposedSnapshot, scanned);
                    proofComplete = true;
                    return true;
                }
            }

            var last = subset.VictimIndices[^1];
            for (var next = last + 1; next < victims.Length; next++)
            {
                var expanded = new int[subset.VictimIndices.Length + 1];
                Array.Copy(subset.VictimIndices, expanded, subset.VictimIndices.Length);
                expanded[^1] = next;
                EnqueueUnifiedSubsetV1170(queue, currentRoots, incomingRoot, scanned, victims, expanded);
            }
        }

        recommendation = IndeterminateRaidPlanV1170(current);
        score = currentScore;
        proofComplete = queue.Count == 0;
        return false;
    }

    private bool TryBuildCurrentOwnedRootsV1170(
        FarmingGuideLoadoutSnapshot current,
        out IReadOnlyList<GlobalOwnedRootV1170> roots)
    {
        var result = new List<GlobalOwnedRootV1170>();

        foreach (var slot in V1170GlobalEquipmentSlots)
        {
            if (!current.Equipment.TryGetValue(slot, out var state))
                continue;
            var item = ResolveItem(state);
            if (item is null)
            {
                roots = [];
                return false;
            }
            result.Add(new GlobalOwnedRootV1170(
                EquipmentInstanceIdV1170(slot),
                state,
                item,
                1,
                GlobalRootOriginV1170.Equipment,
                EquipmentSlot: slot,
                Fixed: _lockedEquipmentSlots.Contains(slot)));
        }

        foreach (var kind in V1170GlobalCarrierSlots)
        {
            var state = CarrierStateV1155(current, kind);
            if (state is null)
                continue;
            var item = ResolveItem(state);
            if (item is null)
            {
                roots = [];
                return false;
            }
            var fixedCarrier = _lockedCarriers.Contains(kind) || RootCarrierHasReservedCellV1170(kind);
            result.Add(new GlobalOwnedRootV1170(
                CarrierInstanceIdV1170(kind),
                state,
                item,
                1,
                GlobalRootOriginV1170.Carrier,
                CarrierKind: kind,
                Fixed: fixedCarrier));
        }

        foreach (var stored in current.StoredItems)
        {
            var item = ResolveItem(stored.Item);
            if (item is null)
            {
                roots = [];
                return false;
            }
            var fixedStored = IsPositionProtectedForGlobalPackingV1170(stored.InstanceId, current.StoredItems) ||
                              _reservedCells.Any(cell =>
                                  string.Equals(cell.ParentInstanceId, stored.InstanceId, StringComparison.Ordinal));
            result.Add(new GlobalOwnedRootV1170(
                stored.InstanceId,
                stored.Item,
                item,
                stored.NormalizedQuantity,
                GlobalRootOriginV1170.Stored,
                StoredSource: stored,
                Fixed: fixedStored));
        }

        roots = result;
        return true;
    }

    private StoredPackingOutcomeV1170 TryPackUnifiedSelectionV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalOwnedRootV1170> selected,
        out FarmingGuideLoadoutSnapshot proposed)
    {
        var surfaces = BuildUnifiedSurfacesV1170(selected).ToArray();
        var surfaceById = surfaces.ToDictionary(surface => surface.Id, StringComparer.Ordinal);
        var selectedById = selected.ToDictionary(root => root.InstanceId, StringComparer.Ordinal);
        var currentRootsById = BuildCurrentRootLookupV1170(selected);

        var coreSurfaces = new List<FarmingGuideRepackingSurface>(surfaces.Length);
        var surfaceOwners = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            var fixedObstacles = BuildUnifiedFixedObstaclesV1170(current, selected, surface).ToArray();
            coreSurfaces.Add(new FarmingGuideRepackingSurface(
                surface.Id,
                surface.OwnerInstanceId,
                surface.Role == GlobalRaidSurfaceRoleV1170.Storage
                    ? surface.Definition!.Width
                    : 1,
                surface.Role == GlobalRaidSurfaceRoleV1170.Storage
                    ? surface.Definition!.Height
                    : 1,
                UnifiedSurfacePriorityV1170(surface),
                fixedObstacles));
            surfaceOwners[surface.Id] = surface.OwnerInstanceId;
        }

        var movable = new List<FarmingGuideGlobalPackingItem>();
        foreach (var root in selected.Where(root => !root.Fixed))
        {
            var options = BuildUnifiedOptionsV1170(root, surfaces).ToArray();
            if (options.Length == 0)
            {
                proposed = current;
                return StoredPackingOutcomeV1170.NoSolution;
            }
            movable.Add(new FarmingGuideGlobalPackingItem(root.InstanceId, options));
        }

        FarmingGuideLoadoutSnapshot? validatedCandidate = null;
        bool FinalValidator(IReadOnlyList<FarmingGuideRepackingPlacement> placements)
        {
            if (!TryRebuildUnifiedSnapshotV1170(
                    current,
                    selected,
                    surfaces,
                    placements,
                    out var candidate))
            {
                return false;
            }
            if (!IsUnifiedTopLevelLegalV1170(candidate))
                return false;
            if (!PreservesExplicitLocksV1163(current, candidate) ||
                !PreservesLockedItemPlacementV1164(current, candidate))
            {
                return false;
            }
            validatedCandidate = candidate;
            return true;
        }

        var plan = FarmingGuideGlobalPackingPlanner.Plan(
            coreSurfaces,
            movable,
            surfaceOwners: surfaceOwners,
            finalValidator: FinalValidator);
        if (plan.Status == FarmingGuideGlobalPackingStatus.BudgetExceeded)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.Indeterminate;
        }
        if (!plan.Found || validatedCandidate is null)
        {
            proposed = current;
            return StoredPackingOutcomeV1170.NoSolution;
        }

        proposed = validatedCandidate;
        return StoredPackingOutcomeV1170.Found;
    }

    private IEnumerable<GlobalRaidSurfaceV1170> BuildUnifiedSurfacesV1170(
        IReadOnlyList<GlobalOwnedRootV1170> selected)
    {
        for (var index = 0; index < _pocketGrids.Count; index++)
        {
            yield return new GlobalRaidSurfaceV1170(
                RootStorageSurfaceIdV1170(FarmingGuideStorageKind.Pockets, index),
                GlobalRaidSurfaceRoleV1170.Storage,
                FarmingGuideStorageKind.Pockets,
                index,
                _pocketGrids[index]);
        }

        for (var index = 0; index < 3; index++)
        {
            yield return new GlobalRaidSurfaceV1170(
                RootStorageSurfaceIdV1170(FarmingGuideStorageKind.SpecialSlots, index),
                GlobalRaidSurfaceRoleV1170.Storage,
                FarmingGuideStorageKind.SpecialSlots,
                index,
                new FarmingGuideStorageGridDefinition(1, 1, FarmingGuideItemFilter.Empty));
        }

        foreach (var root in selected.OrderBy(root => root.InstanceId, StringComparer.Ordinal))
        {
            var grids = root.Item.FarmingGuideData?.StorageGrids;
            if (grids is null)
                continue;
            for (var index = 0; index < grids.Count; index++)
            {
                yield return new GlobalRaidSurfaceV1170(
                    ItemGridSurfaceIdV1170(root.InstanceId, index),
                    GlobalRaidSurfaceRoleV1170.Storage,
                    root.StoredSource?.Storage ?? root.CarrierKind ?? FarmingGuideStorageKind.Pockets,
                    index,
                    grids[index],
                    OwnerInstanceId: root.InstanceId);
            }
        }

        foreach (var slot in V1170GlobalEquipmentSlots)
        {
            yield return new GlobalRaidSurfaceV1170(
                EquipmentSurfaceIdV1170(slot),
                GlobalRaidSurfaceRoleV1170.Equipment,
                FarmingGuideStorageKind.Pockets,
                0,
                null,
                EquipmentSlot: slot);
        }

        foreach (var kind in V1170GlobalCarrierSlots)
        {
            yield return new GlobalRaidSurfaceV1170(
                CarrierSurfaceIdV1170(kind),
                GlobalRaidSurfaceRoleV1170.Carrier,
                kind,
                0,
                null,
                CarrierKind: kind);
        }
    }

    private IEnumerable<FarmingGuideGridPlacement> BuildUnifiedFixedObstaclesV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalOwnedRootV1170> selected,
        GlobalRaidSurfaceV1170 surface)
    {
        var index = 0;
        foreach (var root in selected.Where(root => root.Fixed))
        {
            var fixedSurfaceId = FixedSurfaceIdV1170(root, current, selected);
            if (!string.Equals(fixedSurfaceId, surface.Id, StringComparison.Ordinal))
                continue;

            if (surface.Role != GlobalRaidSurfaceRoleV1170.Storage)
            {
                yield return new FarmingGuideGridPlacement(
                    $"__fixed_root_v1170_{index++}", 0, 0, 1, 1);
                continue;
            }

            if (root.StoredSource is not { } stored)
                continue;
            var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                stored.Storage,
                stored.ParentInstanceId,
                root.Item,
                stored.Rotated);
            yield return new FarmingGuideGridPlacement(
                root.InstanceId,
                stored.X,
                stored.Y,
                footprint.Width,
                footprint.Height);
        }

        if (surface.Role == GlobalRaidSurfaceRoleV1170.Equipment &&
            surface.EquipmentSlot is { } slot &&
            _lockedEquipmentSlots.Contains(slot) &&
            !selected.Any(root => root.Fixed && root.EquipmentSlot == slot))
        {
            yield return new FarmingGuideGridPlacement("__locked_empty_equipment_v1170", 0, 0, 1, 1);
        }

        if (surface.Role == GlobalRaidSurfaceRoleV1170.Carrier &&
            surface.CarrierKind is { } kind &&
            _lockedCarriers.Contains(kind) &&
            !selected.Any(root => root.Fixed && root.CarrierKind == kind))
        {
            yield return new FarmingGuideGridPlacement("__locked_empty_carrier_v1170", 0, 0, 1, 1);
        }

        if (surface.Role != GlobalRaidSurfaceRoleV1170.Storage)
            yield break;

        foreach (var cell in _reservedCells)
        {
            var reservedSurface = ReservedSurfaceIdV1170(cell, selected);
            if (!string.Equals(reservedSurface, surface.Id, StringComparison.Ordinal))
                continue;
            yield return new FarmingGuideGridPlacement(
                $"__reserved_unified_v1170_{index++}",
                cell.X,
                cell.Y,
                1,
                1);
        }
    }

    private IEnumerable<FarmingGuideRepackingOption> BuildUnifiedOptionsV1170(
        GlobalOwnedRootV1170 root,
        IReadOnlyList<GlobalRaidSurfaceV1170> surfaces)
    {
        var currentSurface = CurrentSurfaceIdV1170(root, null);
        var preference = 10;
        foreach (var surface in surfaces)
        {
            var surfacePreference = string.Equals(currentSurface, surface.Id, StringComparison.Ordinal)
                ? 0
                : preference++;

            if (surface.Role == GlobalRaidSurfaceRoleV1170.Equipment)
            {
                if (root.Quantity == 1 && surface.EquipmentSlot is { } slot &&
                    FarmingGuideCompatibility.IsEquipmentSlotCompatible(slot, root.Item))
                {
                    yield return new FarmingGuideRepackingOption(surface.Id, 1, 1, false, surfacePreference);
                }
                continue;
            }

            if (surface.Role == GlobalRaidSurfaceRoleV1170.Carrier)
            {
                if (root.Quantity == 1 && surface.CarrierKind is { } kind &&
                    FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, root.Item))
                {
                    yield return new FarmingGuideRepackingOption(surface.Id, 1, 1, false, surfacePreference);
                }
                continue;
            }

            var definition = surface.Definition!;
            if (!FarmingGuideStoragePlacementPolicy.CanStore(
                    surface.StorageKind,
                    surface.OwnerInstanceId,
                    root.Item,
                    definition.Filters))
            {
                continue;
            }

            var rotations = FarmingGuideStoragePlacementPolicy.SupportsRotation(
                surface.StorageKind,
                surface.OwnerInstanceId,
                root.Item)
                ? new[] { false, true }
                : new[] { false };
            foreach (var rotated in rotations)
            {
                var footprint = FarmingGuideStoragePlacementPolicy.Footprint(
                    surface.StorageKind,
                    surface.OwnerInstanceId,
                    root.Item,
                    rotated);
                if (footprint.Width <= definition.Width && footprint.Height <= definition.Height)
                {
                    yield return new FarmingGuideRepackingOption(
                        surface.Id,
                        footprint.Width,
                        footprint.Height,
                        rotated,
                        surfacePreference);
                }
            }
        }
    }

    private bool TryRebuildUnifiedSnapshotV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalOwnedRootV1170> selected,
        IReadOnlyList<GlobalRaidSurfaceV1170> surfaces,
        IReadOnlyList<FarmingGuideRepackingPlacement> placements,
        out FarmingGuideLoadoutSnapshot snapshot)
    {
        var surfaceById = surfaces.ToDictionary(surface => surface.Id, StringComparer.Ordinal);
        var placementById = placements.ToDictionary(value => value.InstanceId, StringComparer.Ordinal);
        var selectedById = selected.ToDictionary(root => root.InstanceId, StringComparer.Ordinal);

        var finalSurfaceByRoot = new Dictionary<string, GlobalRaidSurfaceV1170>(StringComparer.Ordinal);
        foreach (var root in selected)
        {
            string? surfaceId;
            if (root.Fixed)
                surfaceId = FixedSurfaceIdV1170(root, current, selected);
            else if (placementById.TryGetValue(root.InstanceId, out var placement))
                surfaceId = placement.SurfaceId;
            else
            {
                snapshot = current;
                return false;
            }

            if (surfaceId is null || !surfaceById.TryGetValue(surfaceId, out var surface))
            {
                snapshot = current;
                return false;
            }
            finalSurfaceByRoot[root.InstanceId] = surface;
        }

        var equipment = current.Equipment
            .Where(pair => !V1170GlobalEquipmentSlots.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        FarmingGuideItemState? rig = null;
        FarmingGuideItemState? backpack = null;
        FarmingGuideItemState? secure = null;
        var storedResult = new List<FarmingGuideStoredItemState>();

        foreach (var root in selected)
        {
            var surface = finalSurfaceByRoot[root.InstanceId];
            if (surface.Role == GlobalRaidSurfaceRoleV1170.Equipment)
            {
                equipment[surface.EquipmentSlot!.Value] = root.State;
                continue;
            }
            if (surface.Role == GlobalRaidSurfaceRoleV1170.Carrier)
            {
                switch (surface.CarrierKind!.Value)
                {
                    case FarmingGuideStorageKind.Rig:
                        rig = root.State;
                        break;
                    case FarmingGuideStorageKind.Backpack:
                        backpack = root.State;
                        break;
                    case FarmingGuideStorageKind.SecureContainer:
                        secure = root.State;
                        break;
                }
                continue;
            }

            int x;
            int y;
            bool rotated;
            if (root.Fixed && root.StoredSource is { } fixedStored)
            {
                x = fixedStored.X;
                y = fixedStored.Y;
                rotated = fixedStored.Rotated;
            }
            else
            {
                var placement = placementById[root.InstanceId];
                x = placement.X;
                y = placement.Y;
                rotated = placement.Rotated;
            }

            var storage = surface.StorageKind;
            string? parentInstanceId = null;
            if (!string.IsNullOrWhiteSpace(surface.OwnerInstanceId))
            {
                var ownerId = surface.OwnerInstanceId!;
                if (!finalSurfaceByRoot.TryGetValue(ownerId, out var ownerSurface))
                {
                    snapshot = current;
                    return false;
                }

                if (ownerSurface.Role == GlobalRaidSurfaceRoleV1170.Carrier)
                {
                    storage = ownerSurface.CarrierKind!.Value;
                }
                else
                {
                    parentInstanceId = ownerId;
                    if (!TryResolveUnifiedRootStorageKindV1170(ownerId, finalSurfaceByRoot, out storage))
                    {
                        snapshot = current;
                        return false;
                    }
                }
            }

            storedResult.Add(new FarmingGuideStoredItemState(
                root.InstanceId,
                root.State,
                storage,
                surface.GridIndex,
                x,
                y,
                rotated,
                parentInstanceId,
                root.Quantity));
        }

        if (!TryNormalizeRootStorageKinds(storedResult, out var normalized))
        {
            snapshot = current;
            return false;
        }

        snapshot = new FarmingGuideLoadoutSnapshot(
            equipment,
            rig,
            backpack,
            secure,
            normalized);
        return true;
    }

    private bool IsUnifiedTopLevelLegalV1170(FarmingGuideLoadoutSnapshot snapshot)
    {
        foreach (var pair in snapshot.Equipment.Where(pair => V1170GlobalEquipmentSlots.Contains(pair.Key)))
        {
            var item = ResolveItem(pair.Value);
            if (item is null || !FarmingGuideCompatibility.IsEquipmentSlotCompatible(pair.Key, item))
                return false;
        }
        foreach (var kind in V1170GlobalCarrierSlots)
        {
            var state = CarrierStateV1155(snapshot, kind);
            if (state is null)
                continue;
            var item = ResolveItem(state);
            if (item is null || !FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item))
                return false;
        }

        var rigItem = ResolveItem(snapshot.Rig);
        if (rigItem?.FarmingGuideData?.IsArmoredRig == true &&
            snapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor))
        {
            return false;
        }

        if (snapshot.Equipment.TryGetValue(FarmingGuideEquipmentSlot.Helmet, out var helmetState) &&
            ResolveItem(helmetState)?.FarmingGuideData?.BlocksHeadphones == true &&
            snapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.Headset))
        {
            return false;
        }

        var topLevel = snapshot.Equipment.Values
            .Select(ResolveItem)
            .Concat(new[] { ResolveItem(snapshot.Rig), ResolveItem(snapshot.Backpack), ResolveItem(snapshot.SecureContainer) })
            .Where(static item => item is not null)
            .Cast<GameItem>()
            .ToArray();
        for (var left = 0; left < topLevel.Length; left++)
        {
            for (var right = left + 1; right < topLevel.Length; right++)
            {
                if (FarmingGuideCompatibility.ItemsConflict(topLevel[left], topLevel[right]))
                    return false;
            }
        }
        return true;
    }

    private RaidRecommendation BuildUnifiedRecommendationV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        GlobalOwnedRootV1170 incoming,
        int removedCount)
    {
        var action = FarmingGuideInstructionAction.Store;
        if (TryFindNewEquipmentPlacementV1170(current, proposed, incoming.Item.Id, out var equipmentHadItem) ||
            TryFindNewCarrierPlacementV1170(current, proposed, incoming.Item.Id, out equipmentHadItem))
        {
            action = equipmentHadItem || removedCount > 0
                ? FarmingGuideInstructionAction.ReplaceEquip
                : FarmingGuideInstructionAction.Equip;
        }
        else if (removedCount > 0)
        {
            action = FarmingGuideInstructionAction.Replace;
        }

        return new RaidRecommendation(
            action switch
            {
                FarmingGuideInstructionAction.Equip => "장착",
                FarmingGuideInstructionAction.ReplaceEquip => "교체 장착",
                FarmingGuideInstructionAction.Replace => "교체",
                _ => "보관",
            },
            action,
            proposed);
    }

    private static bool TryFindNewEquipmentPlacementV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        string incomingItemId,
        out bool hadItem)
    {
        foreach (var slot in V1170GlobalEquipmentSlots)
        {
            current.Equipment.TryGetValue(slot, out var before);
            proposed.Equipment.TryGetValue(slot, out var after);
            if (after is null || !after.RaidAcquired ||
                !string.Equals(after.ItemId, incomingItemId, StringComparison.Ordinal) ||
                ReferenceEquals(before, after))
            {
                continue;
            }
            hadItem = before is not null;
            return true;
        }
        hadItem = false;
        return false;
    }

    private static bool TryFindNewCarrierPlacementV1170(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed,
        string incomingItemId,
        out bool hadItem)
    {
        foreach (var kind in V1170GlobalCarrierSlots)
        {
            var before = kind switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                FarmingGuideStorageKind.SecureContainer => current.SecureContainer,
                _ => null,
            };
            var after = kind switch
            {
                FarmingGuideStorageKind.Rig => proposed.Rig,
                FarmingGuideStorageKind.Backpack => proposed.Backpack,
                FarmingGuideStorageKind.SecureContainer => proposed.SecureContainer,
                _ => null,
            };
            if (after is null || !after.RaidAcquired ||
                !string.Equals(after.ItemId, incomingItemId, StringComparison.Ordinal) ||
                ReferenceEquals(before, after))
            {
                continue;
            }
            hadItem = before is not null;
            return true;
        }
        hadItem = false;
        return false;
    }

    private void EnqueueUnifiedSubsetV1170(
        PriorityQueue<UnifiedSubsetV1170, UnifiedSubsetPriorityV1170> queue,
        IReadOnlyList<GlobalOwnedRootV1170> currentRoots,
        GlobalOwnedRootV1170 incoming,
        ScannerItemSnapshot scanned,
        IReadOnlyList<GlobalOwnedRootV1170> victims,
        int[] indices)
    {
        var remove = indices
            .Select(index => victims[index].InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var selected = currentRoots
            .Where(root => !remove.Contains(root.InstanceId))
            .Append(incoming)
            .ToArray();
        var objective = ScoreOwnedSelectionV1170(selected, scanned);
        var key = string.Join("|", indices.Select(index => victims[index].InstanceId));
        queue.Enqueue(
            new UnifiedSubsetV1170(indices, objective),
            new UnifiedSubsetPriorityV1170(
                -objective.SatisfiedFirUnits,
                -objective.RetainedFleaValue,
                indices.Length,
                key));
    }

    private FarmingGuideOptimizationScore ScoreOwnedSelectionV1170(
        IReadOnlyList<GlobalOwnedRootV1170> selected,
        ScannerItemSnapshot currentScan)
    {
        var stored = selected.Select((root, index) => new FarmingGuideStoredItemState(
            $"__score_root_{index}_{root.InstanceId}",
            root.State,
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false,
            Quantity: root.Quantity)).ToArray();
        return ScoreRaidStateV1170(
            FarmingGuideLoadoutSnapshot.Empty with { StoredItems = stored },
            currentScan);
    }

    private bool HasProvableOwnedRootFactsV1170(GlobalOwnedRootV1170 root)
    {
        var snapshot = _raidBridge?.ResolveSnapshot(root.Item.Id);
        if (snapshot is null)
            return false;
        return _raidFleaAveragePrices.ContainsKey(root.Item.Id) || snapshot.FleaAveragePrice is not null;
    }

    private static bool HasUnmodeledAssemblyChoicesV1170(IReadOnlyList<GlobalOwnedRootV1170> roots) =>
        roots.Any(root => HasAssemblyChildrenV1170(root.State));

    private static bool HasAssemblyChildrenV1170(FarmingGuideItemState state) =>
        state.Attachments.Values.Any(static child => child is not null) ||
        state.ArmorPlates.Values.Any(static child => child is not null) ||
        state.Attachments.Values.Where(static child => child is not null).Cast<FarmingGuideItemState>().Any(HasAssemblyChildrenV1170);

    private static bool HasUnmodeledStackSplitChoicesV1170(
        IReadOnlyList<GlobalOwnedRootV1170> roots,
        GlobalOwnedRootV1170 incoming) =>
        roots.Append(incoming).Any(root => root.Quantity > 1);

    private bool RootCarrierHasReservedCellV1170(FarmingGuideStorageKind kind) =>
        _reservedCells.Any(cell => cell.Storage == kind && string.IsNullOrWhiteSpace(cell.ParentInstanceId));

    private string? FixedSurfaceIdV1170(
        GlobalOwnedRootV1170 root,
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalOwnedRootV1170> selected)
    {
        return root.Origin switch
        {
            GlobalRootOriginV1170.Equipment when root.EquipmentSlot is { } slot => EquipmentSurfaceIdV1170(slot),
            GlobalRootOriginV1170.Carrier when root.CarrierKind is { } kind => CarrierSurfaceIdV1170(kind),
            GlobalRootOriginV1170.Stored when root.StoredSource is { } stored => CurrentStoredSurfaceIdV1170(stored, selected),
            _ => null,
        };
    }

    private string? CurrentSurfaceIdV1170(
        GlobalOwnedRootV1170 root,
        IReadOnlyList<GlobalOwnedRootV1170>? selected)
    {
        return root.Origin switch
        {
            GlobalRootOriginV1170.Equipment when root.EquipmentSlot is { } slot => EquipmentSurfaceIdV1170(slot),
            GlobalRootOriginV1170.Carrier when root.CarrierKind is { } kind => CarrierSurfaceIdV1170(kind),
            GlobalRootOriginV1170.Stored when root.StoredSource is { } stored && selected is not null => CurrentStoredSurfaceIdV1170(stored, selected),
            _ => null,
        };
    }

    private string? CurrentStoredSurfaceIdV1170(
        FarmingGuideStoredItemState stored,
        IReadOnlyList<GlobalOwnedRootV1170> selected)
    {
        if (!string.IsNullOrWhiteSpace(stored.ParentInstanceId))
            return ItemGridSurfaceIdV1170(stored.ParentInstanceId!, stored.GridIndex);
        if (stored.Storage is FarmingGuideStorageKind.Pockets or FarmingGuideStorageKind.SpecialSlots)
            return RootStorageSurfaceIdV1170(stored.Storage, stored.GridIndex);

        var carrier = selected.FirstOrDefault(root =>
            root.Origin == GlobalRootOriginV1170.Carrier && root.CarrierKind == stored.Storage);
        return carrier is null ? null : ItemGridSurfaceIdV1170(carrier.InstanceId, stored.GridIndex);
    }

    private string? ReservedSurfaceIdV1170(
        FarmingGuideLockedCell cell,
        IReadOnlyList<GlobalOwnedRootV1170> selected)
    {
        if (!string.IsNullOrWhiteSpace(cell.ParentInstanceId))
            return ItemGridSurfaceIdV1170(cell.ParentInstanceId!, cell.GridIndex);
        if (cell.Storage is FarmingGuideStorageKind.Pockets or FarmingGuideStorageKind.SpecialSlots)
            return RootStorageSurfaceIdV1170(cell.Storage, cell.GridIndex);
        var carrier = selected.FirstOrDefault(root =>
            root.Origin == GlobalRootOriginV1170.Carrier && root.CarrierKind == cell.Storage);
        return carrier is null ? null : ItemGridSurfaceIdV1170(carrier.InstanceId, cell.GridIndex);
    }

    private static bool TryResolveUnifiedRootStorageKindV1170(
        string ownerId,
        IReadOnlyDictionary<string, GlobalRaidSurfaceV1170> finalSurfaceByRoot,
        out FarmingGuideStorageKind storage)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = ownerId;
        while (visited.Add(current) && finalSurfaceByRoot.TryGetValue(current, out var surface))
        {
            if (surface.Role == GlobalRaidSurfaceRoleV1170.Carrier)
            {
                storage = surface.CarrierKind!.Value;
                return true;
            }
            if (surface.Role == GlobalRaidSurfaceRoleV1170.Storage)
            {
                if (string.IsNullOrWhiteSpace(surface.OwnerInstanceId))
                {
                    storage = surface.StorageKind;
                    return true;
                }
                current = surface.OwnerInstanceId!;
                continue;
            }
            storage = FarmingGuideStorageKind.Pockets;
            return false;
        }
        storage = FarmingGuideStorageKind.Pockets;
        return false;
    }

    private static int UnifiedSurfacePriorityV1170(GlobalRaidSurfaceV1170 surface) =>
        surface.Role switch
        {
            GlobalRaidSurfaceRoleV1170.Equipment => 0,
            GlobalRaidSurfaceRoleV1170.Carrier => 1,
            GlobalRaidSurfaceRoleV1170.Storage when surface.StorageKind == FarmingGuideStorageKind.SecureContainer => 2,
            GlobalRaidSurfaceRoleV1170.Storage when surface.StorageKind == FarmingGuideStorageKind.Pockets => 3,
            GlobalRaidSurfaceRoleV1170.Storage => 4,
            _ => 10,
        };

    private static Dictionary<string, GlobalOwnedRootV1170> BuildCurrentRootLookupV1170(
        IReadOnlyList<GlobalOwnedRootV1170> selected) =>
        selected.ToDictionary(root => root.InstanceId, StringComparer.Ordinal);

    private static string EquipmentInstanceIdV1170(FarmingGuideEquipmentSlot slot) =>
        $"{V1170EquipmentInstancePrefix}{(int)slot}";

    private static string CarrierInstanceIdV1170(FarmingGuideStorageKind kind) =>
        $"{V1170CarrierInstancePrefix}{(int)kind}";

    private static string EquipmentSurfaceIdV1170(FarmingGuideEquipmentSlot slot) =>
        $"equip:{(int)slot}";

    private static string CarrierSurfaceIdV1170(FarmingGuideStorageKind kind) =>
        $"carrier:{(int)kind}";

    private static string RootStorageSurfaceIdV1170(FarmingGuideStorageKind kind, int gridIndex) =>
        $"root:{(int)kind}:{gridIndex}";

    private static string ItemGridSurfaceIdV1170(string ownerInstanceId, int gridIndex) =>
        $"item:{ownerInstanceId}:{gridIndex}";
}
