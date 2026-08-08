namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private async Task DeleteProfileAsync(string profileId)
    {
        SetBusy(true, "프로필을 삭제하는 중...");
        await _services.ProfileManagement.DeleteAsync(profileId);

        _activeProfile = null;
        _activeContent = null;
        _activeItemsWorkspace = null;
        ItemsPage.ClearCleanupNotice();

        await LoadProfilesAsync();
    }
}
