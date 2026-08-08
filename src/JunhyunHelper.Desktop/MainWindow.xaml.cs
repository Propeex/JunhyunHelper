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

        QuestPage.SetData(_activeContent, questWorkspace);
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
            var firstUpdate = await _services.ContentUpdater.UpdateAsync(gameMode);
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
            var update = await _services.ContentUpdater.UpdateAsync(gameMode);
            if (!update.Applied)
                throw new InvalidDataException("게임 데이터 복구 업데이트가 검증을 통과하지 못했습니다.", exception);

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(gameMode);
            return snapshot.Content;
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
        if (editor.ShowDialog() != true || editor.Result is not { } result)
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
            var result = await _services.ContentUpdater.UpdateAsync(_activeProfile.GameMode);
            if (!result.Applied)
            {
                throw new InvalidDataException(
                    "새 데이터가 검증을 통과하지 못해 기존 정상 데이터를 유지했습니다.");
            }

            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(_activeProfile.GameMode);
            _activeContent = snapshot.Content;
            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            ShowActiveSection();
            StatusText.Text = cleanupChanges.Count > 0
                ? $"업데이트 완료 · 정리 가능 변화 {cleanupChanges.Count}종"
                : $"업데이트 완료 · {BuildLoadedStatus(_activeProfile.GameMode)}";

            if (cleanupChanges.Count > 0)
            {
                MessageBox.Show(
                    this,
                    $"게임 데이터 변경으로 정리 가능한 보유 아이템이 {cleanupChanges.Count}종 생기거나 늘었습니다. 아이템 탭의 '정리 필요'에서 확인할 수 있습니다.",
                    "필요 아이템 변경",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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

        try
        {
            SetBusy(true, e.Action == QuestActionKind.Complete
                ? "퀘스트 완료를 저장하는 중..."
                : "퀘스트 완료를 취소하는 중...");

            _ = e.Action switch
            {
                QuestActionKind.Complete => await _services.Quests.CompleteAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId),
                QuestActionKind.UndoCompletion => await _services.Quests.UndoCompletionAsync(
                    _activeContent,
                    _activeProfile.ProfileId,
                    e.QuestId),
                _ => throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null),
            };

            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            StatusText.Text = BuildProgressChangeStatus(
                e.Action == QuestActionKind.Complete
                    ? "퀘스트 완료 저장됨"
                    : "퀘스트 완료 취소됨",
                cleanupChanges);
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

        try
        {
            SetBusy(true, "은신처 레벨을 저장하는 중...");
            await _services.Hideout.SetLevelAsync(
                _activeContent,
                _activeProfile.ProfileId,
                e.StationId,
                e.Level);

            var cleanupChanges = await RefreshActiveWorkspacesAsync(detectCleanupChanges: true);
            StatusText.Text = BuildProgressChangeStatus(
                e.Level.HasValue
                    ? "은신처 레벨 저장됨"
                    : "은신처 레벨 입력 해제됨",
                cleanupChanges);
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
            StatusText.Text = "보유량 저장됨";
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

    private void ShowActiveSection()
    {
        if (_activeProfile is null || _activeContent is null)
        {
            QuestPage.Visibility = Visibility.Collapsed;
            HideoutPage.Visibility = Visibility.Collapsed;
            ItemsPage.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            UpdateSectionButtons();
            return;
        }

        EmptyState.Visibility = Visibility.Collapsed;
        QuestPage.Visibility = _activeSection == DesktopSection.Quest
            ? Visibility.Visible
            : Visibility.Collapsed;
        HideoutPage.Visibility = _activeSection == DesktopSection.Hideout
            ? Visibility.Visible
            : Visibility.Collapsed;
        ItemsPage.Visibility = _activeSection == DesktopSection.Items
            ? Visibility.Visible
            : Visibility.Collapsed;
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
        QuestTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Quest;
        HideoutTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Hideout;
        ItemsTabButton.IsEnabled = !busy && _activeProfile is not null && _activeSection != DesktopSection.Items;
        StatusText.Text = status;
    }

    private void UpdateSectionButtons()
    {
        var hasProfile = _activeProfile is not null;
        QuestTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Quest;
        HideoutTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Hideout;
        ItemsTabButton.IsEnabled = hasProfile && _activeSection != DesktopSection.Items;
    }

    private string BuildLoadedStatus(GameMode gameMode)
    {
        if (_activeContent is null)
            return GameModeText(gameMode);

        var cleanupCount = _activeItemsWorkspace?.Plan.CleanupItems.Count ?? 0;
        return $"{GameModeText(gameMode)} · Quest {_activeContent.Quests.Count} · Hideout {_activeContent.HideoutStations.Count} · 정리 {cleanupCount}";
    }

    private static string BuildProgressChangeStatus(
        string baseStatus,
        IReadOnlyList<InventoryCleanupIncrease> cleanupChanges) =>
        cleanupChanges.Count > 0
            ? $"{baseStatus} · 정리 가능 변화 {cleanupChanges.Count}종"
            : baseStatus;

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
    }
}
