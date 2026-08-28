using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerSettingsWindow : Window
{
    private readonly ScannerCoordinator _coordinator;
    private readonly ObservableCollection<MiniInfoRow> _rows = [];
    private CaptureTarget? _captureTarget;
    private Button? _captureButton;

    public ScannerSettingsWindow(ScannerCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

        var settings = coordinator.Settings.Clone();
        settings.Normalize();
        foreach (var key in settings.MiniScannerInfoOrder)
        {
            _rows.Add(new MiniInfoRow(
                key,
                LabelFor(key),
                settings.IsInfoVisible(key)));
        }

        InfoOrderList.ItemsSource = _rows;
        RefreshHotkeyLabels();
    }

    private void InfoVisibilityCheckBox_Click(object sender, RoutedEventArgs e) => SaveDisplaySettings();

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MiniInfoRow row })
            return;
        var index = _rows.IndexOf(row);
        if (index <= 0)
            return;
        _rows.Move(index, index - 1);
        InfoOrderList.SelectedItem = row;
        SaveDisplaySettings();
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MiniInfoRow row })
            return;
        var index = _rows.IndexOf(row);
        if (index < 0 || index >= _rows.Count - 1)
            return;
        _rows.Move(index, index + 1);
        InfoOrderList.SelectedItem = row;
        SaveDisplaySettings();
    }

    private void SaveDisplaySettings()
    {
        var orderedRows = _rows.ToArray();
        _coordinator.UpdateDisplaySettings(settings =>
        {
            settings.ShowItemName = true;
            settings.ShowItemIcon = true;
            settings.MiniScannerInfoOrder = orderedRows.Select(row => row.Key).ToList();
            foreach (var row in orderedRows)
                settings.SetInfoVisible(row.Key, row.IsVisible);
        });
    }

    private void ChangeOneShotTarkovButton_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(CaptureTarget.OneShotTarkov, sender as Button);

    private void ChangeOneShotTestButton_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(CaptureTarget.OneShotTest, sender as Button);

    private void ChangeScannerToggleButton_Click(object sender, RoutedEventArgs e) =>
        BeginCapture(CaptureTarget.ScannerToggle, sender as Button);

    private void BeginCapture(CaptureTarget target, Button? button)
    {
        CancelCapture();
        if (button is null)
            return;

        _captureTarget = target;
        _captureButton = button;
        button.PreviewKeyDown += CaptureButton_PreviewKeyDown;
        button.Focus();
        Keyboard.Focus(button);
        SetTargetText(target, "입력 대기 중...  (Delete: 미지정, Esc: 취소)");
    }

    private void CaptureButton_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_captureTarget is not { } target)
            return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            CancelCapture();
            return;
        }

        if (key is Key.Delete or Key.Back)
        {
            SaveHotkey(target, null);
            FinishCapture();
            return;
        }

        if (ScannerHotkeyGesture.IsModifierKey(key))
        {
            SetTargetText(target, BuildModifierPreview());
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            SetTargetText(target, "Windows 키 조합은 지원하지 않습니다.");
            return;
        }

        var gesture = new ScannerHotkeyGesture(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Shift),
            key);

        if (IsDuplicate(target, gesture))
        {
            SetTargetText(target, "이미 다른 Scanner 기능에서 사용 중입니다.");
            return;
        }

        SaveHotkey(target, gesture);
        FinishCapture();
    }

    private bool IsDuplicate(CaptureTarget target, ScannerHotkeyGesture gesture)
    {
        var settings = _coordinator.Settings;
        var values = new[]
        {
            (CaptureTarget.OneShotTarkov, Parse(settings.OneShotTarkovHotkey)),
            (CaptureTarget.OneShotTest, Parse(settings.OneShotTestHotkey)),
            (CaptureTarget.ScannerToggle, Parse(settings.ScannerToggleHotkey)),
        };
        return values.Any(value => value.Item1 != target && value.Item2 == gesture);
    }

    private void SaveHotkey(CaptureTarget target, ScannerHotkeyGesture? gesture)
    {
        switch (target)
        {
            case CaptureTarget.OneShotTarkov:
                _coordinator.SetOneShotTarkovHotkey(gesture);
                break;
            case CaptureTarget.OneShotTest:
                _coordinator.SetOneShotTestHotkey(gesture);
                break;
            case CaptureTarget.ScannerToggle:
                _coordinator.SetScannerToggleHotkey(gesture);
                break;
        }
    }

    private void FinishCapture()
    {
        if (_captureButton is not null)
            _captureButton.PreviewKeyDown -= CaptureButton_PreviewKeyDown;
        _captureButton = null;
        _captureTarget = null;
        RefreshHotkeyLabels();
    }

    private void CancelCapture()
    {
        if (_captureTarget is null)
            return;
        FinishCapture();
    }

    private void RefreshHotkeyLabels()
    {
        var settings = _coordinator.Settings;
        OneShotTarkovText.Text = Format(Parse(settings.OneShotTarkovHotkey));
        OneShotTestText.Text = Format(Parse(settings.OneShotTestHotkey));
        ScannerToggleText.Text = Format(Parse(settings.ScannerToggleHotkey));
    }

    private void SetTargetText(CaptureTarget target, string value)
    {
        switch (target)
        {
            case CaptureTarget.OneShotTarkov:
                OneShotTarkovText.Text = value;
                break;
            case CaptureTarget.OneShotTest:
                OneShotTestText.Text = value;
                break;
            case CaptureTarget.ScannerToggle:
                ScannerToggleText.Text = value;
                break;
        }
    }

    private static string BuildModifierPreview()
    {
        var parts = new List<string>();
        var modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        return parts.Count == 0 ? "입력 대기 중..." : string.Join(" + ", parts) + " + …";
    }

    private static ScannerHotkeyGesture? Parse(string? value) =>
        ScannerHotkeyGesture.TryParse(value, out var gesture) ? gesture : null;

    private static string Format(ScannerHotkeyGesture? gesture) => gesture?.ToString() ?? "사용 안 함";

    private static string LabelFor(string key) => key switch
    {
        ScannerDisplaySettings.TraderSellPriceField => "상인 판매가",
        ScannerDisplaySettings.FleaAveragePriceField => "플리마켓 평균가",
        ScannerDisplaySettings.FleaMinimumPriceField => "플리마켓 최저가",
        ScannerDisplaySettings.TraderPricePerSlotField => "상점가 / 칸",
        ScannerDisplaySettings.FleaPricePerSlotField => "플리 평균가 / 칸",
        ScannerDisplaySettings.CurrentNeededField => "필요 개수",
        _ => key,
    };

    private sealed class MiniInfoRow(string key, string label, bool isVisible)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public bool IsVisible { get; set; } = isVisible;
    }

    private enum CaptureTarget
    {
        OneShotTarkov,
        OneShotTest,
        ScannerToggle,
    }
}
