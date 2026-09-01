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
    /// child and then clipping the outer Border. A vertical scrollbar is accounted for
    /// before final width is chosen, and horizontal scrolling is enabled only as a
    /// physical fallback for storage wider than the available center column. This keeps
    /// complete cells visible for Key tool, rigs, bags and any future source-backed case.
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
            // Auto means normal cases remain scrollbar-free, while a genuinely wider
            // Tarkov grid scrolls instead of cutting a partial cell off the right edge.
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        var maximumViewportWidth = Math.Max(CellSize, maxWidth - horizontalChrome);
        header?.Measure(new Size(maximumViewportWidth, double.PositiveInfinity));
        var headerHeight = header?.DesiredSize.Height ?? 0d;
        var maximumViewportHeight = Math.Max(
            CellSize,
            maxHeight - verticalChrome - headerHeight);

        // Pass 1 establishes whether the full content height requires a vertical bar.
        gridHost.Measure(new Size(maximumViewportWidth, double.PositiveInfinity));
        var desired = gridHost.DesiredSize;
        var verticalScrollNeeded = desired.Height > maximumViewportHeight + 0.5d;

        // A later vertical scrollbar must never steal the final few pixels from a grid
        // that originally measured as fitting. Reserve its actual system width first.
        var reservedVerticalBar = verticalScrollNeeded
            ? Math.Max(0d, SystemParameters.VerticalScrollBarWidth)
            : 0d;
        var measuredViewportWidth = Math.Max(
            CellSize,
            maximumViewportWidth - reservedVerticalBar);

        gridHost.Measure(new Size(measuredViewportWidth, double.PositiveInfinity));
        desired = gridHost.DesiredSize;
        if (!verticalScrollNeeded && desired.Height > maximumViewportHeight + 0.5d)
        {
            verticalScrollNeeded = true;
            reservedVerticalBar = Math.Max(0d, SystemParameters.VerticalScrollBarWidth);
            measuredViewportWidth = Math.Max(CellSize, maximumViewportWidth - reservedVerticalBar);
            gridHost.Measure(new Size(measuredViewportWidth, double.PositiveInfinity));
            desired = gridHost.DesiredSize;
        }

        var desiredWidth = desired.Width + horizontalChrome + reservedVerticalBar;
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
