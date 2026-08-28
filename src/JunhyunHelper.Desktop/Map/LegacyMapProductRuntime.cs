using System.Windows;
using System.Windows.Controls;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Runtime-only product controls layered on the exact Tarkov Helper Map subsystem.
/// </summary>
public sealed class LegacyMapProductRuntime : IDisposable
{
    private const double SharedPlayerMarkerMinPixels = 9.0;
    private const double SharedPlayerMarkerMaxPixels = 54.0;
    private const string MapSmokeEnvironmentVariable = "JUNHYUNHELPER_MAP_SMOKE";
    private const string MapSmokeSuccessFileName = "junhyun-map-smoke-success.txt";
    private const string MapSmokeDiagnosticFileName = "junhyun-map-smoke-error.txt";
    private const string MiniMapSelectionSmokeFileName = "junhyun-minimap-selection-sync-smoke-success.txt";

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly JunhyunMapHotkeyService _hotkeys;
    private readonly LegacyQuestPresentationSettingsBridge _questSettingsBridge;
    private readonly LegacyMapHotkeySettingsBridge _hotkeySettingsBridge;
    private readonly LegacyMiniMapOpacitySettingsBridge _miniMapOpacitySettingsBridge;
    private readonly LegacyMapMarkerSettingsV2Bridge _markerSettingsBridge;
    private readonly LegacyStandardMarkerFloorPresentationBridge _standardMarkerFloorPresentationBridge;
    private readonly LegacyExtractMarkerPresentationBridge _extractMarkerPresentationBridge;
    private readonly LegacyMapInteractionPolicyBridge _interactionPolicyBridge;
    private readonly LegacyQuestMarkerRenderV3 _questMarkerRenderer;
    private readonly LegacyMapSettingsPersistenceBridge _settingsPersistenceBridge;
    private readonly LegacyMapViewportPolishBridge _viewportPolishBridge;
    private readonly LegacyMapSelectionConsistencyBridge _selectionConsistencyBridge;
    private readonly Slider? _playerMarkerSlider;
    private bool _syncingPlayerMarker;
    private bool _miniMapReopenSmokeStarted;
    private bool _disposed;

    public LegacyMapProductRuntime(
        TarkovHelper.Pages.Map.MapPage page,
        Action refreshQuestProjection)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        ArgumentNullException.ThrowIfNull(refreshQuestProjection);

        _ = new LegacyExtractSettingsMergeBridge(page);
        _questSettingsBridge = new LegacyQuestPresentationSettingsBridge(page, refreshQuestProjection);
        _hotkeySettingsBridge = new LegacyMapHotkeySettingsBridge(page);
        _miniMapOpacitySettingsBridge = new LegacyMiniMapOpacitySettingsBridge(page);
        _markerSettingsBridge = new LegacyMapMarkerSettingsV2Bridge(page);
        _standardMarkerFloorPresentationBridge = new LegacyStandardMarkerFloorPresentationBridge(page);
        _extractMarkerPresentationBridge = new LegacyExtractMarkerPresentationBridge(page);
        _interactionPolicyBridge = new LegacyMapInteractionPolicyBridge(
            page,
            () =>
            {
                _standardMarkerFloorPresentationBridge.Refresh();
                _extractMarkerPresentationBridge.Refresh();
            });
        _questMarkerRenderer = new LegacyQuestMarkerRenderV3(page);
        _settingsPersistenceBridge = new LegacyMapSettingsPersistenceBridge(page);
        _viewportPolishBridge = new LegacyMapViewportPolishBridge(page);
        _selectionConsistencyBridge = new LegacyMapSelectionConsistencyBridge(page);
        _hotkeys = new JunhyunMapHotkeyService(page);
        _playerMarkerSlider = _page.FindName("SliderPlayerMarkerSize") as Slider;

        GlobalKeyboardHookService.Instance.DirectFloorSelectionPressed += DirectFloorSelectionPressed;

        if (_playerMarkerSlider is not null)
        {
            _playerMarkerSlider.Minimum = SharedPlayerMarkerMinPixels;
            _playerMarkerSlider.Maximum = SharedPlayerMarkerMaxPixels;
            _playerMarkerSlider.Value = Math.Clamp(
                _playerMarkerSlider.Value,
                SharedPlayerMarkerMinPixels,
                SharedPlayerMarkerMaxPixels);
            _playerMarkerSlider.ValueChanged += PlayerMarkerSlider_ValueChanged;
        }

        _overlay.SettingsChanged += Overlay_SettingsChanged;
        _overlay.OverlayVisibilityChanged += Overlay_VisibilityChanged;
        _page.Loaded += Page_Loaded;
    }

    private void DirectFloorSelectionPressed(int floorIndex)
    {
        if (_disposed)
            return;

        _ = SelectDirectFloorSafelyAsync(floorIndex);
    }

    private async Task SelectDirectFloorSafelyAsync(int floorIndex)
    {
        try
        {
            await _page.JunhyunSelectFloorAsync(floorIndex);
            await JunhyunMiniMapProductRegistry.SelectFloorIndexAsync(floorIndex);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic($"Direct Map floor selection '{floorIndex}' failed", exception);
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playerMarkerSlider is not null)
            ApplyMainPlayerMarkerSizeToMiniMap(_playerMarkerSlider.Value);

        if (IsMapSmokeEnabled() && !_miniMapReopenSmokeStarted)
        {
            _miniMapReopenSmokeStarted = true;
            _ = VerifyMiniMapReopenSelectionAsync();
        }
    }

    private void Overlay_VisibilityChanged(bool visible)
    {
        if (_disposed || !visible)
            return;

        // Donor HideOverlay intentionally keeps the already-loaded window alive. Showing
        // that same window does not rerun SourceInitialized/Loaded, so the v1.9.1 creation
        // boundary alone cannot guarantee that a newly selected Main Map reaches the first
        // visible frame. OverlayVisibilityChanged is raised synchronously from ShowOverlayCore
        // before WPF yields to rendering; force the visible selector through the canonical
        // product boundary here for both newly-created and reused MiniMap windows.
        _ = LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow();
    }

    private async Task VerifyMiniMapReopenSelectionAsync()
    {
        try
        {
            var mainSmokeSuccess = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                MapSmokeSuccessFileName);
            await WaitForAsync(
                () => System.IO.File.Exists(mainSmokeSuccess),
                TimeSpan.FromSeconds(60));

            if (_disposed)
                return;

            var mapSelector = _page.FindName("CmbMapSelect") as ComboBox
                ?? throw new InvalidOperationException("Map selector was unavailable for MiniMap reopen smoke.");
            var candidates = mapSelector.Items
                .OfType<ComboBoxItem>()
                .Where(item => item.Tag is string key &&
                               !string.IsNullOrWhiteSpace(key) &&
                               MapTrackerService.Instance.GetMapConfig(key) is not null)
                .Take(2)
                .ToArray();
            if (candidates.Length < 2 ||
                candidates[0].Tag is not string rawA ||
                candidates[1].Tag is not string rawB)
            {
                throw new InvalidOperationException("Two usable maps were not available for MiniMap reopen smoke.");
            }

            var mapA = MapTrackerService.Instance.ResolveMapKey(rawA) ?? rawA;
            var mapB = MapTrackerService.Instance.ResolveMapKey(rawB) ?? rawB;
            if (string.Equals(mapA, mapB, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MiniMap reopen smoke selected the same canonical map twice.");

            _overlay.HideOverlay();
            mapSelector.SelectedItem = candidates[0];
            if (!LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow())
                throw new InvalidOperationException("Could not establish MiniMap smoke map A from the visible selector.");

            _overlay.ShowOverlay();
            await WaitForAsync(
                () => _overlay.IsOverlayVisible && JunhyunMiniMapProductRegistry.HasLoadedActiveWindow,
                TimeSpan.FromSeconds(2));

            var window = System.Windows.Application.Current.Windows
                .OfType<TarkovHelper.Windows.OverlayMiniMapWindow>()
                .FirstOrDefault(candidate => candidate.IsVisible)
                ?? throw new InvalidOperationException("Visible MiniMap window was not found for reopen smoke.");
            var mapSvg = window.FindName("MapSvg") as SharpVectors.Converters.SvgViewbox
                ?? throw new InvalidOperationException("MiniMap MapSvg was not found for reopen smoke.");

            await WaitForAsync(
                () => string.Equals(window.JunhyunCurrentMapKey, mapA, StringComparison.OrdinalIgnoreCase) &&
                      mapSvg.Source is not null,
                TimeSpan.FromSeconds(3));
            var renderedSourceA = mapSvg.Source?.ToString()
                ?? throw new InvalidOperationException("MiniMap map A produced no rendered SVG source.");

            // This is the exact missed user path: keep the already-loaded donor Window,
            // change the visible Main Map A -> B, and reopen it immediately before the
            // queued selection bridge gets a ContextIdle turn.
            _overlay.HideOverlay();
            mapSelector.SelectedItem = candidates[1];
            _overlay.ShowOverlay();

            // The show-boundary fix is synchronous. If this is not already B when Show()
            // returns, the first visible frame can still expose stale map A.
            if (!JunhyunMiniMapProductRegistry.IsActiveMapSelectionSynchronized(mapB))
            {
                throw new InvalidOperationException(
                    $"Reused MiniMap was not synchronized to '{mapB}' at the synchronous show boundary.");
            }

            await WaitForAsync(
                () => string.Equals(window.JunhyunCurrentMapKey, mapB, StringComparison.OrdinalIgnoreCase) &&
                      mapSvg.Source is not null &&
                      !string.Equals(renderedSourceA, mapSvg.Source.ToString(), StringComparison.Ordinal),
                TimeSpan.FromSeconds(3));

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), MiniMapSelectionSmokeFileName),
                "main-map-selection-boundary=ok\n" +
                "active-minimap-map-sync=ok\n" +
                "reused-minimap-show-boundary=ok\n" +
                "rendered-minimap-map-sync=ok\n");

            _overlay.HideOverlay();
        }
        catch (Exception exception)
        {
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), MapSmokeDiagnosticFileName),
                    "MiniMap A-to-B reopen/render smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(50);
        }

        throw new TimeoutException("MiniMap reopen smoke condition timed out.");
    }

    private static bool IsMapSmokeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(MapSmokeEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    private void PlayerMarkerSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncingPlayerMarker)
            return;

        ApplyMainPlayerMarkerSizeToMiniMap(e.NewValue);
    }

    private void ApplyMainPlayerMarkerSizeToMiniMap(double mapPixelSize)
    {
        var normalized = Math.Clamp(mapPixelSize / 18.0, 0.5, 3.0);
        if (Math.Abs(_overlay.Settings.PlayerMarkerSize - normalized) > 0.001)
        {
            _overlay.Settings.PlayerMarkerSize = normalized;
            _overlay.SaveSettings();
        }

        JunhyunMiniMapProductRegistry.ApplyPlayerMarkerSize(mapPixelSize);
    }

    private void Overlay_SettingsChanged(TarkovHelper.Models.Map.OverlayMiniMapSettings settings)
    {
        if (_playerMarkerSlider is null)
            return;

        var target = Math.Clamp(
            settings.PlayerMarkerSize * 18.0,
            SharedPlayerMarkerMinPixels,
            SharedPlayerMarkerMaxPixels);

        _page.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed || _playerMarkerSlider is null ||
                Math.Abs(_playerMarkerSlider.Value - target) <= 0.01)
            {
                return;
            }

            _syncingPlayerMarker = true;
            try
            {
                _playerMarkerSlider.Value = target;
            }
            finally
            {
                _syncingPlayerMarker = false;
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        GlobalKeyboardHookService.Instance.DirectFloorSelectionPressed -= DirectFloorSelectionPressed;
        _selectionConsistencyBridge.Dispose();
        _viewportPolishBridge.Dispose();
        _settingsPersistenceBridge.Dispose();
        _questMarkerRenderer.Dispose();
        _interactionPolicyBridge.Dispose();
        _extractMarkerPresentationBridge.Dispose();
        _standardMarkerFloorPresentationBridge.Dispose();
        _markerSettingsBridge.Dispose();
        _miniMapOpacitySettingsBridge.Dispose();
        _hotkeySettingsBridge.Dispose();
        _questSettingsBridge.Dispose();
        _hotkeys.Dispose();
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _overlay.OverlayVisibilityChanged -= Overlay_VisibilityChanged;
        _page.Loaded -= Page_Loaded;
        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged -= PlayerMarkerSlider_ValueChanged;
    }
}
