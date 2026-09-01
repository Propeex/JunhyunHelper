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
        RaidStatusText.Text = active
            ? _raidSession!.State.PendingInstruction is { } pending
                ? pending.Instruction
                : "레이드 진행 중 · 아이템을 스캔하세요."
            : "레이드를 시작하면 현재 상태를 기준으로 파밍 지시를 계산합니다.";
    }

    private FarmingGuideLockState BuildLockState()
    {
        var existingIds = StoredItems
            .Select(item => item.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        _lockedItemInstanceIds.RemoveWhere(id => !existingIds.Contains(id));

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
    }

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
                if (_raidSession is null)
                {
                    RaidStatusText.Text = "테스트 스캔 전에 레이드를 시작하세요.";
                }
                else
                {
                    _raidBridge?.PublishSimulatedScan(row.Item.Id);
                }
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
                        return true;
                    case EquipmentDropTarget equipment:
                        Toggle(_lockedEquipmentSlots, equipment.Slot);
                        CommitLockChange();
                        return true;
                    case CarrierDropTarget carrier:
                        Toggle(_lockedCarriers, carrier.Kind);
                        CommitLockChange();
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
                        return true;
                    }
                }
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void CommitLockChange() => MarkChanged();

    private static void Toggle<T>(ISet<T> set, T value)
    {
        if (!set.Add(value))
            set.Remove(value);
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
            _raidBridge?.ShowMiniScannerStatus(
                $"{_raidSession.State.PendingInstruction.Instruction}\n먼저 수락 [{AcceptHotkeyText()}]");
            return;
        }

        var item = ResolveItem(scanned.ItemId);
        if (item is null || !FarmingGuideSearchPolicy.IsDraggableInventoryItem(item))
            return;

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

        var acceptedAddsItem = pending.Action is FarmingGuideInstructionAction.Store or FarmingGuideInstructionAction.Replace;
        if (!_raidSession.TryAccept(out var snapshot))
            return false;

        ApplySnapshot(snapshot);
        if (acceptedAddsItem)
            _acceptedRaidItemCounts[pending.ItemId] = _acceptedRaidItemCounts.GetValueOrDefault(pending.ItemId) + 1;
        RefreshAll();
        RefreshRaidUi();
        _raidBridge?.ShowMiniScannerStatus("수락 완료");
        return true;
    }

    private RaidRecommendation PlanScannedItem(ScannerItemSnapshot scanned, GameItem item)
    {
        var current = BuildSnapshot();
        var incomingMetrics = ToMetrics(scanned, adjustAcceptedCount: true);
        var surfaces = EnumerateRaidSurfaces().ToArray();

        foreach (var surface in surfaces)
        {
            if (!FarmingGuideCompatibility.FilterAllows(item, surface.Definition.Filters))
                continue;
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
                    $"{DisplayName(item)} → {surface.Label}",
                    FarmingGuideInstructionAction.Store,
                    proposed);
            }
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
                var metrics = existingItem is null ? null : MetricsForExisting(existingItem);
                return (candidate.Surface, candidate.Stored, ExistingItem: existingItem, Metrics: metrics);
            })
            .Where(candidate => candidate.ExistingItem is not null && candidate.Metrics is not null)
            .Where(candidate => FarmingGuideLootPriorityPolicy.ShouldReplace(incomingMetrics, candidate.Metrics!))
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
                $"{DisplayName(candidate.ExistingItem!)} 버리고 {DisplayName(item)} → {candidate.Surface.Label}",
                FarmingGuideInstructionAction.Replace,
                proposed);
        }

        return new RaidRecommendation(
            $"{DisplayName(item)} 버리기",
            FarmingGuideInstructionAction.Discard,
            current);
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
            if (_lockedCarriers.Contains(kind) || !root.TryGetValue(kind, out var storage))
                continue;
            for (var index = 0; index < storage.Grids.Count; index++)
                yield return new RaidSurface(kind, null, index, storage.Grids[index], storage.Label);
        }

        foreach (var stored in StoredItems)
        {
            if (_lockedCarriers.Contains(stored.Storage) || IsInsideLockedItem(stored.InstanceId))
                continue;

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
                var footprint = FarmingGuidePlacementEngine.Footprint(
                    existingItem?.Width ?? 1,
                    existingItem?.Height ?? 1,
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

        foreach (var rotated in item.Width != item.Height ? new[] { false, true } : new[] { false })
        {
            var found = FarmingGuidePlacementEngine.FindFirstFit(
                surface.Definition.Width,
                surface.Definition.Height,
                item.Width ?? 1,
                item.Height ?? 1,
                rotated,
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
