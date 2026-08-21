using System.Windows;
using System.Windows.Input;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerHotkeyCaptureWindow : Window
{
    public ScannerHotkeyCaptureWindow()
    {
        InitializeComponent();
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
        var gesture = new ScannerHotkeyGesture(
            modifiers.HasFlag(ModifierKeys.Control),
            modifiers.HasFlag(ModifierKeys.Alt),
            modifiers.HasFlag(ModifierKeys.Shift),
            key);
        if (!gesture.Control && !gesture.Alt && !gesture.Shift)
        {
            GestureText.Text = "Ctrl / Alt / Shift 중 하나 이상을 함께 눌러주세요.";
            e.Handled = true;
            return;
        }

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
