using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private Canvas? _junhyunQuestV2Layer;
    private DispatcherTimer? _junhyunQuestV2Timer;
    private string? _junhyunQuestV2MapKey;
    private string? _junhyunQuestV2FloorId;
    private int _junhyunQuestV2Signature = int.MinValue;

    public void InitializeQuestV2()
    {
        if (_junhyunQuestV2Layer is not null)
            return;

        _junhyunQuestV2Layer = new Canvas
        {
            IsHitTestVisible = false,
            ClipToBounds = false,
        };
        Panel.SetZIndex(_junhyunQuestV2Layer, 560);
        MapCanvas.Children.Add(_junhyunQuestV2Layer);

        JunhyunMapQuestProjectionV2.Changed += JunhyunQuestV2_Changed;
        _junhyunQuestV2Timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(120),
            DispatcherPriority.Background,
            (_, _) => RenderQuestV2(force: false),
            Dispatcher);
        _junhyunQuestV2Timer.Start();
        RenderQuestV2(force: true);
    }

    public void DisposeQuestV2()
    {
        JunhyunMapQuestProjectionV2.Changed -= JunhyunQuestV2_Changed;
        if (_junhyunQuestV2Timer is not null)
        {
            _junhyunQuestV2Timer.Stop();
            _junhyunQuestV2Timer = null;
        }

        if (_junhyunQuestV2Layer is not null)
        {
            MapCanvas.Children.Remove(_junhyunQuestV2Layer);
            _junhyunQuestV2Layer = null;
        }
    }

    private void JunhyunQuestV2_Changed(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => RenderQuestV2(force: true));

    private void RenderQuestV2(bool force)
    {
        var layer = _junhyunQuestV2Layer;
        if (layer is null)
            return;

        var projectionMap = JunhyunMapQuestProjectionV2.MapKey;
        var markers = JunhyunMapQuestProjectionV2.Markers;
        var mapMatches = string.Equals(projectionMap, _currentMapKey, StringComparison.OrdinalIgnoreCase);
        var visible = mapMatches
            ? markers.Where(marker => FloorMatchesV2(marker.FloorId, _selectedFloorId)).ToArray()
            : Array.Empty<JunhyunQuestMarkerProjectionV2>();

        var signature = new HashCode();
        signature.Add(projectionMap, StringComparer.OrdinalIgnoreCase);
        signature.Add(_currentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        foreach (var marker in visible)
        {
            signature.Add(marker.QuestId, StringComparer.Ordinal);
            signature.Add(marker.ObjectiveId, StringComparer.Ordinal);
            signature.Add(marker.MarkerCode, StringComparer.Ordinal);
            signature.Add(marker.X);
            signature.Add(marker.Y);
        }
        var currentSignature = signature.ToHashCode();

        if (force ||
            currentSignature != _junhyunQuestV2Signature ||
            !string.Equals(_junhyunQuestV2MapKey, _currentMapKey, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_junhyunQuestV2FloorId, _selectedFloorId, StringComparison.OrdinalIgnoreCase))
        {
            layer.Children.Clear();
            foreach (var marker in visible)
            {
                var visual = JunhyunQuestMarkerVisualFactoryV2.Create(marker);
                Canvas.SetLeft(visual, marker.X);
                Canvas.SetTop(visual, marker.Y);
                layer.Children.Add(visual);
            }

            _junhyunQuestV2Signature = currentSignature;
            _junhyunQuestV2MapKey = _currentMapKey;
            _junhyunQuestV2FloorId = _selectedFloorId;
        }

        var inverse = 1.0 / Math.Max(_settings.ZoomLevel, TarkovHelper.Models.Map.OverlayMiniMapSettings.MinZoom);
        foreach (FrameworkElement child in layer.Children)
        {
            child.RenderTransform = new ScaleTransform(inverse, inverse);
            child.RenderTransformOrigin = new Point(0, 0);
        }

        layer.Visibility = visible.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool FloorMatchesV2(string? markerFloor, string? selectedFloor)
    {
        // Unknown Quest floor is deliberately not guessed; keep it visible rather
        // than hiding a valid objective because the source omitted height.
        if (string.IsNullOrWhiteSpace(markerFloor) || string.IsNullOrWhiteSpace(selectedFloor))
            return true;
        return string.Equals(markerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }
}
