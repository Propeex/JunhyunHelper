using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    internal enum WorkbenchSlotKind
    {
        Attachment,
        ArmorPlate,
    }

    internal sealed record WorkbenchSlotDropTarget(
        WorkbenchSlotKind Kind,
        string SlotId,
        FarmingGuideItemFilter Filters,
        IReadOnlyList<string> AllowedItemIds,
        Border Border);

    private enum WorkbenchMode
    {
        Storage,
        Slots,
    }

    private FarmingGuideItemState? _workbenchState;
    private GameItem? _workbenchItem;
    private FarmingGuideStorageKind _workbenchStorageKind;
    private string? _workbenchParentInstanceId;
    private Action<FarmingGuideItemState>? _workbenchApply;
    private WorkbenchMode _workbenchMode;

    internal bool IsWorkbenchOpen => WorkbenchHost.Visibility == Visibility.Visible;

    internal void OpenEquipmentWorkbench(EquipmentDropTarget target)
    {
        var state = target.Fixed ? GetFixed(target.Slot) : Equipment.GetValueOrDefault(target.Slot);
        if (state is null)
            return;

        OpenWorkbench(
            state,
            WorkbenchMode.Slots,
            FarmingGuideStorageKind.Pockets,
            parentInstanceId: null,
            updated =>
            {
                if (target.Fixed)
                    SetFixed(target.Slot, updated);
                else
                    Equipment[target.Slot] = updated;
            });
    }

    internal void OpenCarrierWorkbench(CarrierDropTarget target)
    {
        var state = GetCarrier(target.Kind);
        if (state is null)
            return;

        // The top-level rig storage grids are already visible on the main inventory.
        // Double-clicking a worn rig therefore exposes only its armor/mod slots. A rig
        // stored inside another container is handled by OpenStoredWorkbench and exposes
        // its actual storage cells instead.
        var mode = target.Kind == FarmingGuideStorageKind.Rig
            ? WorkbenchMode.Slots
            : WorkbenchMode.Storage;
        OpenWorkbench(
            state,
            mode,
            target.Kind,
            parentInstanceId: null,
            updated => SetCarrier(target.Kind, updated));
    }

    internal void OpenStoredWorkbench(PlacedItemSource source)
    {
        var item = ResolveItem(source.Placement.Item);
        if (item is null)
            return;

        var mode = item.FarmingGuideData?.StorageGrids.Count > 0
            ? WorkbenchMode.Storage
            : WorkbenchMode.Slots;
        OpenWorkbench(
            source.Placement.Item,
            mode,
            source.Placement.Storage,
            source.Placement.InstanceId,
            updated =>
            {
                var index = StoredItems.FindIndex(item => item.InstanceId == source.Placement.InstanceId);
                if (index >= 0)
                    StoredItems[index] = StoredItems[index] with { Item = updated };
            });
    }

    private void OpenWorkbench(
        FarmingGuideItemState state,
        WorkbenchMode mode,
        FarmingGuideStorageKind storageKind,
        string? parentInstanceId,
        Action<FarmingGuideItemState> apply)
    {
        var item = ResolveItem(state);
        if (item?.FarmingGuideData is not { } layout)
            return;

        var hasStorage = layout.StorageGrids.Count > 0;
        var hasSlots = layout.AttachmentSlots.Count > 0 || layout.ArmorSlots.Any(slot => !slot.Locked);
        if ((mode == WorkbenchMode.Storage && !hasStorage) ||
            (mode == WorkbenchMode.Slots && !hasSlots))
        {
            return;
        }

        _workbenchState = state;
        _workbenchItem = item;
        _workbenchMode = mode;
        _workbenchStorageKind = storageKind;
        _workbenchParentInstanceId = parentInstanceId;
        _workbenchApply = apply;
        StoragePanel.Visibility = Visibility.Collapsed;
        WorkbenchHost.Visibility = Visibility.Visible;
        RenderWorkbench();
    }

    private void CloseWorkbenchButton_Click(object sender, RoutedEventArgs e) => CloseWorkbench();

    private void CloseWorkbench()
    {
        WorkbenchHost.Visibility = Visibility.Collapsed;
        StoragePanel.Visibility = Visibility.Visible;
        WorkbenchPanel.Children.Clear();
        _workbenchState = null;
        _workbenchItem = null;
        _workbenchParentInstanceId = null;
        _workbenchApply = null;
    }

    internal void RenderWorkbench()
    {
        if (!IsWorkbenchOpen || _workbenchItem is null || _workbenchState is null)
            return;

        WorkbenchTitleText.Text = DisplayName(_workbenchItem);
        WorkbenchPanel.Children.Clear();
        var layout = _workbenchItem.FarmingGuideData;
        if (layout is null)
        {
            CloseWorkbench();
            return;
        }

        if (_workbenchMode == WorkbenchMode.Storage)
        {
            if (layout.StorageGrids.Count == 0)
            {
                CloseWorkbench();
                return;
            }

            var grids = new WrapPanel { Orientation = Orientation.Horizontal };
            for (var index = 0; index < layout.StorageGrids.Count; index++)
            {
                grids.Children.Add(CreateGridCanvas(
                    _workbenchStorageKind,
                    index,
                    layout.StorageGrids[index],
                    _workbenchParentInstanceId));
            }
            WorkbenchPanel.Children.Add(grids);
            return;
        }

        var slots = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var slot in layout.AttachmentSlots)
        {
            slots.Children.Add(CreateWorkbenchSlot(
                WorkbenchSlotKind.Attachment,
                slot.Id,
                slot.Name ?? slot.NameId,
                slot.Filters,
                [],
                _workbenchState.Attachments.GetValueOrDefault(slot.Id)));
        }

        foreach (var slot in layout.ArmorSlots.Where(static slot => !slot.Locked))
        {
            slots.Children.Add(CreateWorkbenchSlot(
                WorkbenchSlotKind.ArmorPlate,
                slot.Id,
                slot.Name ?? slot.NameId,
                FarmingGuideItemFilter.Empty,
                slot.AllowedPlateIds,
                _workbenchState.ArmorPlates.GetValueOrDefault(slot.Id)));
        }

        if (slots.Children.Count == 0)
        {
            CloseWorkbench();
            return;
        }
        WorkbenchPanel.Children.Add(slots);
    }

    private Border CreateWorkbenchSlot(
        WorkbenchSlotKind kind,
        string slotId,
        string label,
        FarmingGuideItemFilter filters,
        IReadOnlyList<string> allowedItemIds,
        FarmingGuideItemState? current)
    {
        var border = new Border
        {
            Width = 104,
            Height = 104,
            Margin = new Thickness(0, 0, 10, 10),
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
        };
        var target = new WorkbenchSlotDropTarget(kind, slotId, filters, allowedItemIds, border);
        border.Tag = target;
        border.MouseLeftButtonDown += WorkbenchSlot_MouseLeftButtonDown;

        var content = new Grid();
        var item = ResolveItem(current);
        if (item is not null)
        {
            content.Children.Add(CreateItemImage(item, margin: new Thickness(6, 23, 6, 5)));
            border.ToolTip = DisplayName(item);
        }
        else
        {
            content.Children.Add(new TextBlock
            {
                Text = "비어 있음",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 20, 4, 4),
                IsHitTestVisible = false,
            });
            border.ToolTip = label;
        }

        var labelHost = new Border
        {
            VerticalAlignment = VerticalAlignment.Top,
            Background = (Brush)FindResource("BackgroundDarkBrush"),
            Padding = new Thickness(5, 3, 5, 3),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
            },
        };
        Panel.SetZIndex(labelHost, 2);
        content.Children.Add(labelHost);
        border.Child = content;
        return border;
    }

    private void WorkbenchSlot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1 || sender is not Border { Tag: WorkbenchSlotDropTarget target })
            return;

        var state = GetWorkbenchSlotState(target);
        var item = ResolveItem(state);
        if (state is null || item is null)
            return;

        BeginPotentialDrag(
            item,
            state,
            DragOriginKind.WorkbenchSlot,
            e,
            workbenchSlotKind: target.Kind,
            workbenchSlotId: target.SlotId);
    }

    internal bool CanDropIntoWorkbenchSlot(WorkbenchSlotDropTarget target, GameItem item)
    {
        if (_workbenchItem is null || _workbenchState is null)
            return false;

        // Never overwrite an occupied one-item Tarkov slot implicitly. The current
        // attachment/plate must be dragged out first so inventory state is never lost.
        if (GetWorkbenchSlotState(target) is not null)
            return false;

        var allowed = target.Kind switch
        {
            WorkbenchSlotKind.Attachment => FarmingGuideCompatibility.FilterAllows(item, target.Filters),
            WorkbenchSlotKind.ArmorPlate => target.AllowedItemIds.Contains(item.Id, StringComparer.Ordinal),
            _ => false,
        };
        if (!allowed || FarmingGuideCompatibility.ItemsConflict(_workbenchItem, item))
            return false;

        return EnumerateWorkbenchChildrenExcept(target)
            .Select(ResolveItem)
            .Where(static current => current is not null)
            .All(current => !FarmingGuideCompatibility.ItemsConflict(item, current!));
    }

    private IEnumerable<FarmingGuideItemState> EnumerateWorkbenchChildrenExcept(WorkbenchSlotDropTarget target)
    {
        if (_workbenchState is null)
            yield break;

        foreach (var entry in _workbenchState.Attachments)
        {
            if (target.Kind == WorkbenchSlotKind.Attachment &&
                string.Equals(entry.Key, target.SlotId, StringComparison.Ordinal))
                continue;
            if (entry.Value is not null)
                yield return entry.Value;
        }
        foreach (var entry in _workbenchState.ArmorPlates)
        {
            if (target.Kind == WorkbenchSlotKind.ArmorPlate &&
                string.Equals(entry.Key, target.SlotId, StringComparison.Ordinal))
                continue;
            if (entry.Value is not null)
                yield return entry.Value;
        }
    }

    internal FarmingGuideItemState? GetWorkbenchSlotState(WorkbenchSlotDropTarget target) =>
        target.Kind switch
        {
            WorkbenchSlotKind.Attachment => _workbenchState?.Attachments.GetValueOrDefault(target.SlotId),
            WorkbenchSlotKind.ArmorPlate => _workbenchState?.ArmorPlates.GetValueOrDefault(target.SlotId),
            _ => null,
        };

    internal void SetWorkbenchSlotState(
        WorkbenchSlotKind kind,
        string slotId,
        FarmingGuideItemState? value)
    {
        if (_workbenchState is null)
            return;

        if (kind == WorkbenchSlotKind.Attachment)
        {
            var attachments = _workbenchState.Attachments.ToDictionary(
                entry => entry.Key,
                entry => entry.Value,
                StringComparer.Ordinal);
            attachments[slotId] = value;
            _workbenchState = _workbenchState with { Attachments = attachments };
        }
        else
        {
            var armor = _workbenchState.ArmorPlates.ToDictionary(
                entry => entry.Key,
                entry => entry.Value,
                StringComparer.Ordinal);
            armor[slotId] = value;
            _workbenchState = _workbenchState with { ArmorPlates = armor };
        }

        _workbenchApply?.Invoke(_workbenchState);
    }
}
