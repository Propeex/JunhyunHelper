using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JunhyunHelper.Desktop.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private Canvas? _junhyunQuestV2Layer;
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
        RenderQuestV2(force: true);
    }

    public void DisposeQuestV2()
    {
        JunhyunMapQuestProjectionV2.Changed -= JunhyunQuestV2_Changed;

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
        var visible = mapMatches ? markers.ToArray() : Array.Empty<JunhyunQuestMarkerProjectionV2>();

        var signature = new System.HashCode();
        signature.Add(projectionMap, StringComparer.OrdinalIgnoreCase);
        signature.Add(_currentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        signature.Add(_settings.ZoomLevel);
        signature.Add(_junhyunMarkerScale);
        foreach (var marker in visible)
        {
            signature.Add(marker.QuestId, StringComparer.Ordinal);
            signature.Add(marker.ObjectiveId, StringComparer.Ordinal);
            signature.Add(marker.MarkerCode, StringComparer.Ordinal);
            signature.Add(marker.FloorId, StringComparer.OrdinalIgnoreCase);
            signature.Add(marker.X);
            signature.Add(marker.Y);
        }
        var currentSignature = signature.ToHashCode();

        if (!force &&
            currentSignature == _junhyunQuestV2Signature &&
            string.Equals(_junhyunQuestV2MapKey, _currentMapKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_junhyunQuestV2FloorId, _selectedFloorId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        layer.Children.Clear();
        foreach (var marker in visible)
        {
            var visual = JunhyunQuestMarkerVisualFactoryV3.Create(marker);
            var relation = JunhyunFloorPresentation.Resolve(
                _currentMapConfig,
                marker.FloorId,
                _selectedFloorId,
                JunhyunMissingFloorBehavior.KeepUnknown);
            JunhyunFloorPresentation.ApplyToMarker(visual, relation);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            layer.Children.Add(visual);
        }

        var inverse = 1.0 / Math.Max(_settings.ZoomLevel, TarkovHelper.Models.Map.OverlayMiniMapSettings.MinZoom);
        foreach (FrameworkElement child in layer.Children)
        {
            child.RenderTransform = new ScaleTransform(inverse * _junhyunMarkerScale, inverse * _junhyunMarkerScale);
            child.RenderTransformOrigin = new Point(0, 0);
        }

        layer.Visibility = visible.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        _junhyunQuestV2Signature = currentSignature;
        _junhyunQuestV2MapKey = _currentMapKey;
        _junhyunQuestV2FloorId = _selectedFloorId;
    }
}
