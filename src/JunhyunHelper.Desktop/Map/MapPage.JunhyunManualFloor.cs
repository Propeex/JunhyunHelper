using System.Windows.Controls;
using TarkovHelper.Models.Map;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private bool _junhyunManualFloorPolicyEnabled;

    /// <summary>
    /// Replaces the exact MapPage screenshot-position callback. Screenshot data may
    /// select the Map and update the player position/heading, but never selects a floor.
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
            SelectMapDetectedFromScreenshot(position.MapKey);
            UpdateMarkerPosition(position);
            UpdateTrailPath();
            UpdateCoordinatesDisplay(position);

            // Product policy: screenshot coordinates never choose a floor.
            if (MapSettings.Instance.AutoCenterEnabled)
                CenterOnPosition(position);
        });
    }

    private void SelectMapDetectedFromScreenshot(string detectedMapKey)
    {
        if (string.IsNullOrWhiteSpace(detectedMapKey))
            return;

        var mapKey = _trackerService?.ResolveMapKey(detectedMapKey) ?? detectedMapKey;
        if (string.Equals(_currentMapKey, mapKey, StringComparison.OrdinalIgnoreCase))
            return;

        for (var i = 0; i < CmbMapSelect.Items.Count; i++)
        {
            if (CmbMapSelect.Items[i] is ComboBoxItem item &&
                string.Equals(item.Tag as string, mapKey, StringComparison.OrdinalIgnoreCase))
            {
                CmbMapSelect.SelectedIndex = i;
                return;
            }
        }
    }
}
