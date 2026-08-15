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
    /// The exact Tarkov Helper floor selector itself remains the manual floor control.
    /// </summary>
    public void EnableJunhyunManualFloorPolicy()
    {
        if (_junhyunManualFloorPolicyEnabled || _trackerService is null)
            return;

        _junhyunManualFloorPolicyEnabled = true;
        _trackerService.PositionUpdated -= OnPositionUpdated;
        _trackerService.PositionUpdated += OnJunhyunPositionUpdated;
        MapSettings.Instance.AutoFloorEnabled = false;

        // Keep the original floor-change implementation, but put a product policy hook
        // immediately before it. The legacy loader treats MapFloorConfig.IsDefault as a
        // semi-transparent background floor. JunhyunHelper requires other floors at 0%,
        // so the currently selected floor becomes the temporary visual default and no
        // second floor is emitted as a background layer.
        CmbFloorSelect.SelectionChanged -= CmbFloorSelect_SelectionChanged;
        CmbFloorSelect.SelectionChanged += OnJunhyunManualFloorPreparing;
        CmbFloorSelect.SelectionChanged += CmbFloorSelect_SelectionChanged;
    }

    /// <summary>
    /// The transplanted shared-floor integration owns an older current-floor-only marker
    /// filter and screenshot-driven automatic-floor behavior. Both conflict with the
    /// JunhyunHelper product contract. The product runtime calls this after Loaded so the
    /// legacy class handler has had a chance to attach, then removes that integration at
    /// its source instead of racing its 200 ms marker-filter timer.
    /// </summary>
    public void DisableJunhyunLegacySharedFloorIntegration() =>
        DetachSharedFloorIntegration();

    private void OnJunhyunManualFloorPreparing(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFloorSelect.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string selectedFloor ||
            string.IsNullOrWhiteSpace(selectedFloor))
        {
            return;
        }

        var mapKey = _currentMapKey;
        if (string.IsNullOrWhiteSpace(mapKey) && CmbMapSelect.SelectedItem is ComboBoxItem mapItem)
            mapKey = mapItem.Tag as string;

        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        var floors = _trackerService?.GetMapConfig(mapKey)?.Floors;
        if (floors is null || floors.Count == 0)
            return;

        foreach (var floor in floors)
        {
            floor.IsDefault = string.Equals(
                floor.LayerId,
                selectedFloor,
                StringComparison.OrdinalIgnoreCase);
        }
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
