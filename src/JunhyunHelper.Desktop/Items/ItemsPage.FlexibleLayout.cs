using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    private const string FlexibleCandidateLayoutMarker = "JunhyunHelper.FlexibleCandidateLayout.V2";
    private static readonly bool FlexibleLayoutHandlerRegistered = RegisterFlexibleLayoutHandler();
    private bool _flexibleLayoutInitialized;
    private bool _flexibleLayoutQueued;
    private DependencyPropertyDescriptor? _flexibleLayoutItemsSourceDescriptor;

    private static bool RegisterFlexibleLayoutHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ItemsPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnFlexibleLayoutPageLoaded));
        return true;
    }

    private static void OnFlexibleLayoutPageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsPage page)
            page.InitializeFlexibleLayoutPolish();
    }

    private void InitializeFlexibleLayoutPolish()
    {
        if (_flexibleLayoutInitialized)
            return;
        _flexibleLayoutInitialized = true;

        FlexibleGroupItems.ItemContainerGenerator.StatusChanged += FlexibleContainerGenerator_StatusChanged;
        FlexibleGroupItems.LayoutUpdated += FlexibleGroupItems_LayoutUpdated;
        _flexibleLayoutItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty,
            typeof(ItemsControl));
        _flexibleLayoutItemsSourceDescriptor?.AddValueChanged(
            FlexibleGroupItems,
            FlexibleLayoutItemsSourceChanged);

        QueueFlexibleLayoutPolish();
    }

    private void FlexibleGroupItems_LayoutUpdated(object? sender, EventArgs e)
    {
        if (_viewMode == ItemViewMode.Flexible && FlexibleGroupItems.Visibility == Visibility.Visible)
            QueueFlexibleLayoutPolish();
    }

    private void FlexibleLayoutItemsSourceChanged(object? sender, EventArgs e) =>
        QueueFlexibleLayoutPolish();

    private void FlexibleContainerGenerator_StatusChanged(object? sender, EventArgs e)
    {
        if (FlexibleGroupItems.ItemContainerGenerator.Status ==
            System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            QueueFlexibleLayoutPolish();
        }
    }

    private void QueueFlexibleLayoutPolish()
    {
        if (_flexibleLayoutQueued)
            return;

        _flexibleLayoutQueued = true;
        Dispatcher.BeginInvoke(
            () =>
            {
                _flexibleLayoutQueued = false;
                PolishFlexibleRows();
            },
            DispatcherPriority.ContextIdle);
    }

    private void PolishFlexibleRows()
    {
        if (_viewMode != ItemViewMode.Flexible || FlexibleGroupItems.Visibility == Visibility.Collapsed)
            return;

        foreach (var button in Descendants<Button>(FlexibleGroupItems))
        {
            if (!IsFlexibleCandidateButton(button))
                continue;

            button.Height = 68;
            button.MinHeight = 68;
            button.MaxHeight = 68;
            button.Margin = new Thickness(4, 3, 4, 3);
            button.Padding = new Thickness(10, 6, 10, 6);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.VerticalContentAlignment = VerticalAlignment.Center;

            // v0.1.9 enlarged only the Image to 44px while the original XAML still
            // enclosed it in a 36px Border. That clipped the icon and left alignment
            // dependent on the old template. Replace the realized content once with
            // the same structural rhythm used by the normal item list instead.
            if (button.Content is not Grid { Tag: FlexibleCandidateLayoutMarker })
                button.Content = BuildFlexibleCandidateContent();

            var name = button.DataContext?.GetType()
                .GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(button.DataContext) as string;
            if (!string.IsNullOrWhiteSpace(name))
                button.ToolTip = name;
        }
    }

    private static Grid BuildFlexibleCandidateContent()
    {
        var grid = new Grid
        {
            Tag = FlexibleCandidateLayoutMarker,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

        var iconFrame = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 72)),
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 24)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var icon = new Image
        {
            Width = 38,
            Height = 38,
            Margin = new Thickness(3),
            Stretch = Stretch.Uniform,
        };
        icon.SetBinding(Image.SourceProperty, new Binding("Icon"));
        iconFrame.Child = icon;
        Grid.SetColumn(iconFrame, 0);
        grid.Children.Add(iconFrame);

        var identity = new StackPanel
        {
            Margin = new Thickness(8, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var name = new TextBlock
        {
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        name.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        identity.Children.Add(name);

        var category = new TextBlock
        {
            Margin = new Thickness(0, 2, 0, 0),
            Opacity = 0.62,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        category.SetBinding(TextBlock.TextProperty, new Binding("Category"));
        identity.Children.Add(category);
        Grid.SetColumn(identity, 1);
        grid.Children.Add(identity);

        var fir = BuildOwnedText("OwnedFir", "인레이드 {0}");
        Grid.SetColumn(fir, 2);
        grid.Children.Add(fir);

        var normal = BuildOwnedText("OwnedNonFir", "일반 {0}");
        Grid.SetColumn(normal, 3);
        grid.Children.Add(normal);

        return grid;
    }

    private static TextBlock BuildOwnedText(string propertyName, string format)
    {
        var text = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextAlignment = TextAlignment.Right,
            TextWrapping = TextWrapping.NoWrap,
            Opacity = 0.72,
        };
        text.SetBinding(TextBlock.TextProperty, new Binding(propertyName) { StringFormat = format });
        return text;
    }

    private static bool IsFlexibleCandidateButton(Button button)
    {
        if (button.DataContext is null || button.Tag is not string)
            return false;

        return button.DataContext.GetType().GetProperty(
            "FlexibleOwnedText",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                yield return match;

            foreach (var descendant in Descendants<T>(child))
                yield return descendant;
        }
    }
}
