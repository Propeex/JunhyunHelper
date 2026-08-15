using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private const string MapSmokeDiagnosticFileName = "junhyun-map-smoke-error.txt";
    private const string MapSmokeSuccessFileName = "junhyun-map-smoke-success.txt";

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
            var mapViewer = page.FindName("MapViewerGrid") as FrameworkElement
                ?? throw new InvalidOperationException("Map viewport was not found.");
            var mapScale = page.FindName("MapScale") as ScaleTransform
                ?? throw new InvalidOperationException("Map scale transform was not found.");
            var mapTranslate = page.FindName("MapTranslate") as TranslateTransform
                ?? throw new InvalidOperationException("Map translate transform was not found.");

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

            VerifyOtherFloorDirectionPresentation(floorSelector);
            await VerifyFactoryMainMapFloorPresentationAsync(page, mapSelector, floorSelector);

            // The page can raise Loaded while its containing product section is still
            // Collapsed during asynchronous startup. Viewport geometry is meaningful only
            // after the Map section has actually been arranged and made visible.
            await WaitForAsync(
                () => page.IsVisible && mapViewer.ActualWidth > 0 && mapViewer.ActualHeight > 0,
                TimeSpan.FromSeconds(8));

            page.JunhyunZoomIn();
            mapTranslate.X += 137;
            mapTranslate.Y -= 91;
            await Task.Delay(100);

            var zoomBefore = mapScale.ScaleX;
            if (zoomBefore <= 0 || mapViewer.ActualWidth <= 0 || mapViewer.ActualHeight <= 0)
                throw new InvalidOperationException("Map viewport was not measurable for floor hotkey smoke.");

            var centerX = mapViewer.ActualWidth / 2.0;
            var centerY = mapViewer.ActualHeight / 2.0;
            var canvasXBefore = (centerX - mapTranslate.X) / zoomBefore;
            var canvasYBefore = (centerY - mapTranslate.Y) / zoomBefore;
            var floorBeforeHotkey = floorSelector.SelectedIndex;
            var sourceBeforeHotkey = mapSvg.Source?.ToString();

            if (floorBeforeHotkey < floorSelector.Items.Count - 1)
                await page.JunhyunFloorUpAsync();
            else
                await page.JunhyunFloorDownAsync();

            await WaitForAsync(
                () => floorSelector.SelectedIndex != floorBeforeHotkey &&
                      !string.Equals(sourceBeforeHotkey, mapSvg.Source?.ToString(), StringComparison.Ordinal),
                TimeSpan.FromSeconds(5));

            var zoomAfter = mapScale.ScaleX;
            centerX = mapViewer.ActualWidth / 2.0;
            centerY = mapViewer.ActualHeight / 2.0;
            var canvasXAfter = (centerX - mapTranslate.X) / zoomAfter;
            var canvasYAfter = (centerY - mapTranslate.Y) / zoomAfter;

            if (Math.Abs(zoomAfter - zoomBefore) > 0.001 ||
                Math.Abs(canvasXAfter - canvasXBefore) > 0.75 ||
                Math.Abs(canvasYAfter - canvasYBefore) > 0.75)
            {
                throw new InvalidOperationException(
                    $"Floor hotkey changed Main Map viewport: zoom {zoomBefore:F4}->{zoomAfter:F4}, " +
                    $"center ({canvasXBefore:F2},{canvasYBefore:F2})->({canvasXAfter:F2},{canvasYAfter:F2}).");
            }

            await VerifyMiniMapProductAsync();
            WriteMapSmokeSuccess();
        }
        catch (Exception exception)
        {
            WriteMapSmokeDiagnostic(exception);
            Environment.Exit(86);
        }
    }

    private static void WriteMapSmokeSuccess()
    {
        try
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                MapSmokeSuccessFileName);
            System.IO.File.WriteAllText(path, "OK");
        }
        catch
        {
        }
    }

    private static void WriteMapSmokeDiagnostic(Exception exception)
    {
        System.Diagnostics.Debug.WriteLine($"[MapSmoke] {exception}");
        try
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                MapSmokeDiagnosticFileName);
            System.IO.File.WriteAllText(path, exception.ToString());
        }
        catch
        {
        }
    }

    private static void VerifyOtherFloorDirectionPresentation(ComboBox floorSelector)
    {
        if (floorSelector.Items.Count < 2 ||
            floorSelector.SelectedItem is not ComboBoxItem selectedItem ||
            selectedItem.Tag is not string selectedFloor)
        {
            throw new InvalidOperationException("Floor selector did not expose two smoke floors.");
        }

        var otherItem = floorSelector.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item =>
                item.Tag is string floor &&
                !string.Equals(floor, selectedFloor, StringComparison.OrdinalIgnoreCase));
        if (otherItem?.Tag is not string otherFloor)
            throw new InvalidOperationException("Could not choose an alternate floor for marker smoke.");

        var config = TarkovHelper.Services.Map.MapTrackerService.Instance.GetMapConfig("Customs")
            ?? throw new InvalidOperationException("Customs map config was unavailable for marker smoke.");
        var relation = JunhyunFloorPresentation.Resolve(config, otherFloor, selectedFloor);
        if (!relation.IsOtherFloor || relation.Arrow is not ("↑" or "↓"))
            throw new InvalidOperationException("Other-floor relation did not resolve to an up/down direction.");

        var visual = JunhyunQuestMarkerVisualFactoryV3.Create(
            new JunhyunQuestMarkerProjectionV2(
                "smoke-other-floor",
                "Other Floor Smoke",
                "objective",
                "Objective",
                "A",
                100,
                100,
                otherFloor));
        JunhyunFloorPresentation.ApplyToMarker(visual, relation);
        if (visual is not Canvas canvas || Math.Abs(canvas.Opacity - JunhyunFloorPresentation.OtherFloorOpacity) > 0.001)
            throw new InvalidOperationException("Other-floor marker opacity smoke failed.");

        var arrowFound = canvas.Children
            .OfType<Border>()
            .Select(border => border.Child)
            .OfType<TextBlock>()
            .Any(text => string.Equals(text.Text, relation.Arrow, StringComparison.Ordinal));
        if (!arrowFound)
            throw new InvalidOperationException("Other-floor direction badge smoke failed.");
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
        if (window.FindName("MapCanvas") is not Canvas mapCanvas)
            throw new InvalidOperationException("MiniMap MapCanvas was not found.");
        if (window.FindName("MapScale") is not ScaleTransform miniMapScale)
            throw new InvalidOperationException("MiniMap scale transform was not found.");
        if (window.FindName("MapTranslate") is not TranslateTransform miniMapTranslate)
            throw new InvalidOperationException("MiniMap translate transform was not found.");

        var persistedMarkerScale = JunhyunMapProductSettingsStore.Instance.MiniMapMarkerScale;
        var markerProbe = new Canvas();
        markerContainer.Children.Add(markerProbe);
        window.ApplyJunhyunMarkerScale(1.0);
        if (markerProbe.RenderTransform is not ScaleTransform fullTransform)
            throw new InvalidOperationException("MiniMap marker scale did not apply to a live marker.");
        var fullScale = fullTransform.ScaleX;

        window.ApplyJunhyunMarkerScale(0.5);
        if (markerProbe.RenderTransform is not ScaleTransform halfTransform ||
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

        var zoomBeforeControl = overlay.Settings.ZoomLevel;
        if (zoomBeforeControl < TarkovHelper.Models.Map.OverlayMiniMapSettings.MaxZoom - 0.001)
            overlay.ZoomIn();
        else
            overlay.ZoomOut();

        await WaitForAsync(
            () => Math.Abs(overlay.Settings.ZoomLevel - zoomBeforeControl) > 0.0001,
            TimeSpan.FromSeconds(2));

        if (window.FindName("TxtFloorName") is not TextBlock floorText)
            throw new InvalidOperationException("MiniMap floor indicator was not found.");

        await WaitForAsync(
            () => !string.IsNullOrWhiteSpace(floorText.Text) &&
                  mapContainer.ActualWidth > 0 &&
                  mapContainer.ActualHeight > 0 &&
                  mapCanvas.Width > 0 &&
                  mapCanvas.Height > 0 &&
                  miniMapScale.ScaleX > 0,
            TimeSpan.FromSeconds(4));

        // Reproduce the PlayerTracking failure mode directly: the live transform is the
        // authoritative player-centered viewport, while persisted offsets are stale.
        // The old floor renderer read the stale offsets after swapping SVG artwork and
        // snapped the MiniMap back to them.
        var liveTranslateX = miniMapTranslate.X;
        var liveTranslateY = miniMapTranslate.Y;
        overlay.Settings.MapOffsetX = liveTranslateX + 67;
        overlay.Settings.MapOffsetY = liveTranslateY - 43;

        var miniZoomBefore = miniMapScale.ScaleX;
        var miniCenterX = mapContainer.ActualWidth / 2.0;
        var miniCenterY = mapContainer.ActualHeight / 2.0;
        var miniCanvasXBefore = (miniCenterX - miniMapTranslate.X) / miniZoomBefore;
        var miniCanvasYBefore = (miniCenterY - miniMapTranslate.Y) / miniZoomBefore;
        var floorBefore = floorText.Text;

        await window.JunhyunMoveFloorUpAsync();
        if (string.Equals(floorBefore, floorText.Text, StringComparison.Ordinal))
            await window.JunhyunMoveFloorDownAsync();

        await WaitForAsync(
            () => !string.Equals(floorBefore, floorText.Text, StringComparison.Ordinal),
            TimeSpan.FromSeconds(4));

        var miniZoomAfter = miniMapScale.ScaleX;
        miniCenterX = mapContainer.ActualWidth / 2.0;
        miniCenterY = mapContainer.ActualHeight / 2.0;
        var miniCanvasXAfter = (miniCenterX - miniMapTranslate.X) / miniZoomAfter;
        var miniCanvasYAfter = (miniCenterY - miniMapTranslate.Y) / miniZoomAfter;

        if (Math.Abs(miniZoomAfter - miniZoomBefore) > 0.001 ||
            Math.Abs(miniCanvasXAfter - miniCanvasXBefore) > 0.75 ||
            Math.Abs(miniCanvasYAfter - miniCanvasYBefore) > 0.75)
        {
            throw new InvalidOperationException(
                $"Floor change reset MiniMap viewport: zoom {miniZoomBefore:F4}->{miniZoomAfter:F4}, " +
                $"center ({miniCanvasXBefore:F2},{miniCanvasYBefore:F2})->" +
                $"({miniCanvasXAfter:F2},{miniCanvasYAfter:F2}).");
        }

        if (Math.Abs(overlay.Settings.MapOffsetX - miniMapTranslate.X) > 0.01 ||
            Math.Abs(overlay.Settings.MapOffsetY - miniMapTranslate.Y) > 0.01)
        {
            throw new InvalidOperationException(
                "MiniMap live viewport and persisted offsets diverged after floor preservation.");
        }

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
    }
}