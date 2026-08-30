using TarkovHelper.Services.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private string? _junhyunPendingMapKey;
    private bool _junhyunPendingMapHooked;

    /// <summary>
    /// Product-owned immediate map-selection bridge. The donor MapChanged event remains
    /// authoritative; this closes the UI timing gap between a Main Map combo selection
    /// and the asynchronous donor event callback. Selections received before Loaded are
    /// retained and replayed after the donor tracker initializes.
    /// </summary>
    internal void SynchronizeJunhyunMapSelection(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        var canonical = MapTrackerService.Instance.ResolveMapKey(mapKey) ?? mapKey;
        if (!IsLoaded || _trackerService is null)
        {
            _junhyunPendingMapKey = canonical;
            EnsureJunhyunPendingMapReplay();
            return;
        }

        _junhyunPendingMapKey = null;
        if (string.Equals(_currentMapKey, canonical, StringComparison.OrdinalIgnoreCase) &&
            _currentMapConfig is not null)
        {
            return;
        }

        LoadMap(canonical);
    }

    internal string? JunhyunCurrentMapKey => _currentMapKey;

    private void EnsureJunhyunPendingMapReplay()
    {
        if (_junhyunPendingMapHooked)
            return;

        Loaded += JunhyunMiniMap_ReplayPendingMapSelection;
        _junhyunPendingMapHooked = true;
    }

    private void JunhyunMiniMap_ReplayPendingMapSelection(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= JunhyunMiniMap_ReplayPendingMapSelection;
        _junhyunPendingMapHooked = false;

        var pending = _junhyunPendingMapKey;
        if (!string.IsNullOrWhiteSpace(pending))
            SynchronizeJunhyunMapSelection(pending);
    }
}
