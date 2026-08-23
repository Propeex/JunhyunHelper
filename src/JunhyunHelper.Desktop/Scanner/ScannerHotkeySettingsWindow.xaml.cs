using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerHotkeySettingsWindow : Window
{
    public ScannerHotkeySettingsWindow(
        string? oneShotTarkovHotkey,
        string? oneShotTestHotkey,
        string? scannerToggleHotkey)
    {
        OneShotTarkovGesture = Parse(oneShotTarkovHotkey);
        OneShotTestGesture = Parse(oneShotTestHotkey);
        ScannerToggleGesture = Parse(scannerToggleHotkey);
        InitializeComponent();
        RefreshLabels();
    }

    public ScannerHotkeyGesture? OneShotTarkovGesture { get; private set; }
    public ScannerHotkeyGesture? OneShotTestGesture { get; private set; }
    public ScannerHotkeyGesture? ScannerToggleGesture { get; private set; }

    private void ChangeOneShotTarkovButton_Click(object sender, RoutedEventArgs e)
    {
        if (TryCapture("1회 인게임 스캔", out var gesture))
        {
            OneShotTarkovGesture = gesture;
            RefreshLabels();
        }
    }

    private void ChangeOneShotTestButton_Click(object sender, RoutedEventArgs e)
    {
        if (TryCapture("1회 테스트 스캔", out var gesture))
        {
            OneShotTestGesture = gesture;
            RefreshLabels();
        }
    }

    private void ChangeScannerToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (TryCapture("스캐너 ON/OFF", out var gesture))
        {
            ScannerToggleGesture = gesture;
            RefreshLabels();
        }
    }

    private bool TryCapture(string actionLabel, out ScannerHotkeyGesture? gesture)
    {
        gesture = null;
        var dialog = new ScannerHotkeyCaptureWindow(actionLabel)
        {
            Owner = this,
        };
        if (dialog.ShowDialog() != true)
            return false;

        gesture = dialog.DisableRequested ? null : dialog.ResultGesture;
        return true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var configured = new[]
        {
            OneShotTarkovGesture,
            OneShotTestGesture,
            ScannerToggleGesture,
        }
        .Where(value => value.HasValue)
        .Select(value => value!.Value)
        .ToArray();

        if (configured.Distinct().Count() != configured.Length)
        {
            MessageBox.Show(
                this,
                "서로 다른 기능에 같은 단축키를 사용할 수 없습니다.",
                "Scanner 단축키",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void RefreshLabels()
    {
        OneShotTarkovText.Text = Format(OneShotTarkovGesture);
        OneShotTestText.Text = Format(OneShotTestGesture);
        ScannerToggleText.Text = Format(ScannerToggleGesture);
    }

    private static ScannerHotkeyGesture? Parse(string? value) =>
        ScannerHotkeyGesture.TryParse(value, out var gesture) ? gesture : null;

    private static string Format(ScannerHotkeyGesture? gesture) => gesture?.ToString() ?? "사용 안 함";
}
