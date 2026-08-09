using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Map;
using JunhyunHelper.Desktop.Services;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private const string CurrentMapArtworkMigrationMarker = "wiki-background-v1.ready";

    private readonly SemaphoreSlim _mapAssetEnsureGate = new(1, 1);
    private bool _mapIntegrationInitialized;
    private bool _mapArtworkMigrationAttempted;
    private MapPage? _mapPage;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_mapIntegrationInitialized)
            return;

        _mapIntegrationInitialized = true;
        MapPlaceholder.IsVisibleChanged += MapPlaceholder_IsVisibleChanged;
        _services.ContentUpdater.ContentActivated += ContentUpdater_ContentActivated;
        EnsureMapProfileRefreshHook();

        if (MapPlaceholder.IsVisible)
            _ = RefreshMapPageFromActiveProfileAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_mapIntegrationInitialized)
        {
            _services.ContentUpdater.ContentActivated -= ContentUpdater_ContentActivated;
            MapPlaceholder.IsVisibleChanged -= MapPlaceholder_IsVisibleChanged;
            RemoveMapProfileRefreshHook();
        }

        if (_mapPage is not null)
        {
            _mapPage.QuestNavigationRequested -= MapPage_QuestNavigationRequested;
            _mapPage.MapAssetRetryRequested -= MapPage_MapAssetRetryRequested;
            _mapPage.Dispose();
        }

        _mapAssetEnsureGate.Dispose();
        base.OnClosed(e);
    }

    private MapPage? EnsureMapPageCreated()
    {
        if (_mapPage is not null)
            return _mapPage;

        try
        {
            var page = new MapPage();
            page.SetServices(_services.MapAssets, _services.MapUserData);
            page.QuestNavigationRequested += MapPage_QuestNavigationRequested;
            page.MapAssetRetryRequested += MapPage_MapAssetRetryRequested;
            MapPageHost.Children.Clear();
            MapPageHost.Children.Add(page);
            _mapPage = page;
            return page;
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("MapPage initialization failed", exception);
            MapPageHost.Children.Clear();
            MapPageHost.Children.Add(new Border
            {
                Background = (System.Windows.Media.Brush)FindResource("BackgroundMediumBrush"),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24),
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = $"지도를 초기화하지 못했습니다.\n\n{exception.Message}\n\n다른 탭은 계속 사용할 수 있습니다. 오류 기록: %LocalAppData%\\JunhyunHelper\\logs",
                    Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 620,
                },
            });
            StatusText.Text = "지도 초기화 실패 · 다른 기능은 계속 사용할 수 있습니다.";
            return null;
        }
    }

    private void MapPlaceholder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            _ = RefreshMapPageFromActiveProfileAsync();
    }

    private void ContentUpdater_ContentActivated(GameMode gameMode, GameContentCatalog content)
    {
        if (_activeProfile is null || _activeProfile.GameMode != gameMode || _mapPage is null)
            return;

        _ = Dispatcher.BeginInvoke(async () =>
        {
            if (_activeProfile is null || _activeProfile.GameMode != gameMode || _mapPage is null)
                return;

            try
            {
                var workspace = await _services.Quests.LoadAsync(content, _activeProfile.ProfileId);
                await _mapPage.SetDataAsync(content, workspace);
            }
            catch (Exception exception)
            {
                App.WriteDiagnostic("Map refresh after content activation failed", exception);
                StatusText.Text = $"지도 새로고침 실패 · {exception.Message}";
            }
        });
    }

    private async Task RefreshMapPageFromActiveProfileAsync(bool forceMapAssetRefresh = false)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var page = EnsureMapPageCreated();
        if (page is null)
            return;

        try
        {
            await EnsureMapAssetsReadyAsync(page, _activeContent, forceMapAssetRefresh);

            var workspace = await _services.Quests.LoadAsync(
                _activeContent,
                _activeProfile.ProfileId);
            await page.SetDataAsync(_activeContent, workspace);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Map data load failed", exception);
            page.SetAssetRecoveryState(
                $"지도를 불러오지 못했습니다.\n{exception.Message}",
                retryEnabled: true);
            StatusText.Text = $"지도를 불러오지 못했습니다 · {exception.Message}";
        }
    }

    private async Task<bool> EnsureMapAssetsReadyAsync(
        MapPage page,
        GameContentCatalog content,
        bool forceRefresh)
    {
        await _mapAssetEnsureGate.WaitAsync();
        try
        {
            var hasUsableActiveAssets = await _services.MapAssets.HasUsableActiveAssetsAsync();
            var migrationMarkerPath = Path.Combine(
                _services.MapAssets.ActiveDirectory,
                CurrentMapArtworkMigrationMarker);
            var requiresArtworkMigration =
                hasUsableActiveAssets &&
                !_mapArtworkMigrationAttempted &&
                !File.Exists(migrationMarkerPath);

            if (!forceRefresh && hasUsableActiveAssets && !requiresArtworkMigration)
            {
                page.SetAssetRecoveryState(null, retryEnabled: true);
                return true;
            }

            if (requiresArtworkMigration)
                _mapArtworkMigrationAttempted = true;

            page.SetBusy(true);
            page.SetAssetRecoveryState(
                requiresArtworkMigration
                    ? "새 지도 배경을 준비하는 중입니다..."
                    : "지도 레이아웃과 자산을 내려받는 중입니다...",
                retryEnabled: false);
            StatusText.Text = requiresArtworkMigration
                ? "새 지도 배경으로 업데이트하는 중..."
                : "지도 자산을 준비하는 중...";

            var progress = new Progress<MapAssetUpdateProgress>(value =>
            {
                StatusText.Text = value.Message;
                page.SetAssetRecoveryState(value.Message, retryEnabled: false);
            });

            try
            {
                var result = await _services.MapAssets.UpdateAsync(content, progress);
                if (result.Layouts.Count > 0)
                {
                    try
                    {
                        Directory.CreateDirectory(_services.MapAssets.ActiveDirectory);
                        await File.WriteAllTextAsync(
                            Path.Combine(_services.MapAssets.ActiveDirectory, CurrentMapArtworkMigrationMarker),
                            CurrentMapArtworkMigrationMarker);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        App.WriteDiagnostic("Map artwork migration marker write failed", exception);
                    }
                }

                page.SetAssetRecoveryState(null, retryEnabled: true);
                StatusText.Text = result.Warnings.Count == 0
                    ? $"지도 {result.Layouts.Count}개 준비 완료"
                    : $"지도 {result.Layouts.Count}개 준비 완료 · 일부 자산은 이전본/기본 표시 사용";
                return result.Layouts.Count > 0;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                App.WriteDiagnostic("Map asset automatic recovery failed", exception);

                var hasPrevious = await _services.MapAssets.HasUsableActiveAssetsAsync();
                if (hasPrevious)
                {
                    page.SetAssetRecoveryState(
                        "최신 지도 자산을 다시 받지 못했지만 기존 정상 지도를 유지했습니다.",
                        retryEnabled: true);
                    StatusText.Text = "지도 업데이트 실패 · 기존 정상 지도 유지";
                    return true;
                }

                page.SetAssetRecoveryState(
                    $"지도 자산을 내려받지 못했습니다.\n{exception.Message}\n\n아래 버튼으로 지도만 다시 받을 수 있습니다.",
                    retryEnabled: true);
                StatusText.Text = "지도 자산 다운로드 실패";
                return false;
            }
            finally
            {
                page.SetBusy(false);
            }
        }
        finally
        {
            _mapAssetEnsureGate.Release();
        }
    }

    private async void MapPage_MapAssetRetryRequested(object? sender, EventArgs e)
    {
        await RefreshMapPageFromActiveProfileAsync(forceMapAssetRefresh: true);
    }

    private void MapPage_QuestNavigationRequested(object? sender, MapQuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }
}
