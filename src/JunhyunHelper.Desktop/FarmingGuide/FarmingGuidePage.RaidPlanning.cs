using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private RaidRecommendation PlanScannedItem(ScannerItemSnapshot scanned, GameItem item)
    {
        var current = BuildSnapshot();
        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        var equipTargets = EnumerateRaidEquipTargets(current, item).ToArray();

        // Wearing an item in an empty legal target is always preferable to consuming
        // inventory space for the same item.
        var emptyEquip = equipTargets.FirstOrDefault(static target => target.ExistingItem is null);
        if (emptyEquip is not null)
        {
            return new RaidRecommendation(
                $"{emptyEquip.Label}에 장착",
                FarmingGuideInstructionAction.Equip,
                emptyEquip.ProposedSnapshot);
        }

        var surfaces = EnumerateRaidSurfaces().ToArray();
        foreach (var surface in surfaces)
        {
            if (!TryFindFit(surface, item, current.StoredItems, ignoredInstanceId: null, out var fit))
                continue;

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

        // Equipment and attachment targets are one logical slot, so compare replacement
        // value with a one-slot denominator rather than the ordinary stash footprint.
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

        var replacements = surfaces
            .SelectMany(surface => current.StoredItems
                .Where(stored => stored.GridIndex == surface.GridIndex &&
                                 IsOnStorageSurface(stored, surface.Kind, surface.ParentInstanceId))
                .Select(stored => (Surface: surface, Stored: stored)))
            .Where(candidate => !_lockedItemInstanceIds.Contains(candidate.Stored.InstanceId))
            .Where(candidate => !SubtreeContainsLockedItem(candidate.Stored.InstanceId))
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
            .ToArray();

        foreach (var candidate in replacements)
        {
            var remaining = RemoveStoredSubtree(current.StoredItems, candidate.Stored.InstanceId);
            if (!TryFindFit(candidate.Surface, item, remaining, ignoredInstanceId: null, out var fit))
                continue;

            var added = new FarmingGuideStoredItemState(
                Guid.NewGuid().ToString("N"),
                FarmingGuideItemState.Create(item.Id),
                candidate.Surface.Kind,
                candidate.Surface.GridIndex,
                fit.X,
                fit.Y,
                fit.Rotated,
                candidate.Surface.ParentInstanceId);
            var proposed = current with { StoredItems = remaining.Append(added).ToArray() };
            return new RaidRecommendation(
                $"{candidate.Surface.Label}의 {DisplayName(candidate.ExistingItem!)}과 교체",
                FarmingGuideInstructionAction.Replace,
                proposed);
        }

        return new RaidRecommendation(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
    }

    private IEnumerable<RaidEquipTarget> EnumerateRaidEquipTargets(
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

        foreach (var root in EnumerateRaidAssemblyRoots(current))
        {
            foreach (var target in EnumerateAssemblyTargets(root, incoming))
                yield return target;
        }
    }

    private IEnumerable<RaidAssemblyRoot> EnumerateRaidAssemblyRoots(FarmingGuideLoadoutSnapshot current)
    {
        foreach (var entry in current.Equipment)
        {
            if (_lockedEquipmentSlots.Contains(entry.Key))
                continue;
            var state = entry.Value;
            if (ResolveItem(state)?.FarmingGuideData is null)
                continue;
            var slot = entry.Key;
            yield return new RaidAssemblyRoot(
                state,
                updated =>
                {
                    var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(current.Equipment)
                    {
                        [slot] = updated,
                    };
                    return current with { Equipment = equipment };
                });
        }

        foreach (var kind in new[]
                 {
                     FarmingGuideStorageKind.Rig,
                     FarmingGuideStorageKind.Backpack,
                     FarmingGuideStorageKind.SecureContainer,
                 })
        {
            if (_lockedCarriers.Contains(kind))
                continue;
            var state = kind switch
            {
                FarmingGuideStorageKind.Rig => current.Rig,
                FarmingGuideStorageKind.Backpack => current.Backpack,
                FarmingGuideStorageKind.SecureContainer => current.SecureContainer,
                _ => null,
            };
            if (state is null || ResolveItem(state)?.FarmingGuideData is null)
                continue;
            yield return new RaidAssemblyRoot(
                state,
                updated => kind switch
                {
                    FarmingGuideStorageKind.Rig => current with { Rig = updated },
                    FarmingGuideStorageKind.Backpack => current with { Backpack = updated },
                    FarmingGuideStorageKind.SecureContainer => current with { SecureContainer = updated },
                    _ => current,
                });
        }

        foreach (var stored in current.StoredItems)
        {
            if (IsInsideLockedItem(stored.InstanceId) || ResolveItem(stored.Item)?.FarmingGuideData is null)
                continue;
            var instanceId = stored.InstanceId;
            yield return new RaidAssemblyRoot(
                stored.Item,
                updated => current with
                {
                    StoredItems = current.StoredItems
                        .Select(value => string.Equals(value.InstanceId, instanceId, StringComparison.Ordinal)
                            ? value with { Item = updated }
                            : value)
                        .ToArray(),
                });
        }
    }

    private IEnumerable<RaidEquipTarget> EnumerateAssemblyTargets(
        RaidAssemblyRoot root,
        GameItem incoming)
    {
        var pending = new Stack<string[]>();
        pending.Push([]);
        while (pending.Count > 0)
        {
            var ownerPath = pending.Pop();
            var ownerState = FarmingGuideAssemblyPolicy.GetNode(root.State, ownerPath);
            var ownerItem = ResolveItem(ownerState);
            var layout = ownerItem?.FarmingGuideData;
            if (ownerState is null || ownerItem is null || layout is null)
                continue;

            foreach (var slot in layout.AttachmentSlots)
            {
                var existingState = ownerState.Attachments.GetValueOrDefault(slot.Id);
                var compatibilityRoot = existingState is null
                    ? root.State
                    : FarmingGuideAssemblyPolicy.SetAttachment(root.State, ownerPath, slot.Id, null);
                if (!FarmingGuideAssemblyPolicy.CanAttach(
                        compatibilityRoot,
                        ownerPath,
                        slot,
                        incoming,
                        ItemCatalog))
                {
                    continue;
                }

                var updatedRoot = FarmingGuideAssemblyPolicy.SetAttachment(
                    compatibilityRoot,
                    ownerPath,
                    slot.Id,
                    FarmingGuideItemState.Create(incoming.Id));
                yield return new RaidEquipTarget(
                    $"{DisplayName(ownerItem)} · {FarmingGuideSlotLabelPolicy.Attachment(slot)}",
                    ResolveItem(existingState),
                    root.Apply(updatedRoot));
            }

            foreach (var slot in layout.ArmorSlots.Where(static value => !value.Locked))
            {
                var existingState = ownerState.ArmorPlates.GetValueOrDefault(slot.Id);
                var compatibilityRoot = existingState is null
                    ? root.State
                    : FarmingGuideAssemblyPolicy.SetArmorPlate(root.State, ownerPath, slot.Id, null);
                if (!FarmingGuideAssemblyPolicy.CanInstallArmorPlate(
                        compatibilityRoot,
                        ownerPath,
                        slot,
                        incoming,
                        ItemCatalog))
                {
                    continue;
                }

                var updatedRoot = FarmingGuideAssemblyPolicy.SetArmorPlate(
                    compatibilityRoot,
                    ownerPath,
                    slot.Id,
                    FarmingGuideItemState.Create(incoming.Id));
                yield return new RaidEquipTarget(
                    $"{DisplayName(ownerItem)} · {FarmingGuideSlotLabelPolicy.ArmorPlate(slot)}",
                    ResolveItem(existingState),
                    root.Apply(updatedRoot));
            }

            foreach (var slot in layout.AttachmentSlots.Reverse())
            {
                if (ownerState.Attachments.GetValueOrDefault(slot.Id) is not null)
                    pending.Push(ownerPath.Append(slot.Id).ToArray());
            }
        }
    }

    private bool CanEquipInSnapshot(
        FarmingGuideEquipmentSlot slot,
        GameItem item,
        FarmingGuideLoadoutSnapshot snapshot)
    {
        if (!FarmingGuideCompatibility.IsEquipmentSlotCompatible(slot, item))
            return false;
        if (slot == FarmingGuideEquipmentSlot.BodyArmor &&
            ResolveItem(snapshot.Rig)?.FarmingGuideData?.IsArmoredRig == true)
        {
            return false;
        }

        if (slot == FarmingGuideEquipmentSlot.Headset &&
            snapshot.Equipment.TryGetValue(FarmingGuideEquipmentSlot.Helmet, out var helmetState) &&
            ResolveItem(helmetState)?.FarmingGuideData?.BlocksHeadphones == true)
        {
            return false;
        }
        if (slot == FarmingGuideEquipmentSlot.Helmet &&
            item.FarmingGuideData?.BlocksHeadphones == true &&
            snapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.Headset))
        {
            return false;
        }

        return EnumerateSnapshotEquippedItems(snapshot, slot, replacingCarrier: null)
            .All(other => !FarmingGuideCompatibility.ItemsConflict(item, other));
    }

    private bool CanSetCarrierInSnapshot(
        FarmingGuideStorageKind kind,
        GameItem item,
        FarmingGuideLoadoutSnapshot snapshot)
    {
        if (!FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item))
            return false;

        var targetContainsItems = snapshot.StoredItems.Any(stored =>
            stored.ParentInstanceId is null && stored.Storage == kind);
        var currentCarrier = kind switch
        {
            FarmingGuideStorageKind.Rig => snapshot.Rig,
            FarmingGuideStorageKind.Backpack => snapshot.Backpack,
            FarmingGuideStorageKind.SecureContainer => snapshot.SecureContainer,
            _ => null,
        };
        if (currentCarrier is not null && targetContainsItems)
            return false;

        if (kind == FarmingGuideStorageKind.Rig &&
            item.FarmingGuideData?.IsArmoredRig == true &&
            snapshot.Equipment.ContainsKey(FarmingGuideEquipmentSlot.BodyArmor))
        {
            return false;
        }

        return EnumerateSnapshotEquippedItems(snapshot, replacingEquipment: null, replacingCarrier: kind)
            .All(other => !FarmingGuideCompatibility.ItemsConflict(item, other));
    }

    private IEnumerable<GameItem> EnumerateSnapshotEquippedItems(
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideEquipmentSlot? replacingEquipment,
        FarmingGuideStorageKind? replacingCarrier)
    {
        foreach (var entry in snapshot.Equipment)
        {
            if (replacingEquipment == entry.Key)
                continue;
            var item = ResolveItem(entry.Value);
            if (item is not null)
                yield return item;
        }

        foreach (var pair in new[]
                 {
                     (FarmingGuideStorageKind.Rig, snapshot.Rig),
                     (FarmingGuideStorageKind.Backpack, snapshot.Backpack),
                     (FarmingGuideStorageKind.SecureContainer, snapshot.SecureContainer),
                 })
        {
            if (replacingCarrier == pair.Item1)
                continue;
            var item = ResolveItem(pair.Item2);
            if (item is not null)
                yield return item;
        }
    }

    private FarmingGuideLootMetrics ToMetrics(ScannerItemSnapshot snapshot, bool adjustAcceptedCount)
    {
        var accepted = adjustAcceptedCount ? _acceptedRaidItemCounts.GetValueOrDefault(snapshot.ItemId) : 0;
        return new FarmingGuideLootMetrics(
            Math.Max(0, snapshot.CurrentNeeded - accepted),
            snapshot.TraderSellPrice,
            snapshot.FleaAveragePrice,
            Math.Max(1, snapshot.Slots));
    }

    private FarmingGuideLootMetrics MetricsForExisting(GameItem item)
    {
        var snapshot = _raidBridge?.ResolveSnapshot(item.Id);
        if (snapshot is not null)
            return ToMetrics(snapshot, adjustAcceptedCount: true);
        var slots = Math.Max(1, (item.Width ?? 1) * (item.Height ?? 1));
        return new FarmingGuideLootMetrics(0, item.BasePrice, null, slots);
    }

    private static FarmingGuideLootMetrics AsSingleSlot(FarmingGuideLootMetrics metrics) =>
        new(metrics.CurrentNeeded, metrics.TraderSellPrice, metrics.FleaAveragePrice, 1);

    private FarmingGuideLootMetrics MetricsForStorageSurface(GameItem item, RaidSurface surface)
    {
        var metrics = MetricsForExisting(item);
        return MetricsForStorageSurface(metrics, surface);
    }

    private static FarmingGuideLootMetrics MetricsForStorageSurface(
        FarmingGuideLootMetrics metrics,
        RaidSurface surface) =>
        FarmingGuideStoragePlacementPolicy.IsSpecialSlotSurface(surface.Kind, surface.ParentInstanceId)
            ? AsSingleSlot(metrics)
            : metrics;

    private IEnumerable<RaidSurface> EnumerateRaidSurfaces()
    {
        var root = StorageDefinitions().ToDictionary(value => value.Kind);
        var order = new[]
        {
            FarmingGuideStorageKind.SecureContainer,
            FarmingGuideStorageKind.Pockets,
            FarmingGuideStorageKind.Rig,
            FarmingGuideStorageKind.Backpack,
            FarmingGuideStorageKind.SpecialSlots,
        };
        foreach (var kind in order)
        {
            if (!root.TryGetValue(kind, out var storage))
                continue;
            for (var index = 0; index < storage.Grids.Count; index++)
                yield return new RaidSurface(kind, null, index, storage.Grids[index], storage.Label);
        }

        foreach (var stored in StoredItems)
        {
            var owner = ResolveItem(stored.Item);
            var grids = owner?.FarmingGuideData?.StorageGrids;
            if (grids is null || grids.Count == 0)
                continue;
            for (var index = 0; index < grids.Count; index++)
            {
                yield return new RaidSurface(
                    stored.Storage,
                    stored.InstanceId,
                    index,
                    grids[index],
                    $"{DisplayName(owner!)} 내부");
            }
        }
    }

    private bool TryFindFit(
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
                .Where(cell => cell.Storage == surface.Kind &&
                               cell.GridIndex == surface.GridIndex &&
                               string.Equals(cell.ParentInstanceId, surface.ParentInstanceId, StringComparison.Ordinal))
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

    private bool IsInsideLockedItem(string instanceId)
    {
        string? currentId = instanceId;
        while (!string.IsNullOrWhiteSpace(currentId))
        {
            if (_lockedItemInstanceIds.Contains(currentId))
                return true;
            currentId = StoredItems.FirstOrDefault(item =>
                string.Equals(item.InstanceId, currentId, StringComparison.Ordinal))?.ParentInstanceId;
        }
        return false;
    }

    private bool SubtreeContainsLockedItem(string instanceId)
    {
        var pending = new Stack<string>();
        pending.Push(instanceId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (_lockedItemInstanceIds.Contains(current))
                return true;
            foreach (var child in StoredItems.Where(item =>
                         string.Equals(item.ParentInstanceId, current, StringComparison.Ordinal)))
                pending.Push(child.InstanceId);
        }
        return false;
    }

    private static IReadOnlyList<FarmingGuideStoredItemState> RemoveStoredSubtree(
        IReadOnlyList<FarmingGuideStoredItemState> source,
        string instanceId)
    {
        var remove = new HashSet<string>(StringComparer.Ordinal) { instanceId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var item in source)
            {
                if (item.ParentInstanceId is not null &&
                    remove.Contains(item.ParentInstanceId) &&
                    remove.Add(item.InstanceId))
                {
                    changed = true;
                }
            }
        }
        return source.Where(item => !remove.Contains(item.InstanceId)).ToArray();
    }

    private static string EquipmentLabel(FarmingGuideEquipmentSlot slot) => slot switch
    {
        FarmingGuideEquipmentSlot.Headset => "헤드셋",
        FarmingGuideEquipmentSlot.Helmet => "헬멧",
        FarmingGuideEquipmentSlot.FaceCover => "얼굴",
        FarmingGuideEquipmentSlot.Armband => "완장",
        FarmingGuideEquipmentSlot.BodyArmor => "방탄복",
        FarmingGuideEquipmentSlot.Eyewear => "안경",
        FarmingGuideEquipmentSlot.PrimaryWeapon1 => "무기 1",
        FarmingGuideEquipmentSlot.PrimaryWeapon2 => "무기 2",
        FarmingGuideEquipmentSlot.Holster => "권총",
        FarmingGuideEquipmentSlot.Melee => "칼",
        FarmingGuideEquipmentSlot.Dogtag => "인식표",
        _ => "장비",
    };

    private static string CarrierLabel(FarmingGuideStorageKind kind) => kind switch
    {
        FarmingGuideStorageKind.Rig => "리그",
        FarmingGuideStorageKind.Backpack => "가방",
        FarmingGuideStorageKind.SecureContainer => "보안 컨테이너",
        _ => "장비",
    };

    private sealed record RaidSurface(
        FarmingGuideStorageKind Kind,
        string? ParentInstanceId,
        int GridIndex,
        FarmingGuideStorageGridDefinition Definition,
        string Label);

    private readonly record struct RaidFit(int X, int Y, bool Rotated);

    private sealed record RaidRecommendation(
        string Instruction,
        FarmingGuideInstructionAction Action,
        FarmingGuideLoadoutSnapshot ProposedSnapshot);

    private sealed record RaidEquipTarget(
        string Label,
        GameItem? ExistingItem,
        FarmingGuideLoadoutSnapshot ProposedSnapshot);

    private sealed record RaidEquipCandidate(
        RaidEquipTarget Target,
        FarmingGuideLootMetrics Metrics);

    private sealed record RaidAssemblyRoot(
        FarmingGuideItemState State,
        Func<FarmingGuideItemState, FarmingGuideLoadoutSnapshot> Apply);

    private sealed class LootMetricsComparer : IComparer<FarmingGuideLootMetrics>
    {
        public static LootMetricsComparer Instance { get; } = new();

        public int Compare(FarmingGuideLootMetrics? x, FarmingGuideLootMetrics? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;
            return FarmingGuideLootPriorityPolicy.Compare(x, y);
        }
    }
}
