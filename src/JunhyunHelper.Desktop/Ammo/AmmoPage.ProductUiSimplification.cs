using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private void ApplyProductUiSimplification()
    {
        // v1.7.13: the summary line duplicates information already visible in the table.
        SummaryText.Visibility = Visibility.Collapsed;
        if (_productRootGrid is { RowDefinitions.Count: > 1 })
            _productRootGrid.RowDefinitions[1].Height = new GridLength(0);

        // A new application session always starts with the detail panel collapsed.
        _productDetailsExpanded = false;
        ApplyProductDetailExpansionState();

        if (CaliberComboBox.Parent is not Grid header)
            return;

        // Product order: caliber -> favorite toggle -> favorite selection -> search,
        // with the displayed-columns menu pinned to the right edge.
        header.ColumnDefinitions.Clear();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(CaliberComboBox, 0);
        Grid.SetColumn(FavoriteCaliberButton, 1);
        Grid.SetColumn(FavoriteCaliberMenuButton, 2);
        Grid.SetColumn(FavoriteCaliberPopup, 2);

        // The visible favorites selector is created at runtime. Keep it in the same
        // product slot as the legacy selector instead of leaving it in the column it
        // occupied before the search toolbar was rebuilt. Otherwise it can overlap
        // and hide the displayed-columns button at the right edge.
        if (_productFavoriteCaliberComboBox is not null)
        {
            Grid.SetRow(_productFavoriteCaliberComboBox, 0);
            Grid.SetColumn(_productFavoriteCaliberComboBox, 2);
            Grid.SetColumnSpan(_productFavoriteCaliberComboBox, 1);
            _productFavoriteCaliberComboBox.Margin = new Thickness(0);
            _productFavoriteCaliberComboBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        Grid.SetColumn(ColumnMenuButton, 6);
        Grid.SetColumn(ColumnMenuPopup, 6);
        ColumnMenuButton.Visibility = Visibility.Visible;
        ColumnMenuButton.IsHitTestVisible = true;

        FavoriteCaliberButton.Margin = new Thickness(8, 0, 8, 0);
        FavoriteCaliberMenuButton.Margin = new Thickness(0);
        ColumnMenuButton.HorizontalAlignment = HorizontalAlignment.Right;

        if (_productSearchBox?.Parent is Grid searchHost)
        {
            Grid.SetColumn(searchHost, 4);
            searchHost.Margin = new Thickness(0);
        }

        if (_productSearchPopup is not null)
            Grid.SetColumn(_productSearchPopup, 4);
    }
}
