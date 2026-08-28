using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _productMarkerPanelBodyLayoutActivated;
    private bool _productMarkerPanelBodyLayoutSyncing;
    private bool _productMarkerPanelBodySmokeCompleted;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        EnsureProductMarkerPanelBodyLayoutActivation();
    }

    internal void EnsureProductMarkerPanelBodyLayoutActivation()
    {
        if (_productMarkerPanelBodyLayoutActivated)
            return;

        // The JunhyunHelper Map surface is a product-owned delta over the pinned donor.
        // Apply it immediately after XAML initialization instead of relying on a class-level
        // Loaded handler. This guarantees the marker launcher, viewport wrapper and body
        // sizing exist before the page can be shown or interacted with.
        ApplyJunhyunUiSimplification();
        ApplyJunhyunMarkerPanelPolish();
        ActivateProductMarkerPanelBodyLayout();
    }

    private void ActivateProductMarkerPanelBodyLayout()
    {
        if (_productMarkerPanelBodyLayoutActivated)
            return;
        _productMarkerPanelBodyLayoutActivated = true;

        if (_junhyunMarkerListViewport is null)
            throw new InvalidOperationException("Map marker checkbox viewport was not created during product activation.");

        // Replace the v1.7.15 content-sized synchronization handlers. The panel itself is
        // intentionally tall enough for the marker groups; the checkbox viewport must own
        // the entire remaining body instead of shrinking to its content and leaving unused
        // space below a separately scrolling list.
        SizeChanged -= JunhyunMarkerPanel_SizeChanged;
        MapMarkersContent.SizeChanged -= JunhyunMarkerContent_SizeChanged;
        BtnToggleMapMarkersPanel.Click -= JunhyunMarkerPanelToggleButton_Click;

        SizeChanged += ProductMarkerPanelBody_SizeChanged;
        MapMarkersContent.SizeChanged += ProductMarkerPanelBodyContent_SizeChanged;
        BtnToggleMapMarkersPanel.Click += ProductMarkerPanelBodyToggleButton_Click;

        _junhyunMarkerListViewport.VerticalAlignment = VerticalAlignment.Stretch;
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
            var requestedPanelHeight = Math.Max(0, MapMarkersOverlay.MinHeight);
            var desiredPanelHeight = headerHeight + Math.Min(contentHeight, availableListHeight) + verticalChrome;
            var panelHeight = Math.Min(maximumPanelHeight, Math.Max(requestedPanelHeight, desiredPanelHeight));
            var listHeight = Math.Max(120, panelHeight - headerHeight - verticalChrome);

            _junhyunMarkerListViewport.Height = listHeight;
            _junhyunMarkerListViewport.MaxHeight = listHeight;
            _junhyunMarkerListViewport.VerticalScrollBarVisibility =
                contentHeight <= listHeight + 0.5
                    ? ScrollBarVisibility.Hidden
                    : ScrollBarVisibility.Auto;

            MapMarkersOverlay.MinHeight = 0;
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

            var headerHeight = BtnToggleMapMarkersPanel.Parent is FrameworkElement header
                ? header.DesiredSize.Height
                : 0d;
            var verticalChrome = MapMarkersOverlay.Padding.Top + MapMarkersOverlay.Padding.Bottom + 8;
            var expectedBodyHeight = Math.Max(120, MapMarkersOverlay.Height - headerHeight - verticalChrome);

            if (!double.IsFinite(_junhyunMarkerListViewport.Height) ||
                Math.Abs(_junhyunMarkerListViewport.Height - expectedBodyHeight) > 1.0)
            {
                throw new InvalidOperationException(
                    $"Map marker checkbox viewport does not fill the panel body. viewport={_junhyunMarkerListViewport.Height:0.##}, expected={expectedBodyHeight:0.##}.");
            }

            MapMarkersContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var contentHeight = MapMarkersContent.DesiredSize.Height;
            var expectedScroll = contentHeight <= _junhyunMarkerListViewport.Height + 0.5
                ? ScrollBarVisibility.Hidden
                : ScrollBarVisibility.Auto;
            if (_junhyunMarkerListViewport.VerticalScrollBarVisibility != expectedScroll)
            {
                throw new InvalidOperationException(
                    "Map marker checkbox viewport scrollbar does not reflect the full available body height.");
            }

            var marker = Path.Combine(Path.GetTempPath(), "junhyun-map-marker-body-smoke-success.txt");
            File.WriteAllText(marker, "marker-list-fills-panel-body=ok\nscrollbar-only-on-real-overflow=ok\n");
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
