using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private Grid? _productRootGrid;
    private Border? _productDetailHost;
    private Button? _productDetailToggleButton;
    private bool _productDetailsPrepared;
    private bool _productDetailsExpanded = false;
    private bool _productFavoriteHandlersAttached;

    private void InitializeProductSearchAndDetails()
    {
        if (Content is not Grid root)
            return;

        _productRootGrid ??= root;
        ProductSearchClearButtonBehavior.Attach(ProductSearchBox);

        if (!_productDetailsPrepared)
        {
            _productDetailsPrepared = true;
            PrepareCollapsibleDetailPanel(root);
        }

        AttachProductFavoritePresentation();
        NormalizeProductFavoriteButton();
    }



    private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = ProductSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            ProductSearchPopup.IsOpen = false;
            ProductSearchResults.ItemsSource = null;
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

        ProductSearchResults.ItemsSource = hits;
        ProductSearchPopup.IsOpen = hits.Length > 0;
        if (hits.Length > 0)
            ProductSearchResults.SelectedIndex = 0;
    }

    private void ProductSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ProductSearchPopup.IsOpen = false;
            return;
        }

        if (e.Key == Key.Enter && ProductSearchResults.SelectedItem is AmmoSearchHit hit)
        {
            NavigateToSearchHit(hit.Row);
            e.Handled = true;
        }
    }

    private void ProductSearchResults_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ProductSearchResults.SelectedItem is AmmoSearchHit hit)
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

        ProductSearchPopup.IsOpen = false;
    }

    private void PrepareCollapsibleDetailPanel(Grid root)
    {
        if (root.RowDefinitions.Count < 4 || _productDetailToggleButton is not null)
            return;

        // Canonical XAML owns the visual tree. Runtime code owns state only.
        _productDetailHost = ProductDetailHost;
        _productDetailToggleButton = ProductDetailToggleButton;
        root.RowDefinitions[2].Height = GridLength.Auto;
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
            _productRootGrid.RowDefinitions.Count < 4 ||
            _productDetailHost is null ||
            _productDetailToggleButton is null)
        {
            return;
        }

        var detailRow = _productRootGrid.RowDefinitions[3];
        if (_productDetailsExpanded)
        {
            _productDetailHost.Visibility = Visibility.Visible;
            detailRow.MinHeight = 190;
            detailRow.Height = new GridLength(2, GridUnitType.Star);
            _productDetailToggleButton.Content = "▼";
            _productDetailToggleButton.ToolTip = "상세정보 접기";
        }
        else
        {
            _productDetailHost.Visibility = Visibility.Collapsed;
            detailRow.MinHeight = 0;
            detailRow.Height = new GridLength(0);
            _productDetailToggleButton.Content = "▲";
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
