using System.Windows.Controls;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Runtime-only synchronization for product controls that sit at the boundary
/// between the exact Main Map and MiniMap window.
/// </summary>
public sealed class LegacyMapProductRuntime : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly JunhyunMapHotkeyService _hotkeys = new();
    private readonly Slider? _playerMarkerSlider;
    private bool _syncingPlayerMarker;
    private bool _disposed;

    public LegacyMapProductRuntime(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _playerMarkerSlider = _page.FindName("SliderPlayerMarkerSize") as Slider;

        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged += PlayerMarkerSlider_ValueChanged;

        _overlay.SettingsChanged += Overlay_SettingsChanged;
        _page.Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_playerMarkerSlider is not null)
            ApplyMainPlayerMarkerSizeToMiniMap(_playerMarkerSlider.Value);
    }

    private void PlayerMarkerSlider_ValueChanged(
        object sender,
        System.Windows.RoutedPropertyChangedEventArgs<double> e)
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

        var target = Math.Clamp(settings.PlayerMarkerSize * 18.0,
            _playerMarkerSlider.Minimum,
            _playerMarkerSlider.Maximum);

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
                // The original MapPage handler remains attached and updates the
                // Main Map marker immediately. Only our mirror handler is guarded.
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

        _hotkeys.Dispose();
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _page.Loaded -= Page_Loaded;
        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged -= PlayerMarkerSlider_ValueChanged;
    }
}
