using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Product hotkey editor embedded directly in the Main Map Settings panel.
/// Existing keys and the temporary-MiniMap-hide action are persisted by
/// JunhyunHelper rather than the transplanted legacy settings database.
/// </summary>
public sealed class LegacyMapHotkeySettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly JunhyunMapProductSettingsStore _store = JunhyunMapProductSettingsStore.Instance;
    private readonly Dictionary<OverlayMiniMapHotkeyAction, Button> _buttons = new();
    private readonly DispatcherTimer _authoritativeRestoreTimer;
    private OverlayMiniMapHotkeyAction? _captureAction;
    private Button? _temporaryHideButton;
    private Slider? _temporaryHideSecondsSlider;
    private TextBlock? _temporaryHideSecondsText;
    private bool _captureTemporaryHide;
    private bool _disposed;
    private int _restoreTicks;

    public LegacyMapHotkeySettingsBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        RestorePersistedHotkeys();
        Inject();
        SyncLegacyHook();
        _overlay.SettingsChanged += Overlay_SettingsChanged;

        // OverlayMiniMapService loads the transplanted legacy DB late in MapPage's
        // asynchronous Loaded flow. Keep the JunhyunHelper-owned values authoritative
        // across that initialization window so legacy values cannot win after restart.
        _authoritativeRestoreTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Background,
            (_, _) => ReassertProductHotkeys(),
            _page.Dispatcher);
        _authoritativeRestoreTimer.Start();
    }

    private void ReassertProductHotkeys()
    {
        if (_disposed)
        {
            _authoritativeRestoreTimer.Stop();
            return;
        }

        RestorePersistedHotkeys();
        SyncLegacyHook();
        UpdateDisplays();
        _restoreTicks++;
        if (_restoreTicks >= 15)
            _authoritativeRestoreTimer.Stop();
    }

    private void RestorePersistedHotkeys()
    {
        foreach (var action in Enum.GetValues<OverlayMiniMapHotkeyAction>())
        {
            var current = _overlay.Settings.GetHotkey(action);
            var persisted = _store.GetHotkey(action, current);
            _overlay.Settings.SetHotkey(action, persisted);
        }

        var temporary = _store.TemporaryHideKey;
        if (temporary != 0)
        {
            foreach (var action in Enum.GetValues<OverlayMiniMapHotkeyAction>())
            {
                if (_overlay.Settings.GetHotkey(action) == temporary)
                    _overlay.Settings.SetHotkey(action, 0);
            }
        }
    }

    private void Inject()
    {
        if (_page.FindName("SettingsPanel") is not Border settingsPanel ||
            settingsPanel.Child is not ScrollViewer scrollViewer ||
            scrollViewer.Content is not StackPanel stack)
        {
            return;
        }

        stack.Children.Add(new Border
        {
            Height = 1,
            Background = Brush("BorderBrush", Brushes.DimGray),
            Margin = new Thickness(0, 8, 0, 14),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "단축키",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            Margin = new Thickness(0, 0, 0, 6),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "버튼을 누른 뒤 키를 입력합니다. Delete/Backspace는 미지정, Esc는 취소입니다. 같은 키는 마지막으로 지정한 동작에만 남습니다.",
            Foreground = Brush("TextSecondaryBrush", Brushes.Gray),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        });

        AddRow(stack, OverlayMiniMapHotkeyAction.ToggleOverlay, "미니맵 ON / OFF");
        AddRow(stack, OverlayMiniMapHotkeyAction.ZoomIn, "지도 확대");
        AddRow(stack, OverlayMiniMapHotkeyAction.ZoomOut, "지도 축소");
        AddRow(stack, OverlayMiniMapHotkeyAction.FloorUp, "위층 전환");
        AddRow(stack, OverlayMiniMapHotkeyAction.FloorDown, "아래층 전환");
        AddRow(stack, OverlayMiniMapHotkeyAction.SizeIncrease, "미니맵 크기 증가");
        AddRow(stack, OverlayMiniMapHotkeyAction.SizeDecrease, "미니맵 크기 감소");
        AddTemporaryHideRow(stack);
        UpdateDisplays();
    }

    private void AddRow(StackPanel stack, OverlayMiniMapHotkeyAction action, string label)
    {
        var row = CreateRow(label);
        var button = CreateHotkeyButton(action);
        Grid.SetColumn(button, 1);
        row.Children.Add(button);
        stack.Children.Add(row);
        _buttons[action] = button;
    }

    private void AddTemporaryHideRow(StackPanel stack)
    {
        var row = CreateRow("미니맵 일시 투명");
        _temporaryHideButton = new Button
        {
            Width = 118,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(8, 0, 0, 0),
        };
        _temporaryHideButton.Click += TemporaryHideButton_Click;
        _temporaryHideButton.PreviewKeyDown += HotkeyButton_PreviewKeyDown;
        Grid.SetColumn(_temporaryHideButton, 1);
        row.Children.Add(_temporaryHideButton);
        stack.Children.Add(row);

        var durationRow = new Grid { Margin = new Thickness(0, 3, 0, 8) };
        durationRow.ColumnDefinitions.Add(new ColumnDefinition());
        durationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        durationRow.Children.Add(new TextBlock
        {
            Text = "일시 투명 시간",
            Foreground = Brush("TextSecondaryBrush", Brushes.LightGray),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var durationPanel = new StackPanel { Orientation = Orientation.Horizontal };
        _temporaryHideSecondsSlider = new Slider
        {
            Minimum = 1,
            Maximum = 15,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Width = 82,
            Value = _store.TemporaryHideSeconds,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _temporaryHideSecondsSlider.ValueChanged += TemporaryHideSecondsSlider_ValueChanged;
        durationPanel.Children.Add(_temporaryHideSecondsSlider);
        _temporaryHideSecondsText = new TextBlock
        {
            Width = 36,
            Margin = new Thickness(8, 0, 0, 0),
            Foreground = Brush("TextPrimaryBrush", Brushes.White),
            VerticalAlignment = VerticalAlignment.Center,
        };
        durationPanel.Children.Add(_temporaryHideSecondsText);
        Grid.SetColumn(durationPanel, 1);
        durationRow.Children.Add(durationPanel);
        stack.Children.Add(durationRow);
        UpdateDurationText();
    }

    private Grid CreateRow(string label)
    {
        var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brush("TextSecondaryBrush", Brushes.LightGray),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return row;
    }

    private Button CreateHotkeyButton(OverlayMiniMapHotkeyAction action)
    {
        var button = new Button
        {
            Width = 118,
            Padding = new Thickness(6, 4, 6, 4),
            Tag = action,
            Margin = new Thickness(8, 0, 0, 0),
        };
        button.Click += HotkeyButton_Click;
        button.PreviewKeyDown += HotkeyButton_PreviewKeyDown;
        return button;
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not OverlayMiniMapHotkeyAction action)
            return;

        _captureTemporaryHide = false;
        _captureAction = action;
        BeginCapture(button);
    }

    private void TemporaryHideButton_Click(object sender, RoutedEventArgs e)
    {
        if (_temporaryHideButton is null)
            return;
        _captureAction = null;
        _captureTemporaryHide = true;
        BeginCapture(_temporaryHideButton);
    }

    private void BeginCapture(Button button)
    {
        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = true;
        UpdateDisplays();
        button.Focus();
        Keyboard.Focus(button);
    }

    private void HotkeyButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_captureAction.HasValue && !_captureTemporaryHide)
            return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            FinishCapture(save: false);
            return;
        }

        if (key is Key.Delete or Key.Back)
        {
            AssignCapturedKey(0);
            FinishCapture(save: true);
            return;
        }

        if (IsModifier(key))
            return;

        if (IsReserved(key))
        {
            MessageBox.Show(
                Window.GetWindow(_page),
                "NumPad 0~5는 직접 층 선택에 사용하므로 다른 지도 단축키로 지정할 수 없습니다.",
                "예약된 단축키",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey <= 0)
            return;

        AssignCapturedKey(virtualKey);
        FinishCapture(save: true);
    }

    private void AssignCapturedKey(int virtualKey)
    {
        if (_captureTemporaryHide)
        {
            if (virtualKey != 0)
            {
                foreach (var action in Enum.GetValues<OverlayMiniMapHotkeyAction>())
                {
                    if (_overlay.Settings.GetHotkey(action) != virtualKey)
                        continue;
                    _overlay.Settings.SetHotkey(action, 0);
                    _store.SetHotkey(action, 0);
                }
            }
            _store.TemporaryHideKey = virtualKey;
            return;
        }

        if (!_captureAction.HasValue)
            return;

        var targetAction = _captureAction.Value;
        if (virtualKey != 0)
        {
            foreach (var action in Enum.GetValues<OverlayMiniMapHotkeyAction>())
            {
                if (action == targetAction || _overlay.Settings.GetHotkey(action) != virtualKey)
                    continue;
                _overlay.Settings.SetHotkey(action, 0);
                _store.SetHotkey(action, 0);
            }

            if (_store.TemporaryHideKey == virtualKey)
                _store.TemporaryHideKey = 0;
        }

        _overlay.Settings.SetHotkey(targetAction, virtualKey);
        _store.SetHotkey(targetAction, virtualKey);
    }

    private void FinishCapture(bool save)
    {
        _captureAction = null;
        _captureTemporaryHide = false;
        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = false;
        if (save)
        {
            SyncLegacyHook();
            _overlay.SaveSettings();
        }
        UpdateDisplays();
    }

    private void TemporaryHideSecondsSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        _store.TemporaryHideSeconds = Math.Round(e.NewValue);
        UpdateDurationText();
    }

    private void UpdateDurationText()
    {
        if (_temporaryHideSecondsText is not null)
            _temporaryHideSecondsText.Text = $"{_store.TemporaryHideSeconds:0}초";
    }

    private void SyncLegacyHook()
    {
        var hook = GlobalKeyboardHookService.Instance;
        hook.ZoomInKey = 0;
        hook.ZoomOutKey = 0;
        hook.FloorUpKey = 0;
        hook.FloorDownKey = 0;
        hook.ResumeAutoFloorKey = 0;
    }

    private void Overlay_SettingsChanged(OverlayMiniMapSettings settings)
    {
        if (_disposed)
            return;

        if (_page.Dispatcher.CheckAccess())
        {
            SyncLegacyHook();
            UpdateDisplays();
        }
        else
        {
            _page.Dispatcher.BeginInvoke(() =>
            {
                SyncLegacyHook();
                UpdateDisplays();
            });
        }
    }

    private void UpdateDisplays()
    {
        foreach (var (action, button) in _buttons)
        {
            if (_captureAction == action && !_captureTemporaryHide)
            {
                button.Content = "입력 대기...";
                continue;
            }

            var virtualKey = _overlay.Settings.GetHotkey(action);
            button.Content = KeyText(virtualKey);
        }

        if (_temporaryHideButton is not null)
        {
            _temporaryHideButton.Content = _captureTemporaryHide
                ? "입력 대기..."
                : KeyText(_store.TemporaryHideKey);
        }
        UpdateDurationText();
    }

    private static string KeyText(int virtualKey) => virtualKey == 0
        ? "미지정"
        : KeyInterop.KeyFromVirtualKey(virtualKey).ToString();

    private Brush Brush(string key, Brush fallback) =>
        _page.TryFindResource(key) as Brush ?? fallback;

    private static bool IsModifier(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or
        Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin;

    private static bool IsReserved(Key key) => key is
        Key.NumPad0 or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4 or Key.NumPad5;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _authoritativeRestoreTimer.Stop();
        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = false;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        foreach (var button in _buttons.Values)
        {
            button.Click -= HotkeyButton_Click;
            button.PreviewKeyDown -= HotkeyButton_PreviewKeyDown;
        }
        _buttons.Clear();

        if (_temporaryHideButton is not null)
        {
            _temporaryHideButton.Click -= TemporaryHideButton_Click;
            _temporaryHideButton.PreviewKeyDown -= HotkeyButton_PreviewKeyDown;
        }
        if (_temporaryHideSecondsSlider is not null)
            _temporaryHideSecondsSlider.ValueChanged -= TemporaryHideSecondsSlider_ValueChanged;
    }
}
