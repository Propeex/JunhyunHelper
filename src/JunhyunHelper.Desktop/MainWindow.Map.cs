using System.Windows;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Map;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _mapIntegrationInitialized;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_mapIntegrationInitialized)
            return;

        _mapIntegrationInitialized = true;
        MapPage.SetServices(_services.MapAssets, _services.MapUserData);
        MapPage.QuestNavigationRequested += MapPage_QuestNavigationRequested;
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
            MapPage.QuestNavigationRequested -= MapPage_QuestNavigationRequested;
            RemoveMapProfileRefreshHook();
        }

        MapPage.Dispose();
        base.OnClosed(e);
    }

    private void MapPlaceholder_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            _ = RefreshMapPageFromActiveProfileAsync();
    }

    private void ContentUpdater_ContentActivated(GameMode gameMode, GameContentCatalog content)
    {
        if (_activeProfile is null || _activeProfile.GameMode != gameMode)
            return;

        _ = Dispatcher.BeginInvoke(async () =>
        {
            if (_activeProfile is null || _activeProfile.GameMode != gameMode)
                return;

            try
            {
                var workspace = await _services.Quests.LoadAsync(content, _activeProfile.ProfileId);
                await MapPage.SetDataAsync(content, workspace);
            }
            catch (Exception exception)
            {
                StatusText.Text = $"지도 새로고침 실패 · {exception.Message}";
            }
        });
    }

    private async Task RefreshMapPageFromActiveProfileAsync()
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        try
        {
            var workspace = await _services.Quests.LoadAsync(
                _activeContent,
                _activeProfile.ProfileId);
            await MapPage.SetDataAsync(_activeContent, workspace);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"지도를 불러오지 못했습니다 · {exception.Message}";
        }
    }

    private void MapPage_QuestNavigationRequested(object? sender, MapQuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }
}
