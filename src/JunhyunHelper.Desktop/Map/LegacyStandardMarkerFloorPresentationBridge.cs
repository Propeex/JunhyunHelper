using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Adds the JunhyunHelper floor-direction badge to the transplanted standard Map markers.
/// The legacy renderer already creates current and other-floor markers with the correct
/// coordinates. This bridge only reacts when the marker tree/map/floor actually changes;
/// it never performs a permanent polling scan on the WPF UI thread.
/// </summary>
public sealed class LegacyStandardMarkerFloorPresentationBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly Canvas? _markers;
    private readonly ComboBox? _floorSelector;
    private readonly DispatcherTimer _debounceTimer;
    private int _lastObservedSignature = int.MinValue;
    private bool _disposed;

    public LegacyStandardMarkerFloorPresentationBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _markers = _page.FindName("MapMarkersContainer") as Canvas;
        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;

        _debounceTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(90),
            DispatcherPriority.Background,
            (_, _) => ApplyPending(),
            _page.Dispatcher)
        {
            IsEnabled = false,
        };

        _tracker.MapChanged += Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;
        if (_markers is not null)
            _markers.LayoutUpdated += Markers_LayoutUpdated;
        _page.Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => ScheduleApply(force: true);

    private void Tracker_MapChanged(string mapKey) =>
        _page.Dispatcher.BeginInvoke(() => ScheduleApply(force: true));

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(() => ScheduleApply(force: true));

    private void Markers_LayoutUpdated(object? sender, EventArgs e) => ScheduleApply(force: false);

    private void ScheduleApply(bool force)
    {
        if (_disposed || _markers is null)
            return;

        var signature = ObserveSignature();
        if (!force && signature == _lastObservedSignature)
            return;

        _lastObservedSignature = signature;

        // Marker loading adds children in batches. Restart a one-shot debounce instead
        // of traversing the full marker collection for every intermediate layout pass.
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private int ObserveSignature()
    {
        var signature = new System.HashCode();
        signature.Add(_tracker.CurrentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add((_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string, StringComparer.OrdinalIgnoreCase);

        var count = _markers?.Children.Count ?? 0;
        signature.Add(count);
        if (_markers is not null && count > 0)
        {
            signature.Add(RuntimeHelpers.GetHashCode(_markers.Children[0]));
            signature.Add(RuntimeHelpers.GetHashCode(_markers.Children[count - 1]));
        }

        return signature.ToHashCode();
    }

    private void ApplyPending()
    {
        _debounceTimer.Stop();
        Apply();
    }

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
            JunhyunFloorPresentation.ApplyToMarker(canvas, relation);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _debounceTimer.Stop();
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        if (_markers is not null)
            _markers.LayoutUpdated -= Markers_LayoutUpdated;
        _page.Loaded -= Page_Loaded;
    }
}
