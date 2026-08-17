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
    private Expander? _productDetailExpander;
    private Grid? _productRootGrid;
    private UIElement? _productDetailSplitter;
    private bool _productDetailsPrepared;
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
        if (_productSearchBox is null)
        {
            var header = root.Children
                .OfType<Grid>()
                .FirstOrDefault(element => Grid.GetRow(element) == 0);
            if (header is not null)
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

    private void CreateProductSearch(Grid header)
    {
        // Search is the primary discovery action, so give it the left-most header lane
        // instead of appending it after all caliber/favorite controls.
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
        if (root.RowDefinitions.Count < 5 || _productDetailExpander is not null)
            return;

        // Row 4 owns one outer Border whose child is a Grid containing both
        // EmptyDetailText and DetailGrid. The v0.1.8 code incorrectly required the
        // Border.Child itself to be DetailGrid, so this lookup could never succeed.
        var detailHost = root.Children
            .OfType<Border>()
            .FirstOrDefault(element => Grid.GetRow(element) == 4);
        if (detailHost is null)
            return;

        _productDetailSplitter = root.Children
            .OfType<GridSplitter>()
            .FirstOrDefault(element => Grid.GetRow(element) == 3);

        root.Children.Remove(detailHost);
        _productDetailExpander = new Expander
        {
            Header = "탄약 / 수급 경로 상세정보",
            IsExpanded = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Foreground = TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            Content = detailHost,
        };
        _productDetailExpander.Expanded += ProductDetailExpander_StateChanged;
        _productDetailExpander.Collapsed += ProductDetailExpander_StateChanged;
        Grid.SetRow(_productDetailExpander, 4);
        root.Children.Add(_productDetailExpander);
        ApplyProductDetailExpansionState();
    }

    private void ProductDetailExpander_StateChanged(object sender, RoutedEventArgs e) =>
        ApplyProductDetailExpansionState();

    private void ApplyProductDetailExpansionState()
    {
        if (_productRootGrid is null ||
            _productRootGrid.RowDefinitions.Count < 5 ||
            _productDetailExpander is null)
        {
            return;
        }

        var detailRow = _productRootGrid.RowDefinitions[4];
        if (_productDetailExpander.IsExpanded)
        {
            detailRow.MinHeight = 190;
            detailRow.Height = new GridLength(2, GridUnitType.Star);
            if (_productDetailSplitter is not null)
                _productDetailSplitter.Visibility = Visibility.Visible;
        }
        else
        {
            detailRow.MinHeight = 0;
            detailRow.Height = GridLength.Auto;
            if (_productDetailSplitter is not null)
                _productDetailSplitter.Visibility = Visibility.Collapsed;
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
        FavoriteCaliberButton.Padding = new Thickness(0);
        FavoriteCaliberButton.FontSize = 16;
        FavoriteCaliberButton.ToolTip = isFavorite ? "즐겨찾기 해제" : "즐겨찾기 추가";
    }

    private sealed record AmmoSearchHit(AmmoRow Row, string Label);
}
