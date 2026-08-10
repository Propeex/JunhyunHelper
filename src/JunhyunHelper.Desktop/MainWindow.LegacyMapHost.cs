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

        // WPF can raise OnInitialized from inside InitializeComponent before the
        // MainWindow constructor finishes attaching its original mutation events.
        // Re-apply the idempotent product wiring here, after construction is complete.
        EnableFastMutationHandlers();

        if (_legacyMapTabHooked)
            return;

        _legacyMapTabHooked = true;
        MapTabButton.Click += LegacyMapTabButton_Click;

        if (IsMapSmokeEnabled())
            EnsureLegacyMapPage();
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

            if (IsMapSmokeEnabled())
                page.Loaded += MapSmoke_PageLoaded;
        }
        catch (Exception exception)
        {
            if (IsMapSmokeEnabled())
                throw;

            MessageBox.Show(
                this,
                $"기존 Tarkov Helper 지도 초기화에 실패했습니다.\n\n{exception.Message}",
                "지도",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool IsMapSmokeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(MapSmokeEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    private async void MapSmoke_PageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TarkovHelper.Pages.Map.MapPage page)
            return;

        page.Loaded -= MapSmoke_PageLoaded;
        try
        {
            var probe = JunhyunQuestMarkerVisualFactoryV3.Create(
                new JunhyunQuestMarkerProjectionV2(
                    "smoke-quest",
                    "Smoke Quest",
                    "smoke-objective",
                    "Smoke Objective",
                    "A",
                    100,
                    100,
                    null));
            if (probe is not Canvas probeCanvas || probeCanvas.Children.Count == 0)
                throw new InvalidOperationException("Quest marker Canvas anchor smoke failed.");

            var mapSelector = page.FindName("CmbMapSelect") as ComboBox
                ?? throw new InvalidOperationException("Map selector was not found.");
            var floorSelector = page.FindName("CmbFloorSelect") as ComboBox
                ?? throw new InvalidOperationException("Floor selector was not found.");
            var mapSvg = page.FindName("MapSvg") as SharpVectors.Converters.SvgViewbox
                ?? throw new InvalidOperationException("Map SVG view was not found.");

            await WaitForAsync(() => mapSelector.Items.Count > 0, TimeSpan.FromSeconds(3));

            var multiFloorIndex = -1;
            for (var index = 0; index < mapSelector.Items.Count; index++)
            {
                if (mapSelector.Items[index] is ComboBoxItem item &&
                    string.Equals(item.Tag as string, "Customs", StringComparison.OrdinalIgnoreCase))
                {
                    multiFloorIndex = index;
                    break;
                }
            }

            if (multiFloorIndex < 0)
                throw new InvalidOperationException("Customs was not available for floor smoke.");

            mapSelector.SelectedIndex = multiFloorIndex;
            await WaitForAsync(
                () => floorSelector.Items.Count >= 2 && floorSelector.Visibility == Visibility.Visible,
                TimeSpan.FromSeconds(3));

            var originalFloorIndex = Math.Max(0, floorSelector.SelectedIndex);
            var targetFloorIndex = originalFloorIndex == 0 ? 1 : 0;
            var sourceBefore = mapSvg.Source?.ToString();
            floorSelector.SelectedIndex = targetFloorIndex;

            await WaitForAsync(
                () => floorSelector.SelectedIndex == targetFloorIndex &&
                      !string.Equals(sourceBefore, mapSvg.Source?.ToString(), StringComparison.Ordinal),
                TimeSpan.FromSeconds(4));

            await VerifyMiniMapProductAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"[MapSmoke] {exception}");
            Environment.Exit(86);
        }
    }

    private static async Task VerifyMiniMapProductAsync()
    {
        var overlay = TarkovHelper.Services.OverlayMiniMapService.Instance;
        overlay.ShowOverlay();

        await WaitForAsync(() => overlay.IsOverlayVisible, TimeSpan.FromSeconds(5));

        var window = System.Windows.Application.Current.Windows
            .OfType<TarkovHelper.Windows.OverlayMiniMapWindow>()
            .FirstOrDefault(candidate => candidate.IsVisible)
            ?? throw new InvalidOperationException("Visible MiniMap window was not found.");

        await WaitForAsync(() => window.IsLoaded, TimeSpan.FromSeconds(2));

        if (window.ResizeMode != ResizeMode.NoResize)
            throw new InvalidOperationException("MiniMap still allows mouse resizing.");

        if (window.FindName("MapContainer") is not Grid mapContainer)
            throw new InvalidOperationException("MiniMap MapContainer was not found.");

        if (mapContainer.Children.OfType<System.Windows.Shapes.Path>().Any())
            throw new InvalidOperationException("MiniMap legacy bottom-right resize grip is still present.");

        if (window.FindName("MapMarkersContainer") is not Canvas markerContainer)
            throw new InvalidOperationException("MiniMap marker container was not found.");

        // Verify the product marker-size path changes the actual live marker transform,
        // not only a saved numeric setting. The synthetic marker avoids depending on
        // external marker DB timing/content.
        var persistedMarkerScale = JunhyunMapProductSettingsStore.Instance.MiniMapMarkerScale;
        var markerProbe = new Canvas();
        markerContainer.Children.Add(markerProbe);
        window.ApplyJunhyunMarkerScale(1.0);
        if (markerProbe.RenderTransform is not System.Windows.Media.ScaleTransform fullTransform)
            throw new InvalidOperationException("MiniMap marker scale did not apply to a live marker.");
        var fullScale = fullTransform.ScaleX;

        window.ApplyJunhyunMarkerScale(0.5);
        if (markerProbe.RenderTransform is not System.Windows.Media.ScaleTransform halfTransform ||
            !(halfTransform.ScaleX < fullScale * 0.75))
        {
            throw new InvalidOperationException("MiniMap marker scale did not shrink live markers.");
        }

        markerContainer.Children.Remove(markerProbe);
        window.ApplyJunhyunMarkerScale(persistedMarkerScale);

        var legacyHook = TarkovHelper.Services.GlobalKeyboardHookService.Instance;
        await WaitForAsync(
            () => legacyHook.ZoomInKey == 0 &&
                  legacyHook.ZoomOutKey == 0 &&
                  legacyHook.FloorUpKey == 0 &&
                  legacyHook.FloorDownKey == 0,
            TimeSpan.FromSeconds(2));

        var zoomBefore = overlay.Settings.ZoomLevel;
        if (zoomBefore < TarkovHelper.Models.Map.OverlayMiniMapSettings.MaxZoom - 0.001)
            overlay.ZoomIn();
        else
            overlay.ZoomOut();

        await WaitForAsync(
            () => Math.Abs(overlay.Settings.ZoomLevel - zoomBefore) > 0.0001,
            TimeSpan.FromSeconds(2));

        if (window.FindName("TxtFloorName") is not TextBlock floorText)
            throw new InvalidOperationException("MiniMap floor indicator was not found.");

        await WaitForAsync(
            () => !string.IsNullOrWhiteSpace(floorText.Text),
            TimeSpan.FromSeconds(4));

        var floorBefore = floorText.Text;
        overlay.MoveFloorUp();
        await Task.Delay(350);
        if (string.Equals(floorBefore, floorText.Text, StringComparison.Ordinal))
            overlay.MoveFloorDown();

        await WaitForAsync(
            () => !string.Equals(floorBefore, floorText.Text, StringComparison.Ordinal),
            TimeSpan.FromSeconds(4));

        // MiniMap actions raise SettingsChanged, which used to re-arm the transplanted
        // legacy direct hook. Verify it remains disabled after real zoom/floor actions.
        await WaitForAsync(
            () => legacyHook.ZoomInKey == 0 &&
                  legacyHook.ZoomOutKey == 0 &&
                  legacyHook.FloorUpKey == 0 &&
                  legacyHook.FloorDownKey == 0,
            TimeSpan.FromSeconds(2));

        overlay.HideOverlay();
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(100);
        }

        throw new TimeoutException("Map smoke condition timed out.");
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
