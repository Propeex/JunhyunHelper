using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;

namespace TarkovHelper.Windows;

/// <summary>
/// Mirrors JunhyunHelper-only data-backed marker categories into MiniMap using the
/// same shared projection and visual factory as Main Map.
/// </summary>
public partial class OverlayMiniMapWindow
{
    private static readonly bool JunhyunAdditionalMarkerClassHandlerRegistered =
        RegisterJunhyunAdditionalMarkerClassHandler();

    private DispatcherTimer? _junhyunAdditionalMarkerTimer;
    private int _junhyunAdditionalMarkerSignature = int.MinValue;
    private string? _junhyunAdditionalMarkerMapKey;

    private static bool RegisterJunhyunAdditionalMarkerClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(OverlayMiniMapWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunAdditionalMarkerWindowLoaded));
        return true;
    }

    private static void OnJunhyunAdditionalMarkerWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is OverlayMiniMapWindow window)
            window.InitializeJunhyunAdditionalMarkers();
    }

    private void InitializeJunhyunAdditionalMarkers()
    {
        if (_junhyunAdditionalMarkerTimer is not null)
            return;

        JunhyunAdditionalMapMarkerProjection.Changed += JunhyunAdditionalMarkerProjection_Changed;
        Closed += JunhyunAdditionalMarkerWindow_Closed;

        _junhyunAdditionalMarkerTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(150),
            DispatcherPriority.Background,
            (_, _) => RenderJunhyunAdditionalMarkers(force: false),
            Dispatcher);
        _junhyunAdditionalMarkerTimer.Start();
        RenderJunhyunAdditionalMarkers(force: true);
    }

    private void JunhyunAdditionalMarkerProjection_Changed(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => RenderJunhyunAdditionalMarkers(force: true));

    private void RenderJunhyunAdditionalMarkers(bool force)
    {
        var mapKey = JunhyunAdditionalMapMarkerProjection.MapKey;
        if (!string.Equals(mapKey, _currentMapKey, StringComparison.OrdinalIgnoreCase))
        {
            RemoveJunhyunAdditionalMarkerChildren();
            _junhyunAdditionalMarkerMapKey = mapKey;
            _junhyunAdditionalMarkerSignature = 0;
            return;
        }

        var markers = JunhyunAdditionalMapMarkerProjection.Markers.ToArray();
        var signature = new HashCode();
        signature.Add(mapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        foreach (var marker in markers)
        {
            signature.Add(marker.Id, StringComparer.Ordinal);
            signature.Add(marker.FloorId, StringComparer.OrdinalIgnoreCase);
            signature.Add(marker.X);
            signature.Add(marker.Y);
        }
        var currentSignature = signature.ToHashCode();

        var existing = MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Count(element => element.Tag is JunhyunAdditionalMapMarker);
        if (!force &&
            string.Equals(_junhyunAdditionalMarkerMapKey, mapKey, StringComparison.OrdinalIgnoreCase) &&
            _junhyunAdditionalMarkerSignature == currentSignature &&
            existing == markers.Length)
        {
            return;
        }

        RemoveJunhyunAdditionalMarkerChildren();
        foreach (var marker in markers)
        {
            var visual = JunhyunAdditionalMarkerVisualFactory.Create(marker, baseSize: 18);
            var relation = JunhyunFloorPresentation.Resolve(_currentMapConfig, marker.FloorId, _selectedFloorId);
            JunhyunFloorPresentation.ApplyToMarker(visual, relation, badgeOffsetX: 6, badgeOffsetY: -14);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            ApplyJunhyunAdditionalMarkerScale(visual);
            MapMarkersContainer.Children.Add(visual);
        }

        _junhyunAdditionalMarkerMapKey = mapKey;
        _junhyunAdditionalMarkerSignature = currentSignature;
    }

    private void ApplyJunhyunAdditionalMarkerScale(FrameworkElement marker)
    {
        var inverse = 1.0 / Math.Max(_settings.ZoomLevel, OverlayMiniMapSettings.MinZoom);
        var scale = inverse * _junhyunMarkerScale;
        marker.RenderTransform = new ScaleTransform(scale, scale);
        marker.RenderTransformOrigin = marker is Canvas
            ? new Point(0, 0)
            : new Point(0.5, 0.5);
    }

    private void RemoveJunhyunAdditionalMarkerChildren()
    {
        var children = MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Where(element => element.Tag is JunhyunAdditionalMapMarker)
            .ToArray();
        foreach (var child in children)
            MapMarkersContainer.Children.Remove(child);
    }

    private void JunhyunAdditionalMarkerWindow_Closed(object? sender, EventArgs e)
    {
        Closed -= JunhyunAdditionalMarkerWindow_Closed;
        JunhyunAdditionalMapMarkerProjection.Changed -= JunhyunAdditionalMarkerProjection_Changed;
        if (_junhyunAdditionalMarkerTimer is not null)
        {
            _junhyunAdditionalMarkerTimer.Stop();
            _junhyunAdditionalMarkerTimer = null;
        }
    }
}
