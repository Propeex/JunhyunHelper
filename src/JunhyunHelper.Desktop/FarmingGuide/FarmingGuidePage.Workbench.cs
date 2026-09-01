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
    /// the width decision; a long title must not inflate a tiny case. Scrollbars are a
    /// physical fallback only. When the complete grid fits, both axes are explicitly
    /// disabled so WPF ScrollViewer auto-scrollbar feedback cannot steal a few pixels and
    /// manufacture the clipping it is supposed to solve.
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
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        // ScrollViewer templates retain a few pixels of non-content chrome even when no
        // scrollbar is visible. v1.15.4 accounted for this on width only, leaving common
        // compact containers (for example a 4x4 key tool) a few pixels short vertically.
        // Keep the allowance symmetric so the host owns enough real arranged viewport.
        const double scrollViewerChromeAllowance = 4d;
        var noBarViewportWidth = Math.Max(
            CellSize,
            maxWidth - horizontalChrome - scrollViewerChromeAllowance);

        header?.Measure(new Size(noBarViewportWidth, double.PositiveInfinity));
        var headerHeight = header?.DesiredSize.Height ?? 0d;

        var verticalScrollNeeded = false;
        var horizontalScrollNeeded = false;
        var desired = default(Size);
        var effectiveViewportWidth = noBarViewportWidth;
        var effectiveViewportHeight = Math.Max(
            CellSize,
            maxHeight - verticalChrome - headerHeight - scrollViewerChromeAllowance);

        // Vertical and horizontal scrollbars affect the other axis. Resolve the pair to a
        // stable state instead of allowing ScrollViewer Auto to create a self-sustaining
        // scrollbar loop during arrange.
        for (var pass = 0; pass < 3; pass++)
        {
            effectiveViewportWidth = Math.Max(
                CellSize,
                noBarViewportWidth -
                (verticalScrollNeeded ? Math.Max(0d, SystemParameters.VerticalScrollBarWidth) : 0d));
            effectiveViewportHeight = Math.Max(
                CellSize,
                maxHeight -
                verticalChrome -
                headerHeight -
                scrollViewerChromeAllowance -
                (horizontalScrollNeeded ? Math.Max(0d, SystemParameters.HorizontalScrollBarHeight) : 0d));

            gridHost.Measure(new Size(effectiveViewportWidth, double.PositiveInfinity));
            desired = gridHost.DesiredSize;

            var nextHorizontal = desired.Width > effectiveViewportWidth + 0.5d;
            var nextVertical = desired.Height > effectiveViewportHeight + 0.5d;
            if (nextHorizontal == horizontalScrollNeeded && nextVertical == verticalScrollNeeded)
                break;

            horizontalScrollNeeded = nextHorizontal;
            verticalScrollNeeded = nextVertical;
        }

        if (scrollViewer is not null)
        {
            scrollViewer.HorizontalScrollBarVisibility = horizontalScrollNeeded
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
            scrollViewer.VerticalScrollBarVisibility = verticalScrollNeeded
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled;
        }

        var reservedVerticalBar = verticalScrollNeeded
            ? Math.Max(0d, SystemParameters.VerticalScrollBarWidth)
            : 0d;
        var reservedHorizontalBar = horizontalScrollNeeded
            ? Math.Max(0d, SystemParameters.HorizontalScrollBarHeight)
            : 0d;
        var desiredWidth = desired.Width +
                           horizontalChrome +
                           scrollViewerChromeAllowance +
                           reservedVerticalBar;
        var desiredHeight = desired.Height +
                            headerHeight +
                            verticalChrome +
                            scrollViewerChromeAllowance +
                            reservedHorizontalBar;

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
