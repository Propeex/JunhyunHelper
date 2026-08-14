using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Windows;

/// <summary>
/// Product presentation for markers that belong to a known floor above or below the
/// selected MiniMap floor. The SVG artwork remains selected-floor only; marker context
/// is retained at reduced opacity with a compact directional badge.
/// </summary>
public partial class OverlayMiniMapWindow
{
    private static readonly bool JunhyunOtherFloorMarkerHandlersRegistered =
        RegisterJunhyunOtherFloorMarkerHandlers();

    private DispatcherTimer? _junhyunOtherFloorMarkerTimer;
    private Canvas? _junhyunOtherFloorExtractLayer;
    private int _junhyunOtherFloorExtractSignature = int.MinValue;

    private static bool RegisterJunhyunOtherFloorMarkerHandlers()
    {
        EventManager.RegisterClassHandler(
            typeof(OverlayMiniMapWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunOtherFloorMarkerLoaded));
        return true;
    }

    private static void OnJunhyunOtherFloorMarkerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is OverlayMiniMapWindow window)
            window.InitializeJunhyunOtherFloorMarkerPresentation();
    }

    private void InitializeJunhyunOtherFloorMarkerPresentation()
    {
        if (_junhyunOtherFloorMarkerTimer is not null)
            return;

        _junhyunOtherFloorExtractLayer = new Canvas
        {
            IsHitTestVisible = false,
            ClipToBounds = false,
        };
        Panel.SetZIndex(_junhyunOtherFloorExtractLayer, 555);
        MapCanvas.Children.Add(_junhyunOtherFloorExtractLayer);

        Closed += JunhyunOtherFloorMarker_Closed;
        _junhyunOtherFloorMarkerTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(200),
            DispatcherPriority.Background,
            (_, _) => RefreshJunhyunOtherFloorMarkerPresentation(),
            Dispatcher);
        _junhyunOtherFloorMarkerTimer.Start();
        RefreshJunhyunOtherFloorMarkerPresentation();
    }

    private void RefreshJunhyunOtherFloorMarkerPresentation()
    {
        if (string.IsNullOrWhiteSpace(_currentMapKey) || _currentMapConfig is null)
            return;

        ApplyJunhyunStandardMarkerFloorPresentation();
        RenderJunhyunOtherFloorExtracts();
    }

    private void ApplyJunhyunStandardMarkerFloorPresentation()
    {
        foreach (var canvas in MapMarkersContainer.Children.OfType<Canvas>())
        {
            if (canvas.Tag is not MapMarker marker)
                continue;

            var relation = JunhyunFloorPresentation.Resolve(
                _currentMapConfig,
                marker.FloorId,
                _selectedFloorId);
            if (relation.IsOtherFloor)
            {
                JunhyunFloorPresentation.ApplyToMarker(canvas, relation, badgeOffsetX: 6, badgeOffsetY: -14);
            }
            else
            {
                JunhyunFloorPresentation.RemoveDirectionBadge(canvas);
                canvas.Opacity = 0.95;
            }
        }
    }

    private void RenderJunhyunOtherFloorExtracts()
    {
        var layer = _junhyunOtherFloorExtractLayer;
        if (layer is null || _currentMapConfig is null || string.IsNullOrWhiteSpace(_currentMapKey))
            return;

        var settings = MapSettings.Instance;
        if (!settings.ShowExtracts)
        {
            if (layer.Children.Count > 0)
                layer.Children.Clear();
            _junhyunOtherFloorExtractSignature = int.MinValue;
            return;
        }

        // ExtractService finishes loading asynchronously. Do not cache an empty state
        // while it is still loading or the later valid data would be skipped.
        if (!ExtractService.Instance.IsLoaded)
        {
            _junhyunOtherFloorExtractSignature = int.MinValue;
            return;
        }

        var extracts = ExtractService.Instance.GetExtractsForMap(_currentMapKey, _currentMapConfig);
        var displays = MapExtractDisplayGrouping.GroupForDisplay(extracts).ToArray();

        var signature = new System.HashCode();
        signature.Add(_currentMapKey, StringComparer.OrdinalIgnoreCase);
        signature.Add(_selectedFloorId, StringComparer.OrdinalIgnoreCase);
        signature.Add(settings.ShowPmcExtracts);
        signature.Add(settings.ShowScavExtracts);
        signature.Add(settings.ShowTransits);
        signature.Add(settings.ExtractNameSize);
        signature.Add(_junhyunMarkerScale);
        foreach (var display in displays)
        {
            signature.Add(display.Extract.Name, StringComparer.Ordinal);
            signature.Add(display.Extract.FloorId, StringComparer.OrdinalIgnoreCase);
            signature.Add(display.Extract.X);
            signature.Add(display.Extract.Z);
            signature.Add(display.Faction);
        }
        var currentSignature = signature.ToHashCode();
        if (currentSignature == _junhyunOtherFloorExtractSignature)
            return;

        layer.Children.Clear();
        _junhyunOtherFloorExtractSignature = currentSignature;

        foreach (var display in displays)
        {
            if (!IsExtractVisible(settings, display.Faction))
                continue;

            var extract = display.Extract;
            var relation = JunhyunFloorPresentation.Resolve(
                _currentMapConfig,
                extract.FloorId,
                _selectedFloorId);
            if (!relation.IsOtherFloor)
                continue;

            var (screenX, screenY) = _currentMapConfig.GameToScreenForPlayer(extract.X, extract.Z);
            var visual = CreateSynchronizedExtractVisual(extract, display.Faction, currentFloor: false);
            JunhyunFloorPresentation.ApplyToMarker(visual, relation, badgeOffsetX: 6, badgeOffsetY: -14);
            Canvas.SetLeft(visual, screenX);
            Canvas.SetTop(visual, screenY);
            layer.Children.Add(visual);
        }
    }

    private void JunhyunOtherFloorMarker_Closed(object? sender, EventArgs e)
    {
        Closed -= JunhyunOtherFloorMarker_Closed;
        if (_junhyunOtherFloorMarkerTimer is not null)
        {
            _junhyunOtherFloorMarkerTimer.Stop();
            _junhyunOtherFloorMarkerTimer = null;
        }

        if (_junhyunOtherFloorExtractLayer is not null)
        {
            MapCanvas.Children.Remove(_junhyunOtherFloorExtractLayer);
            _junhyunOtherFloorExtractLayer = null;
        }
    }
}
