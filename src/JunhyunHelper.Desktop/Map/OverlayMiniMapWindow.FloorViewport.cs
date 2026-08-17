using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Product-owned MiniMap floor movement. Every floor is a layer inside the same
    /// canonical map SVG/canvas, so switching floors must not reframe the map at all.
    /// Preserve the exact live scale + translation and replace only the rendered layer.
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

        // The legacy renderer calls UpdateMapView() after replacing the SVG and reads
        // persisted offsets. PlayerTracking intentionally keeps its authoritative live
        // position in MapTranslate, so copy the exact frame into settings first. This
        // prevents the renderer from using a stale player-centered offset while the new
        // floor layer is installed.
        if (viewport is { } live)
        {
            _settings.ZoomLevel = live.Zoom;
            _appliedZoomLevel = live.Zoom;
            _settings.MapOffsetX = live.TranslateX;
            _settings.MapOffsetY = live.TranslateY;
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

        // RenderCurrentFloorAsync does not change the canonical canvas dimensions; it
        // only swaps which SVG floor layer is visible. Restore the exact live transform
        // immediately. Recomputing from a map-space center or clamping the offset here
        // can move the image by a few pixels and makes floor switching look like a zoom/
        // pan even though the user asked for only the floor artwork to change.
        RestoreJunhyunMiniMapViewport(viewport);
    }

    private JunhyunMiniMapViewportSnapshot? CaptureJunhyunMiniMapViewport()
    {
        var zoom = MapScale.ScaleX;
        if (!double.IsFinite(zoom) || zoom <= 0)
            zoom = _settings.ZoomLevel;

        var translateX = MapTranslate.X;
        var translateY = MapTranslate.Y;
        if (!double.IsFinite(zoom) || zoom <= 0 ||
            !double.IsFinite(translateX) || !double.IsFinite(translateY))
        {
            return null;
        }

        return new JunhyunMiniMapViewportSnapshot(zoom, translateX, translateY);
    }

    private void RestoreJunhyunMiniMapViewport(JunhyunMiniMapViewportSnapshot? snapshot)
    {
        if (snapshot is not { } value)
            return;

        var zoom = Math.Clamp(
            value.Zoom,
            OverlayMiniMapSettings.MinZoom,
            OverlayMiniMapSettings.MaxZoom);

        _settings.ZoomLevel = zoom;
        _appliedZoomLevel = zoom;
        _settings.MapOffsetX = value.TranslateX;
        _settings.MapOffsetY = value.TranslateY;
        MapScale.ScaleX = zoom;
        MapScale.ScaleY = zoom;
        MapTranslate.X = value.TranslateX;
        MapTranslate.Y = value.TranslateY;
        UpdateOverlayMarkerScales();
    }

    private readonly record struct JunhyunMiniMapViewportSnapshot(
        double Zoom,
        double TranslateX,
        double TranslateY);
}
