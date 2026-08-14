using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
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
    private int _junhyunLastExtractSignature = -1;
    private int _junhyunLastGeneralMarkerSignature = int.MinValue;
    private double _junhyunMarkerScale = 1.0;

    public double JunhyunMarkerScale => _junhyunMarkerScale;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        MouseLeftButtonDown -= Window_MouseLeftButtonDown;

        _settings.Opacity = 1.0;
        MainBorder.Opacity = 1.0;
        PositionToTopRight();
        JunhyunMiniMapProductRegistry.Register(this);

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
        RenderQuestV2(force: true);
        SynchronizeGeneralMarkerScale(force: true);
        RenderJunhyunAdditionalMarkers(force: true);
        SynchronizeExtractPresentation(force: true);
    }

    public void IncreaseAnchoredSize() => ChangeAnchoredSize(+40);

    public void DecreaseAnchoredSize() => ChangeAnchoredSize(-40);

    public void ApplySharedPlayerMarkerSize(double mapPixelSize)
    {
        _settings.PlayerMarkerSize = Math.Clamp(mapPixelSize / 18.0, 0.5, 3.0);
        UpdateMapView();
        SaveSettings();
    }

    /// <summary>
    /// Applies a MiniMap-only scale to Quest, general, and extract marker visuals.
    /// Player position marker sizing remains independent.
    /// </summary>
    public void ApplyJunhyunMarkerScale(double scale)
    {
        _junhyunMarkerScale = Math.Clamp(scale, 0.25, 1.50);
        _junhyunLastGeneralMarkerSignature = int.MinValue;
        SynchronizeGeneralMarkerScale(force: true);
        RenderJunhyunAdditionalMarkers(force: true);
        RenderJunhyunQuestProjection(force: true);
        RenderQuestV2(force: true);
        _junhyunLastExtractSignature = -1;
        SynchronizeExtractPresentation(force: true);
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

        // Timed transparency and hover transparency are independent product intents.
        // Either one may temporarily hide the MiniMap; the separate opacity mask owns
        // the configured normal-state opacity when neither hide condition is active.
        var shouldHide = JunhyunTemporaryHideActive || IsCursorInsideMiniMap();
        var targetOpacity = shouldHide ? 0.0 : 1.0;
        if (Math.Abs(Opacity - targetOpacity) > 0.001)
            Opacity = targetOpacity;

        RenderJunhyunQuestProjection(force: false);
        RenderQuestV2(force: false);
        SynchronizeGeneralMarkerScale(force: false);
        RenderJunhyunAdditionalMarkers(force: false);
        SynchronizeExtractPresentation(force: false);
    }

    private bool IsCursorInsideMiniMap()
    {
        if (!IsVisible || !GetCursorPos(out var cursor))
            return false;

        try
        {
            var local = PointFromScreen(new Point(cursor.X, cursor.Y));
            var width = ActualWidth > 0 ? ActualWidth : Width;
            var height = ActualHeight > 0 ? ActualHeight : Height;
            return local.X >= 0 && local.X < width &&
                   local.Y >= 0 && local.Y < height;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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
            ApplyJunhyunMarkerVisualScale(visual);
            QuestMarkersContainer.Children.Add(visual);
        }

        QuestMarkersContainer.Visibility = visibleMarkers.Length > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        _junhyunLastQuestMarkerCount = visibleMarkers.Length;
        _junhyunLastQuestMapKey = mapKey;
    }

    private void SynchronizeGeneralMarkerScale(bool force)
    {
        var inverse = 1.0 / Math.Max(_settings.ZoomLevel, OverlayMiniMapSettings.MinZoom);
        var synchronizedScale = inverse * (24.0 / 18.0) * _junhyunMarkerScale;

        var signatureBuilder = new System.HashCode();
        signatureBuilder.Add(_currentMapKey, StringComparer.OrdinalIgnoreCase);
        signatureBuilder.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        signatureBuilder.Add(_settings.ZoomLevel);
        signatureBuilder.Add(_junhyunMarkerScale);
        var count = MapMarkersContainer.Children.Count;
        signatureBuilder.Add(count);
        if (count > 0)
        {
            signatureBuilder.Add(RuntimeHelpers.GetHashCode(MapMarkersContainer.Children[0]));
            signatureBuilder.Add(RuntimeHelpers.GetHashCode(MapMarkersContainer.Children[count - 1]));
        }
        var signature = signatureBuilder.ToHashCode();
        if (!force && signature == _junhyunLastGeneralMarkerSignature)
            return;

        foreach (FrameworkElement element in MapMarkersContainer.Children)
        {
            // Junhyun-only markers own a different base size and scaling contract.
            // Leave them to RenderJunhyunAdditionalMarkers so the standard-marker
            // synchronization path cannot overwrite their transform.
            if (element.Tag is JunhyunAdditionalMapMarker)
                continue;

            element.RenderTransform = new ScaleTransform(synchronizedScale, synchronizedScale);
            element.RenderTransformOrigin = element is Canvas
                ? new Point(0, 0)
                : new Point(0.5, 0.5);

            if (element is Canvas canvas && canvas.Tag is MapMarker marker)
            {
                var relation = JunhyunFloorPresentation.Resolve(
                    _currentMapConfig,
                    marker.FloorId,
                    _selectedFloorId);
                JunhyunFloorPresentation.ApplyToMarker(
                    canvas,
                    relation,
                    badgeOffsetX: 6,
                    badgeOffsetY: -14,
                    currentFloorOpacity: 0.95);
            }
        }

        _junhyunLastGeneralMarkerSignature = signature;
    }

    private void SynchronizeExtractPresentation(bool force)
    {
        if (string.IsNullOrWhiteSpace(_currentMapKey) || _currentMapConfig is null)
            return;

        var settings = MapSettings.Instance;
        var signatureBuilder = new System.HashCode();
        signatureBuilder.Add(_currentMapKey, StringComparer.OrdinalIgnoreCase);
        signatureBuilder.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        signatureBuilder.Add(settings.ShowExtracts);
        signatureBuilder.Add(settings.ShowPmcExtracts);
        signatureBuilder.Add(settings.ShowScavExtracts);
        signatureBuilder.Add(settings.ShowTransits);
        signatureBuilder.Add(settings.ExtractNameSize);
        signatureBuilder.Add(_junhyunMarkerScale);
        var signature = signatureBuilder.ToHashCode();

        if (!force && signature == _junhyunLastExtractSignature &&
            ExtractMarkersContainer.Children.Cast<FrameworkElement>()
                .All(child => child.Tag is JunhyunSynchronizedExtractTag))
        {
            return;
        }

        if (!ExtractService.Instance.IsLoaded)
            return;

        var extracts = ExtractService.Instance.GetExtractsForMap(_currentMapKey, _currentMapConfig);
        ExtractMarkersContainer.Children.Clear();

        if (!settings.ShowExtracts)
        {
            _junhyunLastExtractSignature = signature;
            return;
        }

        foreach (var display in MapExtractDisplayGrouping.GroupForDisplay(extracts))
        {
            if (!IsExtractVisible(settings, display.Faction))
                continue;

            var extract = display.Extract;
            var (screenX, screenY) = _currentMapConfig.GameToScreenForPlayer(extract.X, extract.Z);
            var relation = JunhyunFloorPresentation.Resolve(
                _currentMapConfig,
                extract.FloorId,
                _selectedFloorId);
            var visual = CreateSynchronizedExtractVisual(extract, display.Faction);
            JunhyunFloorPresentation.ApplyToMarker(
                visual,
                relation,
                badgeOffsetX: 7,
                badgeOffsetY: -15);
            Canvas.SetLeft(visual, screenX);
            Canvas.SetTop(visual, screenY);
            ExtractMarkersContainer.Children.Add(visual);
        }

        _junhyunLastExtractSignature = signature;
    }

    private FrameworkElement CreateSynchronizedExtractVisual(
        MapExtract extract,
        ExtractFaction faction)
    {
        var mapScale = _currentMapConfig?.MarkerScale ?? 1.0;
        var markerSize = 20.0 * mapScale;
        var textSize = MapSettings.Instance.ExtractNameSize * mapScale;
        var fill = faction switch
        {
            ExtractFaction.Pmc => Color.FromRgb(76, 175, 80),
            ExtractFaction.Scav => Color.FromRgb(158, 158, 158),
            ExtractFaction.Shared => Color.FromRgb(76, 175, 80),
            ExtractFaction.Transit => Color.FromRgb(255, 152, 0),
            _ => Color.FromRgb(158, 158, 158),
        };

        var canvas = new Canvas
        {
            Width = 0,
            Height = 0,
            IsHitTestVisible = false,
            Tag = new JunhyunSynchronizedExtractTag(),
            Opacity = 1.0,
        };

        var glowSize = markerSize * 1.5;
        var glow = new Ellipse
        {
            Width = glowSize,
            Height = glowSize,
            Fill = new SolidColorBrush(Color.FromArgb(80, fill.R, fill.G, fill.B)),
        };
        Canvas.SetLeft(glow, -glowSize / 2);
        Canvas.SetTop(glow, -glowSize / 2);
        canvas.Children.Add(glow);

        var circle = new Ellipse
        {
            Width = markerSize,
            Height = markerSize,
            Fill = new SolidColorBrush(fill),
            Stroke = Brushes.White,
            StrokeThickness = 2 * mapScale,
        };
        Canvas.SetLeft(circle, -markerSize / 2);
        Canvas.SetTop(circle, -markerSize / 2);
        canvas.Children.Add(circle);

        var iconSize = markerSize * 0.7;
        var icon = JunhyunExtractMarkerIcon.Create(iconSize, Colors.White);
        Canvas.SetLeft(icon, -iconSize / 2);
        Canvas.SetTop(icon, -iconSize / 2);
        canvas.Children.Add(icon);

        var displayName = !string.IsNullOrWhiteSpace(extract.NameKo) ? extract.NameKo : extract.Name;
        var label = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30)),
            CornerRadius = new CornerRadius(3 * mapScale),
            Padding = new Thickness(4 * mapScale, 2 * mapScale, 4 * mapScale, 2 * mapScale),
            Child = new TextBlock
            {
                Text = displayName,
                Foreground = new SolidColorBrush(fill),
                FontSize = textSize,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
            },
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(label, -label.DesiredSize.Width / 2);
        Canvas.SetTop(label, -markerSize - label.DesiredSize.Height - 4 * mapScale);
        canvas.Children.Add(label);

        ApplyJunhyunMarkerVisualScale(canvas);
        return canvas;
    }

    private void ApplyJunhyunMarkerVisualScale(FrameworkElement marker)
    {
        var inverse = 1.0 / Math.Max(_settings.ZoomLevel, OverlayMiniMapSettings.MinZoom);
        var scale = inverse * _junhyunMarkerScale;
        marker.RenderTransform = new ScaleTransform(scale, scale);
        marker.RenderTransformOrigin = marker is Canvas
            ? new Point(0, 0)
            : new Point(0.5, 0.5);
    }

    private static bool IsExtractVisible(MapSettings settings, ExtractFaction faction) => faction switch
    {
        ExtractFaction.Pmc => settings.ShowPmcExtracts,
        ExtractFaction.Scav => settings.ShowScavExtracts,
        ExtractFaction.Shared => settings.ShowPmcExtracts || settings.ShowScavExtracts,
        ExtractFaction.Transit => settings.ShowTransits,
        _ => true,
    };

    private sealed class JunhyunSynchronizedExtractTag
    {
    }

    private void JunhyunMiniMap_Closed(object? sender, EventArgs e)
    {
        JunhyunMiniMapProductRegistry.Unregister(this);
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
