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
/// The legacy renderer owns coordinates and category visibility. This bridge reacts only
/// around real marker/map/floor changes and performs a bounded settle after floor switches;
/// it never returns to the old permanent full-tree polling behavior.
/// </summary>
public sealed class LegacyStandardMarkerFloorPresentationBridge : IDisposable
{
    private const double VerticalStackGameDistance = 8.0;
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
        if (force)
            _settleChecksRemaining = Math.Max(_settleChecksRemaining, ForcedSettleChecks);

        if (!force && signature == _lastObservedSignature)
            return;

        _lastObservedSignature = signature;

        // Marker loading can replace children asynchronously after SelectionChanged.
        // Restart a short one-shot settle window instead of traversing the whole marker
        // collection forever. Full presentation work only runs while that bounded settle
        // is active or when the O(1) marker signature changes.
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
        var rendered = new List<RenderedMarker>();

        foreach (var canvas in _markers.Children.OfType<Canvas>())
        {
            if (canvas.Tag is not MapMarker marker)
                continue;

            var relation = JunhyunFloorPresentation.Resolve(config, marker.FloorId, selectedFloor);
            JunhyunFloorPresentation.ApplyToMarker(canvas, relation);
            rendered.Add(new RenderedMarker(canvas, marker, relation));
        }

        if (config is not null && !string.IsNullOrWhiteSpace(selectedFloor))
            CollapseVerticalStacks(rendered, config, selectedFloor);
    }

    private static void CollapseVerticalStacks(
        IReadOnlyList<RenderedMarker> markers,
        MapConfig config,
        string selectedFloor)
    {
        if (markers.Count < 2 || config.Floors.Count == 0)
            return;

        var selectedOrder = FloorOrder(config, selectedFloor);
        if (!selectedOrder.HasValue)
            return;

        var consumed = new bool[markers.Count];
        var maxDistanceSquared = VerticalStackGameDistance * VerticalStackGameDistance;

        for (var start = 0; start < markers.Count; start++)
        {
            if (consumed[start] || markers[start].Canvas.Visibility != Visibility.Visible)
                continue;

            var cluster = new List<int> { start };
            consumed[start] = true;

            for (var cursor = 0; cursor < cluster.Count; cursor++)
            {
                var leftIndex = cluster[cursor];
                var left = markers[leftIndex];
                var leftOrder = FloorOrder(config, EffectiveFloorId(left.Marker.FloorId));
                if (!leftOrder.HasValue)
                    continue;

                for (var candidateIndex = 0; candidateIndex < markers.Count; candidateIndex++)
                {
                    if (consumed[candidateIndex])
                        continue;

                    var right = markers[candidateIndex];
                    if (right.Canvas.Visibility != Visibility.Visible || right.Marker.Type != left.Marker.Type)
                        continue;

                    var rightOrder = FloorOrder(config, EffectiveFloorId(right.Marker.FloorId));
                    if (!rightOrder.HasValue || rightOrder.Value == leftOrder.Value)
                        continue;

                    var dx = right.Marker.X - left.Marker.X;
                    var dz = right.Marker.Z - left.Marker.Z;
                    if ((dx * dx) + (dz * dz) > maxDistanceSquared)
                        continue;

                    consumed[candidateIndex] = true;
                    cluster.Add(candidateIndex);
                }
            }

            if (cluster.Count <= 1)
                continue;

            var representative = cluster
                .Select(index => (Index: index, Rank: RepresentativeRank(
                    markers[index],
                    config,
                    selectedOrder.Value)))
                .OrderBy(candidate => candidate.Rank)
                .ThenBy(candidate => candidate.Index)
                .First()
                .Index;

            foreach (var index in cluster)
            {
                if (index == representative)
                    continue;

                // Keep category visibility ownership with the legacy renderer. Opacity is
                // reset by ApplyToMarker on every real map/floor/tree change before the
                // current representative is chosen again.
                markers[index].Canvas.Opacity = 0.0;
            }
        }
    }

    private static int RepresentativeRank(
        RenderedMarker marker,
        MapConfig config,
        int selectedOrder)
    {
        if (marker.Relation.Relation == JunhyunFloorRelation.Current)
            return 0;

        var order = FloorOrder(config, EffectiveFloorId(marker.Marker.FloorId));
        return order.HasValue
            ? 10 + Math.Abs(order.Value - selectedOrder)
            : 10_000;
    }

    private static int? FloorOrder(MapConfig config, string? floorId)
    {
        if (string.IsNullOrWhiteSpace(floorId))
            return null;

        var floor = config.Floors.FirstOrDefault(candidate =>
            string.Equals(candidate.LayerId, floorId, StringComparison.OrdinalIgnoreCase));
        return floor?.Order;
    }

    private static string EffectiveFloorId(string? floorId) =>
        string.IsNullOrWhiteSpace(floorId) ? "main" : floorId;

    private string? CurrentFloorId() =>
        (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;

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

    private sealed record RenderedMarker(
        Canvas Canvas,
        MapMarker Marker,
        JunhyunFloorRelationInfo Relation);
}
