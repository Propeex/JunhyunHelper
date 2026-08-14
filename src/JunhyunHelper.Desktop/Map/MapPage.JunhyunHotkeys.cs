using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _junhyunFloorHotkeyBusy;

    /// <summary>
    /// Product hotkey endpoint. Zoom around the visible Map viewport center so a
    /// keyboard zoom does not jump the user's current point of interest.
    /// </summary>
    public void JunhyunZoomIn() => JunhyunZoom(1.15);

    public void JunhyunZoomOut() => JunhyunZoom(1.0 / 1.15);

    // Keep the original void endpoints for compatibility with any existing callers.
    // The product hotkey service uses the async endpoints below so Main Map rendering
    // finishes before the MiniMap starts its own floor render.
    public void JunhyunFloorUp() => _ = JunhyunFloorUpAsync();

    public void JunhyunFloorDown() => _ = JunhyunFloorDownAsync();

    public Task JunhyunFloorUpAsync() => JunhyunMoveFloorAsync(+1);

    public Task JunhyunFloorDownAsync() => JunhyunMoveFloorAsync(-1);

    private void JunhyunZoom(double factor)
    {
        if (factor <= 0 || MapViewerGrid.ActualWidth <= 0 || MapViewerGrid.ActualHeight <= 0)
            return;

        var oldZoom = _zoomLevel;
        var newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) < 0.001)
            return;

        var viewportCenter = new Point(
            MapViewerGrid.ActualWidth / 2.0,
            MapViewerGrid.ActualHeight / 2.0);
        var canvasX = (viewportCenter.X - MapTranslate.X) / oldZoom;
        var canvasY = (viewportCenter.Y - MapTranslate.Y) / oldZoom;

        MapTranslate.X = viewportCenter.X - canvasX * newZoom;
        MapTranslate.Y = viewportCenter.Y - canvasY * newZoom;
        SetZoom(newZoom);
    }

    private async Task JunhyunMoveFloorAsync(int delta)
    {
        if (_junhyunFloorHotkeyBusy || delta == 0 || CmbFloorSelect.Items.Count == 0)
            return;

        var current = Math.Max(0, CmbFloorSelect.SelectedIndex);
        var next = Math.Clamp(current + delta, 0, CmbFloorSelect.Items.Count - 1);
        if (next == current ||
            CmbFloorSelect.Items[next] is not ComboBoxItem floorItem ||
            floorItem.Tag is not string floorId ||
            string.IsNullOrWhiteSpace(_currentMapKey))
        {
            return;
        }

        var viewport = CaptureJunhyunViewport();
        _junhyunFloorHotkeyBusy = true;
        try
        {
            // A normal ComboBox selection is known-good, but its async-void handler lets
            // the hotkey dispatcher continue immediately into the MiniMap render. For a
            // product hotkey we perform the same state transition here and await the full
            // Main Map SVG/marker pipeline before the MiniMap is allowed to move floors.
            CmbFloorSelect.SelectionChanged -= CmbFloorSelect_SelectionChanged;
            try
            {
                CmbFloorSelect.SelectedIndex = next;
            }
            finally
            {
                CmbFloorSelect.SelectionChanged += CmbFloorSelect_SelectionChanged;
            }

            var mapKey = _currentMapKey;
            var ct = GetNewCancellationToken();
            _currentFloorId = floorId;

            await LoadMapImageAsync(mapKey, centerView: false, ct);
            ct.ThrowIfCancellationRequested();

            await RefreshExtractMarkers(ct);
            await RefreshQuestMarkers(ct);
            await RefreshMapMarkers(ct);
            UpdateCustomMarkersParam();
            _customMarkerManager?.UpdateMarkerDisplay();

            // SvgViewbox may complete layout after LoadMapImageAsync. Restore the exact
            // map-space point that was under the viewport center only after the floor
            // artwork and marker refresh have settled, so a floor hotkey never behaves
            // like a new map selection or forces the user to re-center on tracking.
            await Dispatcher.InvokeAsync(
                () => RestoreJunhyunViewport(viewport),
                DispatcherPriority.ContextIdle,
                ct);
        }
        catch (OperationCanceledException)
        {
            _log.Info("Junhyun floor hotkey render cancelled");
        }
        catch (Exception ex)
        {
            _log.Error("Error in Junhyun floor hotkey render", ex);
        }
        finally
        {
            _junhyunFloorHotkeyBusy = false;
        }
    }

    private JunhyunViewportSnapshot? CaptureJunhyunViewport()
    {
        var zoom = _zoomLevel;
        if (!double.IsFinite(zoom) || zoom <= 0 ||
            MapViewerGrid.ActualWidth <= 0 || MapViewerGrid.ActualHeight <= 0)
        {
            return null;
        }

        var centerX = MapViewerGrid.ActualWidth / 2.0;
        var centerY = MapViewerGrid.ActualHeight / 2.0;
        return new JunhyunViewportSnapshot(
            zoom,
            (centerX - MapTranslate.X) / zoom,
            (centerY - MapTranslate.Y) / zoom);
    }

    private void RestoreJunhyunViewport(JunhyunViewportSnapshot? snapshot)
    {
        if (snapshot is not { } value ||
            MapViewerGrid.ActualWidth <= 0 || MapViewerGrid.ActualHeight <= 0)
        {
            return;
        }

        var zoom = Math.Clamp(value.Zoom, MinZoom, MaxZoom);
        SetZoom(zoom);

        // Re-evaluate the current viewport center in case layout changed by a few pixels
        // while the SVG floor artwork was replaced.
        var centerX = MapViewerGrid.ActualWidth / 2.0;
        var centerY = MapViewerGrid.ActualHeight / 2.0;
        MapTranslate.X = centerX - value.CanvasX * zoom;
        MapTranslate.Y = centerY - value.CanvasY * zoom;
    }

    private readonly record struct JunhyunViewportSnapshot(
        double Zoom,
        double CanvasX,
        double CanvasY);
}
