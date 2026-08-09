using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Product hotkey editor embedded directly in the Main Map Settings panel.
/// </summary>
public sealed class LegacyMapHotkeySettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly Dictionary<OverlayMiniMapHotkeyAction, Button> _buttons = new();
    private OverlayMiniMapHotkeyAction? _captureAction;
    private bool _disposed;

    public LegacyMapHotkeySettingsBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        Inject();
        _overlay.SettingsChanged += Overlay_SettingsChanged;
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
        UpdateDisplays();
    }

    private void AddRow(StackPanel stack, OverlayMiniMapHotkeyAction action, string label)
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

        var button = new Button
        {
            Width = 118,
            Padding = new Thickness(6, 4, 6, 4),
            Tag = action,
            Margin = new Thickness(8, 0, 0, 0),
        };
        Grid.SetColumn(button, 1);
        button.Click += HotkeyButton_Click;
        button.PreviewKeyDown += HotkeyButton_PreviewKeyDown;
        row.Children.Add(button);
        stack.Children.Add(row);
        _buttons[action] = button;
    }

    private void HotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not OverlayMiniMapHotkeyAction action)
            return;

        _captureAction = action;
        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = true;
        UpdateDisplays();
        button.Focus();
        Keyboard.Focus(button);
    }

    private void HotkeyButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_captureAction.HasValue)
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
            _overlay.Settings.SetHotkey(_captureAction.Value, 0);
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

        _overlay.Settings.SetHotkey(_captureAction.Value, virtualKey);
        FinishCapture(save: true);
    }

    private void FinishCapture(bool save)
    {
        _captureAction = null;
        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = false;
        if (save)
        {
            SyncLegacyHook();
            _overlay.SaveSettings();
        }
        UpdateDisplays();
    }

    private void SyncLegacyHook()
    {
        var settings = _overlay.Settings;
        var hook = GlobalKeyboardHookService.Instance;
        hook.ZoomInKey = settings.ZoomInKey;
        hook.ZoomOutKey = settings.ZoomOutKey;
        hook.FloorUpKey = settings.FloorUpKey;
        hook.FloorDownKey = settings.FloorDownKey;
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
            if (_captureAction == action)
            {
                button.Content = "입력 대기...";
                continue;
            }

            var virtualKey = _overlay.Settings.GetHotkey(action);
            button.Content = virtualKey == 0
                ? "미지정"
                : KeyInterop.KeyFromVirtualKey(virtualKey).ToString();
        }
    }

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

        GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed = false;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        foreach (var button in _buttons.Values)
        {
            button.Click -= HotkeyButton_Click;
            button.PreviewKeyDown -= HotkeyButton_PreviewKeyDown;
        }
        _buttons.Clear();
    }
}
