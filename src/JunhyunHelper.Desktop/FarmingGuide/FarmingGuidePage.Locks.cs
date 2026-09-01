using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    private readonly HashSet<FarmingGuideEquipmentSlot> _lockedEquipmentSlots = [];
    private readonly HashSet<FarmingGuideStorageKind> _lockedCarriers = [];
    private readonly HashSet<string> _lockedItemInstanceIds = new(StringComparer.Ordinal);
    private readonly HashSet<FarmingGuideLockedCell> _reservedCells = [];

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

    private async void Root_ProductPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ActiveDrag is { Started: true })
        {
            Root_PreviewKeyDown(sender, e);
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.T)
        {
            // Search normally leaves keyboard focus in the TextBox. Hovering a result is
            // the explicit gesture that turns T into the Farming Guide simulated-scan
            // command; without a hovered result T remains ordinary search text input.
            var row = FindHoveredSearchRow();
            if (row is not null)
            {
                e.Handled = true;
                if (_raidSession is not null && _raidBridge is not null)
                {
                    var published = await _raidBridge.PublishSimulatedScanAsync(row.Item.Id);
                    if (!published)
                    {
                        RaidStatusText.Text = "테스트 스캔 데이터를 준비하지 못했습니다.";
                        _raidBridge.ShowTransientStatus("테스트 스캔 데이터를 준비하지 못했습니다.");
                    }
                }
                return;
            }
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

        ApplyUnlockedBorder(border);
    }

    private void ApplyLockVisuals()
    {
        if (!IsLoaded)
            return;

        foreach (var element in EnumerateVisuals(RootGrid).OfType<FrameworkElement>())
        {
            switch (element)
            {
                case Border border when border.Tag is PlacedItemSource placed:
                    if (_lockedItemInstanceIds.Contains(placed.Placement.InstanceId))
                        ApplyLockedBorder(border);
                    else
                        ApplyUnlockedBorder(border);
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

    private void ApplyUnlockedBorder(Border border)
    {
        border.BorderThickness = new Thickness(1);
        border.BorderBrush = (Brush)FindResource("BorderBrush");
        var tooltip = border.ToolTip?.ToString();
        if (!string.IsNullOrWhiteSpace(tooltip) && tooltip.EndsWith(" · 잠금", StringComparison.Ordinal))
            border.ToolTip = tooltip[..^5];
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

    private sealed class ReservedCellOverlayMarker
    {
    }
}
