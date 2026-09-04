using System.IO;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Application.Hideout;
using JunhyunHelper.Application.Items;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Hideout;
using JunhyunHelper.Desktop.Items;
using JunhyunHelper.Desktop.Profiles;
using JunhyunHelper.Desktop.Quests;
using JunhyunHelper.Desktop.Services;
using JunhyunHelper.Infrastructure.Content;

namespace JunhyunHelper.Desktop;

public partial class MainWindow : Window
{
    private readonly DesktopServices _services = new();
    private IReadOnlyList<GameProfileSnapshot> _profiles = [];
    private GameProfileSnapshot? _activeProfile;
    private GameContentCatalog? _activeContent;
    private ItemsWorkspace? _activeItemsWorkspace;
    private DesktopSection _activeSection = DesktopSection.Quest;
    private bool _initializing;

    public MainWindow()
    {
        InitializeComponent();
        QuestPage.ActionRequested += QuestPage_ActionRequested;
        HideoutPage.LevelChangeRequested += HideoutPage_LevelChangeRequested;
        ItemsPage.InventoryChangeRequested += ItemsPage_InventoryChangeRequested;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProfilesAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _services.Dispose();
    }

    private async Task LoadProfilesAsync(string? selectedProfileId = null)
    {
        try
        {
            SetBusy(true);
            _profiles = await _services.ProfileManagement.LoadAllAsync();

            var targetProfileId = selectedProfileId ?? _activeProfile?.ProfileId;
            var choices = _profiles.Select(profile => new ProfileChoice(profile)).ToArray();

            _initializing = true;
            ProfileComboBox.ItemsSource = choices;
            ProfileComboBox.SelectedItem = choices.FirstOrDefault(choice =>
                string.Equals(choice.Profile.ProfileId, targetProfileId, StringComparison.Ordinal))
                ?? choices.FirstOrDefault();
            _initializing = false;

            if (_profiles.Count == 0)
            {
                ShowNoProfileState();
                return;
            }

            await LoadSelectedProfileAsync();
        }
        catch (Exception exception)
        {
            ShowFailure("초기화하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowNoProfileState()
    {
        _activeProfile = null;
        _activeContent = null;
        _activeItemsWorkspace = null;
        QuestPage.Visibility = Visibility.Collapsed;
        HideoutPage.Visibility = Visibility.Collapsed;
        ItemsPage.Visibility = Visibility.Collapsed;
        AmmoPage.Visibility = Visibility.Collapsed;
        MapPlaceholder.Visibility = Visibility.Collapsed;
        ScannerPlaceholder.Visibility = Visibility.Collapsed;
        ItemsPage.ClearCleanupNotice();
        EmptyState.Visibility = Visibility.Visible;

        EditProfileButton.IsEnabled = false;
        UpdateDataButton.IsEnabled = false;
        UpdateSectionButtons();
    }

    private async void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || !IsLoaded)
            return;

        try
        {
            await LoadSelectedProfileAsync();
        }
        catch (Exception exception)
        {
            ShowFailure("프로필을 불러오지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadSelectedProfileAsync()
    {
        if (ProfileComboBox.SelectedItem is not ProfileChoice choice)
            return;

        SetBusy(true);
        _activeProfile = choice.Profile;
        _activeContent = await ReadOrCreateContentAsync(choice.Profile.GameMode);
        _activeItemsWorkspace = null;
        ItemsPage.ClearCleanupNotice();

        await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
        AmmoPage.SetData(_activeContent);
        EmptyState.Visibility = Visibility.Collapsed;
        ShowActiveSection();

    }

    private async Task<IReadOnlyList<InventoryCleanupIncrease>> RefreshActiveWorkspacesAsync(
        bool detectCleanupChanges)
    {
        if (_activeProfile is null || _activeContent is null)
            return Array.Empty<InventoryCleanupIncrease>();

        var previousPlan = detectCleanupChanges ? _activeItemsWorkspace?.Plan : null;
        var profileId = _activeProfile.ProfileId;

        var questWorkspace = await _services.Quests.LoadAsync(_activeContent, profileId);
        var hideoutWorkspace = await _services.Hideout.LoadAsync(_activeContent, profileId);
        var itemsWorkspace = await _services.Items.LoadAsync(_activeContent, profileId);

        _activeProfile = itemsWorkspace.Profile;
        _activeItemsWorkspace = itemsWorkspace;

        QuestPage.SetDataPreservingScroll(_activeContent, questWorkspace);
        HideoutPage.SetData(_activeContent, hideoutWorkspace);
        ItemsPage.SetData(_activeContent, itemsWorkspace);

        if (previousPlan is null)
        {
            if (!detectCleanupChanges)
                ItemsPage.ClearCleanupNotice();
            return Array.Empty<InventoryCleanupIncrease>();
        }

        var changes = InventoryCleanupChangeDetector.FindIncreases(previousPlan, itemsWorkspace.Plan);
        ItemsPage.SetCleanupChanges(changes);
        return changes;
    }

    private async Task<GameContentCatalog> ReadOrCreateContentAsync(GameMode gameMode)
    {
        var paths = _services.Content.GetPaths(gameMode);
        if (!File.Exists(paths.ActivePath))
        {
            var firstUpdate = await RunContentUpdateAsync(gameMode);
            if (!firstUpdate.Applied)
                throw new InvalidDataException("최초 게임 데이터 업데이트가 검증을 통과하지 못했습니다.");
        }

        try
        {
            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            return snapshot.Content;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var update = await RunContentUpdateAsync(gameMode);
            if (!update.Applied)
                throw new InvalidDataException("게임 데이터 복구 업데이트가 검증을 통과하지 못했습니다.", exception);

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            return snapshot.Content;
        }
    }

    private async Task<ContentUpdateResult> RunContentUpdateAsync(GameMode gameMode)
    {
        UpdateProgressBar.Value = 0;
        UpdateProgressStageText.Text = "업데이트 준비 중...";
        UpdateProgressPercentText.Text = "0%";
        UpdateProgressOverlay.Visibility = Visibility.Visible;

        var progress = new Progress<ContentUpdateProgress>(value =>
        {
            var percent = Math.Clamp((int)Math.Round(value.Percent * 0.85), 0, 85);
            UpdateProgressBar.Value = percent;
            UpdateProgressStageText.Text = value.Message;
            UpdateProgressPercentText.Text = $"{percent}%";

        });

        try
        {
            var result = await _services.ContentUpdater.UpdateAsync(gameMode, progress: progress);
            if (!result.Applied)
                return result;

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            var imageProgress = new Progress<ImagePrefetchProgress>(value =>
            {
                var fraction = value.Total <= 0 ? 1d : value.Completed / (double)value.Total;
                var percent = 85 + Math.Clamp((int)Math.Round(fraction * 15), 0, 15);
                UpdateProgressBar.Value = percent;
                UpdateProgressStageText.Text = value.Total <= 0
                    ? "아이콘 준비 완료"
                    : $"아이콘 다운로드 중... {value.Completed}/{value.Total}";
                UpdateProgressPercentText.Text = $"{percent}%";

            });
            await _services.Images.PrefetchAsync(snapshot.Content, imageProgress);

            UpdateProgressBar.Value = 100;
            UpdateProgressStageText.Text = "업데이트 완료";
            UpdateProgressPercentText.Text = "100%";
            return result;
        }
        finally
        {
            UpdateProgressOverlay.Visibility = Visibility.Collapsed;
        }
    }




    private void QuestTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
    }

    private void HideoutTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Hideout;
        ShowActiveSection();
    }

    private void ItemsTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Items;
        ShowActiveSection();
    }

    private void AmmoTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Ammo;
        ShowActiveSection();
    }

    private void MapTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Map;
        ShowActiveSection();
    }

    private void ScannerTabButton_Click(object sender, RoutedEventArgs e)
    {
        _activeSection = DesktopSection.Scanner;
        ShowActiveSection();
    }

    private void ShowActiveSection()
    {
        if (_activeProfile is null || _activeContent is null)
        {
            QuestPage.Visibility = Visibility.Collapsed;
            HideoutPage.Visibility = Visibility.Collapsed;
            ItemsPage.Visibility = Visibility.Collapsed;
            AmmoPage.Visibility = Visibility.Collapsed;
            MapPlaceholder.Visibility = Visibility.Collapsed;
            ScannerPlaceholder.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            UpdateSectionButtons();
            return;
        }


        EmptyState.Visibility = Visibility.Collapsed;
        QuestPage.Visibility = _activeSection == DesktopSection.Quest ? Visibility.Visible : Visibility.Collapsed;
        HideoutPage.Visibility = _activeSection == DesktopSection.Hideout ? Visibility.Visible : Visibility.Collapsed;
        ItemsPage.Visibility = _activeSection == DesktopSection.Items ? Visibility.Visible : Visibility.Collapsed;
        AmmoPage.Visibility = _activeSection == DesktopSection.Ammo ? Visibility.Visible : Visibility.Collapsed;
        MapPlaceholder.Visibility = _activeSection == DesktopSection.Map ? Visibility.Visible : Visibility.Collapsed;
        ScannerPlaceholder.Visibility = _activeSection == DesktopSection.Scanner ? Visibility.Visible : Visibility.Collapsed;
        UpdateSectionButtons();
    }

    private void SetBusy(bool busy)
    {
        ProfileComboBox.IsEnabled = !busy && _profiles.Count > 0;
        EditProfileButton.IsEnabled = !busy && _activeProfile is not null;
        CreateProfileButton.IsEnabled = !busy &&
                                        _profiles.Select(profile => profile.GameMode).Distinct().Count() <
                                        Enum.GetValues<GameMode>().Length;
        UpdateDataButton.IsEnabled = !busy && _activeProfile is not null;
        QuestPage.SetBusy(busy);
        HideoutPage.SetBusy(busy);
        ItemsPage.SetBusy(busy);
        AmmoPage.SetBusy(busy);
        QuestTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Quest;
        HideoutTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Hideout;
        ItemsTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Items;
        AmmoTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Ammo;
        MapTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Map;
        ScannerTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Scanner;

    }

    private void UpdateSectionButtons()
    {
        var hasProfile = _activeProfile is not null;
        QuestTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Quest;
        HideoutTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Hideout;
        ItemsTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Items;
        AmmoTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Ammo;
        MapTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Map;
        ScannerTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Scanner;
    }


    private void ShowFailure(string title, Exception exception)
    {

        MessageBox.Show(
            this,
            exception.Message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string GameModeText(GameMode gameMode) => gameMode switch
    {
        GameMode.Regular => "PvP",
        GameMode.Pve => "PvE",
        GameMode.PvpSeason => "시즌",
        _ => gameMode.ToString(),
    };

    private static string FactionText(PmcFaction faction) => faction switch
    {
        PmcFaction.Usec => "USEC",
        PmcFaction.Bear => "BEAR",
        _ => faction.ToString(),
    };

    private sealed record ProfileChoice(GameProfileSnapshot Profile)
    {
        public override string ToString() =>
            $"{GameModeText(Profile.GameMode)} · Lv.{Profile.Level} · {FactionText(Profile.Faction)}";
    }

    private enum DesktopSection
    {
        Quest,
        Hideout,
        Items,
        Ammo,
        Map,
        Scanner,
    }
}
