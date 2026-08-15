using System.Windows;
using System.Windows.Threading;
using TarkovHelper.Models.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Product-owned MiniMap floor movement. The legacy floor renderer replaces the SVG
    /// and then calls UpdateMapView(), which reads persisted MapOffsetX/Y. In
    /// PlayerTracking mode the live player-centered translation is intentionally kept only
    /// in MapTranslate, so those persisted offsets can be stale. Capture the live viewport
    /// before replacing the floor artwork and restore the same map-space center afterward.
    /// </summary>
    public Task JunhyunMoveFloorUpAsync() => JunhyunMoveFloorPreservingViewportAsync(+1);

    public Task JunhyunMoveFloorDownAsync() => JunhyunMoveFloorPreservingViewportAsync(-1);

    public Task JunhyunSelectFloorIndexAsync(int floorIndex)
    {
        var floors = GetOrderedFloors();
        if (floorIndex < 0 || floorIndex >= floors.Count)
            return Task.CompletedTask;

        return JunhyunSelectFloorPreservingViewportAsync(floors[floorIndex].LayerId);
    }

    private Task JunhyunMoveFloorPreservingViewportAsync(int direction)
    {
        var floors = GetOrderedFloors();
        if (floors.Count < 2)
            return Task.CompletedTask;

        var targetFloorId = MiniMapFloorSelection.Move(floors, _selectedFloorId, direction);
        if (string.IsNullOrWhiteSpace(targetFloorId) ||
            string.Equals(targetFloorId, _selectedFloorId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        return JunhyunSelectFloorPreservingViewportAsync(targetFloorId);
    }

    private async Task JunhyunSelectFloorPreservingViewportAsync(string targetFloorId)
    {
        if (string.IsNullOrWhiteSpace(targetFloorId) ||
            string.Equals(targetFloorId, _selectedFloorId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var floors = GetOrderedFloors();
        if (!floors.Any(floor => string.Equals(
                floor.LayerId,
                targetFloorId,
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var viewport = CaptureJunhyunMiniMapViewport();

        // Make the live transform authoritative before the transplanted renderer runs.
        // This prevents its internal UpdateMapView() from visibly jumping to an old
        // PlayerTracking offset even before the post-render restoration executes.
        if (viewport is { } live)
        {
            _settings.ZoomLevel = live.Zoom;
            _appliedZoomLevel = live.Zoom;
            _settings.MapOffsetX = MapTranslate.X;
            _settings.MapOffsetY = MapTranslate.Y;
        }

        _selectedFloorId = targetFloorId;
        _manualFloorSelection = true;
        _settings.AutoFloorSelection = false;
        _appliedAutoFloorSelection = false;
        UpdateFloorIndicator();
        SettingsChanged?.Invoke(_settings);

        _floorRenderCts?.Cancel();
        _floorRenderCts?.Dispose();
        _floorRenderCts = new CancellationTokenSource();
        var ct = _floorRenderCts.Token;

        await RenderCurrentFloorAsync(fitMap: false, ct);
        if (ct.IsCancellationRequested)
            return;

        await Dispatcher.InvokeAsync(
            () => RestoreJunhyunMiniMapViewport(viewport),
            DispatcherPriority.ContextIdle,
            ct);
    }

    private JunhyunMiniMapViewportSnapshot? CaptureJunhyunMiniMapViewport()
    {
        var zoom = MapScale.ScaleX;
        if (!double.IsFinite(zoom) || zoom <= 0)
            zoom = _settings.ZoomLevel;

        var viewWidth = MapContainer.ActualWidth > 0 ? MapContainer.ActualWidth : ActualWidth;
        var viewHeight = MapContainer.ActualHeight > 0 ? MapContainer.ActualHeight : ActualHeight;
        if (!double.IsFinite(zoom) || zoom <= 0 || viewWidth <= 0 || viewHeight <= 0)
            return null;

        var centerX = viewWidth / 2.0;
        var centerY = viewHeight / 2.0;
        return new JunhyunMiniMapViewportSnapshot(
            zoom,
            (centerX - MapTranslate.X) / zoom,
            (centerY - MapTranslate.Y) / zoom);
    }

    private void RestoreJunhyunMiniMapViewport(JunhyunMiniMapViewportSnapshot? snapshot)
    {
        if (snapshot is not { } value)
            return;

        var viewWidth = MapContainer.ActualWidth > 0 ? MapContainer.ActualWidth : ActualWidth;
        var viewHeight = MapContainer.ActualHeight > 0 ? MapContainer.ActualHeight : ActualHeight;
        if (viewWidth <= 0 || viewHeight <= 0)
            return;

        var zoom = Math.Clamp(
            value.Zoom,
            OverlayMiniMapSettings.MinZoom,
            OverlayMiniMapSettings.MaxZoom);
        var centerX = viewWidth / 2.0;
        var centerY = viewHeight / 2.0;
        var translateX = centerX - (value.CanvasX * zoom);
        var translateY = centerY - (value.CanvasY * zoom);
        (translateX, translateY) = ClampMapOffset(translateX, translateY);

        _settings.ZoomLevel = zoom;
        _appliedZoomLevel = zoom;
        _settings.MapOffsetX = translateX;
        _settings.MapOffsetY = translateY;
        MapScale.ScaleX = zoom;
        MapScale.ScaleY = zoom;
        MapTranslate.X = translateX;
        MapTranslate.Y = translateY;
        UpdateOverlayMarkerScales();
    }

    private readonly record struct JunhyunMiniMapViewportSnapshot(
        double Zoom,
        double CanvasX,
        double CanvasY);
}
