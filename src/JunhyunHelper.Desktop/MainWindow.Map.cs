using System.Windows;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Desktop.Map;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _mapIntegrationInitialized;
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
            _mapPage.Dispose();
        }

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

    private async Task RefreshMapPageFromActiveProfileAsync()
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        var page = EnsureMapPageCreated();
        if (page is null)
            return;

        try
        {
            var workspace = await _services.Quests.LoadAsync(
                _activeContent,
                _activeProfile.ProfileId);
            await page.SetDataAsync(_activeContent, workspace);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Map data load failed", exception);
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
