using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;
using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop;

/// <summary>
/// Host boundary for the exact Tarkov Helper Map subsystem plus the approved
/// JunhyunHelper product delta. Quest is the only cross-feature dependency.
/// </summary>
public partial class MainWindow : TarkovHelper.MainWindow
{
    private const string MapSmokeEnvironmentVariable = "JUNHYUNHELPER_MAP_SMOKE";

    private TarkovHelper.Pages.Map.MapPage? _legacyMapPage;
    private LegacyMapProductAdapter? _legacyMapProductAdapter;
    private LegacyMapProductRuntime? _legacyMapProductRuntime;
    private LegacyAdditionalMapMarkerController? _legacyAdditionalMapMarkers;
    private LegacyMapQuestV2Controller? _legacyMapQuestV2;
    private LegacyMapQuestSidebarV2? _legacyMapQuestSidebarV2;
    private LegacyMapQuestSidebarPolishBridge? _legacyMapQuestSidebarPolish;
    private readonly HashSet<Core.Profiles.GameMode> _questMapGeometryUpgradeAttempted = [];
    private bool _legacyMapTabHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_legacyMapTabHooked)
            return;

        _legacyMapTabHooked = true;
        MapTabButton.Click += LegacyMapTabButton_Click;

        if (string.Equals(
                Environment.GetEnvironmentVariable(MapSmokeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            EnsureLegacyMapPage();
        }
    }

    private async void LegacyMapTabButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureLegacyMapPage();
        await EnsureQuestMapGeometryCurrentAsync();
        _legacyMapProductAdapter?.Refresh();
        _legacyMapQuestV2?.Refresh();
        _legacyAdditionalMapMarkers?.Refresh();
    }

    /// <summary>
    /// v3 content remains a valid offline fallback. First Map use attempts an atomic
    /// content update so Quest geometry becomes v4 without requiring manual cleanup.
    /// </summary>
    private async Task EnsureQuestMapGeometryCurrentAsync()
    {
        if (_activeProfile is null ||
            !_questMapGeometryUpgradeAttempted.Add(_activeProfile.GameMode))
        {
            return;
        }

        try
        {
            var snapshot = await _services.Content.ReadActiveOrRecoverAsync(_activeProfile.GameMode);
            if (snapshot.SchemaVersion >= ContentSnapshotStore.CurrentSchemaVersion)
                return;

            StatusText.Text = "퀘스트 지도 좌표를 최신 데이터로 준비하는 중...";
            var update = await RunContentUpdateAsync(_activeProfile.GameMode);
            if (!update.Applied)
            {
                StatusText.Text = "기존 게임 데이터로 지도 표시 중";
                return;
            }

            var upgraded = await _services.Content.ReadActiveOrRecoverAsync(_activeProfile.GameMode);
            _activeContent = upgraded.Content;
            await RefreshActiveWorkspacesAsync(detectCleanupChanges: false);
            AmmoPage.SetData(_activeContent);
            StatusText.Text = "퀘스트 지도 좌표 업데이트 완료";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText.Text = "기존 게임 데이터로 지도 표시 중";
        }
    }

    private void EnsureLegacyMapPage()
    {
        if (_legacyMapPage is not null)
            return;

        try
        {
            var page = new TarkovHelper.Pages.Map.MapPage();

            // V1 adapter still owns the minimal exact-source UI cleanup. Its old Quest
            // sidebar/projection is kept disconnected; V2 owns all real Quest UI/data.
            var disconnectedV1Sidebar = new LegacyMapQuestSidebar();
            var adapter = new LegacyMapProductAdapter(
                page,
                disconnectedV1Sidebar,
                () => null,
                () => null);

            var sidebar = new LegacyMapQuestSidebarV2();
            var host = new Grid();
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(sidebar, 0);
            Grid.SetColumn(page, 1);
            host.Children.Add(sidebar);
            host.Children.Add(page);

            MapPlaceholder.Children.Clear();
            MapPlaceholder.Children.Add(host);

            _legacyMapPage = page;
            _legacyMapProductAdapter = adapter;
            _legacyMapQuestSidebarV2 = sidebar;
            _legacyMapQuestSidebarPolish = new LegacyMapQuestSidebarPolishBridge(sidebar);
            _legacyMapQuestV2 = new LegacyMapQuestV2Controller(
                page,
                sidebar,
                () => QuestPage.CurrentContentForMap,
                () => QuestPage.CurrentWorkspaceForMap,
                OpenQuestFromMap);
            _legacyMapProductRuntime = new LegacyMapProductRuntime(
                page,
                () => _legacyMapQuestV2?.Refresh());
            _legacyAdditionalMapMarkers = new LegacyAdditionalMapMarkerController(page);
        }
        catch (Exception exception)
        {
            if (string.Equals(
                    Environment.GetEnvironmentVariable(MapSmokeEnvironmentVariable),
                    "1",
                    StringComparison.Ordinal))
            {
                throw;
            }

            MessageBox.Show(
                this,
                $"기존 Tarkov Helper 지도 초기화에 실패했습니다.\n\n{exception.Message}",
                "지도",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenQuestFromMap(string questId)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.FocusQuest(questId);
    }

    public void SetFullScreenMode(bool enabled)
    {
        // Product requirement: full-screen Map mode is removed.
    }
}
