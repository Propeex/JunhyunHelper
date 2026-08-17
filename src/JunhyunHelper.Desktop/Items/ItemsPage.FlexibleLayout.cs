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

            // The normal item list is the product reference: 68px row, 52px icon lane,
            // 44px icon. Keep every flexible candidate on the same visual rhythm.
            button.Height = 68;
            button.MinHeight = 68;
            button.MaxHeight = 68;
            button.Padding = new Thickness(10, 7, 10, 7);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            button.VerticalContentAlignment = VerticalAlignment.Center;

            if (button.Content is Grid grid && grid.ColumnDefinitions.Count >= 3)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(52);
                grid.ColumnDefinitions[^1].Width = new GridLength(118);
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            }

            foreach (var image in Descendants<Image>(button))
            {
                image.Width = 44;
                image.Height = 44;
            }

            foreach (var text in Descendants<TextBlock>(button))
            {
                text.TextWrapping = TextWrapping.NoWrap;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
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
