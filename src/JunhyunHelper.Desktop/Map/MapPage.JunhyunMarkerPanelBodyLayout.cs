using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _productMarkerPanelBodyLayoutActivated;
    private bool _productMarkerPanelBodyLayoutSyncing;
    private bool _productMarkerPanelBodySmokeCompleted;

    private void ActivateProductMarkerPanelBodyLayout()
    {
        if (_productMarkerPanelBodyLayoutActivated)
            return;

        if (_junhyunMarkerListViewport is null)
            throw new InvalidOperationException("Map marker checkbox viewport was not created before product body activation.");

        _productMarkerPanelBodyLayoutActivated = true;

        // Replace the old content-sized synchronization handlers. Activation happens
        // after the real Map Loaded event, so none of this runs while the pinned donor
        // constructor is still preparing map/floor state.
        SizeChanged -= JunhyunMarkerPanel_SizeChanged;
        MapMarkersContent.SizeChanged -= JunhyunMarkerContent_SizeChanged;
        BtnToggleMapMarkersPanel.Click -= JunhyunMarkerPanelToggleButton_Click;

        SizeChanged += ProductMarkerPanelBody_SizeChanged;
        MapMarkersContent.SizeChanged += ProductMarkerPanelBodyContent_SizeChanged;
        BtnToggleMapMarkersPanel.Click += ProductMarkerPanelBodyToggleButton_Click;

        _junhyunMarkerListViewport.VerticalAlignment = VerticalAlignment.Stretch;
        _junhyunMarkerListViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Dispatcher.BeginInvoke(SyncProductMarkerPanelBodyLayout, DispatcherPriority.ContextIdle);

        if (string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
            Dispatcher.BeginInvoke(VerifyProductMarkerPanelBodySmoke, DispatcherPriority.ContextIdle);
    }

    private void ProductMarkerPanelBodyToggleButton_Click(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(SyncProductMarkerPanelBodyLayout, DispatcherPriority.ContextIdle);

    private void ProductMarkerPanelBody_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MapMarkersContent.Visibility == Visibility.Visible)
            Dispatcher.BeginInvoke(SyncProductMarkerPanelBodyLayout, DispatcherPriority.ContextIdle);
    }

    private void ProductMarkerPanelBodyContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MapMarkersContent.Visibility == Visibility.Visible && !_productMarkerPanelBodyLayoutSyncing)
            Dispatcher.BeginInvoke(SyncProductMarkerPanelBodyLayout, DispatcherPriority.ContextIdle);
    }

    private void SyncProductMarkerPanelBodyLayout()
    {
        if (_junhyunMarkerListViewport is null || _productMarkerPanelBodyLayoutSyncing)
            return;

        _productMarkerPanelBodyLayoutSyncing = true;
        try
        {
            var expanded = MapMarkersContent.Visibility == Visibility.Visible;
            _junhyunMarkerListViewport.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            if (!expanded)
            {
                _junhyunMarkerListViewport.ClearValue(HeightProperty);
                _junhyunMarkerListViewport.ClearValue(MaxHeightProperty);
                MapMarkersOverlay.ClearValue(HeightProperty);
                MapMarkersOverlay.ClearValue(MaxHeightProperty);
                return;
            }

            var mapHeight = MapViewerGrid.ActualHeight;
            if (!double.IsFinite(mapHeight) || mapHeight <= 0)
                mapHeight = ActualHeight;
            if (!double.IsFinite(mapHeight) || mapHeight <= 0)
                mapHeight = 590;

            // The expanded panel is a viewport, not a content-sized popup. v1.11.2
            // measured a temporarily incomplete DesiredSize and could freeze a tall
            // window to a short panel, clipping the late/reparented extraction rows.
            // Always consume the available map height; ScrollViewer alone decides whether
            // the complete marker content genuinely needs vertical scrolling.
            var maximumPanelHeight = Math.Max(120, mapHeight - 16);

            var headerHeight = 0d;
            if (BtnToggleMapMarkersPanel.Parent is FrameworkElement header)
            {
                header.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                headerHeight = Math.Max(header.ActualHeight, header.DesiredSize.Height);
            }

            var verticalChrome = MapMarkersOverlay.Padding.Top + MapMarkersOverlay.Padding.Bottom + 8;
            var panelHeight = maximumPanelHeight;
            var listHeight = Math.Max(1, panelHeight - headerHeight - verticalChrome);

            _junhyunMarkerListViewport.Height = listHeight;
            _junhyunMarkerListViewport.MaxHeight = listHeight;
            _junhyunMarkerListViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            MapMarkersOverlay.MinHeight = 0;
            MapMarkersOverlay.MaxHeight = maximumPanelHeight;
            MapMarkersOverlay.Height = panelHeight;
        }
        finally
        {
            _productMarkerPanelBodyLayoutSyncing = false;
        }
    }

    private void VerifyProductMarkerPanelBodySmoke()
    {
        if (_productMarkerPanelBodySmokeCompleted)
            return;
        _productMarkerPanelBodySmokeCompleted = true;

        var originalVisibility = MapMarkersContent.Visibility;
        var originalCollapsed = _isMapMarkersPanelCollapsed;
        try
        {
            MapMarkersContent.Visibility = Visibility.Visible;
            _isMapMarkersPanelCollapsed = false;
            ApplyMapMarkerPanelChrome(expanded: true);
            SyncProductMarkerPanelBodyLayout();
            UpdateLayout();

            if (_junhyunMarkerListViewport is null || !_productMarkerPanelBodyLayoutActivated)
                throw new InvalidOperationException("Map marker body layout was not activated in the published UI.");

            var mapHeight = MapViewerGrid.ActualHeight;
            if (!double.IsFinite(mapHeight) || mapHeight <= 0)
                mapHeight = ActualHeight;
            if (!double.IsFinite(mapHeight) || mapHeight <= 0)
                mapHeight = 590;
            var expectedPanelHeight = Math.Max(120, mapHeight - 16);

            if (!double.IsFinite(MapMarkersOverlay.Height) ||
                Math.Abs(MapMarkersOverlay.Height - expectedPanelHeight) > 1.0)
            {
                throw new InvalidOperationException(
                    $"Expanded map marker panel is not using the available map height. " +
                    $"panel={MapMarkersOverlay.Height:0.##}, expected={expectedPanelHeight:0.##}.");
            }

            var headerHeight = BtnToggleMapMarkersPanel.Parent is FrameworkElement header
                ? Math.Max(header.ActualHeight, header.DesiredSize.Height)
                : 0d;
            var verticalChrome = MapMarkersOverlay.Padding.Top + MapMarkersOverlay.Padding.Bottom + 8;
            var expectedBodyHeight = Math.Max(1, MapMarkersOverlay.Height - headerHeight - verticalChrome);

            if (!double.IsFinite(_junhyunMarkerListViewport.Height) ||
                Math.Abs(_junhyunMarkerListViewport.Height - expectedBodyHeight) > 1.0)
            {
                throw new InvalidOperationException(
                    $"Map marker checkbox viewport does not fill the panel body. viewport={_junhyunMarkerListViewport.Height:0.##}, expected={expectedBodyHeight:0.##}.");
            }

            if (_junhyunMarkerListViewport.VerticalScrollBarVisibility != ScrollBarVisibility.Auto)
                throw new InvalidOperationException("Map marker viewport is not using automatic rendered overflow handling.");

            // Verify the scrollbar that WPF actually rendered, not an earlier content-size
            // estimate. If all marker rows fit in the full-height body there must be no
            // visible scrollbar; if they genuinely overflow, Auto must expose one.
            var hasRenderedOverflow = _junhyunMarkerListViewport.ScrollableHeight > 0.5;
            var scrollbarIsVisible =
                _junhyunMarkerListViewport.ComputedVerticalScrollBarVisibility == Visibility.Visible;
            if (hasRenderedOverflow != scrollbarIsVisible)
            {
                throw new InvalidOperationException(
                    $"Map marker rendered scrollbar state is inconsistent. scrollable={_junhyunMarkerListViewport.ScrollableHeight:0.##}, " +
                    $"viewport={_junhyunMarkerListViewport.ViewportHeight:0.##}, extent={_junhyunMarkerListViewport.ExtentHeight:0.##}, " +
                    $"computed={_junhyunMarkerListViewport.ComputedVerticalScrollBarVisibility}.");
            }

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-map-marker-body-smoke-success.txt");
            File.WriteAllText(
                marker,
                $"marker-panel-uses-available-height=ok\nmarker-list-fills-panel-body=ok\nscrollbar-only-on-real-overflow=ok\n" +
                $"panel={MapMarkersOverlay.Height:0.##}\nviewport={_junhyunMarkerListViewport.ViewportHeight:0.##}\nextent={_junhyunMarkerListViewport.ExtentHeight:0.##}\n");
        }
        catch (Exception exception)
        {
            try
            {
                var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                File.WriteAllText(diagnostic, "Map marker panel body smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
        finally
        {
            _isMapMarkersPanelCollapsed = originalCollapsed;
            MapMarkersContent.Visibility = originalVisibility;
            ApplyMapMarkerPanelChrome(originalVisibility == Visibility.Visible);
            SyncProductMarkerPanelBodyLayout();
        }
    }
}
