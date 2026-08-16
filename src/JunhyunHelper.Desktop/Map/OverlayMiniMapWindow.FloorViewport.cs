using System.Windows;
using System.Windows.Threading;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Product-owned MiniMap floor movement. All floor views are rendered from layers
    /// inside the same map SVG/canvas, so a floor change must not recompute the viewport.
    /// Preserve the live affine transform and replace only the floor artwork. This is
    /// stronger than preserving an inferred map-space center: scale and translation stay
    /// pixel-stable, including PlayerTracking where persisted offsets may be stale.
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
        // Its UpdateMapView() reads persisted settings immediately after replacing the SVG.
        // Copying the exact live values prevents a one-frame jump even before restoration.
        if (viewport is { } live)
        {
            _settings.ZoomLevel = live.ScaleX;
            _appliedZoomLevel = live.ScaleX;
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

        try
        {
            await Dispatcher.InvokeAsync(
                () => RestoreJunhyunMiniMapViewport(viewport),
                DispatcherPriority.ContextIdle,
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // A newer floor render superseded this request after the SVG render completed.
            // The exact live transform was already copied into persisted settings above,
            // so the replacement render inherits the same visual viewport.
        }
    }

    private JunhyunMiniMapViewportSnapshot? CaptureJunhyunMiniMapViewport()
    {
        var scaleX = MapScale.ScaleX;
        var scaleY = MapScale.ScaleY;
        if (!double.IsFinite(scaleX) || scaleX <= 0)
            scaleX = _settings.ZoomLevel;
        if (!double.IsFinite(scaleY) || scaleY <= 0)
            scaleY = scaleX;

        var viewWidth = MapContainer.ActualWidth > 0 ? MapContainer.ActualWidth : ActualWidth;
        var viewHeight = MapContainer.ActualHeight > 0 ? MapContainer.ActualHeight : ActualHeight;
        if (!double.IsFinite(scaleX) || scaleX <= 0 ||
            !double.IsFinite(scaleY) || scaleY <= 0 ||
            !double.IsFinite(MapTranslate.X) ||
            !double.IsFinite(MapTranslate.Y) ||
            !double.IsFinite(viewWidth) ||
            !double.IsFinite(viewHeight) ||
            viewWidth <= 0 || viewHeight <= 0)
        {
            return null;
        }

        return new JunhyunMiniMapViewportSnapshot(
            scaleX,
            scaleY,
            MapTranslate.X,
            MapTranslate.Y,
            viewWidth,
            viewHeight);
    }

    private void RestoreJunhyunMiniMapViewport(JunhyunMiniMapViewportSnapshot? snapshot)
    {
        if (snapshot is not { } value)
            return;

        var viewWidth = MapContainer.ActualWidth > 0 ? MapContainer.ActualWidth : ActualWidth;
        var viewHeight = MapContainer.ActualHeight > 0 ? MapContainer.ActualHeight : ActualHeight;
        if (!double.IsFinite(viewWidth) || !double.IsFinite(viewHeight) ||
            viewWidth <= 0 || viewHeight <= 0)
        {
            return;
        }

        // Floors share one SVG coordinate system. Preserve the exact screen-space scale.
        // If the window itself resized while the async SVG swap was running, compensate
        // only for the viewport center delta; never derive a new zoom or clamp the saved
        // translation, because either would make the artwork visibly jump on floor change.
        var translateX = value.TranslateX + ((viewWidth - value.ViewWidth) / 2.0);
        var translateY = value.TranslateY + ((viewHeight - value.ViewHeight) / 2.0);

        _settings.ZoomLevel = value.ScaleX;
        _appliedZoomLevel = value.ScaleX;
        _settings.MapOffsetX = translateX;
        _settings.MapOffsetY = translateY;
        MapScale.ScaleX = value.ScaleX;
        MapScale.ScaleY = value.ScaleY;
        MapTranslate.X = translateX;
        MapTranslate.Y = translateY;
        UpdateOverlayMarkerScales();
    }

    private readonly record struct JunhyunMiniMapViewportSnapshot(
        double ScaleX,
        double ScaleY,
        double TranslateX,
        double TranslateY,
        double ViewWidth,
        double ViewHeight);
}
