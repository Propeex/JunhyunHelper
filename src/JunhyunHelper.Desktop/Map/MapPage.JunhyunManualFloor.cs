using TarkovHelper.Models.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _junhyunManualFloorPolicyEnabled;

    /// <summary>
    /// Replaces the exact MapPage screenshot-position callback with the same player
    /// tracking behavior minus screenshot-based floor inference. Map detection is
    /// handled separately by the JunhyunHelper product bridge.
    /// </summary>
    public void EnableJunhyunManualFloorPolicy()
    {
        if (_junhyunManualFloorPolicyEnabled || _trackerService is null)
            return;

        _junhyunManualFloorPolicyEnabled = true;
        _trackerService.PositionUpdated -= OnPositionUpdated;
        _trackerService.PositionUpdated += OnJunhyunPositionUpdated;
        MapSettings.Instance.AutoFloorEnabled = false;
    }

    private void OnJunhyunPositionUpdated(object? sender, ScreenPosition position)
    {
        DispatchUi(() =>
        {
            UpdateMarkerPosition(position);
            UpdateTrailPath();
            UpdateCoordinatesDisplay(position);

            // Product policy: screenshot coordinates never choose a floor.
            if (MapSettings.Instance.AutoCenterEnabled)
                CenterOnPosition(position);
        });
    }
}
