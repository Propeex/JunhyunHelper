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
            SetBusy(true, "프로필을 불러오는 중...");
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
            SetBusy(false, StatusText.Text);
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
        StatusText.Text = "프로필 설정 필요";
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
            SetBusy(false, StatusText.Text);
        }
    }

    private async Task LoadSelectedProfileAsync()
    {
        if (ProfileComboBox.SelectedItem is not ProfileChoice choice)
            return;

        SetBusy(true, "게임 데이터를 불러오는 중...");
        _activeProfile = choice.Profile;
        _activeContent = await ReadOrCreateContentAsync(choice.Profile.GameMode);
        _activeItemsWorkspace = null;
        ItemsPage.ClearCleanupNotice();

        await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
        AmmoPage.SetData(_activeContent);
        EmptyState.Visibility = Visibility.Collapsed;
        ShowActiveSection();
        StatusText.Text = BuildLoadedStatus(choice.Profile.GameMode);
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
            StatusText.Text = value.Message;
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
                StatusText.Text = UpdateProgressStageText.Text;
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

    private async void CreateProfileButton_Click(object sender, RoutedEventArgs e)
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

        var modeWindow = new ProfileModeWindow(availableModes)
        {
            Owner = this,
        };
        if (modeWindow.ShowDialog() != true || modeWindow.SelectedMode is not { } mode)
            return;

        try
        {
            SetBusy(true, $"{GameModeText(mode)} 데이터를 준비하는 중...");
            var content = await ReadOrCreateContentAsync(mode);
            SetBusy(false, "프로필 정보를 입력해주세요.");

            var editor = new ProfileEditorWindow(mode, content)
            {
                Owner = this,
            };
            if (editor.ShowDialog() != true || editor.Result is not { } result)
                return;

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

    private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
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

        if (editor.DeleteRequested)
        {
            try
            {
                SetBusy(true, "프로필을 삭제하는 중...");
                await _services.ProfileManagement.DeleteAsync(profileId);
                _activeProfile = null;
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
            return;
        }

        if (editor.Result is not { } result)
            return;

        try
        {
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
            ShowFailure("프로필을 수정하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void UpdateDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
            return;

        try
        {
            SetBusy(true, "최신 게임 데이터를 업데이트하는 중...");
            var result = await RunContentUpdateAsync(_activeProfile.GameMode);
            if (!result.Applied)
            {
                throw new InvalidDataException(
                    "새 데이터가 검증을 통과하지 못해 기존 정상 데이터를 유지했습니다.");
            }

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(_activeProfile.GameMode);
            _activeContent = snapshot.Content;
            AmmoPage.SetData(_activeContent);
            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            ShowActiveSection();

            if (cleanupChanges.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"게임 데이터 변경으로 정리 가능한 보유 아이템이 {cleanupChanges.Count}종 생기거나 늘었습니다. 아이템 탭의 '정리 필요'에서 확인할 수 있습니다.",
                    "필요 아이템 변경",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("게임 데이터를 업데이트하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void QuestPage_ActionRequested(object? sender, QuestActionRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var restoreInventory = false;
        if (e.Action == QuestActionKind.UndoCompletion &&
            _activeProfile.QuestConsumptions.TryGetValue(e.QuestId, out var consumption) &&
            !consumption.IsEmpty)
        {
            var decision = MessageBox.Show(
                this,
                "이 퀘스트를 완료할 때 자동으로 차감한 보유 아이템 기록이 있습니다.\n\n차감했던 수량을 보유량에 다시 복원할까요?",
                "퀘스트 완료 취소",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);
            if (decision == MessageBoxResult.Cancel)
                return;
            restoreInventory = decision == MessageBoxResult.Yes;
        }

        try
        {
            SetBusy(true, e.Action switch
            {
                QuestActionKind.Complete => "퀘스트 완료를 저장하는 중...",
                QuestActionKind.UndoCompletion => "퀘스트 완료를 취소하는 중...",
                QuestActionKind.Fail => "퀘스트 실패를 저장하는 중...",
                QuestActionKind.UndoFailure => "퀘스트 실패를 취소하는 중...",
                _ => "퀘스트 진행 상태를 저장하는 중...",
            });

            _ = e.Action switch
            {
                QuestActionKind.Complete => await _services.Quests.CompleteAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId),
                QuestActionKind.UndoCompletion => await _services.Quests.UndoCompletionAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId,
                    restoreInventory),
                QuestActionKind.Fail => await _services.Quests.FailAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId),
                QuestActionKind.UndoFailure => await _services.Quests.UndoFailureAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId),
                _ => throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null),
            };

            await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("퀘스트 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void HideoutPage_LevelChangeRequested(
        object? sender,
        HideoutLevelChangeRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var currentLevel = _activeProfile.HideoutLevels.TryGetValue(e.StationId, out var savedLevel)
            ? savedLevel
            : 0;
        var targetLevel = e.Level ?? 0;
        var restoreInventory = false;

        if (targetLevel < currentLevel)
        {
            var hasConsumption = Enumerable.Range(targetLevel + 1, currentLevel - targetLevel)
                .Any(level => _activeProfile.HideoutUpgradeConsumptions.ContainsKey(
                    HideoutApplicationService.UpgradeConsumptionKey(e.StationId, level)));
            if (hasConsumption)
            {
                var decision = MessageBox.Show(
                    this,
                    "되돌리는 은신처 업그레이드에서 자동으로 차감한 보유 아이템 기록이 있습니다.\n\n차감했던 수량을 보유량에 다시 복원할까요?",
                    "은신처 레벨 되돌리기",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);
                if (decision == MessageBoxResult.Cancel)
                    return;
                restoreInventory = decision == MessageBoxResult.Yes;
            }
        }

        try
        {
            SetBusy(true, "은신처 레벨을 저장하는 중...");
            await _services.Hideout.SetLevelAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.StationId,
                e.Level,
                restoreInventory);

            await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("은신처 진행 상태를 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
        }
    }

    private async void ItemsPage_InventoryChangeRequested(
        object? sender,
        InventoryChangeRequestedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        try
        {
            SetBusy(true, "보유 아이템 수량을 저장하는 중...");
            var workspace = await _services.Items.SetInventoryAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.ItemId,
                e.Fir,
                e.NonFir);

            _activeProfile = workspace.Profile;
            _activeItemsWorkspace = workspace;
            ItemsPage.SetData(_activeContent, workspace);
            ItemsPage.ClearCleanupNotice();
            StatusText.Text = BuildLoadedStatus(_activeProfile.GameMode);
        }
        catch (Exception exception)
        {
            ShowFailure("보유 아이템 수량을 변경하지 못했습니다.", exception);
        }
        finally
        {
            SetBusy(false, StatusText.Text);
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

    private void SetBusy(bool busy, string status)
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
        StatusText.Text = status;
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

    private string BuildLoadedStatus(GameMode gameMode)
    {
        _ = gameMode;
        var cleanupCount = _activeItemsWorkspace?.Plan.CleanupItems.Count ?? 0;
        return $"정리 필요 {cleanupCount}";
    }

    private void ShowFailure(string title, Exception exception)
    {
        StatusText.Text = title;
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
