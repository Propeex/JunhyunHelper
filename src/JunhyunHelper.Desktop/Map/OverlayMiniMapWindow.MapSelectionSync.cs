using TarkovHelper.Services.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Product-owned immediate map-selection bridge. The donor MapChanged event remains
    /// authoritative; this closes the UI timing gap between a Main Map combo selection
    /// and the asynchronous donor event callback when the MiniMap is already visible.
    /// </summary>
    internal void SynchronizeJunhyunMapSelection(string mapKey)
    {
        if (!IsLoaded || _trackerService is null || string.IsNullOrWhiteSpace(mapKey))
            return;

        var canonical = MapTrackerService.Instance.ResolveMapKey(mapKey) ?? mapKey;
        if (string.Equals(_currentMapKey, canonical, StringComparison.OrdinalIgnoreCase) &&
            _currentMapConfig is not null)
        {
            return;
        }

        LoadMap(canonical);
    }

    internal string? JunhyunCurrentMapKey => _currentMapKey;
}
