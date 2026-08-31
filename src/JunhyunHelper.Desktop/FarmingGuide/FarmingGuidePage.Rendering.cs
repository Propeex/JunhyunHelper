using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    internal sealed record EquipmentDropTarget(FarmingGuideEquipmentSlot Slot, bool Fixed, Border Border);
    internal sealed record CarrierDropTarget(FarmingGuideStorageKind Kind, Border Border);
    internal sealed record GridDropTarget(
        FarmingGuideStorageKind Kind,
        string? ParentInstanceId,
        int GridIndex,
        int Width,
        int Height,
        FarmingGuideItemFilter Filter,
        Canvas Canvas);
    internal sealed record PlacedItemSource(FarmingGuideStoredItemState Placement);

    private void RenderEquipment()
    {
        EquipmentPanel.Children.Clear();

        var board = new Grid
        {
            Margin = new Thickness(-4, 0, -4, 8),
        };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(98) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });

        foreach (var definition in EquipmentSlots)
        {
            var state = definition.Fixed
                ? GetFixed(definition.Slot)
                : Equipment.TryGetValue(definition.Slot, out var equipped) ? equipped : null;
            var item = ResolveItem(state);
            var slot = CreateEquipmentSlot(definition.Slot, definition.Label, definition.Fixed, state, item);
            var placement = EquipmentBoardPlacement(definition.Slot);

            Grid.SetRow(slot, placement.Row);
            Grid.SetColumn(slot, placement.Column);
            Grid.SetRowSpan(slot, placement.RowSpan);
            Grid.SetColumnSpan(slot, placement.ColumnSpan);
            board.Children.Add(slot);
        }

        EquipmentPanel.Children.Add(board);
    }

    private Border CreateEquipmentSlot(
        FarmingGuideEquipmentSlot slot,
        string label,
        bool fixedSlot,
        FarmingGuideItemState? state,
        GameItem? item)
    {
        var border = new Border
        {
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
            ToolTip = item is null ? $"{label} 슬롯" : DisplayName(item),
        };
        var target = new EquipmentDropTarget(slot, fixedSlot, border);
        border.Tag = target;
        border.MouseLeftButtonDown += Equipment_MouseLeftButtonDown;

        var content = new Grid();
        if (item is not null && state is not null)
        {
            content.Children.Add(CreateAssemblyVisual(state, item, margin: new Thickness(8, 24, 8, 6)));
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
                Margin = new Thickness(5, 20, 5, 4),
                IsHitTestVisible = false,
            });
        }

        var labelHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Background = (Brush)FindResource("BackgroundDarkBrush"),
            Padding = new Thickness(6, 3, 6, 3),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
            },
        };
        Panel.SetZIndex(labelHost, 2);
        content.Children.Add(labelHost);
        border.Child = content;
        return border;
    }

    private static (int Row, int Column, int RowSpan, int ColumnSpan) EquipmentBoardPlacement(
        FarmingGuideEquipmentSlot slot) => slot switch
    {
        FarmingGuideEquipmentSlot.Headset => (0, 0, 1, 1),
        FarmingGuideEquipmentSlot.Helmet => (0, 1, 1, 1),
        FarmingGuideEquipmentSlot.FaceCover => (0, 2, 1, 1),
        FarmingGuideEquipmentSlot.Armband => (1, 0, 1, 1),
        FarmingGuideEquipmentSlot.BodyArmor => (1, 1, 2, 1),
        FarmingGuideEquipmentSlot.Eyewear => (1, 2, 1, 1),
        FarmingGuideEquipmentSlot.Holster => (2, 0, 1, 1),
        FarmingGuideEquipmentSlot.PrimaryWeapon1 => (3, 0, 1, 2),
        FarmingGuideEquipmentSlot.Melee => (3, 2, 1, 1),
        FarmingGuideEquipmentSlot.PrimaryWeapon2 => (4, 0, 1, 2),
        _ => (4, 2, 1, 1),
    };

    private void RenderStorage()
    {
        StoragePanel.Children.Clear();
        var definitions = StorageDefinitions().ToDictionary(storage => storage.Kind);

        if (definitions.TryGetValue(FarmingGuideStorageKind.Rig, out var rig))
            StoragePanel.Children.Add(CreateCarrierStorageSection(rig));

        if (definitions.TryGetValue(FarmingGuideStorageKind.Pockets, out var pockets) &&
            definitions.TryGetValue(FarmingGuideStorageKind.SpecialSlots, out var specialSlots))
        {
            StoragePanel.Children.Add(CreatePocketAndSpecialSection(pockets, specialSlots));
        }

        if (definitions.TryGetValue(FarmingGuideStorageKind.Backpack, out var backpack))
            StoragePanel.Children.Add(CreateCarrierStorageSection(backpack));

        if (definitions.TryGetValue(FarmingGuideStorageKind.SecureContainer, out var secureContainer))
            StoragePanel.Children.Add(CreateCarrierStorageSection(secureContainer));
    }

    private FrameworkElement CreatePocketAndSpecialSection(
        StorageDefinition pockets,
        StorageDefinition specialSlots)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pocketSection = CreateFixedStorageSection(pockets);
        pocketSection.Margin = new Thickness(0, 0, 10, 0);
        row.Children.Add(pocketSection);

        var specialSection = CreateFixedStorageSection(specialSlots);
        specialSection.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(specialSection, 1);
        row.Children.Add(specialSection);
        return row;
    }

    private FrameworkElement CreateFixedStorageSection(StorageDefinition storage)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
        section.Children.Add(new TextBlock
        {
            Text = storage.Label,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 0, 0, 6),
        });

        section.Children.Add(CreateCompactGridHost(storage.Kind, storage.Grids, parentInstanceId: null));
        return section;
    }

    private FrameworkElement CreateCarrierStorageSection(StorageDefinition storage)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 17) };
        section.Children.Add(new TextBlock
        {
            Text = storage.Label,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 0, 0, 6),
        });

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var carrierItem = ResolveItem(storage.Carrier);
        var carrier = new Border
        {
            Width = 104,
            Height = 104,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 8, 8),
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Background = (Brush)FindResource("BackgroundMediumBrush"),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
            ToolTip = carrierItem is null ? $"{storage.Label} 장비 슬롯" : DisplayName(carrierItem),
        };
        var target = new CarrierDropTarget(storage.Kind, carrier);
        carrier.Tag = target;
        carrier.MouseLeftButtonDown += Carrier_MouseLeftButtonDown;

        if (carrierItem is not null && storage.Carrier is not null)
        {
            carrier.Child = CreateAssemblyVisual(storage.Carrier, carrierItem, margin: new Thickness(5));
        }
        else
        {
            carrier.Child = new TextBlock
            {
                Text = "장비를\n놓으세요",
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
            };
        }
        body.Children.Add(carrier);

        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        Grid.SetColumn(content, 1);
        if (storage.Grids.Count > 0)
        {
            content.Children.Add(CreateCompactGridHost(storage.Kind, storage.Grids, parentInstanceId: null));
        }
        else
        {
            content.Children.Add(new TextBlock
            {
                Text = carrierItem is null
                    ? "장비를 장착하면 내부 칸이 표시됩니다."
                    : "현재 데이터에 수납 구조가 없습니다.",
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(4, 8, 4, 4),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        body.Children.Add(content);
        section.Children.Add(body);
        return section;
    }

    private WrapPanel CreateCompactGridHost(
        FarmingGuideStorageKind kind,
        IReadOnlyList<FarmingGuideStorageGridDefinition> grids,
        string? parentInstanceId)
    {
        var host = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        for (var index = 0; index < grids.Count; index++)
            host.Children.Add(CreateGridCanvas(kind, index, grids[index], parentInstanceId));
        return host;
    }

    private Canvas CreateGridCanvas(
        FarmingGuideStorageKind kind,
        int gridIndex,
        FarmingGuideStorageGridDefinition definition,
        string? parentInstanceId = null)
    {
        var canvas = new Canvas
        {
            Width = definition.Width * CellSize,
            Height = definition.Height * CellSize,
            Margin = new Thickness(0, 0, 7, 7),
            Background = (Brush)FindResource("BackgroundDarkBrush"),
            ClipToBounds = true,
        };
        canvas.Tag = new GridDropTarget(
            kind,
            parentInstanceId,
            gridIndex,
            definition.Width,
            definition.Height,
            definition.Filters,
            canvas);

        for (var y = 0; y < definition.Height; y++)
        {
            for (var x = 0; x < definition.Width; x++)
            {
                var cell = new Border
                {
                    Width = CellSize,
                    Height = CellSize,
                    BorderBrush = (Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(0.5),
                    Background = Brushes.Transparent,
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(cell, x * CellSize);
                Canvas.SetTop(cell, y * CellSize);
                canvas.Children.Add(cell);
            }
        }

        foreach (var placement in StoredItems.Where(item =>
                     item.GridIndex == gridIndex &&
                     IsOnStorageSurface(item, kind, parentInstanceId)))
        {
            var item = ResolveItem(placement.Item);
            if (item is null)
                continue;
            var (width, height) = FarmingGuidePlacementEngine.Footprint(
                item.Width ?? 1,
                item.Height ?? 1,
                placement.Rotated);
            var card = new Border
            {
                Width = width * CellSize - 2,
                Height = height * CellSize - 2,
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("AccentBrush"),
                Background = (Brush)FindResource("BackgroundMediumBrush"),
                Cursor = Cursors.Hand,
                Tag = new PlacedItemSource(placement),
                ToolTip = DisplayName(item),
                ClipToBounds = true,
            };
            card.MouseLeftButtonDown += PlacedItem_MouseLeftButtonDown;
            card.Child = CreateAssemblyVisual(placement.Item, item, placement.Rotated, new Thickness(2));
            Canvas.SetLeft(card, placement.X * CellSize + 1);
            Canvas.SetTop(card, placement.Y * CellSize + 1);
            canvas.Children.Add(card);
        }

        return canvas;
    }

    internal static bool IsOnStorageSurface(
        FarmingGuideStoredItemState placement,
        FarmingGuideStorageKind kind,
        string? parentInstanceId)
    {
        if (parentInstanceId is not null)
        {
            return string.Equals(
                placement.ParentInstanceId,
                parentInstanceId,
                StringComparison.Ordinal);
        }

        return placement.ParentInstanceId is null && placement.Storage == kind;
    }

    internal void EditEquipmentTarget(EquipmentDropTarget target) => OpenEquipmentWorkbench(target);

    internal void EditCarrierTarget(CarrierDropTarget target) => OpenCarrierWorkbench(target);

    internal void EditPlacedItem(PlacedItemSource source) => OpenStoredWorkbench(source);
}
