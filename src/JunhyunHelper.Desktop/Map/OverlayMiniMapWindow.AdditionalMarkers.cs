using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;

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
    private int _junhyunAdditionalMarkerCount = -1;
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
            _junhyunAdditionalMarkerCount = 0;
            return;
        }

        var visible = JunhyunAdditionalMapMarkerProjection.Markers
            .Where(marker => MiniMapMarkerVisibilityState.IsCurrentFloor(marker.FloorId, _selectedFloorId))
            .ToArray();

        var existing = MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Count(element => element.Tag is JunhyunAdditionalMapMarker);
        if (!force &&
            string.Equals(_junhyunAdditionalMarkerMapKey, mapKey, StringComparison.OrdinalIgnoreCase) &&
            _junhyunAdditionalMarkerCount == visible.Length &&
            existing == visible.Length)
        {
            return;
        }

        RemoveJunhyunAdditionalMarkerChildren();
        foreach (var marker in visible)
        {
            // MiniMap's synchronization pass scales the original 18px marker base by
            // 4/3, so use 18px here to land on the exact Main Map 24px presentation.
            var visual = JunhyunAdditionalMarkerVisualFactory.Create(marker, baseSize: 18);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            MapMarkersContainer.Children.Add(visual);
        }

        _junhyunAdditionalMarkerMapKey = mapKey;
        _junhyunAdditionalMarkerCount = visible.Length;
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
