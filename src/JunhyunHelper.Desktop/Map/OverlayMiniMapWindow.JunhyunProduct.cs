using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Windows;

/// <summary>
/// JunhyunHelper product delta for the exact Tarkov Helper MiniMap window.
/// Rendering/coordinate/floor tracking remains in the original partial class.
/// </summary>
public partial class OverlayMiniMapWindow
{
    [StructLayout(LayoutKind.Sequential)]
    private struct JunhyunCursorPoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out JunhyunCursorPoint point);

    private DispatcherTimer? _junhyunProductTimer;
    private bool _junhyunReanchoring;
    private int _junhyunLastQuestMarkerCount = -1;
    private string? _junhyunLastQuestMapKey;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        MouseLeftButtonDown -= Window_MouseLeftButtonDown;

        _settings.Opacity = 1.0;
        MainBorder.Opacity = 1.0;
        PositionToTopRight();

        SizeChanged += JunhyunMiniMap_SizeChanged;
        LocationChanged += JunhyunMiniMap_LocationChanged;
        Closed += JunhyunMiniMap_Closed;
        JunhyunMapQuestProjection.Changed += JunhyunQuestProjection_Changed;

        _junhyunProductTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(80),
            DispatcherPriority.Background,
            JunhyunProductTimer_Tick,
            Dispatcher);
        _junhyunProductTimer.Start();

        RenderJunhyunQuestProjection(force: true);
    }

    public void IncreaseAnchoredSize() => ChangeAnchoredSize(+40);

    public void DecreaseAnchoredSize() => ChangeAnchoredSize(-40);

    public void ApplySharedPlayerMarkerSize(double mapPixelSize)
    {
        _settings.PlayerMarkerSize = Math.Clamp(mapPixelSize / 18.0, 0.5, 3.0);
        UpdateMapView();
        SaveSettings();
    }

    private void ChangeAnchoredSize(double delta)
    {
        var currentWidth = double.IsFinite(ActualWidth) && ActualWidth > 0 ? ActualWidth : Width;
        var currentHeight = double.IsFinite(ActualHeight) && ActualHeight > 0 ? ActualHeight : Height;
        var ratio = currentWidth > 0 ? currentHeight / currentWidth : 1.0;

        var width = Math.Clamp(
            currentWidth + delta,
            OverlayMiniMapSettings.MinWidth,
            OverlayMiniMapSettings.MaxWidth);
        var height = Math.Clamp(
            width * ratio,
            OverlayMiniMapSettings.MinHeight,
            OverlayMiniMapSettings.MaxHeight);

        Width = width;
        Height = height;
        PositionToTopRight();
        SaveSettings();
    }

    private void JunhyunMiniMap_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_junhyunReanchoring)
            PositionToTopRightSafe();
    }

    private void JunhyunMiniMap_LocationChanged(object? sender, EventArgs e)
    {
        if (!_junhyunReanchoring)
            PositionToTopRightSafe();
    }

    private void PositionToTopRightSafe()
    {
        if (_junhyunReanchoring)
            return;

        _junhyunReanchoring = true;
        try
        {
            PositionToTopRight();
        }
        finally
        {
            _junhyunReanchoring = false;
        }
    }

    private void JunhyunProductTimer_Tick(object? sender, EventArgs e)
    {
        _settings.Opacity = 1.0;
        if (Math.Abs(MainBorder.Opacity - 1.0) > 0.001)
            MainBorder.Opacity = 1.0;

        var shouldHideForHover = IsCursorInsideMiniMap();
        var targetOpacity = shouldHideForHover ? 0.0 : 1.0;
        if (Math.Abs(Opacity - targetOpacity) > 0.001)
            Opacity = targetOpacity;

        RenderJunhyunQuestProjection(force: false);
    }

    private bool IsCursorInsideMiniMap()
    {
        if (!IsVisible || !GetCursorPos(out var cursor))
            return false;

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        return cursor.X >= Left && cursor.X < Left + width &&
               cursor.Y >= Top && cursor.Y < Top + height;
    }

    private void JunhyunQuestProjection_Changed(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => RenderJunhyunQuestProjection(force: true));

    private void RenderJunhyunQuestProjection(bool force)
    {
        var mapKey = JunhyunMapQuestProjection.MapKey;
        var allMarkers = JunhyunMapQuestProjection.Markers;

        if (!string.Equals(mapKey, _currentMapKey, StringComparison.OrdinalIgnoreCase))
        {
            if (QuestMarkersContainer.Children.Count > 0)
                QuestMarkersContainer.Children.Clear();
            QuestMarkersContainer.Visibility = Visibility.Collapsed;
            _junhyunLastQuestMarkerCount = 0;
            _junhyunLastQuestMapKey = mapKey;
            return;
        }

        var visibleMarkers = allMarkers
            .Where(marker => IsCurrentFloor(marker.FloorId, _selectedFloorId))
            .ToArray();

        if (!force &&
            string.Equals(_junhyunLastQuestMapKey, mapKey, StringComparison.OrdinalIgnoreCase) &&
            _junhyunLastQuestMarkerCount == visibleMarkers.Length &&
            QuestMarkersContainer.Children.Count == visibleMarkers.Length)
        {
            return;
        }

        QuestMarkersContainer.Children.Clear();
        foreach (var marker in visibleMarkers)
        {
            var visual = JunhyunQuestMarkerVisualFactory.Create(
                marker,
                MapSettings.Instance.QuestMarkerSize,
                MapSettings.Instance.QuestNameSize);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            ApplyInverseMapScale(visual);
            QuestMarkersContainer.Children.Add(visual);
        }

        QuestMarkersContainer.Visibility = visibleMarkers.Length > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _junhyunLastQuestMarkerCount = visibleMarkers.Length;
        _junhyunLastQuestMapKey = mapKey;
    }

    private void JunhyunMiniMap_Closed(object? sender, EventArgs e)
    {
        Closed -= JunhyunMiniMap_Closed;
        SizeChanged -= JunhyunMiniMap_SizeChanged;
        LocationChanged -= JunhyunMiniMap_LocationChanged;
        JunhyunMapQuestProjection.Changed -= JunhyunQuestProjection_Changed;

        if (_junhyunProductTimer is not null)
        {
            _junhyunProductTimer.Stop();
            _junhyunProductTimer.Tick -= JunhyunProductTimer_Tick;
            _junhyunProductTimer = null;
        }
    }
}
