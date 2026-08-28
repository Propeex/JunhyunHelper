using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
    private Border? _barterUsageHost;
    private StackPanel? _barterUsageItems;
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
        (_barterUsageHost, _barterUsageItems) = CreateRelationshipSection("교환 재료 사용처");
        (_acquisitionHost, _acquisitionItems) = CreateRelationshipSection("수급처");

        var neededIndex = _neededSourcesHost is null ? SelectedItemPanel.Children.Count : SelectedItemPanel.Children.IndexOf(_neededSourcesHost);
        if (neededIndex < 0) neededIndex = SelectedItemPanel.Children.Count;
        SelectedItemPanel.Children.Insert(neededIndex, _questUsageHost);
        SelectedItemPanel.Children.Insert(neededIndex + 1, _hideoutUsageHost);
        SelectedItemPanel.Children.Insert(neededIndex + 2, _craftUsageHost);
        SelectedItemPanel.Children.Insert(neededIndex + 3, _barterUsageHost);

        var acquisitionIndex = _neededSourcesHost is null
            ? SelectedItemPanel.Children.Count
            : SelectedItemPanel.Children.IndexOf(_neededSourcesHost) + 1;
        SelectedItemPanel.Children.Insert(acquisitionIndex, _acquisitionHost);
    }

    private (Border Host, StackPanel Items) CreateRelationshipSection(string title, double topMargin = 12)
    {
        var items = new StackPanel();
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
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
        RenderBasicInfo(details.Basic);
        RenderItemRelationships(details.Relationships);
        RefreshNeededSources(details.Snapshot.ItemId);
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
        finally { _suppressSearchRefresh = false; }

        SearchResultsPopup.IsOpen = false;
        RenderSearchDetails(details);
        RenderProductItemExtensions(details);
    }

    private void RefreshProductItemExtensions(string itemId)
    {
        var details = _coordinator?.GetSearchItemDetails(itemId);
        if (details is not null)
            RenderProductItemExtensions(details);
    }

    private void RenderBasicInfo(ScannerItemBasicDetails basic)
    {
        if (_basicInfoHost is null || _basicInfoItems is null)
            return;
        _basicInfoItems.Children.Clear();
        AddBasicRow("종류", basic.TypeName);
        AddBasicRow("크기", basic.Width > 0 && basic.Height > 0 ? $"{basic.Width} × {basic.Height}" : "정보 없음");
        AddBasicRow("무게", basic.WeightKg is { } weight ? $"{weight:0.###} kg" : "정보 없음");
        AddBasicRow("플리마켓 거래", basic.FleaTradable switch { true => "가능", false => "불가", _ => "정보 없음" });
        AddBasicRow("기본 가격", basic.BasePrice is > 0 ? FormatRoubles(basic.BasePrice.Value) : "정보 없음");
        _basicInfoHost.Visibility = Visibility.Visible;
    }

    private void AddBasicRow(string label, string value)
    {
        if (_basicInfoItems is null) return;
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text = label, Foreground = TryFindResource("TextSecondaryBrush") as Brush });
        var valueText = new TextBlock { Text = value, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(valueText, 1);
        grid.Children.Add(valueText);
        _basicInfoItems.Children.Add(grid);
    }

    private void RenderItemRelationships(ScannerItemRelationshipDetails? relationships)
    {
        if (_questUsageHost is null || _questUsageItems is null || _hideoutUsageHost is null || _hideoutUsageItems is null ||
            _craftUsageHost is null || _craftUsageItems is null || _barterUsageHost is null || _barterUsageItems is null ||
            _acquisitionHost is null || _acquisitionItems is null) return;

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

        foreach (var usage in relationships.QuestUsages) _questUsageItems.Children.Add(CreateRequirementUsageRow(usage));
        foreach (var usage in relationships.HideoutUsages) _hideoutUsageItems.Children.Add(CreateRequirementUsageRow(usage));
        foreach (var usage in relationships.CraftUsages) _craftUsageItems.Children.Add(CreateUsageRow(usage));
        foreach (var usage in relationships.BarterUsages) _barterUsageItems.Children.Add(CreateUsageRow(usage));
        foreach (var acquisition in relationships.Acquisitions) _acquisitionItems.Children.Add(CreateAcquisitionRow(acquisition));

        _questUsageHost.Visibility = relationships.QuestUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _hideoutUsageHost.Visibility = relationships.HideoutUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _craftUsageHost.Visibility = relationships.CraftUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _barterUsageHost.Visibility = relationships.BarterUsages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _acquisitionHost.Visibility = relationships.Acquisitions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private UIElement CreateRequirementUsageRow(ScannerItemRequirementUsageRow usage)
    {
        var detail = usage.Kind == ScannerItemRequirementUsageKind.Hideout && usage.TargetLevel is { } level
            ? $"Lv.{level} 업그레이드 · {usage.Count:N0}개"
            : $"{usage.Count:N0}개";
        if (usage.FoundInRaid) detail += " · FIR 필요";
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = usage.SourceName, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        stack.Children.Add(new TextBlock { Text = detail, FontSize = 11, Foreground = TryFindResource("TextSecondaryBrush") as Brush, Margin = new Thickness(0, 2, 0, 0) });
        var button = new Button { Tag = usage, Content = stack, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(9, 7, 9, 7), Margin = new Thickness(0, 0, 0, 6) };
        button.Click += RequirementUsageButton_Click;
        return button;
    }

    private void RequirementUsageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ScannerItemRequirementUsageRow usage } || Window.GetWindow(this) is not MainWindow mainWindow) return;
        var kind = usage.Kind == ScannerItemRequirementUsageKind.Quest ? ScannerNeededSourceKind.Quest : ScannerNeededSourceKind.Hideout;
        mainWindow.NavigateFromScannerNeededSource(new ScannerNeededSourceRow(kind, usage.TargetId, usage.SourceName,
            usage.TargetLevel is { } level ? $"Lv.{level} 업그레이드" : "아이템 요구사항"));
    }

    private UIElement CreateUsageRow(ScannerItemUsageRow usage)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 9) };
        var top = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        top.Children.Add(new TextBlock { Text = $"{usage.SourceName} {usage.RequiredLevel}레벨  →  ", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
        top.Children.Add(CreateItemLinkButton(usage.Product, usage.ProductCount));
        row.Children.Add(top);
        AddMaterialsRow(row, usage.Materials);
        return row;
    }

    private UIElement CreateAcquisitionRow(ScannerItemAcquisitionRow acquisition)
    {
        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        row.Children.Add(new TextBlock
        {
            Text = acquisition.RequiredLevel is { } level ? $"{acquisition.SourceName} {level}레벨" : acquisition.SourceName,
            FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
        });
        var detail = BuildAcquisitionDetail(acquisition);
        if (!string.IsNullOrWhiteSpace(detail))
            row.Children.Add(new TextBlock { Text = detail, Foreground = TryFindResource("TextSecondaryBrush") as Brush, FontSize = 11, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap });
        AddMaterialsRow(row, acquisition.Materials);
        return row;
    }

    private string BuildAcquisitionDetail(ScannerItemAcquisitionRow acquisition)
    {
        var parts = new List<string>();
        if (acquisition.Price is { } price) parts.Add($"가격 {FormatCurrency(price, acquisition.CurrencyCode)}");
        if (acquisition.ProductCount != 1m) parts.Add($"결과 ×{FormatCount(acquisition.ProductCount)}");
        if (acquisition.BuyLimit is { } limit) parts.Add($"구매 제한 {limit:N0}개");
        if (!string.IsNullOrWhiteSpace(acquisition.RefreshTime)) parts.Add($"재고 갱신 {FormatRefreshTime(acquisition.RefreshTime)}");
        if (acquisition.DurationSeconds is { } duration) parts.Add($"제작 시간 {FormatDuration(duration)}");
        if (acquisition.FleaAveragePrice is { } flea) parts.Add($"평균가 {FormatRoubles(flea)}");
        return string.Join(" · ", parts);
    }

    private void AddMaterialsRow(StackPanel owner, IReadOnlyList<ScannerItemMaterialRow> materials)
    {
        if (materials.Count == 0) return;
        var materialRow = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
        materialRow.Children.Add(new TextBlock { Text = "재료  ", Foreground = TryFindResource("TextSecondaryBrush") as Brush, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 });
        foreach (var material in materials) materialRow.Children.Add(CreateItemLinkButton(material.Item, material.Count, material.IsTool));
        owner.Children.Add(materialRow);
    }

    private Button CreateItemLinkButton(ScannerItemLink item, decimal count, bool isTool = false)
    {
        var suffix = count == 1m ? string.Empty : $" ×{FormatCount(count)}";
        if (isTool) suffix += " · 도구";
        var button = new Button { Tag = item.ItemId, Content = item.OfficialName + suffix, Padding = new Thickness(7, 3, 7, 3), Margin = new Thickness(0, 0, 5, 4), FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        button.Click += RelationshipItemButton_Click;
        return button;
    }

    private void RelationshipItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string itemId } && !string.IsNullOrWhiteSpace(itemId)) SelectSearchItemById(itemId);
    }

    private void ClearItemRelationshipPresentation()
    {
        foreach (var items in new[] { _questUsageItems, _hideoutUsageItems, _craftUsageItems, _barterUsageItems, _acquisitionItems }) items?.Children.Clear();
        foreach (var host in new[] { _questUsageHost, _hideoutUsageHost, _craftUsageHost, _barterUsageHost, _acquisitionHost }) if (host is not null) host.Visibility = Visibility.Collapsed;
    }

    private static string FormatCount(decimal value) => value.ToString(value == decimal.Truncate(value) ? "0" : "0.##", CultureInfo.InvariantCulture);
    private static string FormatCurrency(decimal value, string? code) => (code ?? string.Empty).ToUpperInvariant() switch
    {
        "RUB" => value.ToString("N0", CultureInfo.InvariantCulture) + "₽",
        "USD" => "$" + value.ToString("N0", CultureInfo.InvariantCulture),
        "EUR" => "€" + value.ToString("N0", CultureInfo.InvariantCulture),
        _ => value.ToString("N0", CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(code) ? string.Empty : $" {code}"),
    };
    private static string FormatRefreshTime(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)
        ? timestamp.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.CurrentCulture)
        : value;
    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
        if (duration.TotalHours >= 1) return $"{(int)duration.TotalHours}시간 {duration.Minutes}분";
        if (duration.TotalMinutes >= 1) return $"{duration.Minutes}분 {duration.Seconds}초";
        return $"{duration.Seconds}초";
    }
}
