using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Small runtime layout corrections for the transplanted Map surface. Keeps the
/// artwork visually below the status/header boundary and prevents the floating
/// marker controls from growing upward into the MiniMap control area.
/// </summary>
public sealed class LegacyMapViewportPolishBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly Border? _markerOverlay;
    private readonly StackPanel? _markerContent;
    private readonly Grid? _mapViewer;
    private ScrollViewer? _markerScroll;
    private bool _disposed;

    public LegacyMapViewportPolishBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _markerOverlay = _page.FindName("MapMarkersOverlay") as Border;
        _markerContent = _page.FindName("MapMarkersContent") as StackPanel;
        _mapViewer = _page.FindName("MapViewerGrid") as Grid;

        _page.Loaded += Page_Loaded;
        _page.SizeChanged += Page_SizeChanged;
        _page.Dispatcher.BeginInvoke(Apply, DispatcherPriority.Loaded);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => Apply();
    private void Page_SizeChanged(object sender, SizeChangedEventArgs e) => Apply();

    private void Apply()
    {
        if (_disposed)
            return;

        if (_mapViewer is not null)
        {
            _mapViewer.ClipToBounds = true;
            _mapViewer.Margin = new Thickness(0, 2, 0, 0);
        }

        if (_markerOverlay is not null)
        {
            // Reserve the upper-right Map controls. On smaller windows the marker
            // panel scrolls internally instead of extending upward over MiniMap.
            var available = _mapViewer?.ActualHeight ?? _page.ActualHeight;
            _markerOverlay.MaxHeight = Math.Clamp(available - 105, 190, 360);
            _markerOverlay.VerticalAlignment = VerticalAlignment.Bottom;
        }

        EnsureMarkerScroll();
    }

    private void EnsureMarkerScroll()
    {
        if (_markerScroll is not null || _markerContent?.Parent is not Panel parent)
            return;

        var index = parent.Children.IndexOf(_markerContent);
        if (index < 0)
            return;

        parent.Children.RemoveAt(index);
        _markerScroll = new ScrollViewer
        {
            Content = _markerContent,
            MaxHeight = 300,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        parent.Children.Insert(index, _markerScroll);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _page.Loaded -= Page_Loaded;
        _page.SizeChanged -= Page_SizeChanged;
    }
}
