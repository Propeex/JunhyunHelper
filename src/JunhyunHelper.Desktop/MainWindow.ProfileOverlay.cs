using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Profiles;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _profileOverlayLaunchersAttached;

    private void AttachProfileOverlayLaunchers()
    {
        if (_profileOverlayLaunchersAttached)
            return;
        _profileOverlayLaunchersAttached = true;

        var createButton = FindButtons(EmptyState)
            .FirstOrDefault(button => string.Equals(button.Content as string, "프로필 만들기", StringComparison.Ordinal));
        if (createButton is not null)
        {
            createButton.Click -= CreateProfileButton_Click;
            createButton.Click += CreateProfileOverlayButton_Click;
        }
    }

    private async void CreateProfileOverlayButton_Click(object sender, RoutedEventArgs e) =>
        await CreateProfileOverlayAsync();

    private async Task CreateProfileOverlayAsync()
    {
        var existingModes = _profiles.Select(profile => profile.GameMode).ToHashSet();
        var availableModes = Enum.GetValues<GameMode>()
            .Where(mode => !existingModes.Contains(mode))
            .ToArray();

        if (availableModes.Length == 0)
        {
            MessageBox.Show(
                this,
                "현재 지원하는 모든 게임 모드의 프로필이 이미 있습니다.",
                "새 프로필",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var modeWindow = new ProfileModeWindow(availableModes);
        if (await ToggleInAppWindowAsync("profile-create-mode", modeWindow) != true ||
            modeWindow.SelectedMode is not { } mode)
        {
            return;
        }

        try
        {
            SetBusy(true, $"{GameModeText(mode)} 데이터를 준비하는 중...");
            var content = await ReadOrCreateContentAsync(mode);
            SetBusy(false, "프로필 정보를 입력해주세요.");

            var editor = new ProfileEditorWindow(mode, content);
            if (await ToggleInAppWindowAsync("profile-create-editor", editor) != true ||
                editor.Result is not { } result)
            {
                return;
            }

            SetBusy(true, "프로필을 저장하는 중...");
            var created = await _services.ProfileManagement.CreateAsync(
                mode,
                result.Level,
                result.Faction,
                result.EditionId,
                result.PrestigeLevel,
                result.Traders);

            await LoadProfilesAsync(created.ProfileId);
        }
        catch (Exception exception)
        {
            ShowFailure("프로필을 만들지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async Task EditActiveProfileOverlayAsync()
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var profileId = _activeProfile.ProfileId;
        var editor = new ProfileEditorWindow(
            _activeProfile.GameMode,
            _activeContent,
            _activeProfile);

        if (await ToggleInAppWindowAsync("profile-edit", editor) != true)
            return;

        try
        {
            if (editor.DeleteRequested)
            {
                SetBusy(true, "프로필을 삭제하는 중...");
                await _services.ProfileManagement.DeleteAsync(profileId);
                _activeProfile = null;
                _activeContent = null;
                _activeItemsWorkspace = null;
                await LoadProfilesAsync();
                return;
            }

            if (editor.Result is not { } result)
                return;

            SetBusy(true, "프로필을 저장하는 중...");
            var updated = await _services.ProfileManagement.UpdateSettingsAsync(
                profileId,
                result.Level,
                result.Faction,
                result.EditionId,
                result.PrestigeLevel,
                result.Traders);

            await LoadProfilesAsync(updated.ProfileId);
        }
        catch (Exception exception)
        {
            ShowFailure(
                editor.DeleteRequested ? "프로필을 삭제하지 못했습니다." : "프로필을 수정하지 못했습니다.",
                exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private static IEnumerable<Button> FindButtons(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is Button button)
                yield return button;
            if (child is DependencyObject dependencyObject)
            {
                foreach (var descendant in FindButtons(dependencyObject))
                    yield return descendant;
            }
        }
    }
}
