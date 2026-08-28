using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private Border? _basicInfoHost;
    private StackPanel? _basicInfoItems;
    private Border? _questUsageHost;
    private StackPanel? _questUsageItems;
    private Border? _hideoutUsageHost;
    private StackPanel? _hideoutUsageItems;
    private Border? _craftUsageHost;
    private StackPanel? _craftUsageItems;
    private Border? _acquisitionHost;
    private StackPanel? _acquisitionItems;

    private void BuildItemRelationshipPresentation()
    {
        if (_craftUsageHost is not null)
            return;

        (_basicInfoHost, _basicInfoItems) = CreateRelationshipSection("기본 정보", 10);
        SelectedItemPanel.Children.Insert(Math.Min(1, SelectedItemPanel.Children.Count), _basicInfoHost);

        (_questUsageHost, _questUsageItems) = CreateRelationshipSection("퀘스트 사용처");
        (_hideoutUsageHost, _hideoutUsageItems) = CreateRelationshipSection("은신처 업그레이드 사용처");
        (_craftUsageHost, _craftUsageItems) = CreateRelationshipSection("제작 재료 사용처");
        (_acquisitionHost, _acquisitionItems) = CreateRelationshipSection("수급처");

        var insertIndex = _neededSourcesHost is null
            ? SelectedItemPanel.Children.Count
            : SelectedItemPanel.Children.IndexOf(_neededSourcesHost);
        if (insertIndex < 0)
            insertIndex = SelectedItemPanel.Children.Count;

        SelectedItemPanel.Children.Insert(insertIndex, _questUsageHost);
        SelectedItemPanel.Children.Insert(insertIndex + 1, _hideoutUsageHost);
        SelectedItemPanel.Children.Insert(insertIndex + 2, _craftUsageHost);
        SelectedItemPanel.Children.Insert(insertIndex + 3, _acquisitionHost);

        // v1.8.4 item detail replaces the older separate "필요한 곳" block with the
        // canonical quest/hideout usage sections above. Keep the old host out of the
        // visible detail so the one-column presentation has no duplicate information.
        if (_neededSourcesHost is not null)
            _neededSourcesHost.Visibility = Visibility.Collapsed;
    }

    private (Border Host, StackPanel Items) CreateRelationshipSection(string title, double topMargin = 12)
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
            Margin = new Thickness(0, topMargin, 0, 0),
            Visibility = Visibility.Collapsed,
            Child = stack,
        }, items);
    }

    private void RenderProductItemExtensions(ScannerItemSearchDetails details)
    {
        // The legacy three-row summary is replaced by the exact four-field basic-info
        // section below. Collapsing its actual parent keeps XAML compatibility without
        // carrying duplicate flea/trader/needed rows in the visible detail.
        if (FleaAverageText.Parent is FrameworkElement legacySummary)
            legacySummary.Visibility = Visibility.Collapsed;
        if (_neededSourcesHost is not null)
            _neededSourcesHost.Visibility = Visibility.Collapsed;

        RenderBasicInfo(details.Basic, details.Snapshot);
        RenderItemRelationships(details.Relationships, details.Snapshot);
    }

    private void SelectSearchItemById(string itemId)
    {
        if (_coordinator is null)
            return;
        var details = _coordinator.GetSearchItemDetails(itemId);
        if (details is null)
            return;

        _suppressSearchRefresh = true;
        try
        {
            ItemSearchBox.Text = details.Snapshot.OfficialName;
            ItemSearchBox.CaretIndex = ItemSearchBox.Text.Length;
        }
        finally
        {
            _suppressSearchRefresh = false;
        }

        SearchResultsPopup.IsOpen = false;
        OpenScannerItemDetails(details);
    }

    private void RenderBasicInfo(ScannerItemBasicDetails basic, ScannerItemSnapshot snapshot)
    {
        if (_basicInfoHost is null || _basicInfoItems is null)
            return;

        _basicInfoItems.Children.Clear();
        AddBasicRow("크기", basic.Width > 0 && basic.Height > 0 ? $"{basic.Width}×{basic.Height}" : "-");
        AddBasicRow("플리마켓 평균가", snapshot.FleaAveragePrice is > 0 ? FormatRoubles(snapshot.FleaAveragePrice.Value) : "-");
        AddBasicRow(
            "최고 상인 판매가",
            snapshot.TraderSellPrice is > 0
                ? $"{(string.IsNullOrWhiteSpace(snapshot.BestTraderName) ? "상인" : snapshot.BestTraderName)} {FormatRoubles(snapshot.TraderSellPrice.Value)}"
                : "-");
        AddBasicRow("필요 개수", $"{Math.Max(0, snapshot.CurrentNeeded):N0}개");
        _basicInfoHost.Visibility = Visibility.Visible;
    }

    private void AddBasicRow(string label, string value)
    {
        if (_basicInfoItems is null)
            return;

        var grid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = TryFindResource("TextSecondaryBrush") as Brush,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var valueText = new TextBlock
        {
            Text = value,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(valueText, 1);
        grid.Children.Add(valueText);
        _basicInfoItems.Children.Add(grid);
    }

    private void RenderItemRelationships(ScannerItemRelationshipDetails? relationships, ScannerItemSnapshot snapshot)
    {
        if (_questUsageHost is null || _questUsageItems is null ||
            _hideoutUsageHost is null || _hideoutUsageItems is null ||
            _craftUsageHost is null || _craftUsageItems is null ||
            _acquisitionHost is null || _acquisitionItems is null)
            return;

        ClearItemRelationshipPresentation();
        if (relationships is null)
        {
            // Legacy snapshots do not have relationship data. Do not render empty category
            // shells; the detail remains useful through the basic-info block.
            return;
        }

        foreach (var usage in relationships.QuestUsages)
            _questUsageItems.Children.Add(CreateRequirementUsageRow(usage));
        foreach (var usage in relationships.HideoutUsages)
            _hideoutUsageItems.Children.Add(CreateRequirementUsageRow(usage));
        foreach (var usage in relationships.CraftUsages)
            _craftUsageItems.Children.Add(CreateRecipeUsageCard(usage));

        RenderAcquisitionGroups(relationships.Acquisitions, snapshot);

        _questUsageHost.Visibility = relationships.QuestUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _hideoutUsageHost.Visibility = relationships.HideoutUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _craftUsageHost.Visibility = relationships.CraftUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _acquisitionHost.Visibility = _acquisitionItems.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private UIElement CreateRequirementUsageRow(ScannerItemRequirementUsageRow usage)
    {
        var detail = usage.Kind == ScannerItemRequirementUsageKind.Hideout && usage.TargetLevel is { } level
            ? $"Lv.{level} 업그레이드 · {usage.Count:N0}개"
            : $"{usage.Count:N0}개";
        if (usage.FoundInRaid)
            detail += " · FIR 필요";

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = usage.SourceName,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(new TextBlock
        {
            Text = detail,
            FontSize = 11,
            Foreground = TryFindResource("TextSecondaryBrush") as Brush,
            Margin = new Thickness(0, 2, 0, 0),
        });
        var button = new Button
        {
            Tag = usage,
            Content = stack,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(9, 7, 9, 7),
            Margin = new Thickness(0, 0, 0, 6),
        };
        button.Click += RequirementUsageButton_Click;
        return button;
    }

    private void RequirementUsageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ScannerItemRequirementUsageRow usage } || Window.GetWindow(this) is not MainWindow mainWindow)
            return;
        var kind = usage.Kind == ScannerItemRequirementUsageKind.Quest
            ? ScannerNeededSourceKind.Quest
            : ScannerNeededSourceKind.Hideout;
        mainWindow.NavigateFromScannerNeededSource(new ScannerNeededSourceRow(
            kind,
            usage.TargetId,
            usage.SourceName,
            usage.TargetLevel is { } level ? $"Lv.{level} 업그레이드" : "아이템 요구사항"));
    }

    private UIElement CreateRecipeUsageCard(ScannerItemUsageRow usage)
    {
        var body = new StackPanel();
        var header = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(CreateItemLinkButton(usage.Product, usage.ProductCount, showCount: false, prominent: true));
        header.Children.Add(new TextBlock
        {
            Text = $" ({usage.SourceName} {usage.RequiredLevel}레벨)",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        body.Children.Add(header);
        AddMaterialsRecipeRow(body, usage.Materials);
        return CreateRecipeCard(body);
    }

    private void RenderAcquisitionGroups(IReadOnlyList<ScannerItemAcquisitionRow> acquisitions, ScannerItemSnapshot snapshot)
    {
        if (_acquisitionItems is null)
            return;

        var current = new ScannerItemLink(snapshot.ItemId, snapshot.OfficialName, snapshot.Icon);
        var crafts = acquisitions.Where(row => row.Kind == ScannerItemAcquisitionKind.HideoutCraft).ToArray();
        var barters = acquisitions.Where(row => row.Kind == ScannerItemAcquisitionKind.TraderBarter).ToArray();
        var purchases = acquisitions.Where(row => row.Kind is ScannerItemAcquisitionKind.TraderPurchase or ScannerItemAcquisitionKind.FleaMarket).ToArray();
        var raid = acquisitions.Any(row => row.Kind == ScannerItemAcquisitionKind.Raid);

        if (crafts.Length > 0)
        {
            var items = AddAcquisitionSubsection("제작");
            foreach (var craft in crafts)
                items.Children.Add(CreateAcquisitionRecipeCard(craft, current));
        }

        if (barters.Length > 0)
        {
            var items = AddAcquisitionSubsection("교환");
            foreach (var barter in barters)
                items.Children.Add(CreateAcquisitionRecipeCard(barter, current));
        }

        if (purchases.Length > 0)
        {
            var items = AddAcquisitionSubsection("구매");
            foreach (var purchase in purchases)
                items.Children.Add(CreatePurchaseRow(purchase));
        }

        if (raid)
        {
            var items = AddAcquisitionSubsection("레이드 획득");
            var hasOtherSource = crafts.Length > 0 || barters.Length > 0 || purchases.Length > 0;
            items.Children.Add(new TextBlock
            {
                Text = hasOtherSource ? "레이드 획득 가능" : "레이드에서만 획득 가능",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });
        }
    }

    private StackPanel AddAcquisitionSubsection(string title)
    {
        var items = new StackPanel();
        var container = new StackPanel { Margin = new Thickness(0, _acquisitionItems!.Children.Count == 0 ? 0 : 10, 0, 0) };
        container.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = TryFindResource("TextSecondaryBrush") as Brush,
            Margin = new Thickness(0, 0, 0, 6),
        });
        container.Children.Add(items);
        _acquisitionItems.Children.Add(container);
        return items;
    }

    private UIElement CreateAcquisitionRecipeCard(ScannerItemAcquisitionRow acquisition, ScannerItemLink current)
    {
        var body = new StackPanel();
        var header = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(CreateItemLinkButton(current, acquisition.ProductCount, showCount: false, prominent: true));
        header.Children.Add(new TextBlock
        {
            Text = acquisition.RequiredLevel is { } level
                ? $" ({acquisition.SourceName} {level}레벨)"
                : $" ({acquisition.SourceName})",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        body.Children.Add(header);
        AddMaterialsRecipeRow(body, acquisition.Materials);
        return CreateRecipeCard(body);
    }

    private UIElement CreatePurchaseRow(ScannerItemAcquisitionRow acquisition)
    {
        string text;
        if (acquisition.Kind == ScannerItemAcquisitionKind.FleaMarket)
        {
            text = acquisition.FleaAveragePrice is > 0
                ? $"플리마켓 : {FormatRoubles(acquisition.FleaAveragePrice.Value)}"
                : "플리마켓 : -";
        }
        else
        {
            var source = acquisition.RequiredLevel is { } level
                ? $"{acquisition.SourceName} {level}레벨"
                : acquisition.SourceName;
            text = acquisition.Price is { } price
                ? $"{source} : {FormatCurrency(price, acquisition.CurrencyCode)}"
                : $"{source} : -";
        }

        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 5),
        };
    }

    private Border CreateRecipeCard(UIElement body) => new()
    {
        Background = TryFindResource("BackgroundMediumBrush") as Brush,
        BorderBrush = TryFindResource("BorderBrush") as Brush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(8),
        Margin = new Thickness(0, 0, 0, 7),
        Child = body,
    };

    private void AddMaterialsRecipeRow(StackPanel owner, IReadOnlyList<ScannerItemMaterialRow> materials)
    {
        if (materials.Count == 0)
            return;

        var outer = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        outer.Children.Add(new TextBlock
        {
            Text = "→",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4),
        });
        var wrap = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
        for (var index = 0; index < materials.Count; index++)
        {
            if (index > 0)
            {
                wrap.Children.Add(new TextBlock
                {
                    Text = "+",
                    Margin = new Thickness(3, 0, 3, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                });
            }
            var material = materials[index];
            wrap.Children.Add(CreateItemLinkButton(material.Item, material.Count, showCount: true));
        }
        outer.Children.Add(wrap);
        owner.Children.Add(outer);
    }

    private Button CreateItemLinkButton(
        ScannerItemLink item,
        decimal count,
        bool showCount = true,
        bool prominent = false)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        if (item.Icon is not null)
        {
            content.Children.Add(new Image
            {
                Source = item.Icon,
                Width = prominent ? 30 : 24,
                Height = prominent ? 30 : 24,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        var suffix = showCount ? $" × {FormatCount(count)}" : string.Empty;
        content.Children.Add(new TextBlock
        {
            Text = item.OfficialName + suffix,
            FontWeight = prominent ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var button = new Button
        {
            Tag = item.ItemId,
            Content = content,
            Padding = prominent ? new Thickness(5, 3, 5, 3) : new Thickness(5, 2, 5, 2),
            Margin = new Thickness(0, 0, 0, 4),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
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
        foreach (var items in new[] { _questUsageItems, _hideoutUsageItems, _craftUsageItems, _acquisitionItems })
            items?.Children.Clear();
        foreach (var host in new[] { _questUsageHost, _hideoutUsageHost, _craftUsageHost, _acquisitionHost })
        {
            if (host is not null)
                host.Visibility = Visibility.Collapsed;
        }
    }

    private static string FormatCount(decimal value) =>
        value.ToString(value == decimal.Truncate(value) ? "0" : "0.##", CultureInfo.InvariantCulture);

    private static string FormatCurrency(decimal value, string? code) => (code ?? string.Empty).ToUpperInvariant() switch
    {
        "RUB" => value.ToString("N0", CultureInfo.InvariantCulture) + " ₽",
        "USD" => "$" + value.ToString("N0", CultureInfo.InvariantCulture),
        "EUR" => "€" + value.ToString("N0", CultureInfo.InvariantCulture),
        _ => value.ToString("N0", CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(code) ? string.Empty : $" {code}"),
    };
}
