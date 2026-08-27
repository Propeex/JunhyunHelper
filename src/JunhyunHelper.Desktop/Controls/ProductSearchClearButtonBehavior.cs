using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Desktop.Hideout;
using JunhyunHelper.Desktop.Items;
using JunhyunHelper.Desktop.Quests;

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

    [ModuleInitializer]
    internal static void Register()
    {
        EventManager.RegisterClassHandler(typeof(QuestPage), FrameworkElement.LoadedEvent, new RoutedEventHandler(PageLoaded));
        EventManager.RegisterClassHandler(typeof(HideoutPage), FrameworkElement.LoadedEvent, new RoutedEventHandler(PageLoaded));
        EventManager.RegisterClassHandler(typeof(ItemsPage), FrameworkElement.LoadedEvent, new RoutedEventHandler(PageLoaded));
    }

    private static void PageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement page && page.FindName("SearchBox") is TextBox searchBox)
            Attach(searchBox);
    }

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

        var clearButton = new Button
        {
            Content = "×",
            Width = 24,
            Height = 24,
            MinWidth = 0,
            MinHeight = 0,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, Math.Max(4, searchBox.Margin.Right + 4), 0),
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
