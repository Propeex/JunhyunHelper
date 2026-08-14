using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// The transplanted Main Map already keeps standard markers from other floors visible,
/// but most marker types only differ by opacity. Add a persistent compact up/down badge
/// without changing the pinned legacy marker renderer.
/// </summary>
public sealed class LegacyStandardMarkerFloorPresentationBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly Canvas? _markers;
    private readonly ComboBox? _floorSelector;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public LegacyStandardMarkerFloorPresentationBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _markers = _page.FindName("MapMarkersContainer") as Canvas;
        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(200),
            DispatcherPriority.Background,
            (_, _) => Apply(),
            _page.Dispatcher);
        _timer.Start();

        _tracker.MapChanged += Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;
        _page.Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => Apply();

    private void Tracker_MapChanged(string mapKey) => _page.Dispatcher.BeginInvoke(Apply);

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(Apply);

    private void Apply()
    {
        if (_disposed || _markers is null)
            return;

        var mapKey = _tracker.CurrentMapKey;
        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        var config = _tracker.GetMapConfig(mapKey);
        var selectedFloor = (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;
        foreach (var canvas in _markers.Children.OfType<Canvas>())
        {
            if (canvas.Tag is not MapMarker marker)
                continue;

            var relation = JunhyunFloorPresentation.Resolve(config, marker.FloorId, selectedFloor);
            if (relation.IsOtherFloor)
            {
                JunhyunFloorPresentation.ApplyToMarker(canvas, relation);
            }
            else
            {
                JunhyunFloorPresentation.RemoveDirectionBadge(canvas);
                canvas.Opacity = 1.0;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _timer.Stop();
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        _page.Loaded -= Page_Loaded;
    }
}
