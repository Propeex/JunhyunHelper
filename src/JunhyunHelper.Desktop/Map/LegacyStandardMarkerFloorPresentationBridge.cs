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
        var rendered = new List<RenderedMarker>();

        foreach (var canvas in _markers.Children.OfType<Canvas>())
        {
            if (canvas.Tag is not MapMarker marker)
                continue;

            canvas.IsHitTestVisible = marker.Type is MarkerType.BossSpawn or MarkerType.Lever;

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
        var floors = config.Floors;
        if (markers.Count < 2 || floors is null || floors.Count == 0)
            return;

        var selectedOrder = FloorOrder(config, selectedFloor);
        if (!selectedOrder.HasValue)
            return;

        var consumed = new bool[markers.Count];
        var maxDistanceSquared = VerticalStackGameDistance * VerticalStackGameDistance;

        // Choose the visible representative first, then form the stack around that exact
        // representative. Current-floor markers have first priority; otherwise the closest
        // known Floor.Order wins. This guarantees every hidden member is physically within
        // the overlap threshold of the icon that remains visible.
        var representativeOrder = Enumerable.Range(0, markers.Count)
            .Where(index =>
                markers[index].Canvas.Visibility == Visibility.Visible &&
                FloorOrder(config, EffectiveFloorId(markers[index].Marker.FloorId)).HasValue)
            .OrderBy(index => RepresentativeRank(markers[index], config, selectedOrder.Value))
            .ThenBy(index => index)
            .ToArray();

        foreach (var representativeIndex in representativeOrder)
        {
            if (consumed[representativeIndex])
                continue;

            var representative = markers[representativeIndex];
            var representativeFloorOrder = FloorOrder(
                config,
                EffectiveFloorId(representative.Marker.FloorId));
            if (!representativeFloorOrder.HasValue)
                continue;

            consumed[representativeIndex] = true;

            // At most one marker from each other floor is collapsed into this physical
            // site, selecting the closest candidate to the visible representative.
            var candidatesByFloor = new Dictionary<int, (int Index, double DistanceSquared)>();
            for (var candidateIndex = 0; candidateIndex < markers.Count; candidateIndex++)
            {
                if (consumed[candidateIndex] || candidateIndex == representativeIndex)
                    continue;

                var candidate = markers[candidateIndex];
                if (candidate.Canvas.Visibility != Visibility.Visible ||
                    candidate.Marker.Type != representative.Marker.Type)
                {
                    continue;
                }

                var candidateFloorOrder = FloorOrder(
                    config,
                    EffectiveFloorId(candidate.Marker.FloorId));
                if (!candidateFloorOrder.HasValue ||
                    candidateFloorOrder.Value == representativeFloorOrder.Value)
                {
                    continue;
                }

                var dx = candidate.Marker.X - representative.Marker.X;
                var dz = candidate.Marker.Z - representative.Marker.Z;
                var distanceSquared = (dx * dx) + (dz * dz);
                if (distanceSquared > maxDistanceSquared)
                    continue;

                if (!candidatesByFloor.TryGetValue(candidateFloorOrder.Value, out var previous) ||
                    distanceSquared < previous.DistanceSquared)
                {
                    candidatesByFloor[candidateFloorOrder.Value] = (candidateIndex, distanceSquared);
                }
            }

            foreach (var candidate in candidatesByFloor.Values)
            {
                if (consumed[candidate.Index])
                    continue;

                consumed[candidate.Index] = true;
                markers[candidate.Index].Canvas.Opacity = 0.0;
                markers[candidate.Index].Canvas.IsHitTestVisible = false;
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
        if (string.IsNullOrWhiteSpace(floorId) || config.Floors is not { } floors)
            return null;

        var floor = floors.FirstOrDefault(candidate =>
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
