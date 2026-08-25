using System.Windows;
using System.Windows.Input;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerHotkeyCaptureWindow : Window
{
    public ScannerHotkeyCaptureWindow(string actionLabel = "Scanner")
    {
        InitializeComponent();
        var label = string.IsNullOrWhiteSpace(actionLabel) ? "Scanner" : actionLabel.Trim();
        Title = $"{label} 단축키 설정";
        ActionText.Text = $"{label} 단축키를 누르세요";
        Loaded += (_, _) => Keyboard.Focus(this);
    }

    public ScannerHotkeyGesture? ResultGesture { get; private set; }
    public bool DisableRequested { get; private set; }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (ScannerHotkeyGesture.IsModifierKey(key))
        {
            GestureText.Text = BuildModifierPreview();
            e.Handled = true;
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            GestureText.Text = "Windows 키 조합은 지원하지 않습니다.";
            e.Handled = true;
            return;
        }

        var gesture = new ScannerHotkeyGesture(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Shift),
            key);

        ResultGesture = gesture;
        GestureText.Text = gesture.ToString();
        e.Handled = true;
        DialogResult = true;
    }

    private static string BuildModifierPreview()
    {
        var modifiers = Keyboard.Modifiers;
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        return parts.Count == 0 ? "입력 대기 중..." : string.Join(" + ", parts) + " + …";
    }

    private void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        DisableRequested = true;
        ResultGesture = null;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
