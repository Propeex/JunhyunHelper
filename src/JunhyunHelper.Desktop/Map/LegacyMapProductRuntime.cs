using System.Windows;
using System.Windows.Controls;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Runtime-only product controls layered on the exact Tarkov Helper Map subsystem.
/// </summary>
public sealed class LegacyMapProductRuntime : IDisposable
{
    private const double SharedPlayerMarkerMinPixels = 9.0;
    private const double SharedPlayerMarkerMaxPixels = 54.0;

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly JunhyunMapHotkeyService _hotkeys;
    private readonly LegacyQuestPresentationSettingsBridge _questSettingsBridge;
    private readonly LegacyMapHotkeySettingsBridge _hotkeySettingsBridge;
    private readonly LegacyMiniMapOpacitySettingsBridge _miniMapOpacitySettingsBridge;
    private readonly LegacyMapMarkerSettingsV2Bridge _markerSettingsBridge;
    private readonly LegacyStandardMarkerFloorPresentationBridge _standardMarkerFloorPresentationBridge;
    private readonly LegacyMapInteractionPolicyBridge _interactionPolicyBridge;
    private readonly LegacyQuestMarkerRenderV3 _questMarkerRenderer;
    private readonly LegacyMapSettingsPersistenceBridge _settingsPersistenceBridge;
    private readonly LegacyMapViewportPolishBridge _viewportPolishBridge;
    private readonly Slider? _playerMarkerSlider;
    private bool _syncingPlayerMarker;
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
        _interactionPolicyBridge = new LegacyMapInteractionPolicyBridge(page);
        _questMarkerRenderer = new LegacyQuestMarkerRenderV3(page);
        _settingsPersistenceBridge = new LegacyMapSettingsPersistenceBridge(page);
        _viewportPolishBridge = new LegacyMapViewportPolishBridge(page);
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
        _page.Loaded += Page_Loaded;
    }

    private async void DirectFloorSelectionPressed(int floorIndex)
    {
        if (_disposed)
            return;

        await _page.JunhyunSelectFloorAsync(floorIndex);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playerMarkerSlider is not null)
            ApplyMainPlayerMarkerSizeToMiniMap(_playerMarkerSlider.Value);
    }

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
        _viewportPolishBridge.Dispose();
        _settingsPersistenceBridge.Dispose();
        _questMarkerRenderer.Dispose();
        _interactionPolicyBridge.Dispose();
        _standardMarkerFloorPresentationBridge.Dispose();
        _markerSettingsBridge.Dispose();
        _miniMapOpacitySettingsBridge.Dispose();
        _hotkeySettingsBridge.Dispose();
        _questSettingsBridge.Dispose();
        _hotkeys.Dispose();
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _page.Loaded -= Page_Loaded;
        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged -= PlayerMarkerSlider_ValueChanged;
    }
}
