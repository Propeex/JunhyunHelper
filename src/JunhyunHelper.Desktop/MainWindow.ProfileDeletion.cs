using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
            return;

        var profile = _activeProfile;
        var confirmation = MessageBox.Show(
            this,
            $"{GameModeText(profile.GameMode)} 프로필을 삭제합니다.\n\n" +
            "완료/실패 퀘스트, 은신처 레벨, 상인 진행, 보유 아이템을 포함한 이 프로필의 사용자 진행 기록이 모두 삭제됩니다.\n" +
            "다운로드된 게임 데이터는 삭제되지 않으며 다른 게임 모드의 프로필도 유지됩니다.\n\n" +
            "삭제 후 되돌릴 수 없습니다. 계속하시겠습니까?",
            "프로필 삭제",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            SetBusy(true, "프로필을 삭제하는 중...");
            await _services.ProfileManagement.DeleteAsync(profile.ProfileId);

            _activeProfile = null;
            _activeContent = null;
            _activeItemsWorkspace = null;
            ItemsPage.ClearCleanupNotice();

            await LoadProfilesAsync();
        }
        catch (Exception exception)
        {
            ShowFailure("프로필을 삭제하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }
}
