using System.Windows;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private const int JunhyunStandardMarkerRepairTicks = 4;

    private DispatcherTimer? _junhyunMarkerRecoveryTimer;
    private string? _junhyunMarkerRecoveryMapKey;
    private string? _junhyunMarkerRecoveryFloorId;
    private int _junhyunEmptyStandardMarkerTicks;

    public void InitializeJunhyunMarkerRefreshRecovery()
    {
        if (_junhyunMarkerRecoveryTimer is not null)
            return;

        _junhyunMarkerRecoveryTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(240),
            DispatcherPriority.Background,
            JunhyunMarkerRecoveryTimer_Tick,
            Dispatcher);
        _junhyunMarkerRecoveryTimer.Start();
    }

    public void DisposeJunhyunMarkerRefreshRecovery()
    {
        if (_junhyunMarkerRecoveryTimer is null)
            return;

        _junhyunMarkerRecoveryTimer.Stop();
        _junhyunMarkerRecoveryTimer.Tick -= JunhyunMarkerRecoveryTimer_Tick;
        _junhyunMarkerRecoveryTimer = null;
        ResetJunhyunMarkerRecoverySnapshot();
    }

    private void JunhyunMarkerRecoveryTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsLoaded || string.IsNullOrWhiteSpace(_currentMapKey) || _currentMapConfig is null)
        {
            ResetJunhyunMarkerRecoverySnapshot();
            return;
        }

        RepairEmptyExtractProjectionIfNeeded();
        ObserveStandardMarkerLayer();
    }

    private void RepairEmptyExtractProjectionIfNeeded()
    {
        if (ExtractMarkersContainer.Children.Count != 0 || !ExtractService.Instance.IsLoaded)
            return;

        var settings = MapSettings.Instance;
        if (!settings.ShowExtracts ||
            (!settings.ShowPmcExtracts && !settings.ShowScavExtracts && !settings.ShowTransits))
        {
            return;
        }

        var hasVisibleExtract = MapExtractDisplayGrouping
            .GroupForDisplay(ExtractService.Instance.GetExtractsForMap(_currentMapKey!, _currentMapConfig!))
            .Any(display => IsExtractVisible(settings, display.Faction));
        if (hasVisibleExtract)
            SynchronizeExtractPresentation(force: true);
    }

    private void ObserveStandardMarkerLayer()
    {
        var standardMarkerCount = MapMarkersContainer.Children
            .Cast<FrameworkElement>()
            .Count(static marker => marker.Tag is not JunhyunAdditionalMapMarker);

        if (!string.Equals(_junhyunMarkerRecoveryMapKey, _currentMapKey, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_junhyunMarkerRecoveryFloorId, _selectedFloorId, StringComparison.OrdinalIgnoreCase))
        {
            _junhyunMarkerRecoveryMapKey = _currentMapKey;
            _junhyunMarkerRecoveryFloorId = _selectedFloorId;
            _junhyunEmptyStandardMarkerTicks = 0;
        }

        if (standardMarkerCount > 0)
        {
            _junhyunEmptyStandardMarkerTicks = 0;
            return;
        }

        if (!HasExpectedStandardMarkers())
        {
            _junhyunEmptyStandardMarkerTicks = 0;
            return;
        }

        // The donor refresh clears the live marker layer before its asynchronous work.
        // A later refresh can cancel that work after the clear, leaving the layer empty.
        // Wait long enough to ignore a normal transient clear, then rebuild only the
        // standard marker layer from the already-loaded in-memory marker DB. Do not call
        // QueueMarkerRefresh here: doing so would create another clear/cancel race.
        _junhyunEmptyStandardMarkerTicks++;
        if (_junhyunEmptyStandardMarkerTicks < JunhyunStandardMarkerRepairTicks)
            return;

        _junhyunEmptyStandardMarkerTicks = 0;
        RebuildStandardMarkerLayerFromLoadedData();
    }

    private bool HasExpectedStandardMarkers()
    {
        var markerService = MapMarkerDbService.Instance;
        if (!markerService.IsLoaded || string.IsNullOrWhiteSpace(_currentMapKey))
            return false;

        var visibility = MiniMapMarkerVisibilityState.Capture(MapSettings.Instance);
        return markerService
            .GetMarkersForMap(_currentMapKey)
            .Any(marker => visibility.IsMapMarkerVisible(marker.Type));
    }

    private void RebuildStandardMarkerLayerFromLoadedData()
    {
        var markerService = MapMarkerDbService.Instance;
        if (!markerService.IsLoaded || string.IsNullOrWhiteSpace(_currentMapKey) || _currentMapConfig is null)
            return;

        var visibility = MiniMapMarkerVisibilityState.Capture(MapSettings.Instance);
        var added = 0;
        foreach (var marker in markerService.GetMarkersForMap(_currentMapKey))
        {
            if (!visibility.IsMapMarkerVisible(marker.Type))
                continue;

            var (screenX, screenY) = _currentMapConfig.GameToScreenForPlayer(marker.X, marker.Z);
            var isCurrentFloor = IsCurrentFloor(marker.FloorId, _selectedFloorId);
            var element = CreateMapMarkerElement(marker, screenX, screenY, isCurrentFloor);
            MapMarkersContainer.Children.Add(element);
            added++;
        }

        if (added <= 0)
            return;

        _junhyunLastGeneralMarkerSignature = int.MinValue;
        SynchronizeGeneralMarkerScale(force: true);
        RenderJunhyunAdditionalMarkers(force: true);
    }

    private void ResetJunhyunMarkerRecoverySnapshot()
    {
        _junhyunMarkerRecoveryMapKey = null;
        _junhyunMarkerRecoveryFloorId = null;
        _junhyunEmptyStandardMarkerTicks = 0;
    }
}
