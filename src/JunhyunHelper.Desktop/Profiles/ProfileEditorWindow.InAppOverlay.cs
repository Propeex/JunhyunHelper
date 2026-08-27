using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop;

namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileEditorWindow : IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;
    private bool _inAppOverlayPrepared;

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested)
    {
        _inAppCloseRequested = closeRequested;
        PrepareInAppOverlayButtons();
    }

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        if (_editingExistingProfile && !DeleteRequested)
        {
            if (!TryBuildResult(out var result))
                return false;
            Result = result;
            _inAppCloseRequested?.Invoke(true);
            return true;
        }

        _inAppCloseRequested?.Invoke(false);
        return true;
    }

    private void PrepareInAppOverlayButtons()
    {
        if (_inAppOverlayPrepared || Content is not DependencyObject root)
            return;
        _inAppOverlayPrepared = true;

        foreach (var button in EnumerateLogicalButtons(root))
        {
            if (ReferenceEquals(button, DeleteProfileButton))
            {
                button.Click -= DeleteProfileButton_Click;
                button.Click += InAppDeleteProfileButton_Click;
                continue;
            }

            if (button.Content is not string label)
                continue;

            if (string.Equals(label, "저장", StringComparison.Ordinal))
            {
                button.IsDefault = false;
                button.Click -= SaveButton_Click;
                button.Click += InAppSaveButton_Click;
                if (_editingExistingProfile)
                    button.Content = "닫기";
            }
            else if (string.Equals(label, "취소", StringComparison.Ordinal))
            {
                button.IsCancel = false;
                button.Click += InAppCancelButton_Click;
                if (_editingExistingProfile)
                    button.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void InAppSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildResult(out var result))
            return;

        Result = result;
        _inAppCloseRequested?.Invoke(true);
    }

    private void InAppCancelButton_Click(object sender, RoutedEventArgs e) =>
        _inAppCloseRequested?.Invoke(false);

    private void InAppDeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_editingExistingProfile)
            return;

        var owner = Window.GetWindow(DeleteProfileButton) ?? Application.Current.MainWindow;
        var decision = MessageBox.Show(
            owner,
            "이 프로필의 퀘스트 진행, 은신처 레벨, 상인 진행, 보유 아이템 기록을 모두 삭제합니다. 다운로드된 게임 데이터와 다른 프로필은 유지됩니다.\n\n삭제하시겠습니까?",
            "프로필 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (decision != MessageBoxResult.Yes)
            return;

        DeleteRequested = true;
        Result = null;
        _inAppCloseRequested?.Invoke(true);
    }

    private static IEnumerable<Button> EnumerateLogicalButtons(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button)
                yield return button;
            if (child is DependencyObject dependencyObject)
            {
                foreach (var descendant in EnumerateLogicalButtons(dependencyObject))
                    yield return descendant;
            }
        }
    }
}
