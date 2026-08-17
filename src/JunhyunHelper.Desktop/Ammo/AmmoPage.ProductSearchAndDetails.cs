using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private TextBox? _productSearchBox;
    private Popup? _productSearchPopup;
    private ListBox? _productSearchResults;
    private Grid? _productRootGrid;
    private Border? _productDetailHost;
    private Button? _productDetailToggleButton;
    private bool _productDetailsPrepared;
    private bool _productDetailsExpanded = true;
    private bool _productFavoriteHandlersAttached;

    static AmmoPage()
    {
        EventManager.RegisterClassHandler(
            typeof(AmmoPage),
            LoadedEvent,
            new RoutedEventHandler(ProductLoaded));
    }

    private static void ProductLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is AmmoPage page)
            page.InitializeProductSearchAndDetails();
    }

    private void InitializeProductSearchAndDetails()
    {
        if (Content is not Grid root)
            return;

        _productRootGrid ??= root;
        var header = root.Children
            .OfType<Grid>()
            .FirstOrDefault(element => Grid.GetRow(element) == 0);
        if (header is not null)
        {
            NormalizeProductHeader(header);
            if (_productSearchBox is null)
                CreateProductSearch(header);
        }

        if (!_productDetailsPrepared)
        {
            _productDetailsPrepared = true;
            PrepareCollapsibleDetailPanel(root);
        }

        AttachProductFavoritePresentation();
        NormalizeProductFavoriteButton();
    }

    private void NormalizeProductHeader(Grid header)
    {
        // The labels repeat information already expressed by the controls themselves.
        // Collapse them instead of merely shrinking their text so the header follows
        // the compact search -> caliber -> star -> favorites -> columns rhythm.
        foreach (var label in header.Children.OfType<TextBlock>())
        {
            if (label.Text is "구경" or "즐겨찾기")
                label.Visibility = Visibility.Collapsed;
        }

        CaliberComboBox.Width = 160;
        CaliberComboBox.MinWidth = 160;
        CaliberComboBox.MaxWidth = 160;

        FavoriteCaliberMenuButton.Width = 170;
        FavoriteCaliberMenuButton.MinWidth = 170;
        FavoriteCaliberMenuButton.MaxWidth = 170;

        // Before the product search column is inserted these are the two label lanes.
        // Once search has already been inserted, the labels are still collapsed and
        // these widths are harmless because the named controls own their own width.
        if (_productSearchBox is null && header.ColumnDefinitions.Count >= 8)
        {
            header.ColumnDefinitions[0].Width = new GridLength(0);
            header.ColumnDefinitions[1].Width = new GridLength(160);
            header.ColumnDefinitions[4].Width = new GridLength(0);
            header.ColumnDefinitions[5].Width = new GridLength(170);
        }
    }

    private void CreateProductSearch(Grid header)
    {
        // Search is the primary discovery action, so give it the left-most header lane.
        header.ColumnDefinitions.Insert(0, new ColumnDefinition { Width = new GridLength(300) });
        foreach (UIElement child in header.Children)
            Grid.SetColumn(child, Grid.GetColumn(child) + 1);

        var searchHost = new Grid
        {
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(searchHost, 0);

        _productSearchBox = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "탄약 이름 또는 구경 검색",
        };
        _productSearchBox.TextChanged += ProductSearchBox_TextChanged;
        _productSearchBox.PreviewKeyDown += ProductSearchBox_PreviewKeyDown;
        searchHost.Children.Add(_productSearchBox);
        ProductSearchClearButtonBehavior.Attach(_productSearchBox);
        header.Children.Add(searchHost);

        _productSearchResults = new ListBox
        {
            MinWidth = 288,
            MaxHeight = 360,
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.Black,
            Foreground = TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            BorderThickness = new Thickness(0),
            ItemTemplate = BuildAmmoSearchResultTemplate(),
        };
        _productSearchResults.PreviewMouseLeftButtonUp += ProductSearchResults_PreviewMouseLeftButtonUp;

        var border = new Border
        {
            MinWidth = 288,
            Background = TryFindResource("BackgroundMediumBrush") as Brush ?? Brushes.Black,
            BorderBrush = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            Child = _productSearchResults,
        };

        _productSearchPopup = new Popup
        {
            PlacementTarget = _productSearchBox,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = border,
        };
        header.Children.Add(_productSearchPopup);
    }

    private static DataTemplate BuildAmmoSearchResultTemplate()
    {
#pragma warning disable CS0618 // FrameworkElementFactory is sufficient for this small runtime-owned template.
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        panel.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 3, 4, 3));

        var iconBorder = new FrameworkElementFactory(typeof(Border));
        iconBorder.SetValue(FrameworkElement.WidthProperty, 38.0);
        iconBorder.SetValue(FrameworkElement.HeightProperty, 38.0);
        iconBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        iconBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 9, 0));

        var icon = new FrameworkElementFactory(typeof(Image));
        icon.SetValue(FrameworkElement.WidthProperty, 34.0);
        icon.SetValue(FrameworkElement.HeightProperty, 34.0);
        icon.SetValue(Image.StretchProperty, Stretch.Uniform);
        icon.SetBinding(Image.SourceProperty, new Binding("Row.Icon"));
        iconBorder.AppendChild(icon);
        panel.AppendChild(iconBorder);

        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        name.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        name.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
        name.SetBinding(TextBlock.TextProperty, new Binding(nameof(AmmoSearchHit.Label)));
        panel.AppendChild(name);

        return new DataTemplate(typeof(AmmoSearchHit)) { VisualTree = panel };
#pragma warning restore CS0618
    }

    private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_productSearchBox is null || _productSearchPopup is null || _productSearchResults is null)
            return;

        var query = _productSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            _productSearchPopup.IsOpen = false;
            _productSearchResults.ItemsSource = null;
            return;
        }

        var hits = _allRows
            .Where(row =>
                row.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                row.CaliberLabel.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                row.RawCaliber.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(20)
            .Select(row => new AmmoSearchHit(row, row.Name))
            .ToArray();

        _productSearchResults.ItemsSource = hits;
        _productSearchPopup.IsOpen = hits.Length > 0;
        if (hits.Length > 0)
            _productSearchResults.SelectedIndex = 0;
    }

    private void ProductSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_productSearchPopup is not null)
                _productSearchPopup.IsOpen = false;
            return;
        }

        if (e.Key == Key.Enter && _productSearchResults?.SelectedItem is AmmoSearchHit hit)
        {
            NavigateToSearchHit(hit.Row);
            e.Handled = true;
        }
    }

    private void ProductSearchResults_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_productSearchResults?.SelectedItem is AmmoSearchHit hit)
            NavigateToSearchHit(hit.Row);
    }

    private void NavigateToSearchHit(AmmoRow row)
    {
        if (CaliberComboBox.ItemsSource is IEnumerable<CaliberChoice> choices)
        {
            var caliber = choices.FirstOrDefault(choice =>
                string.Equals(choice.RawCaliber, row.RawCaliber, StringComparison.Ordinal));
            if (caliber is not null)
                CaliberComboBox.SelectedItem = caliber;
        }

        ApplyFilter();
        _selectedRow = row;
        AmmoGrid.SelectedItem = row;
        AmmoGrid.ScrollIntoView(row);
        ShowDetail(row);

        if (_productSearchPopup is not null)
            _productSearchPopup.IsOpen = false;
    }

    private void PrepareCollapsibleDetailPanel(Grid root)
    {
        if (root.RowDefinitions.Count < 5 || _productDetailToggleButton is not null)
            return;

        // Canonical XAML owns the visual tree. Runtime code owns state only.
        _productDetailHost = ProductDetailHost;
        _productDetailToggleButton = ProductDetailToggleButton;
        root.RowDefinitions[3].Height = GridLength.Auto;
        ApplyProductDetailExpansionState();
    }

    private void ProductDetailToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _productDetailsExpanded = !_productDetailsExpanded;
        ApplyProductDetailExpansionState();
    }

    private void ApplyProductDetailExpansionState()
    {
        if (_productRootGrid is null ||
            _productRootGrid.RowDefinitions.Count < 5 ||
            _productDetailHost is null ||
            _productDetailToggleButton is null)
        {
            return;
        }

        var detailRow = _productRootGrid.RowDefinitions[4];
        if (_productDetailsExpanded)
        {
            _productDetailHost.Visibility = Visibility.Visible;
            detailRow.MinHeight = 190;
            detailRow.Height = new GridLength(2, GridUnitType.Star);
            _productDetailToggleButton.Content = "▲  탄약 / 수급 경로 상세정보";
            _productDetailToggleButton.ToolTip = "상세정보 접기";
        }
        else
        {
            _productDetailHost.Visibility = Visibility.Collapsed;
            detailRow.MinHeight = 0;
            detailRow.Height = new GridLength(0);
            _productDetailToggleButton.Content = "▼  탄약 / 수급 경로 상세정보";
            _productDetailToggleButton.ToolTip = "상세정보 펼치기";
        }
    }

    private void AttachProductFavoritePresentation()
    {
        if (_productFavoriteHandlersAttached)
            return;

        _productFavoriteHandlersAttached = true;
        FavoriteCaliberButton.Click += ProductFavoritePresentationChanged;
        CaliberComboBox.SelectionChanged += ProductFavoritePresentationChanged;
    }

    private void ProductFavoritePresentationChanged(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(NormalizeProductFavoriteButton, DispatcherPriority.Loaded);

    private void NormalizeProductFavoriteButton()
    {
        if (FavoriteCaliberButton is null || CaliberComboBox is null)
            return;

        var caliber = (CaliberComboBox.SelectedItem as CaliberChoice)?.RawCaliber;
        var isFavorite = caliber is not null && _favoriteCalibers.Contains(caliber);
        FavoriteCaliberButton.Content = isFavorite ? "★" : "☆";
        FavoriteCaliberButton.Width = 38;
        FavoriteCaliberButton.MinWidth = 38;
        FavoriteCaliberButton.MaxWidth = 38;
        FavoriteCaliberButton.Padding = new Thickness(0);
        FavoriteCaliberButton.FontSize = 16;
        FavoriteCaliberButton.ToolTip = isFavorite ? "즐겨찾기 해제" : "즐겨찾기 추가";
    }

    private sealed record AmmoSearchHit(AmmoRow Row, string Label);
}
