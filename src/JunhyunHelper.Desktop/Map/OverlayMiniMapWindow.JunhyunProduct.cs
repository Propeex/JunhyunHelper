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

        // Position is a product-owned anchor now. Keep the original map interactions,
        // but remove the original window drag handler.
        MouseLeftButtonDown -= Window_MouseLeftButtonDown;

        _settings.Opacity = 1.0;
        MainBorder.Opacity = 1.0;
        PositionToTopRight();

        SizeChanged += JunhyunMiniMap_SizeChanged;
        LocationChanged += JunhyunMiniMap_LocationChanged;
        JunhyunMapQuestProjection.Changed += JunhyunQuestProjection_Changed;

        _junhyunProductTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(80),
            DispatcherPriority.Background,
            JunhyunProductTimer_Tick,
            Dispatcher);
        _junhyunProductTimer.Start();

        RenderJunhyunQuestProjection(force: true);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_junhyunProductTimer is not null)
        {
            _junhyunProductTimer.Stop();
            _junhyunProductTimer.Tick -= JunhyunProductTimer_Tick;
            _junhyunProductTimer = null;
        }

        SizeChanged -= JunhyunMiniMap_SizeChanged;
        LocationChanged -= JunhyunMiniMap_LocationChanged;
        JunhyunMapQuestProjection.Changed -= JunhyunQuestProjection_Changed;

        base.OnClosed(e);
    }

    /// <summary>
    /// Hotkey target. Changes the window size while retaining the same top-right
    /// anchor used by the original double-click reset behavior.
    /// </summary>
    public void IncreaseAnchoredSize() => ChangeAnchoredSize(+40);

    /// <summary>
    /// Hotkey target. Changes the window size while retaining the same top-right
    /// anchor used by the original double-click reset behavior.
    /// </summary>
    public void DecreaseAnchoredSize() => ChangeAnchoredSize(-40);

    /// <summary>
    /// Main Map player-marker setting is canonical. Apply the same relative scale
    /// to the MiniMap player marker.
    /// </summary>
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
        if (_junhyunReanchoring)
            return;

        PositionToTopRightSafe();
    }

    private void JunhyunMiniMap_LocationChanged(object? sender, EventArgs e)
    {
        if (_junhyunReanchoring)
            return;

        // Any external/user attempt to move the overlay is snapped back to the
        // canonical top-right anchor.
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
        // Overall MiniMap opacity is product-fixed to 100%. Window.Opacity is used
        // only for temporary hover reveal and is independent from Click-through.
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

        // The original asynchronous marker refresh clears QuestMarkersContainer.
        // Count mismatch lets us restore the shared projection after that refresh
        // without a second independent marker-loading pipeline.
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
}
