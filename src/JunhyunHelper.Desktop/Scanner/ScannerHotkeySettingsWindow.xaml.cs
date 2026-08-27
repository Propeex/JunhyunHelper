using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JunhyunHelper.Desktop;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerHotkeySettingsWindow : Window, IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;
    private CaptureTarget? _captureTarget;
    private Button? _captureButton;

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

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested) =>
        _inAppCloseRequested = closeRequested ?? throw new ArgumentNullException(nameof(closeRequested));

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        CancelCapture();
        _inAppCloseRequested?.Invoke(false);
        return true;
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
            Assign(target, null);
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

        Assign(target, new ScannerHotkeyGesture(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Shift),
            key));
        FinishCapture();
    }

    private void Assign(CaptureTarget target, ScannerHotkeyGesture? gesture)
    {
        switch (target)
        {
            case CaptureTarget.OneShotTarkov:
                OneShotTarkovGesture = gesture;
                break;
            case CaptureTarget.OneShotTest:
                OneShotTestGesture = gesture;
                break;
            case CaptureTarget.ScannerToggle:
                ScannerToggleGesture = gesture;
                break;
        }
    }

    private void FinishCapture()
    {
        if (_captureButton is not null)
            _captureButton.PreviewKeyDown -= CaptureButton_PreviewKeyDown;
        _captureButton = null;
        _captureTarget = null;
        RefreshLabels();
    }

    private void CancelCapture()
    {
        if (_captureTarget is null)
            return;
        FinishCapture();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        CancelCapture();
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
                Window.GetWindow(sender as DependencyObject) ?? System.Windows.Application.Current.MainWindow,
                "서로 다른 기능에 같은 단축키를 사용할 수 없습니다.",
                "Scanner 단축키",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_inAppCloseRequested is not null)
            _inAppCloseRequested(true);
        else
            DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelCapture();
        if (_inAppCloseRequested is not null)
            _inAppCloseRequested(false);
        else
            DialogResult = false;
    }

    private void RefreshLabels()
    {
        OneShotTarkovText.Text = Format(OneShotTarkovGesture);
        OneShotTestText.Text = Format(OneShotTestGesture);
        ScannerToggleText.Text = Format(ScannerToggleGesture);
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

    private enum CaptureTarget
    {
        OneShotTarkov,
        OneShotTest,
        ScannerToggle,
    }
}
