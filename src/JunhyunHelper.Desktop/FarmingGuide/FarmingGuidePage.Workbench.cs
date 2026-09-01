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
    /// Sizes nested storage against the real viewport instead of measuring an unbounded
    /// child and then clipping the outer Border. Scroll-viewer chrome is reserved before
    /// final width is chosen, and horizontal scrolling remains only as a physical fallback
    /// for storage wider than the available center column.
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
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        // WPF ScrollViewer can reserve non-client scrollbar space only after arranging.
        // Always budget one system scrollbar width so the final arrange pass cannot steal
        // the last pixels of a cell from content that otherwise fits the workbench.
        var scrollbarAllowance = Math.Max(18d, SystemParameters.VerticalScrollBarWidth);
        var maximumViewportWidth = Math.Max(
            CellSize,
            maxWidth - horizontalChrome - scrollbarAllowance);

        header?.Measure(new Size(maximumViewportWidth, double.PositiveInfinity));
        var headerHeight = header?.DesiredSize.Height ?? 0d;
        var headerWidth = header?.DesiredSize.Width ?? 0d;
        var maximumViewportHeight = Math.Max(
            CellSize,
            maxHeight - verticalChrome - headerHeight);

        gridHost.Measure(new Size(maximumViewportWidth, double.PositiveInfinity));
        var desired = gridHost.DesiredSize;
        var verticalScrollNeeded = desired.Height > maximumViewportHeight + 0.5d;

        if (verticalScrollNeeded)
        {
            // maximumViewportWidth already reserves one bar. Re-measure so WrapPanel-based
            // multi-grid layouts use the same width they will receive at arrange time.
            gridHost.Measure(new Size(maximumViewportWidth, double.PositiveInfinity));
            desired = gridHost.DesiredSize;
        }

        var desiredContentWidth = Math.Max(desired.Width, headerWidth);
        var desiredWidth = desiredContentWidth + horizontalChrome + scrollbarAllowance;
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
