using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private static readonly bool HeaderStatusPolishHandlerRegistered = RegisterHeaderStatusPolishHandler();

    private bool _headerStatusPolishApplied;
    private Ellipse? _itemsCleanupIndicator;
    private DependencyPropertyDescriptor? _statusTextDescriptor;

    private static bool RegisterHeaderStatusPolishHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnHeaderStatusPolishLoaded));
        return true;
    }

    private static void OnHeaderStatusPolishLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || !ReferenceEquals(e.OriginalSource, window))
            return;

        window.Dispatcher.BeginInvoke(window.ApplyHeaderStatusPolish, DispatcherPriority.Loaded);
    }

    private void ApplyHeaderStatusPolish()
    {
        if (_headerStatusPolishApplied)
            return;
        _headerStatusPolishApplied = true;

        // The header status lane is product identity only. Update/download progress keeps
        // using the dedicated progress overlay; transient internal status can continue to
        // drive lifecycle code without competing visually with the version label.
        StatusText.Visibility = Visibility.Collapsed;
        StatusText.IsHitTestVisible = false;
        if (StatusText.Parent is Grid statusHost && statusHost.ColumnDefinitions.Count > 1)
            statusHost.ColumnDefinitions[1].Width = new GridLength(0);
        VersionText.Margin = new Thickness(0);

        CreateItemsCleanupIndicator();
        _statusTextDescriptor = DependencyPropertyDescriptor.FromProperty(
            TextBlock.TextProperty,
            typeof(TextBlock));
        _statusTextDescriptor?.AddValueChanged(StatusText, StatusText_ValueChanged);
        RefreshItemsCleanupIndicator();
    }

    private void CreateItemsCleanupIndicator()
    {
        if (_itemsCleanupIndicator is not null || ItemsTabButton.Parent is not Grid header)
            return;

        var index = header.Children.IndexOf(ItemsTabButton);
        var column = Grid.GetColumn(ItemsTabButton);
        var row = Grid.GetRow(ItemsTabButton);
        var columnSpan = Grid.GetColumnSpan(ItemsTabButton);
        var rowSpan = Grid.GetRowSpan(ItemsTabButton);
        var margin = ItemsTabButton.Margin;
        var horizontalAlignment = ItemsTabButton.HorizontalAlignment;
        var verticalAlignment = ItemsTabButton.VerticalAlignment;

        header.Children.Remove(ItemsTabButton);
        ItemsTabButton.Margin = new Thickness(0);
        ItemsTabButton.HorizontalAlignment = HorizontalAlignment.Stretch;
        ItemsTabButton.VerticalAlignment = VerticalAlignment.Stretch;

        var host = new Grid
        {
            Margin = margin,
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment,
        };
        Grid.SetColumn(host, column);
        Grid.SetRow(host, row);
        Grid.SetColumnSpan(host, columnSpan);
        Grid.SetRowSpan(host, rowSpan);
        header.Children.Insert(Math.Clamp(index, 0, header.Children.Count), host);
        host.Children.Add(ItemsTabButton);

        _itemsCleanupIndicator = new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 4, 0),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true,
        };
        Panel.SetZIndex(_itemsCleanupIndicator, 10);
        host.Children.Add(_itemsCleanupIndicator);
    }

    private void StatusText_ValueChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(RefreshItemsCleanupIndicator, DispatcherPriority.DataBind);

    private void RefreshItemsCleanupIndicator()
    {
        if (_itemsCleanupIndicator is null)
            return;

        _itemsCleanupIndicator.Visibility =
            (_activeItemsWorkspace?.Plan.CleanupItems.Count ?? 0) > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
}