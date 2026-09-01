using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    // Retained only as a compatibility surface for drag-session code compiled against
    // the older assembly editor. No WorkbenchSlotDropTarget is rendered in v1.15.2+.
    internal enum WorkbenchSlotKind
    {
        Attachment,
        ArmorPlate,
    }

    internal sealed record WorkbenchSlotDropTarget(
        WorkbenchSlotKind Kind,
        string SlotId,
        string[] OwnerPath,
        FarmingGuideItemFilter Filters,
        IReadOnlyList<string> AllowedItemIds,
        Border Border);

    private GameItem? _workbenchItem;
    private FarmingGuideStorageKind _workbenchStorageKind;
    private string? _workbenchParentInstanceId;

    internal bool IsWorkbenchOpen => WorkbenchHost.Visibility == Visibility.Visible;

    internal void OpenEquipmentWorkbench(EquipmentDropTarget target)
    {
        // Equipment is an opaque complete item. There is intentionally no
        // attachment/armor workbench for top-level equipment.
    }

    internal void OpenCarrierWorkbench(CarrierDropTarget target)
    {
        // Root rig/backpack/secure-container storage is already visible in the main
        // storage column. A separate carrier workbench would only duplicate that surface.
    }

    internal void OpenStoredWorkbench(PlacedItemSource source)
    {
        var item = ResolveItem(source.Placement.Item);
        if (item is null || !FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(item))
            return;

        _workbenchItem = item;
        _workbenchStorageKind = source.Placement.Storage;
        _workbenchParentInstanceId = source.Placement.InstanceId;
        WorkbenchHost.Visibility = Visibility.Visible;
        RenderWorkbench();
    }

    private void CloseWorkbenchButton_Click(object sender, RoutedEventArgs e) => CloseWorkbench();

    private void CloseWorkbench()
    {
        WorkbenchHost.Visibility = Visibility.Collapsed;
        WorkbenchPanel.Children.Clear();
        WorkbenchHost.ClearValue(WidthProperty);
        WorkbenchHost.ClearValue(HeightProperty);
        _workbenchItem = null;
        _workbenchParentInstanceId = null;
    }

    internal void RenderWorkbench()
    {
        if (!IsWorkbenchOpen || _workbenchItem is null)
            return;

        var grids = _workbenchItem.FarmingGuideData?.StorageGrids;
        if (grids is null || grids.Count == 0 ||
            !FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(_workbenchItem))
        {
            CloseWorkbench();
            return;
        }

        WorkbenchTitleText.Text = DisplayName(_workbenchItem);
        WorkbenchPanel.Children.Clear();

        var gridHost = CreateCompactGridHost(
            _workbenchStorageKind,
            grids,
            _workbenchParentInstanceId);
        WorkbenchPanel.Children.Add(gridHost);
        SizeWorkbenchToGrid(gridHost);
        ApplyLockVisuals();
    }

    /// <summary>
    /// Size nested storage against the real center-column viewport. The content grid owns
    /// the width decision; a long title must not inflate a tiny case. Horizontal scrolling
    /// stays disabled whenever the arranged grid can fit the effective viewport, avoiding
    /// the WPF Auto-scrollbar feedback where a horizontal bar can trigger a vertical bar
    /// and then steal enough width to make itself appear necessary.
    /// </summary>
    private void SizeWorkbenchToGrid(FrameworkElement gridHost)
    {
        var columnWidth = RootGrid.ColumnDefinitions.Count > 1
            ? RootGrid.ColumnDefinitions[1].ActualWidth
            : ActualWidth;
        if (columnWidth <= 0)
            columnWidth = Math.Max(240d, ActualWidth * 0.45d);

        var availableHeight = RootGrid.ActualHeight > 0
            ? RootGrid.ActualHeight
            : Math.Max(240d, ActualHeight);

        var maxWidth = Math.Max(180d, columnWidth - WorkbenchHost.Margin.Left - WorkbenchHost.Margin.Right);
        var maxHeight = Math.Max(150d, availableHeight - WorkbenchHost.Margin.Top - WorkbenchHost.Margin.Bottom);
        var horizontalChrome = WorkbenchHost.Padding.Left +
                               WorkbenchHost.Padding.Right +
                               WorkbenchHost.BorderThickness.Left +
                               WorkbenchHost.BorderThickness.Right;
        var verticalChrome = WorkbenchHost.Padding.Top +
                             WorkbenchHost.Padding.Bottom +
                             WorkbenchHost.BorderThickness.Top +
                             WorkbenchHost.BorderThickness.Bottom;

        var dock = WorkbenchHost.Child as DockPanel;
        var header = dock?.Children.OfType<Grid>().FirstOrDefault();
        var scrollViewer = dock?.Children.OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer is not null)
        {
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        // A small permanent allowance covers ScrollViewer template chrome even when no bar
        // is visible. The system vertical scrollbar width is added only after height proves
        // that a vertical bar is actually required.
        const double scrollViewerChromeAllowance = 3d;
        var noBarViewportWidth = Math.Max(
            CellSize,
            maxWidth - horizontalChrome - scrollViewerChromeAllowance);

        header?.Measure(new Size(noBarViewportWidth, double.PositiveInfinity));
        var headerHeight = header?.DesiredSize.Height ?? 0d;
        var maximumViewportHeight = Math.Max(
            CellSize,
            maxHeight - verticalChrome - headerHeight);

        gridHost.Measure(new Size(noBarViewportWidth, double.PositiveInfinity));
        var desired = gridHost.DesiredSize;
        var verticalScrollNeeded = desired.Height > maximumViewportHeight + 0.5d;
        var reservedVerticalBar = verticalScrollNeeded
            ? Math.Max(0d, SystemParameters.VerticalScrollBarWidth)
            : 0d;
        var effectiveViewportWidth = Math.Max(
            CellSize,
            noBarViewportWidth - reservedVerticalBar);

        if (verticalScrollNeeded)
        {
            gridHost.Measure(new Size(effectiveViewportWidth, double.PositiveInfinity));
            desired = gridHost.DesiredSize;
        }

        // WrapPanel can legally wrap multiple grids. A horizontal scrollbar is a fallback
        // only when the content itself still reports a width larger than the effective
        // viewport after constrained measurement (for example one physically wider grid).
        var horizontalScrollNeeded = desired.Width > effectiveViewportWidth + 0.5d;
        if (scrollViewer is not null)
        {
            scrollViewer.HorizontalScrollBarVisibility = horizontalScrollNeeded
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
        }

        var desiredWidth = desired.Width +
                           horizontalChrome +
                           scrollViewerChromeAllowance +
                           reservedVerticalBar;
        var desiredHeight = desired.Height + headerHeight + verticalChrome;

        WorkbenchHost.HorizontalAlignment = HorizontalAlignment.Left;
        WorkbenchHost.VerticalAlignment = VerticalAlignment.Top;
        WorkbenchHost.Width = Math.Min(maxWidth, Math.Max(180d, desiredWidth));
        WorkbenchHost.Height = Math.Min(maxHeight, Math.Max(150d, desiredHeight));
    }

    // Complete-equipment mode intentionally exposes no equipment-internal drop targets.
    // These stubs make any stale drag state fail closed instead of mutating an assembly tree.
    internal bool CanDropIntoWorkbenchSlot(WorkbenchSlotDropTarget target, GameItem item) => false;

    internal FarmingGuideItemState? GetWorkbenchSlotState(WorkbenchSlotDropTarget target) => null;

    internal void SetWorkbenchSlotState(
        WorkbenchSlotKind kind,
        string slotId,
        FarmingGuideItemState? value)
    {
    }

    internal void SetWorkbenchSlotState(
        WorkbenchSlotKind kind,
        IReadOnlyList<string> ownerPath,
        string slotId,
        FarmingGuideItemState? value)
    {
    }
}
