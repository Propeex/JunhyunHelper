using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop;

namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileModeWindow : IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;
    private bool _inAppOverlayPrepared;

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested)
    {
        _inAppCloseRequested = closeRequested ?? throw new ArgumentNullException(nameof(closeRequested));
        PrepareInAppOverlayButtons();
    }

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        _inAppCloseRequested?.Invoke(false);
        return true;
    }

    private void PrepareInAppOverlayButtons()
    {
        if (_inAppOverlayPrepared || Content is not DependencyObject root)
            return;
        _inAppOverlayPrepared = true;

        foreach (var button in EnumerateButtons(root))
        {
            if (button.Content is not string label)
                continue;

            if (string.Equals(label, "다음", StringComparison.Ordinal))
            {
                button.IsDefault = false;
                button.Click -= NextButton_Click;
                button.Click += InAppNextButton_Click;
            }
            else if (string.Equals(label, "취소", StringComparison.Ordinal))
            {
                button.IsCancel = false;
                button.Click += InAppCancelButton_Click;
            }
        }
    }

    private void InAppNextButton_Click(object sender, RoutedEventArgs e)
    {
        if (ModeList.SelectedItem is not ModeChoice choice)
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                "생성할 게임 모드를 선택해주세요.",
                "새 프로필",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedMode = choice.Mode;
        _inAppCloseRequested?.Invoke(true);
    }

    private void InAppCancelButton_Click(object sender, RoutedEventArgs e) =>
        _inAppCloseRequested?.Invoke(false);

    private static IEnumerable<Button> EnumerateButtons(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button)
                yield return button;
            if (child is DependencyObject dependencyObject)
            {
                foreach (var descendant in EnumerateButtons(dependencyObject))
                    yield return descendant;
            }
        }
    }
}
