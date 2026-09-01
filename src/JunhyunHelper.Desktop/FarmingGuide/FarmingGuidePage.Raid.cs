using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly HashSet<FarmingGuideEquipmentSlot> _lockedEquipmentSlots = [];
    private readonly HashSet<FarmingGuideStorageKind> _lockedCarriers = [];
    private readonly HashSet<string> _lockedItemInstanceIds = new(StringComparer.Ordinal);
    private readonly HashSet<FarmingGuideLockedCell> _reservedCells = [];
    private readonly Dictionary<string, int> _acceptedRaidItemCounts = new(StringComparer.Ordinal);

    private FarmingGuideRaidBridge? _raidBridge;
    private FarmingGuideRaidSession? _raidSession;
    private Func<string>? _acceptHotkeyTextProvider;
    private string? _raidStartSelectedPresetName;

    public bool IsRaidActive => _raidSession is not null;

    public void ConfigureRaid(
        FarmingGuideRaidBridge bridge,
        Func<string>? acceptHotkeyTextProvider = null)
    {
        _raidBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _acceptHotkeyTextProvider = acceptHotkeyTextProvider;
        bridge.Bind(HandleScannedItem, TryAcceptPendingInstruction, bridge.ShowMiniScannerStatus);
        RefreshRaidUi();
    }

    private void RaidToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_raidSession is null)
            StartRaid();
        else
            EndRaid();
    }

    private void StartRaid()
    {
        if (_content is null || string.IsNullOrWhiteSpace(_profileId))
            return;

        var snapshot = BuildSnapshot();
        var locks = BuildLockState();
        _raidSession = new FarmingGuideRaidSession(snapshot, locks);
        _raidStartSelectedPresetName = _selectedPresetName;
        _acceptedRaidItemCounts.Clear();
        _raidBridge?.ResetScannerIdentity();
        CloseWorkbench();
        RefreshPresetChoices();
        RefreshRaidUi();
        RefreshAll();
    }

    private void EndRaid()
    {
        if (_raidSession is null)
            return;

        var baselineSnapshot = _raidSession.BaselineSnapshot;
        var baselineLocks = _raidSession.BaselineLocks;
        _raidSession = null;
        ApplySnapshot(baselineSnapshot);
        ApplyLockState(baselineLocks);
        _selectedPresetName = _raidStartSelectedPresetName;
        _raidStartSelectedPresetName = null;
        _acceptedRaidItemCounts.Clear();
        _raidBridge?.ResetScannerIdentity();
        CloseWorkbench();
        RefreshPresetChoices();
        RefreshRaidUi();
        RefreshAll();
    }

    private void ResetRaidForDataChange()
    {
        _raidSession = null;
        _raidStartSelectedPresetName = null;
        _acceptedRaidItemCounts.Clear();
        _raidBridge?.ResetScannerIdentity();
    }

    private void RefreshRaidUi()
    {
        if (RaidToggleButton is null || RaidStatusText is null)
            return;

        var active = _raidSession is not null;
        RaidToggleButton.Content = active ? "레이드 종료" : "레이드 시작";
        RaidStatusText.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        RaidStatusText.Text = active
            ? _raidSession!.State.PendingInstruction is { } pending
                ? pending.Instruction
                : "레이드 진행 중 · 아이템을 스캔하세요."
            : string.Empty;
    }

    private FarmingGuideLockState BuildLockState()
    {
        var existingIds = StoredItems
            .Select(item => item.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        _lockedItemInstanceIds.RemoveWhere(id => !existingIds.Contains(id));
        _lockedEquipmentSlots.RemoveWhere(slot => GetEquipmentState(slot) is null);
        _lockedCarriers.RemoveWhere(kind => GetCarrier(kind) is null);

        return new FarmingGuideLockState(
            _lockedEquipmentSlots.OrderBy(value => value).ToArray(),
            _lockedCarriers.OrderBy(value => value).ToArray(),
            _lockedItemInstanceIds.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            _reservedCells
                .OrderBy(value => value.Storage)
                .ThenBy(value => value.ParentInstanceId, StringComparer.Ordinal)
                .ThenBy(value => value.GridIndex)
                .ThenBy(value => value.Y)
                .ThenBy(value => value.X)
                .ToArray());
    }

    private FarmingGuideItemState? GetEquipmentState(FarmingGuideEquipmentSlot slot) =>
        slot is FarmingGuideEquipmentSlot.Melee or FarmingGuideEquipmentSlot.Dogtag
            ? GetFixed(slot)
            : Equipment.GetValueOrDefault(slot);

    private void ApplyLockState(FarmingGuideLockState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _lockedEquipmentSlots.Clear();
        _lockedEquipmentSlots.UnionWith(state.EquipmentSlots);
        _lockedCarriers.Clear();
        _lockedCarriers.UnionWith(state.Carriers);
        _lockedItemInstanceIds.Clear();
        _lockedItemInstanceIds.UnionWith(state.ItemInstanceIds.Where(static value => !string.IsNullOrWhiteSpace(value)));
        _reservedCells.Clear();
        _reservedCells.UnionWith(state.ReservedCells);
        _ = BuildLockState();
    }

    internal void ClearEquipmentLock(FarmingGuideEquipmentSlot slot) => _lockedEquipmentSlots.Remove(slot);

    internal void ClearCarrierLock(FarmingGuideStorageKind kind) => _lockedCarriers.Remove(kind);

    private void Root_ProductPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ActiveDrag is { Started: true })
        {
            Root_PreviewKeyDown(sender, e);
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.T && !SearchTextBox.IsKeyboardFocusWithin)
        {
            var row = FindHoveredSearchRow();
            if (row is not null)
            {
                if (_raidSession is not null)
                    _raidBridge?.PublishSimulatedScan(row.Item.Id);
                e.Handled = true;
            }
            return;
        }

        if (key == Key.F && !SearchTextBox.IsKeyboardFocusWithin && TryToggleHoveredLock())
        {
            e.Handled = true;
            return;
        }

        Root_PreviewKeyDown(sender, e);
    }

    private SearchItemViewModel? FindHoveredSearchRow()
    {
        DependencyObject? current = Mouse.DirectlyOver as DependencyObject;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is FrameworkElement { DataContext: SearchItemViewModel row })
                return row;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private bool TryToggleHoveredLock()
    {
        DependencyObject? current = Mouse.DirectlyOver as DependencyObject;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is FrameworkElement element)
            {
                switch (element.Tag)
                {
                    case PlacedItemSource placed:
                        Toggle(_lockedItemInstanceIds, placed.Placement.InstanceId);
                        CommitLockChange();
                        RefreshLockVisual(element);
                        return true;
                    case EquipmentDropTarget equipment when GetEquipmentState(equipment.Slot) is not null:
                        Toggle(_lockedEquipmentSlots, equipment.Slot);
                        CommitLockChange();
                        RefreshLockVisual(element);
                        return true;
                    case CarrierDropTarget carrier when GetCarrier(carrier.Kind) is not null:
                        Toggle(_lockedCarriers, carrier.Kind);
                        CommitLockChange();
                        RefreshLockVisual(element);
                        return true;
                    case GridDropTarget grid:
                    {
                        var point = Mouse.GetPosition(grid.Canvas);
                        var x = (int)Math.Floor(point.X / CellSize);
                        var y = (int)Math.Floor(point.Y / CellSize);
                        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height)
                            return false;
                        var cell = new FarmingGuideLockedCell(
                            grid.Kind,
                            grid.GridIndex,
                            x,
                            y,
                            grid.ParentInstanceId);
                        Toggle(_reservedCells, cell);
                        CommitLockChange();
                        RefreshReservedCellVisuals(grid.Canvas, grid);
                        return true;
                    }
                }
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void CommitLockChange()
    {
        var locks = BuildLockState();
        if (_raidSession is not null)
        {
            _raidSession.ReplaceLocks(locks);
            _raidBridge?.SetMiniScannerInstruction(null);
            RefreshRaidUi();
            return;
        }

        _selectedPresetName = null;
        RefreshPresetChoices();
        PersistWorking();
    }

    private static void Toggle<T>(ISet<T> set, T value)
    {
        if (!set.Add(value))
            set.Remove(value);
    }

    private void RefreshLockVisual(FrameworkElement element)
    {
        if (element is not Border border)
            return;

        var locked = element.Tag switch
        {
            PlacedItemSource placed => _lockedItemInstanceIds.Contains(placed.Placement.InstanceId),
            EquipmentDropTarget equipment => _lockedEquipmentSlots.Contains(equipment.Slot),
            CarrierDropTarget carrier => _lockedCarriers.Contains(carrier.Kind),
            _ => false,
        };
        if (locked)
        {
            ApplyLockedBorder(border);
            return;
        }

        border.BorderThickness = new Thickness(1);
        border.BorderBrush = element.Tag is PlacedItemSource
            ? (Brush)FindResource("AccentBrush")
            : (Brush)FindResource("BorderBrush");
        var tooltip = border.ToolTip?.ToString();
        if (!string.IsNullOrWhiteSpace(tooltip) && tooltip.EndsWith(" · 잠금", StringComparison.Ordinal))
            border.ToolTip = tooltip[..^5];
    }

    private void ApplyLockVisuals()
    {
        if (!IsLoaded)
            return;

        foreach (var element in EnumerateVisuals(RootGrid).OfType<FrameworkElement>())
        {
            switch (element)
            {
                case Border border when border.Tag is PlacedItemSource placed &&
                                        _lockedItemInstanceIds.Contains(placed.Placement.InstanceId):
                    ApplyLockedBorder(border);
                    break;
                case Border border when border.Tag is EquipmentDropTarget equipment &&
                                        _lockedEquipmentSlots.Contains(equipment.Slot):
                    ApplyLockedBorder(border);
                    break;
                case Border border when border.Tag is CarrierDropTarget carrier &&
                                        _lockedCarriers.Contains(carrier.Kind):
                    ApplyLockedBorder(border);
                    break;
                case Canvas canvas when canvas.Tag is GridDropTarget grid:
                    AddReservedCellVisuals(canvas, grid);
                    break;
            }
        }
    }

    private void ApplyLockedBorder(Border border)
    {
        border.BorderBrush = (Brush)FindResource("AccentBrush");
        border.BorderThickness = new Thickness(2);
        var original = border.ToolTip?.ToString();
        if (string.IsNullOrWhiteSpace(original) || !original.Contains("잠금", StringComparison.Ordinal))
            border.ToolTip = string.IsNullOrWhiteSpace(original) ? "잠금" : $"{original} · 잠금";
    }

    private void RefreshReservedCellVisuals(Canvas canvas, GridDropTarget grid)
    {
        foreach (var overlay in canvas.Children
                     .OfType<FrameworkElement>()
                     .Where(static child => child.Tag is ReservedCellOverlayMarker)
                     .ToArray())
        {
            canvas.Children.Remove(overlay);
        }
        AddReservedCellVisuals(canvas, grid);
    }

    private void AddReservedCellVisuals(Canvas canvas, GridDropTarget grid)
    {
        foreach (var cell in _reservedCells.Where(cell =>
                     cell.Storage == grid.Kind &&
                     cell.GridIndex == grid.GridIndex &&
                     string.Equals(cell.ParentInstanceId, grid.ParentInstanceId, StringComparison.Ordinal)))
        {
            var overlay = new Border
            {
                Width = CellSize - 2,
                Height = CellSize - 2,
                Margin = new Thickness(0),
                BorderBrush = (Brush)FindResource("AccentBrush"),
                BorderThickness = new Thickness(2),
                Background = (Brush)FindResource("BackgroundMediumBrush"),
                Opacity = 0.72,
                IsHitTestVisible = false,
                ToolTip = "자동 배치 사용 금지",
                Tag = new ReservedCellOverlayMarker(),
            };
            Canvas.SetLeft(overlay, cell.X * CellSize + 1);
            Canvas.SetTop(overlay, cell.Y * CellSize + 1);
            Panel.SetZIndex(overlay, 50);
            canvas.Children.Add(overlay);
        }
    }

    private static IEnumerable<DependencyObject> EnumerateVisuals(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in EnumerateVisuals(child))
                yield return descendant;
        }
    }

    private void HandleScannedItem(ScannerItemSnapshot scanned)
    {
        if (_raidSession is null || _content is null)
            return;

        if (_raidSession.State.PendingInstruction is not null)
        {
            _raidSession.ClearPending();
            _raidBridge?.SetMiniScannerInstruction(null);
        }

        var item = ResolveItem(scanned.ItemId);
        if (item is null || !FarmingGuideSearchPolicy.IsDraggableInventoryItem(item))
        {
            RefreshRaidUi();
            return;
        }

        var recommendation = PlanScannedItem(scanned, item);
        _raidSession.SetPending(
            scanned.ItemId,
            recommendation.Instruction,
            recommendation.Action,
            recommendation.ProposedSnapshot);
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus(
            $"{recommendation.Instruction}\n수락 [{AcceptHotkeyText()}]");
    }

    private string AcceptHotkeyText()
    {
        var value = _acceptHotkeyTextProvider?.Invoke();
        return string.IsNullOrWhiteSpace(value) ? "사용 안 함" : value.Trim();
    }

    private bool TryAcceptPendingInstruction()
    {
        if (_raidSession?.State.PendingInstruction is not { } pending)
            return false;

        var acceptedAddsItem = pending.Action is
            FarmingGuideInstructionAction.Store or
            FarmingGuideInstructionAction.Replace or
            FarmingGuideInstructionAction.Equip or
            FarmingGuideInstructionAction.ReplaceEquip;
        if (!_raidSession.TryAccept(out var snapshot))
            return false;

        ApplySnapshot(snapshot);
        if (acceptedAddsItem)
            _acceptedRaidItemCounts[pending.ItemId] = _acceptedRaidItemCounts.GetValueOrDefault(pending.ItemId) + 1;
        RefreshAll();
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus("반영 완료");
        return true;
    }

    private RaidRecommendation PlanScannedItem(ScannerItemSnapshot scanned, GameItem item)
    {
        var current = BuildSnapshot();
        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        var equipTargets = EnumerateRaidEquipTargets(current, item).ToArray();

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
            if (!FarmingGuideStoragePlacementPolicy.CanStore(
                    surface.Kind,
                    surface.ParentInstanceId,
                    item,
                    surface.Definition.Filters))
            {
                continue;
            }
            if (TryFindFit(surface, item, current.StoredItems, ignoredInstanceId: null, out var fit))
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
        }

        var incomingEquipMetrics = AsSingleSlot(incomingMetrics);
        var equipReplacement = equipTargets
            .Where(static target => target.ExistingItem is not null)
            .Select(target => (Target: target, Metrics: AsSingleSlot(MetricsForExisting(target.ExistingItem!))))
            .Where(candidate => FarmingGuideLootPriorityPolicy.ShouldReplace(incomingEquipMetrics, candidate.Metrics))
            .OrderBy(candidate => candidate.Metrics, LootMetricsComparer.Instance)
            .FirstOrDefault();
        if (equipReplacement.Target is not null)
        {
            return new RaidRecommendation(
                $"{equipReplacement.Target.Label}의 {DisplayName(equipReplacement.Target.ExistingItem!)}과 교체",
                FarmingGuideInstructionAction.ReplaceEquip,
                equipReplacement.Target.ProposedSnapshot);
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
            foreach (var target in EnumerateAssemblyTargets(current, root, incoming))
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
        FarmingGuideLoadoutSnapshot current,
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

    private sealed record RaidAssemblyRoot(
        FarmingGuideItemState State,
        Func<FarmingGuideItemState, FarmingGuideLoadoutSnapshot> Apply);

    private sealed class ReservedCellOverlayMarker;

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
