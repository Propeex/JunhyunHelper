using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Runtime-only synchronization for product controls that sit at the boundary
/// between the exact Main Map and MiniMap window.
/// </summary>
public sealed class LegacyMapProductRuntime : IDisposable
{
    private const double SharedPlayerMarkerMinPixels = 9.0;  // 0.5x of legacy 18px base
    private const double SharedPlayerMarkerMaxPixels = 54.0; // 3.0x of legacy 18px base

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly JunhyunMapHotkeyService _hotkeys = new();
    private readonly LegacyQuestMarkerScaleBridge _questScaleBridge;
    private readonly LegacyQuestMarkerToggleBridge _questToggleBridge;
    private readonly Slider? _playerMarkerSlider;
    private Button? _hotkeySettingsButton;
    private bool _syncingPlayerMarker;
    private bool _disposed;

    public LegacyMapProductRuntime(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _questScaleBridge = new LegacyQuestMarkerScaleBridge(page);
        _questToggleBridge = new LegacyQuestMarkerToggleBridge(page);
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

        InjectHotkeySettingsEntry();
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        _page.Loaded += Page_Loaded;
    }

    private void InjectHotkeySettingsEntry()
    {
        if (_page.FindName("SettingsPanel") is not Border settingsPanel ||
            settingsPanel.Child is not ScrollViewer scrollViewer ||
            scrollViewer.Content is not StackPanel stack)
        {
            return;
        }

        var header = new TextBlock
        {
            Text = "미니맵 / 단축키",
            FontWeight = FontWeights.SemiBold,
            Foreground = _page.TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.White,
            Margin = new Thickness(0, 0, 0, 8),
        };
        _hotkeySettingsButton = new Button
        {
            Content = "미니맵 및 단축키 설정",
            Padding = new Thickness(12, 7, 12, 7),
            Margin = new Thickness(0, 0, 0, 20),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _hotkeySettingsButton.Click += HotkeySettingsButton_Click;

        var insertIndex = Math.Min(1, stack.Children.Count);
        stack.Children.Insert(insertIndex, header);
        stack.Children.Insert(insertIndex + 1, _hotkeySettingsButton);
    }

    private void HotkeySettingsButton_Click(object sender, RoutedEventArgs e) =>
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

        _questToggleBridge.Dispose();
        _questScaleBridge.Dispose();
        _hotkeys.Dispose();
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        _page.Loaded -= Page_Loaded;
        if (_playerMarkerSlider is not null)
            _playerMarkerSlider.ValueChanged -= PlayerMarkerSlider_ValueChanged;
        if (_hotkeySettingsButton is not null)
            _hotkeySettingsButton.Click -= HotkeySettingsButton_Click;
    }
}
