using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Items;

public partial class ItemsPage
{
    private static readonly bool FlexibleLayoutHandlerRegistered = RegisterFlexibleLayoutHandler();
    private bool _flexibleLayoutInitialized;
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
        _flexibleLayoutItemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty,
            typeof(ItemsControl));
        _flexibleLayoutItemsSourceDescriptor?.AddValueChanged(
            FlexibleGroupItems,
            FlexibleLayoutItemsSourceChanged);

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

    private void QueueFlexibleLayoutPolish() =>
        Dispatcher.BeginInvoke(PolishFlexibleRows, DispatcherPriority.Loaded);

    private void PolishFlexibleRows()
    {
        if (_viewMode != ItemViewMode.Flexible || FlexibleGroupItems.Visibility == Visibility.Collapsed)
            return;

        foreach (var button in Descendants<Button>(FlexibleGroupItems))
        {
            if (!IsFlexibleCandidateButton(button))
                continue;

            button.Height = 58;
            button.MinHeight = 58;
            button.MaxHeight = 58;
            button.Padding = new Thickness(8, 6, 8, 6);
            button.VerticalContentAlignment = VerticalAlignment.Center;

            if (button.Content is Grid grid && grid.ColumnDefinitions.Count >= 3)
                grid.ColumnDefinitions[^1].Width = new GridLength(112);

            foreach (var text in Descendants<TextBlock>(button))
            {
                text.TextWrapping = TextWrapping.NoWrap;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                text.VerticalAlignment = VerticalAlignment.Center;
            }

            var name = button.DataContext?.GetType()
                .GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(button.DataContext) as string;
            if (!string.IsNullOrWhiteSpace(name))
                button.ToolTip = name;
        }
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
