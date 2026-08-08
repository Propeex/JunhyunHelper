using System.IO;
using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
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
    private bool _initializing;

    public MainWindow()
    {
        InitializeComponent();
        QuestPage.ActionRequested += QuestPage_ActionRequested;
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
        QuestPage.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        StatusText.Text = "프로필 설정 필요";
        EditProfileButton.IsEnabled = false;
        UpdateDataButton.IsEnabled = false;
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

        var workspace = await _services.Quests.LoadAsync(
            _activeContent,
            choice.Profile.ProfileId);
        _activeProfile = workspace.Profile;

        QuestPage.SetData(_activeContent, workspace);
        QuestPage.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;
        StatusText.Text = $"{GameModeText(choice.Profile.GameMode)} · {_activeContent.Quests.Count}개 퀘스트";
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
            var workspace = await _services.Quests.LoadAsync(
                _activeContent,
                _activeProfile.ProfileId);
            _activeProfile = workspace.Profile;
            QuestPage.SetData(_activeContent, workspace);
            StatusText.Text = $"업데이트 완료 · {_activeContent.Quests.Count}개 퀘스트";
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

            QuestWorkspace workspace = e.Action switch
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

            _activeProfile = workspace.Profile;
            QuestPage.SetData(_activeContent, workspace);
            StatusText.Text = e.Action == QuestActionKind.Complete
                ? "퀘스트 완료 저장됨"
                : "퀘스트 완료 취소됨";
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

    private void SetBusy(bool busy, string status)
    {
        ProfileComboBox.IsEnabled = !busy && _profiles.Count > 0;
        EditProfileButton.IsEnabled = !busy && _activeProfile is not null;
        CreateProfileButton.IsEnabled = !busy && _profiles.Select(profile => profile.GameMode).Distinct().Count() < Enum.GetValues<GameMode>().Length;
        UpdateDataButton.IsEnabled = !busy && _activeProfile is not null;
        QuestPage.SetBusy(busy);
        StatusText.Text = status;
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
}
