using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    // Retained only as a compatibility surface for drag-session code compiled against
    // the older assembly editor. No WorkbenchSlotDropTarget is rendered in v1.15.2.
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
        // Equipment is an opaque complete item in v1.15.2. There is intentionally no
        // attachment/armor workbench for top-level equipment.
    }

    internal void OpenCarrierWorkbench(CarrierDropTarget target)
    {
        // Root rig/backpack storage is already visible in the main storage column.
        // A separate carrier workbench would only duplicate that surface.
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
    }

    private void SizeWorkbenchToGrid(FrameworkElement gridHost)
    {
        gridHost.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var desired = gridHost.DesiredSize;

        var columnWidth = RootGrid.ColumnDefinitions.Count > 1
            ? RootGrid.ColumnDefinitions[1].ActualWidth
            : ActualWidth;
        if (columnWidth <= 0)
            columnWidth = Math.Max(240d, ActualWidth * 0.45d);

        var availableHeight = RootGrid.ActualHeight > 0
            ? RootGrid.ActualHeight
            : Math.Max(240d, ActualHeight);

        const double horizontalChrome = 30d;
        const double verticalChrome = 76d;
        var maxWidth = Math.Max(180d, columnWidth - 24d);
        var maxHeight = Math.Max(150d, availableHeight - 24d);

        WorkbenchHost.HorizontalAlignment = HorizontalAlignment.Left;
        WorkbenchHost.VerticalAlignment = VerticalAlignment.Top;
        WorkbenchHost.Width = Math.Min(maxWidth, Math.Max(180d, desired.Width + horizontalChrome));
        WorkbenchHost.Height = Math.Min(maxHeight, Math.Max(150d, desired.Height + verticalChrome));
    }

    // v1.15.2 intentionally exposes no equipment-internal drop targets. These stubs
    // make any stale drag state fail closed instead of mutating an assembly tree.
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
