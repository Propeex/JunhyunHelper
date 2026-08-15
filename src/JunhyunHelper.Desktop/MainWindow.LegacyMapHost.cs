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
                StatusText.Text = "퀘스트 지도 좌표 업데이트 실패 — 기존 데이터 유지";
                return;
            }

            await ReloadActiveProfileAsync();
            StatusText.Text = "퀘스트 지도 좌표 업데이트 완료";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Quest map geometry refresh failed: {ex}");
            StatusText.Text = "퀘스트 지도 좌표 업데이트 실패 — 기존 데이터 유지";
        }
    }

    private void EnsureLegacyMapPage()
    {
        if (_legacyMapPage is not null)
            return;

        _legacyMapPage = new TarkovHelper.Pages.Map.MapPage();
        _legacyMapProductAdapter = new LegacyMapProductAdapter(_legacyMapPage);
        _legacyMapProductRuntime = new LegacyMapProductRuntime(
            _legacyMapPage,
            RefreshLegacyQuestMapProjection);
        _legacyAdditionalMapMarkers = new LegacyAdditionalMapMarkerController(_legacyMapPage);
        _legacyMapQuestV2 = new LegacyMapQuestV2Controller(_legacyMapPage);
        _legacyMapQuestSidebarV2 = new LegacyMapQuestSidebarV2(_legacyMapPage);
        _legacyMapQuestSidebarPolish = new LegacyMapQuestSidebarPolishBridge(_legacyMapPage);

        MapHost.Content = _legacyMapPage;
        RefreshLegacyQuestMapProjection();

        if (IsMapSmokeEnabled())
            _legacyMapPage.Loaded += MapSmoke_PageLoaded;
    }

    private void RefreshLegacyQuestMapProjection()
    {
        if (_activeProfile is null || _activeContent is null || _questWorkspace is null)
        {
            JunhyunMapQuestProjectionV2.Set(string.Empty, []);
            return;
        }

        var mapKey = TarkovHelper.Services.Map.MapTrackerService.Instance.CurrentMapKey;
        if (string.IsNullOrWhiteSpace(mapKey))
        {
            JunhyunMapQuestProjectionV2.Set(string.Empty, []);
            return;
        }

        var markers = _questWorkspace.Quests
            .Where(entry => entry.Availability.State == Core.Quests.QuestAvailabilityState.Current)
            .Where(entry => string.Equals(
                entry.Quest.MapId,
                mapKey,
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(entry => BuildQuestMapProjection(entry, mapKey))
            .ToArray();

        JunhyunMapQuestProjectionV2.Set(mapKey, markers);
    }

    private IEnumerable<JunhyunMapQuestProjectionV2> BuildQuestMapProjection(
        Core.Quests.QuestCatalogEntry entry,
        string mapKey)
    {
        if (_activeContent is null)
            yield break;

        var objectives = _activeContent.QuestObjectives
            .Where(objective => objective.QuestId == entry.Quest.Id)
            .ToArray();
        var questName = DisplayName(entry.Quest.NameKo, entry.Quest.NameEn, entry.Quest.Id);
        var letter = 'A';

        foreach (var objective in objectives)
        {
            foreach (var location in objective.Locations)
            {
                if (!string.Equals(location.MapId, mapKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                yield return new JunhyunMapQuestProjectionV2(
                    entry.Quest.Id,
                    questName,
                    objective.Id,
                    DisplayName(objective.DescriptionKo, objective.DescriptionEn, objective.Id),
                    letter.ToString(),
                    location.X,
                    location.Y,
                    location.FloorId);
                letter++;
            }
        }
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean)
            ? korean
            : !string.IsNullOrWhiteSpace(english)
                ? english
                : fallback;

    private static bool IsMapSmokeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(MapSmokeEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    private async void MapSmoke_PageLoaded(object sender, RoutedEventArgs e)
    {
        if (_legacyMapPage is not { } page)
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
            new JunhyunMapQuestProjectionV2(
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
            throw new InvalidOperationException("MiniMap still contains legacy resize grip geometry.");

        if (window.FindName("MapCanvas") is not Canvas mapCanvas)
            throw new InvalidOperationException("MiniMap MapCanvas was not found.");

        if (mapCanvas.RenderTransform is not TransformGroup group)
            throw new InvalidOperationException("MiniMap transform group was not found.");

        var scale = group.Children.OfType<ScaleTransform>().FirstOrDefault()
            ?? throw new InvalidOperationException("MiniMap scale transform was not found.");
        var beforeZoom = scale.ScaleX;
        var beforeFloor = window.JunhyunSelectedFloorId;
        var beforeMarkerScale = window.JunhyunMarkerScale;

        overlay.ZoomIn();
        await WaitForAsync(() => scale.ScaleX > beforeZoom, TimeSpan.FromSeconds(2));

        overlay.SelectNextFloor();
        await WaitForAsync(
            () => !string.Equals(beforeFloor, window.JunhyunSelectedFloorId, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(3));

        window.ApplyJunhyunMarkerScale(beforeMarkerScale + 0.05);
        await WaitForAsync(
            () => Math.Abs(window.JunhyunMarkerScale - (beforeMarkerScale + 0.05)) < 0.001,
            TimeSpan.FromSeconds(2));

        overlay.HideOverlay();
        await WaitForAsync(() => !overlay.IsOverlayVisible, TimeSpan.FromSeconds(2));
    }

    private static async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return;

            await Task.Delay(50);
        }

        throw new TimeoutException($"Condition was not met within {timeout.TotalSeconds:F0} seconds.");
    }

    protected override void OnClosed(EventArgs e)
    {
        _legacyMapQuestSidebarPolish?.Dispose();
        _legacyMapQuestSidebarPolish = null;
        _legacyMapQuestSidebarV2?.Dispose();
        _legacyMapQuestSidebarV2 = null;
        _legacyMapQuestV2?.Dispose();
        _legacyMapQuestV2 = null;
        _legacyAdditionalMapMarkers?.Dispose();
        _legacyAdditionalMapMarkers = null;
        _legacyMapProductRuntime?.Dispose();
        _legacyMapProductRuntime = null;
        _legacyMapProductAdapter?.Dispose();
        _legacyMapProductAdapter = null;
        _legacyMapPage = null;
        base.OnClosed(e);
    }
}
