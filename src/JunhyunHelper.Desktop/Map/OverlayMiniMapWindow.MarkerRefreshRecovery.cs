using System.Windows;
using System.Windows.Threading;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private DispatcherTimer? _junhyunMarkerRecoveryTimer;
    private string? _junhyunMarkerRecoveryMapKey;
    private string? _junhyunMarkerRecoveryFloorId;
    private int _junhyunLastStableStandardMarkerCount;
    private int _junhyunEmptyStandardMarkerTicks;
    private bool _junhyunStandardMarkerRecoveryAttempted;

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
            _junhyunLastStableStandardMarkerCount = standardMarkerCount;
            _junhyunEmptyStandardMarkerTicks = 0;
            _junhyunStandardMarkerRecoveryAttempted = false;
            return;
        }

        if (standardMarkerCount > 0)
        {
            _junhyunLastStableStandardMarkerCount = standardMarkerCount;
            _junhyunEmptyStandardMarkerTicks = 0;
            _junhyunStandardMarkerRecoveryAttempted = false;
            return;
        }

        // Zero is only suspicious after this exact map/floor previously rendered standard
        // markers. Two observations filter out the donor's normal clear-then-repopulate
        // interval. A single recovery attempt prevents a user intentionally hiding every
        // category from creating a refresh loop; reappearing markers arm recovery again.
        if (_junhyunLastStableStandardMarkerCount <= 0 || _junhyunStandardMarkerRecoveryAttempted)
            return;

        _junhyunEmptyStandardMarkerTicks++;
        if (_junhyunEmptyStandardMarkerTicks < 2)
            return;

        _junhyunStandardMarkerRecoveryAttempted = true;
        _junhyunEmptyStandardMarkerTicks = 0;
        QueueMarkerRefresh();
    }

    private void ResetJunhyunMarkerRecoverySnapshot()
    {
        _junhyunMarkerRecoveryMapKey = null;
        _junhyunMarkerRecoveryFloorId = null;
        _junhyunLastStableStandardMarkerCount = 0;
        _junhyunEmptyStandardMarkerTicks = 0;
        _junhyunStandardMarkerRecoveryAttempted = false;
    }
}
