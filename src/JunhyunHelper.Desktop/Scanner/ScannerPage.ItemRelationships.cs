using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private Border? _craftUsageHost;
    private StackPanel? _craftUsageItems;
    private Border? _barterUsageHost;
    private StackPanel? _barterUsageItems;
    private Border? _acquisitionHost;
    private StackPanel? _acquisitionItems;

    private void BuildItemRelationshipPresentation()
    {
        if (_craftUsageHost is not null)
            return;

        (_craftUsageHost, _craftUsageItems) = CreateRelationshipSection("제작 재료로써 사용처");
        (_barterUsageHost, _barterUsageItems) = CreateRelationshipSection("교환 재료로써 사용처");
        (_acquisitionHost, _acquisitionItems) = CreateRelationshipSection("수급처");

        var neededIndex = _neededSourcesHost is null
            ? SelectedItemPanel.Children.Count
            : SelectedItemPanel.Children.IndexOf(_neededSourcesHost);
        if (neededIndex < 0)
            neededIndex = SelectedItemPanel.Children.Count;

        SelectedItemPanel.Children.Insert(neededIndex, _craftUsageHost);
        SelectedItemPanel.Children.Insert(neededIndex + 1, _barterUsageHost);

        var acquisitionIndex = _neededSourcesHost is null
            ? SelectedItemPanel.Children.Count
            : SelectedItemPanel.Children.IndexOf(_neededSourcesHost) + 1;
        SelectedItemPanel.Children.Insert(acquisitionIndex, _acquisitionHost);
    }

    private (Border Host, StackPanel Items) CreateRelationshipSection(string title)
    {
        var items = new StackPanel();
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        });
        stack.Children.Add(items);

        return (new Border
        {
            Background = TryFindResource("BackgroundDarkBrush") as Brush,
            BorderBrush = TryFindResource("BorderBrush") as Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 12, 0, 0),
            Visibility = Visibility.Collapsed,
            Child = stack,
        }, items);
    }

    private void RenderItemRelationships(ScannerItemRelationshipDetails? relationships)
    {
        if (_craftUsageHost is null || _craftUsageItems is null ||
            _barterUsageHost is null || _barterUsageItems is null ||
            _acquisitionHost is null || _acquisitionItems is null)
        {
            return;
        }

        ClearItemRelationshipPresentation();
        if (relationships is null)
        {
            _acquisitionItems.Children.Add(new TextBlock
            {
                Text = "게임 데이터를 업데이트하면 수급처와 제작·교환 관계가 표시됩니다.",
                Foreground = TryFindResource("TextSecondaryBrush") as Brush,
                TextWrapping = TextWrapping.Wrap,
            });
            _acquisitionHost.Visibility = Visibility.Visible;
            return;
        }

        foreach (var usage in relationships.CraftUsages)
            _craftUsageItems.Children.Add(CreateUsageRow(usage));
        foreach (var usage in relationships.BarterUsages)
            _barterUsageItems.Children.Add(CreateUsageRow(usage));
        foreach (var acquisition in relationships.Acquisitions)
            _acquisitionItems.Children.Add(CreateAcquisitionRow(acquisition));

        _craftUsageHost.Visibility = relationships.CraftUsages.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _barterUsageHost.Visibility = relationships.BarterUsages.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _acquisitionHost.Visibility = relationships.Acquisitions.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private UIElement CreateUsageRow(ScannerItemUsageRow usage)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 9) };
        var top = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        top.Children.Add(new TextBlock
        {
            Text = $"{usage.SourceName} {usage.RequiredLevel}레벨  →  ",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
        });
        top.Children.Add(CreateItemLinkButton(usage.Product, usage.ProductCount));
        row.Children.Add(top);
        AddMaterialsRow(row, usage.Materials);
        return row;
    }

    private UIElement CreateAcquisitionRow(ScannerItemAcquisitionRow acquisition)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 9) };
        row.Children.Add(new TextBlock
        {
            Text = acquisition.RequiredLevel is { } level
                ? $"{acquisition.SourceName} {level}레벨"
                : acquisition.SourceName,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        AddMaterialsRow(row, acquisition.Materials);
        return row;
    }

    private void AddMaterialsRow(StackPanel owner, IReadOnlyList<ScannerItemMaterialRow> materials)
    {
        if (materials.Count == 0)
            return;

        var materialRow = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        materialRow.Children.Add(new TextBlock
        {
            Text = "재료  ",
            Foreground = TryFindResource("TextSecondaryBrush") as Brush,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
        });
        foreach (var material in materials)
        {
            materialRow.Children.Add(CreateItemLinkButton(
                material.Item,
                material.Count,
                material.IsTool));
        }
        owner.Children.Add(materialRow);
    }

    private Button CreateItemLinkButton(
        ScannerItemLink item,
        decimal count,
        bool isTool = false)
    {
        var suffix = count == 1m ? string.Empty : $" ×{FormatCount(count)}";
        if (isTool)
            suffix += " · 도구";

        var button = new Button
        {
            Tag = item.ItemId,
            Content = item.OfficialName + suffix,
            Padding = new Thickness(7, 3, 7, 3),
            Margin = new Thickness(0, 0, 5, 4),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        };
        button.Click += RelationshipItemButton_Click;
        return button;
    }

    private void RelationshipItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId))
            SelectSearchItemById(itemId);
    }

    private void ClearItemRelationshipPresentation()
    {
        if (_craftUsageItems is not null)
            _craftUsageItems.Children.Clear();
        if (_barterUsageItems is not null)
            _barterUsageItems.Children.Clear();
        if (_acquisitionItems is not null)
            _acquisitionItems.Children.Clear();
        if (_craftUsageHost is not null)
            _craftUsageHost.Visibility = Visibility.Collapsed;
        if (_barterUsageHost is not null)
            _barterUsageHost.Visibility = Visibility.Collapsed;
        if (_acquisitionHost is not null)
            _acquisitionHost.Visibility = Visibility.Collapsed;
    }

    private static string FormatCount(decimal value) =>
        value.ToString(value == decimal.Truncate(value) ? "0" : "0.##", CultureInfo.InvariantCulture);
}
