using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private readonly JunhyunMapHotkeyService _hotkeys = new();
    private readonly LegacyQuestPresentationSettingsBridge _questSettingsBridge;
    private readonly LegacyMapHotkeySettingsBridge _hotkeySettingsBridge;
    private readonly LegacyMapMarkerSettingsV2Bridge _markerSettingsBridge;
    private readonly LegacyMapInteractionPolicyBridge _interactionPolicyBridge;
    private readonly Slider? _playerMarkerSlider;
    private Button? _miniMapSettingsButton;
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
        _markerSettingsBridge = new LegacyMapMarkerSettingsV2Bridge(page);
        _interactionPolicyBridge = new LegacyMapInteractionPolicyBridge(page);
        _playerMarkerSlider = _page.FindName("SliderPlayerMarkerSize") as Slider;

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

        InjectMiniMapSettingsEntry();
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        _page.Loaded += Page_Loaded;
    }

    private void InjectMiniMapSettingsEntry()
    {
        if (_page.FindName("SettingsPanel") is not Border settingsPanel ||
            settingsPanel.Child is not ScrollViewer scrollViewer ||
            scrollViewer.Content is not StackPanel stack)
        {
            return;
        }

        var header = new TextBlock
        {
            Text = "미니맵",
            FontWeight = FontWeights.SemiBold,
            Foreground = _page.TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            Margin = new Thickness(0, 0, 0, 8),
        };
        _miniMapSettingsButton = new Button
        {
            Content = "미니맵 표시 설정",
            Padding = new Thickness(12, 7, 12, 7),
            Margin = new Thickness(0, 0, 0, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _miniMapSettingsButton.Click += MiniMapSettingsButton_Click;

        var insertIndex = Math.Min(1, stack.Children.Count);
        stack.Children.Insert(insertIndex, header);
        stack.Children.Insert(insertIndex + 1, _miniMapSettingsButton);
    }

    private void MiniMapSettingsButton_Click(object sender, RoutedEventArgs e) =>
        _overlay.ShowSettingsWindow();

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

        _interactionPolicyBridge.Dispose();
        _markerSettingsBridge.Dispose();
        _hotkeySettingsBridge.Dispose();
        _questSettingsBridge.Dispose();
        _hotkeys.Dispose();
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _page.Loaded -= Page_Loaded;
        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged -= PlayerMarkerSlider_ValueChanged;
        if (_miniMapSettingsButton is not null)
            _miniMapSettingsButton.Click -= MiniMapSettingsButton_Click;
    }
}
