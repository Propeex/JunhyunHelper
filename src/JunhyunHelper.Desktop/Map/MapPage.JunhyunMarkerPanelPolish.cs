using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private const int JunhyunMarkerPanelPolishMaxImmediateRetries = 4;

    private static readonly bool JunhyunMarkerPanelPolishHandlerRegistered = RegisterJunhyunMarkerPanelPolishHandler();

    private bool _junhyunMarkerPanelPolishApplied;
    private bool _junhyunMarkerPanelPolishScheduled;
    private int _junhyunMarkerPanelPolishRetryCount;
    private ScrollViewer? _junhyunMarkerListViewport;

    private static bool RegisterJunhyunMarkerPanelPolishHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MapPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunMarkerPanelPolishLoaded));
        return true;
    }

    private static void OnJunhyunMarkerPanelPolishLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MapPage page)
            return;

        // Loaded can be routed from a descendant while the class-handler sender is still
        // the MapPage. The old OriginalSource == page guard therefore skipped the product
        // marker viewport on real runs. Do not advance donor construction; queue one
        // lifecycle-safe attempt and let the instance retry at ContextIdle if its XAML
        // overlay content has not settled yet.
        page.ScheduleJunhyunMarkerPanelPolish(DispatcherPriority.Loaded);
    }

    private void ScheduleJunhyunMarkerPanelPolish(DispatcherPriority priority)
    {
        if (_junhyunMarkerPanelPolishApplied || _junhyunMarkerPanelPolishScheduled)
            return;

        _junhyunMarkerPanelPolishScheduled = true;
        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                _junhyunMarkerPanelPolishScheduled = false;
                ApplyJunhyunMarkerPanelPolish();
            }),
            priority);
    }

    private void ApplyJunhyunMarkerPanelPolish()
    {
        if (_junhyunMarkerPanelPolishApplied)
            return;

        // Do not use FrameworkElement.Parent as the activation authority. The transplanted
        // WPF tree can report a transient/non-Panel logical parent while the product-owned
        // MapMarkersOverlay already has the stable child collection we need. Resolve the
        // viewport from that known overlay surface instead.
        if (!TryResolveOrWrapJunhyunMarkerListViewport())
        {
            _junhyunMarkerPanelPolishRetryCount++;
            if (_junhyunMarkerPanelPolishRetryCount <= JunhyunMarkerPanelPolishMaxImmediateRetries)
            {
                ScheduleJunhyunMarkerPanelPolish(DispatcherPriority.ContextIdle);
                return;
            }

            _junhyunMarkerPanelPolishRetryCount = 0;
            if (string.Equals(
                    Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                    "1",
                    StringComparison.Ordinal))
            {
                FailJunhyunMarkerPanelActivationSmoke();
            }

            // Production stays usable if a future donor changes this local XAML shape.
            // A later real Loaded event can start another bounded activation attempt.
            return;
        }

        _junhyunMarkerPanelPolishRetryCount = 0;
        _junhyunMarkerPanelPolishApplied = true;

        PreviewMouseLeftButtonDown += JunhyunMarkerPanel_PreviewMouseLeftButtonDown;
        SizeChanged += JunhyunMarkerPanel_SizeChanged;
        MapMarkersContent.SizeChanged += JunhyunMarkerContent_SizeChanged;
        BtnToggleMapMarkersPanel.Click += JunhyunMarkerPanelToggleButton_Click;

        // v1.8.3 replaces the content-sized viewport synchronization below with the
        // full-panel-body implementation. Activate it here, after the actual Map Loaded
        // lifecycle and only after the viewport insertion/resolution succeeded.
        ActivateProductMarkerPanelBodyLayout();
        Dispatcher.BeginInvoke(SyncProductMarkerPanelBodyLayout, DispatcherPriority.ContextIdle);
    }

    private bool TryResolveOrWrapJunhyunMarkerListViewport()
    {
        if (_junhyunMarkerListViewport is not null)
            return true;

        if (MapMarkersOverlay.Child is not Panel overlayContent)
            return false;

        // If another product layer already supplied the ScrollViewer, adopt it rather
        // than nesting another viewport or relying on MapMarkersContent.Parent.
        foreach (var child in overlayContent.Children.OfType<ScrollViewer>())
        {
            if (!ReferenceEquals(child.Content, MapMarkersContent))
                continue;

            _junhyunMarkerListViewport = child;
            ConfigureJunhyunMarkerListViewport(child);
            return true;
        }

        var index = overlayContent.Children.IndexOf(MapMarkersContent);
        if (index < 0)
            return false;

        var margin = MapMarkersContent.Margin;
        overlayContent.Children.Remove(MapMarkersContent);
        MapMarkersContent.Margin = new Thickness(0);

        var viewport = new ScrollViewer
        {
            Content = MapMarkersContent,
            Margin = margin,
        };
        ConfigureJunhyunMarkerListViewport(viewport);
        _junhyunMarkerListViewport = viewport;
        overlayContent.Children.Insert(index, viewport);
        return true;
    }

    private static void ConfigureJunhyunMarkerListViewport(ScrollViewer viewport)
    {
        viewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        viewport.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        viewport.CanContentScroll = false;
    }

    private void FailJunhyunMarkerPanelActivationSmoke()
    {
        try
        {
            var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
            var logicalParent = MapMarkersContent.Parent?.GetType().FullName ?? "<null>";
            var overlayChild = MapMarkersOverlay.Child?.GetType().FullName ?? "<null>";
            var overlayChildren = MapMarkersOverlay.Child is Panel panel
                ? string.Join(
                    ", ",
                    panel.Children.Cast<UIElement>().Select(child =>
                        child.GetType().FullName +
                        (child is ScrollViewer viewer
                            ? $"(content={viewer.Content?.GetType().FullName ?? "<null>"})"
                            : string.Empty)))
                : "<not-panel>";

            File.WriteAllText(
                diagnostic,
                "Map marker panel activation smoke failed.\n" +
                "The product could not resolve or create the marker checkbox viewport from MapMarkersOverlay.\n" +
                $"MapMarkersContent.Parent={logicalParent}\n" +
                $"MapMarkersOverlay.Child={overlayChild}\n" +
                $"Overlay children={overlayChildren}\n");
        }
        catch
        {
        }

        Environment.Exit(89);
    }

    private void JunhyunMarkerPanelToggleButton_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(SyncJunhyunMarkerPanelViewport, DispatcherPriority.Render);

    private void JunhyunMarkerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MapMarkersContent.Visibility == Visibility.Visible)
            Dispatcher.BeginInvoke(SyncJunhyunMarkerPanelViewport, DispatcherPriority.Render);
    }

    private void JunhyunMarkerContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MapMarkersContent.Visibility == Visibility.Visible)
            Dispatcher.BeginInvoke(SyncJunhyunMarkerPanelViewport, DispatcherPriority.Render);
    }

    private void SyncJunhyunMarkerPanelViewport()
    {
        if (_junhyunMarkerListViewport is null)
            return;

        var expanded = MapMarkersContent.Visibility == Visibility.Visible;
        _junhyunMarkerListViewport.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        if (!expanded)
        {
            _junhyunMarkerListViewport.ClearValue(HeightProperty);
            _junhyunMarkerListViewport.ClearValue(MaxHeightProperty);
            MapMarkersOverlay.ClearValue(HeightProperty);
            return;
        }

        var mapHeight = MapViewerGrid.ActualHeight;
        if (!double.IsFinite(mapHeight) || mapHeight <= 0)
            mapHeight = ActualHeight;
        if (!double.IsFinite(mapHeight) || mapHeight <= 0)
            mapHeight = 590;

        var maximumPanelHeight = Math.Max(220, mapHeight - 32);
        MapMarkersContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var contentHeight = Math.Max(1, MapMarkersContent.DesiredSize.Height);

        var headerHeight = 0d;
        if (BtnToggleMapMarkersPanel.Parent is FrameworkElement header)
        {
            header.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            headerHeight = header.DesiredSize.Height;
        }

        var verticalChrome = MapMarkersOverlay.Padding.Top + MapMarkersOverlay.Padding.Bottom + 8;
        var availableListHeight = Math.Max(120, maximumPanelHeight - headerHeight - verticalChrome);
        var listHeight = Math.Min(contentHeight, availableListHeight);

        _junhyunMarkerListViewport.Height = listHeight;
        _junhyunMarkerListViewport.MaxHeight = listHeight;
        _junhyunMarkerListViewport.VerticalScrollBarVisibility =
            contentHeight <= listHeight + 0.5
                ? ScrollBarVisibility.Hidden
                : ScrollBarVisibility.Auto;

        MapMarkersOverlay.MinHeight = 0;
        MapMarkersOverlay.Height = Math.Min(
            maximumPanelHeight,
            headerHeight + listHeight + verticalChrome);
    }

    private void JunhyunMarkerPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (MapMarkersContent.Visibility != Visibility.Visible ||
            IsWithinJunhyunMarkerPanel(e.OriginalSource as DependencyObject))
        {
            return;
        }

        _isMapMarkersPanelCollapsed = true;
        MapMarkersContent.Visibility = Visibility.Collapsed;
        ApplyMapMarkerPanelChrome(expanded: false);
        SyncProductMarkerPanelBodyLayout();
        // Do not mark the event handled. The click that dismisses the panel must still
        // behave as the user's normal map/control click.
    }

    private bool IsWithinJunhyunMarkerPanel(DependencyObject? source)
    {
        for (var current = source; current is not null; current = JunhyunVisualOrLogicalParent(current))
        {
            if (ReferenceEquals(current, MapMarkersOverlay))
                return true;
        }

        return false;
    }

    private static DependencyObject? JunhyunVisualOrLogicalParent(DependencyObject current)
    {
        if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(current);

        return LogicalTreeHelper.GetParent(current);
    }
}
