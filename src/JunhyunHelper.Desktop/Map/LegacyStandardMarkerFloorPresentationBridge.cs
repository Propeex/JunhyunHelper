using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Applies JunhyunHelper floor presentation to the transplanted Main Map standard markers.
/// The legacy renderer owns coordinates and category visibility. Floor relationship is
/// presentation only: this bridge must never suppress an enabled marker merely because
/// another marker on a different floor occupies the same or a nearby X/Z position.
/// The bridge reacts only around real marker/map/floor changes and performs a bounded
/// settle after floor switches; it never returns to permanent full-tree polling.
/// </summary>
public sealed class LegacyStandardMarkerFloorPresentationBridge : IDisposable
{
    private const int ForcedSettleChecks = 6;

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly Canvas? _markers;
    private readonly ComboBox? _floorSelector;
    private readonly DispatcherTimer _debounceTimer;
    private int _lastObservedSignature = int.MinValue;
    private int _lastAppliedSignature = int.MinValue;
    private int _settleChecksRemaining;
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

        // The pinned donor still contains a bounded current-floor-only marker filter.
        // JunhyunHelper's product contract intentionally keeps enabled markers on other
        // floors visible, so restore only the elements that that donor filter hid and
        // immediately reapply the product-owned floor presentation after each donor tick.
        _page.JunhyunAttachCrossFloorMarkerPolicy(Apply);

        _tracker.MapChanged += Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;
        if (_markers is not null)
            _markers.LayoutUpdated += Markers_LayoutUpdated;
        _page.Loaded += Page_Loaded;
    }

    public void Refresh() => ScheduleApply(force: true);

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
        if (force)
            _settleChecksRemaining = Math.Max(_settleChecksRemaining, ForcedSettleChecks);

        if (!force && signature == _lastObservedSignature)
            return;

        _lastObservedSignature = signature;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private int ObserveSignature()
    {
        var signature = new System.HashCode();
        signature.Add(_tracker.CurrentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add(CurrentFloorId(), StringComparer.OrdinalIgnoreCase);

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
        if (_disposed)
            return;

        var signature = ObserveSignature();
        _lastObservedSignature = signature;
        var forceSettleApply = _settleChecksRemaining > 0;
        if (signature != _lastAppliedSignature || forceSettleApply)
        {
            Apply();
            _lastAppliedSignature = signature;
        }

        if (_settleChecksRemaining <= 0)
            return;

        _settleChecksRemaining--;
        _debounceTimer.Start();
    }

    private void Apply()
    {
        if (_disposed || _markers is null)
            return;

        var mapKey = _tracker.CurrentMapKey;
        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        var config = _tracker.GetMapConfig(mapKey);
        var selectedFloor = CurrentFloorId();

        foreach (var canvas in _markers.Children.OfType<Canvas>())
        {
            if (canvas.Tag is not MapMarker marker)
                continue;

            canvas.IsHitTestVisible = marker.Type is MarkerType.BossSpawn or MarkerType.Lever;

            var relation = JunhyunFloorPresentation.Resolve(config, marker.FloorId, selectedFloor);
            JunhyunFloorPresentation.ApplyToMarker(canvas, relation);
        }
    }

    private string? CurrentFloorId() =>
        (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _page.JunhyunDetachCrossFloorMarkerPolicy(Apply);
        _debounceTimer.Stop();
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        if (_markers is not null)
            _markers.LayoutUpdated -= Markers_LayoutUpdated;
        _page.Loaded -= Page_Loaded;
    }
}
