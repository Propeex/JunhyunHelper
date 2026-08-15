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
///
/// Important: floor is a presentation relation, not a visibility filter. Enabled
/// markers/extracts from other floors stay visible so JunhyunFloorPresentation can
/// distinguish current/above/below. A short stabilization window handles late legacy
/// refreshes without the old permanent 200 ms full-tree scan.
/// </summary>
public sealed class LegacyMapInteractionPolicyBridge : IDisposable
{
    private const int StabilizationPasses = 8;

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly Action? _presentationRefresh;
    private readonly ComboBox? _floorSelector;
    private readonly ComboBox? _mapSelector;
    private readonly Canvas? _mapMarkers;
    private readonly Canvas? _extractMarkers;
    private readonly CheckBox? _pmcSpawnToggle;
    private readonly CheckBox? _sniperToggle;
    private readonly CheckBox? _rogueToggle;
    private readonly CheckBox? _cultistToggle;
    private readonly CheckBox? _leverToggle;
    private readonly CheckBox? _bossToggle;
    private readonly CheckBox? _pmcExtractToggle;
    private readonly CheckBox? _scavExtractToggle;
    private readonly CheckBox? _transitToggle;
    private readonly DispatcherTimer _stabilizationTimer;
    private int _stabilizationPassesRemaining;
    private bool _disposed;

    public LegacyMapInteractionPolicyBridge(
        TarkovHelper.Pages.Map.MapPage page,
        Action? presentationRefresh = null)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _presentationRefresh = presentationRefresh;
        _page.EnableJunhyunManualFloorPolicy();

        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;
        _mapSelector = _page.FindName("CmbMapSelect") as ComboBox;
        _mapMarkers = _page.FindName("MapMarkersContainer") as Canvas;
        _extractMarkers = _page.FindName("ExtractMarkersContainer") as Canvas;
        _pmcSpawnToggle = _page.FindName("ChkShowPmcSpawns") as CheckBox;
        _sniperToggle = _page.FindName("ChkShowSniperScavs") as CheckBox;
        _rogueToggle = _page.FindName("ChkShowRogues") as CheckBox;
        _cultistToggle = _page.FindName("ChkShowCultists") as CheckBox;
        _leverToggle = _page.FindName("ChkShowLeversMarker") as CheckBox;
        _bossToggle = _page.FindName("ChkShowBosses") as CheckBox;
        _pmcExtractToggle = _page.FindName("ChkShowPmcExtracts") as CheckBox;
        _scavExtractToggle = _page.FindName("ChkShowScavExtracts") as CheckBox;
        _transitToggle = _page.FindName("ChkShowTransitExtracts") as CheckBox;

        _stabilizationTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(180),
            DispatcherPriority.Background,
            (_, _) => StabilizationTick(),
            _page.Dispatcher)
        {
            IsEnabled = false,
        };

        RemoveCustomMarkers();
        ApplyFixedPolicies();
        ApplyProductMarkerFilters();

        _tracker.PositionUpdated += Tracker_PositionUpdated;
        _tracker.MapChanged += Tracker_MapChanged;
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        _page.Loaded += Page_Loaded;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;

        HookFilter(_pmcSpawnToggle);
        HookFilter(_sniperToggle);
        HookFilter(_rogueToggle);
        HookFilter(_cultistToggle);
        HookFilter(_leverToggle);
        HookFilter(_bossToggle);
        HookFilter(_pmcExtractToggle);
        HookFilter(_scavExtractToggle);
        HookFilter(_transitToggle);

        // If the page is already loaded, the transplanted class handler may already have
        // attached its obsolete shared-floor filter. Otherwise Page_Loaded performs this
        // after that class handler runs.
        if (_page.IsLoaded)
            ScheduleLegacySharedFloorDetach();

        RestartStabilization();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) =>
        ScheduleLegacySharedFloorDetach();

    private void ScheduleLegacySharedFloorDetach()
    {
        if (_disposed)
            return;

        _page.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
                return;

            // The pinned SharedFloor integration contains a bounded 200 ms
            // current-floor-only marker filter. Leaving it attached makes it race the
            // product presentation and eventually hide enabled off-floor markers.
            _page.DisableJunhyunLegacySharedFloorIntegration();
            ApplyFixedPolicies();
            ApplyProductMarkerFilters();
            _presentationRefresh?.Invoke();
            RestartStabilization();
        }, DispatcherPriority.ContextIdle);
    }

    private void RestartStabilization()
    {
        if (_disposed)
            return;

        _stabilizationPassesRemaining = StabilizationPasses;
        _stabilizationTimer.Stop();
        _stabilizationTimer.Start();
    }

    private void StabilizationTick()
    {
        if (_disposed)
        {
            _stabilizationTimer.Stop();
            return;
        }

        ApplyFixedPolicies();
        ApplyProductMarkerFilters();
        RemoveCustomMarkers();

        _stabilizationPassesRemaining--;
        if (_stabilizationPassesRemaining <= 0)
            _stabilizationTimer.Stop();
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

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _page.Dispatcher.BeginInvoke(() =>
        {
            ApplyProductMarkerFilters();
            _presentationRefresh?.Invoke();
        }, DispatcherPriority.Background);
        RestartStabilization();
    }

    private void Tracker_MapChanged(string mapKey)
    {
        _page.Dispatcher.BeginInvoke(() =>
        {
            ApplyProductMarkerFilters();
            _presentationRefresh?.Invoke();
        }, DispatcherPriority.Background);
        RestartStabilization();
    }

    private void ApplyProductMarkerFilters()
    {
        // Floor must never participate in visibility. The dedicated floor presentation
        // bridges own current/above/below opacity/ring semantics.
        if (_mapMarkers is not null)
        {
            foreach (FrameworkElement child in _mapMarkers.Children)
            {
                if (child.Tag is not MapMarker marker)
                    continue;

                child.Visibility = IsGeneralMarkerEnabled(marker.Type)
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

                child.Visibility = IsExtractEnabled(extract.Faction)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

    private bool IsGeneralMarkerEnabled(MarkerType type) => type switch
    {
        MarkerType.PmcSpawn => IsChecked(_pmcSpawnToggle),
        MarkerType.SniperScavSpawn => IsChecked(_sniperToggle),
        MarkerType.RogueSpawn => IsChecked(_rogueToggle),
        MarkerType.CultistSpawn => IsChecked(_cultistToggle),
        MarkerType.Lever => IsChecked(_leverToggle),
        MarkerType.BossSpawn => IsChecked(_bossToggle),
        _ => true,
    };

    private bool IsExtractEnabled(ExtractFaction faction) => faction switch
    {
        ExtractFaction.Pmc => IsChecked(_pmcExtractToggle),
        ExtractFaction.Scav => IsChecked(_scavExtractToggle),
        ExtractFaction.Shared => IsChecked(_pmcExtractToggle) || IsChecked(_scavExtractToggle),
        ExtractFaction.Transit => IsChecked(_transitToggle),
        _ => true,
    };

    private static bool IsChecked(CheckBox? checkBox) => checkBox?.IsChecked != false;

    private void HookFilter(CheckBox? checkBox)
    {
        if (checkBox is null)
            return;
        checkBox.Checked += FilterToggle_Changed;
        checkBox.Unchecked += FilterToggle_Changed;
    }

    private void UnhookFilter(CheckBox? checkBox)
    {
        if (checkBox is null)
            return;
        checkBox.Checked -= FilterToggle_Changed;
        checkBox.Unchecked -= FilterToggle_Changed;
    }

    private void FilterToggle_Changed(object sender, RoutedEventArgs e)
    {
        _page.Dispatcher.BeginInvoke(() =>
        {
            ApplyProductMarkerFilters();
            _presentationRefresh?.Invoke();
        }, DispatcherPriority.Background);
        RestartStabilization();
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
        if (_disposed)
            return;

        _page.Dispatcher.BeginInvoke(ApplyFixedPolicies);
        RestartStabilization();
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

        _stabilizationTimer.Stop();
        _tracker.PositionUpdated -= Tracker_PositionUpdated;
        _tracker.MapChanged -= Tracker_MapChanged;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _page.Loaded -= Page_Loaded;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;

        UnhookFilter(_pmcSpawnToggle);
        UnhookFilter(_sniperToggle);
        UnhookFilter(_rogueToggle);
        UnhookFilter(_cultistToggle);
        UnhookFilter(_leverToggle);
        UnhookFilter(_bossToggle);
        UnhookFilter(_pmcExtractToggle);
        UnhookFilter(_scavExtractToggle);
        UnhookFilter(_transitToggle);
    }
}
