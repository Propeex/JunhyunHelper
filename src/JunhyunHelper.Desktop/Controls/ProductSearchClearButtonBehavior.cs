using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Controls;

/// <summary>
/// Adds the product-standard clear affordance inside the right edge of an existing
/// search TextBox without duplicating search/filter logic in each page.
/// </summary>
internal static class ProductSearchClearButtonBehavior
{
    private static readonly DependencyProperty AttachedProperty = DependencyProperty.RegisterAttached(
        "ProductSearchClearAttached",
        typeof(bool),
        typeof(ProductSearchClearButtonBehavior),
        new PropertyMetadata(false));


    internal static Button? Attach(TextBox searchBox)
    {
        ArgumentNullException.ThrowIfNull(searchBox);
        if ((bool)searchBox.GetValue(AttachedProperty))
            return null;
        if (searchBox.Parent is not Grid parent)
            return null;

        searchBox.SetValue(AttachedProperty, true);
        var originalPadding = searchBox.Padding;
        searchBox.Padding = new Thickness(
            originalPadding.Left,
            originalPadding.Top,
            Math.Max(originalPadding.Right, 32),
            originalPadding.Bottom);

        // The button is a sibling overlay in the parent Grid, not a child of the
        // TextBox. Mirror every outer TextBox margin so the glyph is centered against
        // the actual input rectangle. Hideout carries its row spacing on SearchBox
        // itself, while the other product searches place spacing on their container;
        // ignoring Top/Bottom previously made Hideout's × appear vertically displaced.
        var clearButton = new Button
        {
            Content = "×",
            Width = 24,
            Height = 24,
            MinWidth = 0,
            MinHeight = 0,
            Padding = new Thickness(0),
            Margin = new Thickness(
                searchBox.Margin.Left,
                searchBox.Margin.Top,
                Math.Max(4, searchBox.Margin.Right + 4),
                searchBox.Margin.Bottom),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = string.IsNullOrEmpty(searchBox.Text) ? Visibility.Collapsed : Visibility.Visible,
            ToolTip = "검색어 지우기",
            Focusable = false,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Foreground = searchBox.TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.LightGray,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
        };
        Grid.SetRow(clearButton, Grid.GetRow(searchBox));
        Grid.SetRowSpan(clearButton, Grid.GetRowSpan(searchBox));
        Grid.SetColumn(clearButton, Grid.GetColumn(searchBox));
        Grid.SetColumnSpan(clearButton, Grid.GetColumnSpan(searchBox));
        Panel.SetZIndex(clearButton, 20);

        searchBox.TextChanged += (_, _) =>
            clearButton.Visibility = string.IsNullOrEmpty(searchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        clearButton.Click += (_, _) =>
        {
            searchBox.Clear();
            searchBox.Focus();
        };

        parent.Children.Add(clearButton);
        return clearButton;
    }
}
