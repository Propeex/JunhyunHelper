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
        string[] OwnerPath,
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
    private string[] _workbenchOwnerPath = [];
    private WorkbenchSlotDropTarget? _compatiblePickerTarget;

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
        _workbenchOwnerPath = [];
        _compatiblePickerTarget = null;
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
        _workbenchOwnerPath = [];
        _compatiblePickerTarget = null;
    }

    internal void RenderWorkbench()
    {
        if (!IsWorkbenchOpen || _workbenchItem is null || _workbenchState is null)
            return;

        WorkbenchPanel.Children.Clear();
        if (_workbenchMode == WorkbenchMode.Storage)
        {
            WorkbenchTitleText.Text = DisplayName(_workbenchItem);
            var layout = _workbenchItem.FarmingGuideData;
            if (layout is null || layout.StorageGrids.Count == 0)
            {
                CloseWorkbench();
                return;
            }

            WorkbenchPanel.Children.Add(CreateCompactGridHost(
                _workbenchStorageKind,
                layout.StorageGrids,
                _workbenchParentInstanceId));
            return;
        }

        var currentState = FarmingGuideAssemblyPolicy.GetNode(_workbenchState, _workbenchOwnerPath);
        var currentItem = ResolveItem(currentState);
        var currentLayout = currentItem?.FarmingGuideData;
        if (currentState is null || currentItem is null || currentLayout is null)
        {
            _workbenchOwnerPath = [];
            _compatiblePickerTarget = null;
            currentState = _workbenchState;
            currentItem = _workbenchItem;
            currentLayout = currentItem.FarmingGuideData;
        }
        if (currentLayout is null)
        {
            CloseWorkbench();
            return;
        }

        WorkbenchTitleText.Text = _workbenchOwnerPath.Length == 0
            ? DisplayName(_workbenchItem)
            : $"{DisplayName(_workbenchItem)}  ›  {DisplayName(currentItem)}";

        WorkbenchPanel.Children.Add(CreateAssemblyWorkbenchHeader(currentState, currentItem));

        var slots = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        foreach (var slot in currentLayout.AttachmentSlots)
        {
            slots.Children.Add(CreateWorkbenchSlot(
                WorkbenchSlotKind.Attachment,
                slot.Id,
                slot.Required ? $"{slot.Name ?? slot.NameId} *" : slot.Name ?? slot.NameId,
                slot.Filters,
                [],
                currentState.Attachments.GetValueOrDefault(slot.Id)));
        }

        foreach (var slot in currentLayout.ArmorSlots.Where(static slot => !slot.Locked))
        {
            slots.Children.Add(CreateWorkbenchSlot(
                WorkbenchSlotKind.ArmorPlate,
                slot.Id,
                slot.Name ?? slot.NameId,
                FarmingGuideItemFilter.Empty,
                slot.AllowedPlateIds,
                currentState.ArmorPlates.GetValueOrDefault(slot.Id)));
        }

        if (slots.Children.Count == 0)
        {
            if (_workbenchOwnerPath.Length == 0)
            {
                CloseWorkbench();
                return;
            }
        }
        else
        {
            WorkbenchPanel.Children.Add(slots);
        }

        if (_compatiblePickerTarget is not null &&
            _compatiblePickerTarget.OwnerPath.SequenceEqual(_workbenchOwnerPath, StringComparer.Ordinal))
        {
            WorkbenchPanel.Children.Add(CreateCompatiblePicker(_compatiblePickerTarget));
        }
    }

    private FrameworkElement CreateAssemblyWorkbenchHeader(FarmingGuideItemState state, GameItem item)
    {
        var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var imageHost = new Border
        {
            Width = 88,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            ClipToBounds = true,
            Child = CreateAssemblyVisual(state, item, margin: new Thickness(4)),
        };
        header.Children.Add(imageHost);

        var details = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        if (_workbenchOwnerPath.Length > 0)
        {
            var back = new Button
            {
                Content = "← 상위 부품",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 0, 5),
            };
            back.Click += (_, _) =>
            {
                _workbenchOwnerPath = _workbenchOwnerPath.Take(_workbenchOwnerPath.Length - 1).ToArray();
                _compatiblePickerTarget = null;
                RenderWorkbench();
            };
            details.Children.Add(back);
        }
        details.Children.Add(new TextBlock
        {
            Text = DisplayName(item),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        details.Children.Add(new TextBlock
        {
            Text = FarmingGuideAssemblyPolicy.HasMissingRequiredSlots(state, ItemCatalog)
                ? "필수 부품 슬롯이 비어 있습니다."
                : "부품을 끌어 놓거나 빈 슬롯을 클릭해 장착할 수 있습니다.",
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(details, 1);
        header.Children.Add(details);
        return header;
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
            Width = 112,
            Height = 112,
            Margin = new Thickness(0, 0, 10, 10),
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
        };
        var target = new WorkbenchSlotDropTarget(
            kind,
            slotId,
            _workbenchOwnerPath.ToArray(),
            filters,
            allowedItemIds,
            border);
        border.Tag = target;
        border.MouseLeftButtonDown += WorkbenchSlot_MouseLeftButtonDown;

        var content = new Grid();
        var item = ResolveItem(current);
        if (item is not null && current is not null)
        {
            content.Children.Add(CreateAssemblyVisual(current, item, margin: new Thickness(6, 23, 6, 5)));
            border.ToolTip = item.FarmingGuideData?.AttachmentSlots.Count > 0
                ? $"{DisplayName(item)}\n더블클릭: 하위 부품 슬롯 열기"
                : DisplayName(item);
        }
        else
        {
            content.Children.Add(new TextBlock
            {
                Text = "비어 있음\n클릭하여 선택",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 20, 4, 4),
                IsHitTestVisible = false,
            });
            border.ToolTip = $"{label}\n클릭: 호환 아이템 보기";
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
        if (sender is not Border { Tag: WorkbenchSlotDropTarget target })
            return;

        var state = GetWorkbenchSlotState(target);
        var item = ResolveItem(state);
        if (e.ClickCount > 1)
        {
            if (target.Kind == WorkbenchSlotKind.Attachment &&
                state is not null &&
                item?.FarmingGuideData is { } layout &&
                (layout.AttachmentSlots.Count > 0 || layout.ArmorSlots.Any(static slot => !slot.Locked)))
            {
                _workbenchOwnerPath = target.OwnerPath.Append(target.SlotId).ToArray();
                _compatiblePickerTarget = null;
                RenderWorkbench();
                e.Handled = true;
            }
            return;
        }

        if (state is null || item is null)
        {
            _compatiblePickerTarget = target;
            RenderWorkbench();
            e.Handled = true;
            return;
        }

        BeginPotentialDrag(
            item,
            state,
            DragOriginKind.WorkbenchSlot,
            e,
            workbenchSlotKind: target.Kind,
            workbenchSlotId: target.SlotId);
    }

    private FrameworkElement CreateCompatiblePicker(WorkbenchSlotDropTarget target)
    {
        var host = new Border
        {
            Margin = new Thickness(0, 8, 0, 8),
            Padding = new Thickness(10),
            BorderBrush = (Brush)FindResource("AccentBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
        };
        var stack = new StackPanel();
        host.Child = stack;

        var titleRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        titleRow.Children.Add(new TextBlock
        {
            Text = "장착 가능한 아이템",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var close = new Button
        {
            Content = "닫기",
            Padding = new Thickness(9, 3, 9, 3),
        };
        close.Click += (_, _) =>
        {
            _compatiblePickerTarget = null;
            RenderWorkbench();
        };
        Grid.SetColumn(close, 1);
        titleRow.Children.Add(close);
        stack.Children.Add(titleRow);

        var candidates = CompatibleItemsFor(target);
        if (candidates.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "현재 조립 상태에서 장착 가능한 아이템이 없습니다.",
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                TextWrapping = TextWrapping.Wrap,
            });
            return host;
        }

        var cards = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        foreach (var candidate in candidates)
        {
            var card = new Border
            {
                Width = 96,
                Height = 112,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(5),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                Background = (Brush)FindResource("BackgroundDarkBrush"),
                Cursor = Cursors.Hand,
                ToolTip = DisplayName(candidate),
            };
            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.Children.Add(CreateItemImage(candidate, margin: new Thickness(2)));
            var name = new TextBlock
            {
                Text = DisplayName(candidate),
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 30,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(name, 1);
            content.Children.Add(name);
            card.Child = content;
            card.MouseLeftButtonDown += (_, args) =>
            {
                if (args.ClickCount != 1 || !CanDropIntoWorkbenchSlot(target, candidate))
                    return;
                SetWorkbenchSlotState(
                    target.Kind,
                    target.OwnerPath,
                    target.SlotId,
                    FarmingGuideItemState.Create(candidate.Id));
                _compatiblePickerTarget = null;
                MarkChanged();
                RenderWorkbench();
                args.Handled = true;
            };
            cards.Children.Add(card);
        }
        stack.Children.Add(cards);
        return host;
    }

    private IReadOnlyList<GameItem> CompatibleItemsFor(WorkbenchSlotDropTarget target)
    {
        if (_workbenchState is null)
            return [];
        var ownerState = FarmingGuideAssemblyPolicy.GetNode(_workbenchState, target.OwnerPath);
        var ownerItem = ResolveItem(ownerState);
        var layout = ownerItem?.FarmingGuideData;
        if (ownerState is null || layout is null)
            return [];

        if (target.Kind == WorkbenchSlotKind.Attachment)
        {
            var slot = layout.AttachmentSlots.FirstOrDefault(value =>
                string.Equals(value.Id, target.SlotId, StringComparison.Ordinal));
            return slot is null
                ? []
                : FarmingGuideAssemblyPolicy.CompatibleItems(_workbenchState, target.OwnerPath, slot, ItemCatalog);
        }

        var armor = layout.ArmorSlots.FirstOrDefault(value =>
            string.Equals(value.Id, target.SlotId, StringComparison.Ordinal));
        return armor is null
            ? []
            : FarmingGuideAssemblyPolicy.CompatibleArmorPlates(_workbenchState, target.OwnerPath, armor, ItemCatalog);
    }

    internal bool CanDropIntoWorkbenchSlot(WorkbenchSlotDropTarget target, GameItem item)
    {
        if (_workbenchState is null || GetWorkbenchSlotState(target) is not null)
            return false;
        var ownerState = FarmingGuideAssemblyPolicy.GetNode(_workbenchState, target.OwnerPath);
        var ownerItem = ResolveItem(ownerState);
        var layout = ownerItem?.FarmingGuideData;
        if (ownerState is null || layout is null)
            return false;

        if (target.Kind == WorkbenchSlotKind.Attachment)
        {
            var slot = layout.AttachmentSlots.FirstOrDefault(value =>
                string.Equals(value.Id, target.SlotId, StringComparison.Ordinal));
            return slot is not null && FarmingGuideAssemblyPolicy.CanAttach(
                _workbenchState,
                target.OwnerPath,
                slot,
                item,
                ItemCatalog);
        }

        var armor = layout.ArmorSlots.FirstOrDefault(value =>
            string.Equals(value.Id, target.SlotId, StringComparison.Ordinal));
        return armor is not null && FarmingGuideAssemblyPolicy.CanInstallArmorPlate(
            _workbenchState,
            target.OwnerPath,
            armor,
            item,
            ItemCatalog);
    }

    internal FarmingGuideItemState? GetWorkbenchSlotState(WorkbenchSlotDropTarget target)
    {
        if (_workbenchState is null)
            return null;
        var owner = FarmingGuideAssemblyPolicy.GetNode(_workbenchState, target.OwnerPath);
        return target.Kind switch
        {
            WorkbenchSlotKind.Attachment => owner?.Attachments.GetValueOrDefault(target.SlotId),
            WorkbenchSlotKind.ArmorPlate => owner?.ArmorPlates.GetValueOrDefault(target.SlotId),
            _ => null,
        };
    }

    internal void SetWorkbenchSlotState(
        WorkbenchSlotKind kind,
        string slotId,
        FarmingGuideItemState? value) =>
        SetWorkbenchSlotState(kind, _workbenchOwnerPath, slotId, value);

    internal void SetWorkbenchSlotState(
        WorkbenchSlotKind kind,
        IReadOnlyList<string> ownerPath,
        string slotId,
        FarmingGuideItemState? value)
    {
        if (_workbenchState is null)
            return;

        _workbenchState = kind == WorkbenchSlotKind.Attachment
            ? FarmingGuideAssemblyPolicy.SetAttachment(_workbenchState, ownerPath, slotId, value)
            : FarmingGuideAssemblyPolicy.SetArmorPlate(_workbenchState, ownerPath, slotId, value);
        _workbenchApply?.Invoke(_workbenchState);
    }
}
