using System.Windows;
using System.Windows.Controls;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    /// <summary>
    /// Product hotkey endpoint. Zoom around the visible Map viewport center so a
    /// keyboard zoom does not jump the user's current point of interest.
    /// </summary>
    public void JunhyunZoomIn() => JunhyunZoom(1.15);

    public void JunhyunZoomOut() => JunhyunZoom(1.0 / 1.15);

    public void JunhyunFloorUp() => JunhyunMoveFloor(+1);

    public void JunhyunFloorDown() => JunhyunMoveFloor(-1);

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

    private void JunhyunMoveFloor(int delta)
    {
        if (delta == 0 || CmbFloorSelect.Visibility != Visibility.Visible || CmbFloorSelect.Items.Count == 0)
            return;

        var current = Math.Max(0, CmbFloorSelect.SelectedIndex);
        var next = Math.Clamp(current + delta, 0, CmbFloorSelect.Items.Count - 1);
        if (next != current)
            CmbFloorSelect.SelectedIndex = next;
    }
}
