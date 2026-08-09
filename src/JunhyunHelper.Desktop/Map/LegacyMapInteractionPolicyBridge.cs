using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Enforces JunhyunHelper product policies while leaving the exact Tarkov Helper
/// floor selector and its native floor-change loading path intact.
/// </summary>
public sealed class LegacyMapInteractionPolicyBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly ComboBox? _floorSelector;
    private readonly ComboBox? _mapSelector;
    private readonly Canvas? _mapMarkers;
    private readonly Canvas? _extractMarkers;
    private readonly DispatcherTimer _policyTimer;
    private bool _disposed;

    public LegacyMapInteractionPolicyBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _page.EnableJunhyunManualFloorPolicy();

        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;
        _mapSelector = _page.FindName("CmbMapSelect") as ComboBox;
        _mapMarkers = _page.FindName("MapMarkersContainer") as Canvas;
        _extractMarkers = _page.FindName("ExtractMarkersContainer") as Canvas;

        RemoveCustomMarkers();
        ApplyFixedPolicies();

        _tracker.PositionUpdated += Tracker_PositionUpdated;
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;

        _policyTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(200),
            DispatcherPriority.Background,
            (_, _) => MaintainPolicies(),
            _page.Dispatcher);
        _policyTimer.Start();
        _page.Dispatcher.BeginInvoke(MaintainPolicies, DispatcherPriority.Loaded);
    }

    private void MaintainPolicies()
    {
        if (_disposed)
            return;

        ApplyFixedPolicies();
        EnforceCurrentFloorAndFilters();
        RemoveCustomMarkers();
    }

    private void ApplyFixedPolicies()
    {
        MapSettings.Instance.AutoCenterEnabled = true;
        MapSettings.Instance.AutoFloorEnabled = false;

        var settings = _overlay.Settings;
        var changed = settings.OtherFloorOpacity != 0.0 ||
                      settings.AutoFloorSelection ||
                      settings.ViewMode != MiniMapViewMode.PlayerTracking ||
                      !settings.ClickThrough ||
                      settings.Opacity != 1.0 ||
                      settings.ResumeAutoFloorKey != 0;

        settings.OtherFloorOpacity = 0.0;
        settings.AutoFloorSelection = false;
        settings.ViewMode = MiniMapViewMode.PlayerTracking;
        settings.ClickThrough = true;
        settings.Opacity = 1.0;
        settings.ResumeAutoFloorKey = 0;
        GlobalKeyboardHookService.Instance.ResumeAutoFloorKey = 0;

        if (changed)
            _overlay.SaveSettings();
    }

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(EnforceCurrentFloorAndFilters, DispatcherPriority.Background);

    private void EnforceCurrentFloorAndFilters()
    {
        var selectedFloor = (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;
        var settings = MapSettings.Instance;

        if (_mapMarkers is not null)
        {
            foreach (FrameworkElement child in _mapMarkers.Children)
            {
                if (child.Tag is not MapMarker marker)
                    continue;

                child.Visibility = IsCurrentFloor(marker.FloorId, selectedFloor) &&
                                   IsGeneralMarkerEnabled(marker.Type, settings)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        if (_extractMarkers is not null)
        {
            foreach (FrameworkElement child in _extractMarkers.Children)
            {
                if (child.Tag is not MapExtract extract)
                    continue;

                child.Visibility = IsCurrentFloor(extract.FloorId, selectedFloor) &&
                                   IsExtractEnabled(extract.Faction, settings)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

    private static bool IsGeneralMarkerEnabled(MarkerType type, MapSettings settings) => type switch
    {
        MarkerType.PmcSpawn => settings.ShowPmcSpawns,
        MarkerType.SniperScavSpawn => settings.ShowSniperScavs,
        MarkerType.RogueSpawn => settings.ShowRogues,
        MarkerType.CultistSpawn => settings.ShowCultists,
        MarkerType.Lever => settings.ShowLevers,
        MarkerType.BossSpawn => settings.ShowBosses,
        _ => true,
    };

    private static bool IsExtractEnabled(ExtractFaction faction, MapSettings settings) => faction switch
    {
        ExtractFaction.Pmc => settings.ShowPmcExtracts,
        ExtractFaction.Scav => settings.ShowScavExtracts,
        ExtractFaction.Shared => settings.ShowPmcExtracts || settings.ShowScavExtracts,
        ExtractFaction.Transit => settings.ShowTransits,
        _ => true,
    };

    private static bool IsCurrentFloor(string? markerFloor, string? selectedFloor)
    {
        if (string.IsNullOrWhiteSpace(selectedFloor))
            return true;

        var effectiveMarkerFloor = string.IsNullOrWhiteSpace(markerFloor) ? "main" : markerFloor;
        return string.Equals(effectiveMarkerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    private void Tracker_PositionUpdated(object? sender, ScreenPosition position)
    {
        if (string.IsNullOrWhiteSpace(position.MapKey))
            return;

        _page.Dispatcher.BeginInvoke(() => SwitchMapFromScreenshot(position.MapKey));
    }

    private void SwitchMapFromScreenshot(string mapKey)
    {
        if (_mapSelector is null)
            return;

        var selectedMapKey = (_mapSelector.SelectedItem as ComboBoxItem)?.Tag as string;
        if (!string.Equals(selectedMapKey, mapKey, StringComparison.OrdinalIgnoreCase))
        {
            for (var i = 0; i < _mapSelector.Items.Count; i++)
            {
                if (_mapSelector.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Tag as string, mapKey, StringComparison.OrdinalIgnoreCase))
                {
                    _mapSelector.SelectedIndex = i;
                    break;
                }
            }
        }

        if (!string.Equals(_tracker.CurrentMapKey, mapKey, StringComparison.OrdinalIgnoreCase))
            _tracker.SetCurrentMap(mapKey);
    }

    private void Overlay_SettingsChanged(OverlayMiniMapSettings settings)
    {
        if (!_disposed)
            _page.Dispatcher.BeginInvoke(ApplyFixedPolicies);
    }

    private void RemoveCustomMarkers()
    {
        Collapse("MarkerListPanel");
        Collapse("BtnToggleCustomMarkersPanel");
        Collapse("CustomMarkersContainer");
        Collapse("MenuAddCustomMarker");

        if (_page.FindName("MarkerListColumn") is ColumnDefinition markerColumn)
        {
            markerColumn.MinWidth = 0;
            markerColumn.Width = new GridLength(0);
        }

        if (_page.FindName("MarkerListPanel") is FrameworkElement panel && panel.Parent is Grid grid &&
            grid.ColumnDefinitions.Count > 4)
        {
            grid.ColumnDefinitions[3].Width = new GridLength(0);
            grid.ColumnDefinitions[4].Width = new GridLength(0);
        }
    }

    private void Collapse(string name)
    {
        if (_page.FindName(name) is FrameworkElement element)
        {
            element.Visibility = Visibility.Collapsed;
            element.IsEnabled = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _policyTimer.Stop();
        _tracker.PositionUpdated -= Tracker_PositionUpdated;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
    }
}
