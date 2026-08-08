using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Profiles;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private const string CreateProfileMenuTag = "create-profile";

    private void ProfileComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        if (_initializing)
            return;

        var selectedProfileId = _activeProfile?.ProfileId;
        var baseChoices = ProfileComboBox.Items
            .Cast<object>()
            .Where(item => item is not ComboBoxItem)
            .ToList();

        var existingModes = _profiles.Select(profile => profile.GameMode).ToHashSet();
        var canCreate = Enum.GetValues<GameMode>().Any(mode => !existingModes.Contains(mode));
        if (canCreate)
        {
            baseChoices.Add(new ComboBoxItem
            {
                Content = "＋ 새 프로필",
                Tag = CreateProfileMenuTag,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            });
        }

        _initializing = true;
        ProfileComboBox.ItemsSource = baseChoices;
        ProfileComboBox.SelectedItem = baseChoices
            .OfType<ProfileChoice>()
            .FirstOrDefault(choice =>
                string.Equals(choice.Profile.ProfileId, selectedProfileId, StringComparison.Ordinal));
        _initializing = false;
    }

    private void ProfileComboBox_CompactSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
            return;

        if (ProfileComboBox.SelectedItem is ComboBoxItem menuItem &&
            string.Equals(menuItem.Tag as string, CreateProfileMenuTag, StringComparison.Ordinal))
        {
            _initializing = true;
            ProfileComboBox.SelectedItem = ProfileComboBox.Items
                .OfType<ProfileChoice>()
                .FirstOrDefault(choice =>
                    string.Equals(choice.Profile.ProfileId, _activeProfile?.ProfileId, StringComparison.Ordinal));
            _initializing = false;

            CreateProfileButton_Click(sender, new RoutedEventArgs());
            return;
        }

        ProfileComboBox_SelectionChanged(sender, e);
    }

    private async void EditProfileCompactButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var profileId = _activeProfile.ProfileId;
        var editor = new ProfileEditorWindow(
            _activeProfile.GameMode,
            _activeContent,
            _activeProfile)
        {
            Owner = this,
        };

        if (editor.ShowDialog() != true)
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
}
