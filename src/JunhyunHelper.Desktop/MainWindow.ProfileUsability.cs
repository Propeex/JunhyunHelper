using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Profiles;

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

            _ = CreateProfileOverlayAsync();
            return;
        }

        ProfileComboBox_SelectionChanged(sender, e);
    }

    private async void EditProfileCompactButton_Click(object sender, RoutedEventArgs e) =>
        await EditActiveProfileOverlayAsync();
}
