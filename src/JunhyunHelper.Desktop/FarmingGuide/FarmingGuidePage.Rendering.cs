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
        int GridIndex,
        int Width,
        int Height,
        FarmingGuideItemFilter Filter,
        Canvas Canvas);
    internal sealed record PlacedItemSource(FarmingGuideStoredItemState Placement);
    internal sealed record EquipmentItemSource(FarmingGuideEquipmentSlot Slot, bool Fixed);
    internal sealed record CarrierItemSource(FarmingGuideStorageKind Kind);

    private void RenderEquipment()
    {
        EquipmentPanel.Children.Clear();
        foreach (var definition in EquipmentSlots)
        {
            var state = definition.Fixed
                ? GetFixed(definition.Slot)
                : Equipment.TryGetValue(definition.Slot, out var equipped) ? equipped : null;
            var item = ResolveItem(state);

            var border = new Border
            {
                MinHeight = definition.Slot is FarmingGuideEquipmentSlot.PrimaryWeapon1 or FarmingGuideEquipmentSlot.PrimaryWeapon2 ? 68 : 52,
                Margin = new Thickness(0, 0, 0, 7),
                Padding = new Thickness(9),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                Background = (Brush)FindResource("BackgroundMediumBrush"),
                Tag = null,
            };
            var target = new EquipmentDropTarget(definition.Slot, definition.Fixed, border);
            border.Tag = target;
            border.MouseLeftButtonDown += Equipment_MouseLeftButtonDown;
            border.MouseDoubleClick += Equipment_MouseDoubleClick;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var label = new TextBlock
            {
                Text = definition.Label,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            grid.Children.Add(label);
            var value = new TextBlock
            {
                Text = item is null ? "비어 있음" : DisplayName(item),
                FontWeight = item is null ? FontWeights.Normal : FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(value, 1);
            grid.Children.Add(value);
            border.Child = grid;
            EquipmentPanel.Children.Add(border);
        }
    }

    private void RenderStorage()
    {
        StoragePanel.Children.Clear();
        foreach (var storage in StorageDefinitions())
        {
            var section = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            var carrierItem = ResolveItem(storage.Carrier);
            var header = new Border
            {
                MinHeight = 38,
                Padding = new Thickness(9, 6, 9, 6),
                Margin = new Thickness(0, 0, 0, 8),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                Background = (Brush)FindResource("BackgroundMediumBrush"),
            };

            if (storage.Kind is FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack or FarmingGuideStorageKind.SecureContainer)
            {
                var target = new CarrierDropTarget(storage.Kind, header);
                header.Tag = target;
                header.MouseLeftButtonDown += Carrier_MouseLeftButtonDown;
                header.MouseDoubleClick += Carrier_MouseDoubleClick;
                header.Child = new TextBlock
                {
                    Text = carrierItem is null ? $"{storage.Label} · 장비를 여기에 놓으세요" : $"{storage.Label} · {DisplayName(carrierItem)}",
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            else
            {
                header.Child = new TextBlock
                {
                    Text = storage.Label,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            section.Children.Add(header);

            if (storage.Grids.Count == 0)
            {
                if (storage.Kind is FarmingGuideStorageKind.Rig or FarmingGuideStorageKind.Backpack or FarmingGuideStorageKind.SecureContainer)
                {
                    section.Children.Add(new TextBlock
                    {
                        Text = carrierItem is null
                            ? "장비를 선택하면 실제 내부 그리드가 표시됩니다."
                            : "이 장비의 수납 구조가 현재 데이터에 없습니다. 게임 데이터를 업데이트해 주세요.",
                        Foreground = (Brush)FindResource("TextSecondaryBrush"),
                        Margin = new Thickness(4, 2, 4, 4),
                    });
                }
                StoragePanel.Children.Add(section);
                continue;
            }

            var gridsPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            for (var index = 0; index < storage.Grids.Count; index++)
            {
                var definition = storage.Grids[index];
                var canvas = CreateGridCanvas(storage.Kind, index, definition);
                gridsPanel.Children.Add(canvas);
            }
            section.Children.Add(gridsPanel);
            StoragePanel.Children.Add(section);
        }
    }

    private Canvas CreateGridCanvas(
        FarmingGuideStorageKind kind,
        int gridIndex,
        FarmingGuideStorageGridDefinition definition)
    {
        var canvas = new Canvas
        {
            Width = definition.Width * CellSize,
            Height = definition.Height * CellSize,
            Margin = new Thickness(0, 0, 8, 8),
            Background = (Brush)FindResource("BackgroundDarkBrush"),
            ClipToBounds = true,
        };
        canvas.Tag = new GridDropTarget(kind, gridIndex, definition.Width, definition.Height, definition.Filters, canvas);

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

        foreach (var placement in StoredItems.Where(item => item.Storage == kind && item.GridIndex == gridIndex))
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
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)FindResource("AccentBrush"),
                Background = (Brush)FindResource("BackgroundLightBrush"),
                ToolTip = DisplayName(item),
                Cursor = Cursors.Hand,
                Tag = new PlacedItemSource(placement),
            };
            card.MouseLeftButtonDown += PlacedItem_MouseLeftButtonDown;
            card.MouseDoubleClick += PlacedItem_MouseDoubleClick;
            card.Child = new TextBlock
            {
                Text = item.ShortNameKo ?? item.ShortNameEn ?? DisplayName(item),
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(card, placement.X * CellSize + 1);
            Canvas.SetTop(card, placement.Y * CellSize + 1);
            canvas.Children.Add(card);
        }

        return canvas;
    }

    private void Equipment_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: EquipmentDropTarget target })
            return;
        var state = target.Fixed ? GetFixed(target.Slot) : Equipment.GetValueOrDefault(target.Slot);
        if (state is null)
            return;
        EditItemConfiguration(state, updated =>
        {
            if (target.Fixed)
                SetFixed(target.Slot, updated);
            else
                Equipment[target.Slot] = updated;
            MarkChanged(target.Fixed);
        });
        e.Handled = true;
    }

    private void Carrier_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: CarrierDropTarget target })
            return;
        var state = GetCarrier(target.Kind);
        if (state is null)
            return;
        EditItemConfiguration(state, updated =>
        {
            SetCarrier(target.Kind, updated);
            MarkChanged();
        });
        e.Handled = true;
    }

    private void PlacedItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: PlacedItemSource source })
            return;
        EditItemConfiguration(source.Placement.Item, updated =>
        {
            var index = StoredItems.FindIndex(item => item.InstanceId == source.Placement.InstanceId);
            if (index >= 0)
                StoredItems[index] = StoredItems[index] with { Item = updated };
            MarkChanged();
        });
        e.Handled = true;
    }

    private void EditItemConfiguration(FarmingGuideItemState state, Action<FarmingGuideItemState> apply)
    {
        var item = ResolveItem(state);
        if (item?.FarmingGuideData is null ||
            (item.FarmingGuideData.AttachmentSlots.Count == 0 && item.FarmingGuideData.ArmorSlots.All(slot => slot.Locked)))
        {
            return;
        }

        var window = new FarmingGuideItemConfigurationWindow(item, state, ItemCatalog)
        {
            Owner = Window.GetWindow(this),
        };
        if (window.ShowDialog() == true && window.Result is not null)
            apply(window.Result);
    }
}
