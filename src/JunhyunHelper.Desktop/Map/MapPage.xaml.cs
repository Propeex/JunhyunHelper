using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Desktop.Services;
using Microsoft.Win32;

namespace JunhyunHelper.Desktop.Map;

public sealed record MapQuestNavigationRequestedEventArgs(string QuestId);

public partial class MapPage : UserControl, IDisposable
{
    private const double SurfaceWidth = 1600;
    private readonly MapScreenshotTracker _screenshotTracker = new();
    private readonly RaidMapWatcher _raidMapWatcher = new();
    private readonly List<MapWorldPosition> _trail = [];
    private readonly ScaleTransform _scaleTransform = new(1, 1);
    private readonly SemaphoreSlim _initializeGate = new(1, 1);

    private MapAssetCacheService? _assetCache;
    private MapUserDataStore? _userDataStore;
    private GameContentCatalog? _content;
    private QuestWorkspace? _questWorkspace;
    private IReadOnlyList<MapLayoutDefinition> _layouts = [];
    private IReadOnlyList<UserMapMarker> _userMarkers = [];
    private IReadOnlyList<MapQuestRow> _mapQuestRows = [];
    private MapUserSettings _settings = new();
    private MapChoice? _currentChoice;
    private MapFloorDefinition? _currentFloor;
    private MapWorldPosition? _playerPosition;
    private double? _playerHeading;
    private MiniMapWindow? _miniMapWindow;
    private bool _initialized;
    private bool _applyingUi;
    private bool _busy;
    private bool _dragging;
    private Point _dragStart;
    private double _dragHorizontalOffset;
    private double _dragVerticalOffset;
    private double _zoom = 1;
    private bool _disposed;

    public MapPage()
    {
        InitializeComponent();
        MapSurface.LayoutTransform = _scaleTransform;
        _screenshotTracker.PositionDetected += ScreenshotTracker_PositionDetected;
        _screenshotTracker.StatusChanged += ScreenshotTracker_StatusChanged;
        _raidMapWatcher.MapAliasDetected += RaidMapWatcher_MapAliasDetected;
    }

    public event EventHandler<MapQuestNavigationRequestedEventArgs>? QuestNavigationRequested;

    public void SetServices(MapAssetCacheService assetCache, MapUserDataStore userDataStore)
    {
        _assetCache = assetCache ?? throw new ArgumentNullException(nameof(assetCache));
        _userDataStore = userDataStore ?? throw new ArgumentNullException(nameof(userDataStore));
    }

    public async Task SetDataAsync(
        GameContentCatalog content,
        QuestWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _questWorkspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        await EnsureInitializedAsync(cancellationToken);
        if (_assetCache is not null)
        {
            try { _layouts = await _assetCache.LoadActiveAsync(cancellationToken); }
            catch { _layouts = []; }
        }
        PopulateMapChoices();
        RenderCurrentMap();
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        MapComboBox.IsEnabled = !busy;
        FloorComboBox.IsEnabled = !busy;
        MiniMapButton.IsEnabled = !busy;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;
        await _initializeGate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;
            if (_userDataStore is not null)
            {
                _settings = await _userDataStore.LoadSettingsAsync(cancellationToken);
                _userMarkers = await _userDataStore.LoadMarkersAsync(cancellationToken);
            }
            ApplySettingsToCheckboxes();
            StartScreenshotTrackingIfConfigured();
            _ = _raidMapWatcher.StartDefault();
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    private async void MapPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await EnsureInitializedAsync();
            if (_content is not null && _assetCache is not null && _layouts.Count == 0)
            {
                try { _layouts = await _assetCache.LoadActiveAsync(); }
                catch { _layouts = []; }
                PopulateMapChoices();
                RenderCurrentMap();
            }
        }
        catch (Exception exception)
        {
            TrackingStatusText.Text = $"지도 초기화 오류 · {exception.Message}";
        }
    }

    private async void MapPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_userDataStore is not null)
        {
            try { await _userDataStore.SaveSettingsAsync(_settings); }
            catch { }
        }
    }

    private void PopulateMapChoices()
    {
        if (_content is null)
            return;
        var mapById = _content.Maps.ToDictionary(map => map.Id, StringComparer.Ordinal);
        var choices = _layouts
            .GroupBy(layout => layout.NormalizedName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var layouts = group.ToArray();
                var mapIds = layouts.Select(layout => layout.MapId).Distinct(StringComparer.Ordinal).ToArray();
                var reference = mapIds.Select(id => mapById.GetValueOrDefault(id)).FirstOrDefault(map => map is not null);
                var name = PhysicalMapName(group.Key, reference);
                return new MapChoice(group.Key, name, layouts[0], mapIds);
            })
            .OrderBy(choice => MapOrder(choice.NormalizedName))
            .ThenBy(choice => choice.Name, StringComparer.CurrentCulture)
            .ToArray();

        _applyingUi = true;
        MapComboBox.ItemsSource = choices;
        MapComboBox.SelectedItem = choices.FirstOrDefault(choice =>
            string.Equals(choice.NormalizedName, _settings.LastMapId, StringComparison.OrdinalIgnoreCase))
            ?? choices.FirstOrDefault();
        _applyingUi = false;
        _currentChoice = MapComboBox.SelectedItem as MapChoice;
        PopulateFloors();
        NoMapPanel.Visibility = choices.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PopulateFloors()
    {
        var floors = _currentChoice?.Layout.Floors ?? Array.Empty<MapFloorDefinition>();
        _applyingUi = true;
        FloorComboBox.ItemsSource = floors;
        var savedFloor = _currentChoice is null
            ? null
            : _settings.LastFloorByMap.GetValueOrDefault(_currentChoice.NormalizedName);
        FloorComboBox.SelectedItem = floors.FirstOrDefault(floor =>
            string.Equals(floor.Id, savedFloor, StringComparison.Ordinal))
            ?? floors.FirstOrDefault(floor => floor.IsDefault)
            ?? floors.FirstOrDefault();
        _currentFloor = FloorComboBox.SelectedItem as MapFloorDefinition;
        var multiFloor = floors.Count > 1;
        FloorLabel.Visibility = multiFloor ? Visibility.Visible : Visibility.Collapsed;
        FloorComboBox.Visibility = multiFloor ? Visibility.Visible : Visibility.Collapsed;
        _applyingUi = false;
    }

    private void RenderCurrentMap()
    {
        MarkerCanvas.Children.Clear();
        ZoneCanvas.Children.Clear();
        UserMarkerCanvas.Children.Clear();
        PlayerCanvas.Children.Clear();
        TrailCanvas.Children.Clear();
        MarkerInfoPanel.Visibility = Visibility.Collapsed;

        if (_currentChoice is null || _assetCache is null)
        {
            NoMapPanel.Visibility = Visibility.Visible;
            MapQuestList.ItemsSource = Array.Empty<MapQuestRow>();
            return;
        }

        var layout = _currentChoice.Layout;
        var svgPath = _assetCache.GetRenderedSvgPath(layout, _currentFloor?.Id);
        if (string.IsNullOrWhiteSpace(svgPath) || !File.Exists(svgPath))
        {
            NoMapPanel.Visibility = Visibility.Visible;
            return;
        }

        NoMapPanel.Visibility = Visibility.Collapsed;
        MapSurface.Width = SurfaceWidth;
        MapSurface.Height = SurfaceWidth * MapCoordinateTransformer.SurfaceAspectRatio(layout);
        MapSvg.Source = new Uri(svgPath, UriKind.Absolute);
        RenderApiMarkers();
        RenderQuestRowsAndMarkers();
        RenderUserMarkers();
        RenderTrail();
        RenderPlayer();
        UpdateMiniMap();
    }

    private void RenderApiMarkers()
    {
        if (_content is null || _currentChoice is null)
            return;
        var layout = _currentChoice.Layout;
        foreach (var marker in _content.MapMarkers.Where(marker =>
                     _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                     IsMarkerVisible(marker.Kind) &&
                     IsOnCurrentFloor(marker.Top, marker.Bottom, marker.Position.Y)))
        {
            if (!MapCoordinateTransformer.TryWorldToSurface(
                    layout, marker.Position, MapSurface.Width, MapSurface.Height, out var point))
                continue;
            if (marker.Outline.Count > 2)
                AddOutline(marker.Outline, Brushes.OrangeRed, 0.25);

            var visual = MapVisualFactory.CreateMarker(marker.Kind, marker.Name);
            PlaceVisual(MarkerCanvas, visual, point);
            visual.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                ShowMarkerInfo(marker.Name, BuildMarkerInfo(marker.Kind, marker.Detail));
            };
        }
    }

    private void RenderQuestRowsAndMarkers()
    {
        if (_content is null || _questWorkspace is null || _currentChoice is null)
            return;
        var questById = _content.Quests.ToDictionary(quest => quest.Id, StringComparer.Ordinal);
        var objectivesByQuest = _content.QuestObjectives
            .GroupBy(objective => objective.QuestId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        _mapQuestRows = _questWorkspace.Quests
            .Where(entry => entry.Availability.State == QuestAvailabilityState.Current)
            .Where(entry => QuestBelongsToCurrentMap(entry.Quest, objectivesByQuest.GetValueOrDefault(entry.Quest.Id) ?? []))
            .Select(entry =>
            {
                var objectives = objectivesByQuest.GetValueOrDefault(entry.Quest.Id) ?? [];
                var locations = objectives
                    .SelectMany(objective => objective.MapLocations.Select(location => new QuestLocationView(objective, location)))
                    .Where(view => _currentChoice.MapIds.Contains(view.Location.MapId, StringComparer.Ordinal))
                    .ToArray();
                return new MapQuestRow(
                    entry.Quest.Id,
                    DisplayName(entry.Quest.NameKo, entry.Quest.NameEn, entry.Quest.Id),
                    locations.Length == 0 ? "정확한 위치 없음" : $"지도 위치 {locations.Length}개",
                    locations);
            })
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToArray();
        MapQuestList.ItemsSource = _mapQuestRows;

        if (!_settings.ShowQuestMarkers)
            return;

        foreach (var row in _mapQuestRows)
        {
            foreach (var view in row.Locations)
            {
                var location = view.Location;
                if (!IsOnCurrentFloor(location.Top, location.Bottom, location.Position.Y))
                    continue;
                if (!MapCoordinateTransformer.TryWorldToSurface(
                        _currentChoice.Layout,
                        location.Position,
                        MapSurface.Width,
                        MapSurface.Height,
                        out var point))
                    continue;

                if (location.Outline.Count > 2)
                    AddOutline(location.Outline, Brushes.Gold, 0.2);
                var objectiveText = DisplayName(
                    view.Objective.DescriptionKo,
                    view.Objective.DescriptionEn,
                    view.Objective.Type);
                var visual = MapVisualFactory.CreateQuestMarker($"{row.Name}\n{objectiveText}");
                PlaceVisual(MarkerCanvas, visual, point);
                visual.MouseLeftButtonUp += (_, e) =>
                {
                    e.Handled = true;
                    MapQuestList.SelectedItem = row;
                    ShowMarkerInfo(row.Name, objectiveText);
                };
            }
        }
    }

    private bool QuestBelongsToCurrentMap(QuestDefinition quest, IReadOnlyList<QuestObjective> objectives)
    {
        if (_currentChoice is null)
            return false;
        if (quest.MapId is not null && _currentChoice.MapIds.Contains(quest.MapId, StringComparer.Ordinal))
            return true;
        return objectives.Any(objective =>
            objective.MapIds.Any(id => _currentChoice.MapIds.Contains(id, StringComparer.Ordinal)) ||
            objective.MapLocations.Any(location => _currentChoice.MapIds.Contains(location.MapId, StringComparer.Ordinal)));
    }

    private void RenderUserMarkers()
    {
        if (!_settings.ShowUserMarkers || _currentChoice is null)
            return;
        foreach (var marker in _userMarkers.Where(marker =>
                     _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                     (string.IsNullOrWhiteSpace(marker.FloorId) ||
                      string.Equals(marker.FloorId, _currentFloor?.Id, StringComparison.Ordinal))))
        {
            if (!MapCoordinateTransformer.TryWorldToSurface(
                    _currentChoice.Layout,
                    marker.Position,
                    MapSurface.Width,
                    MapSurface.Height,
                    out var point))
                continue;
            var visual = MapVisualFactory.CreateUserMarker(marker.Color, marker.Name);
            PlaceVisual(UserMarkerCanvas, visual, point);
            visual.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                ShowMarkerInfo(marker.Name, "사용자 마커");
            };
            var menu = new ContextMenu();
            var edit = new MenuItem { Header = "마커 수정" };
            edit.Click += async (_, _) => await EditUserMarkerAsync(marker);
            var delete = new MenuItem { Header = "마커 삭제" };
            delete.Click += async (_, _) => await DeleteUserMarkerAsync(marker);
            menu.Items.Add(edit);
            menu.Items.Add(delete);
            visual.ContextMenu = menu;
            visual.MouseRightButtonUp += (_, e) => e.Handled = true;
        }
    }

    private void RenderPlayer()
    {
        if (!_settings.ShowPlayerPosition || _currentChoice is null || _playerPosition is null)
            return;
        if (!MapCoordinateTransformer.TryWorldToSurface(
                _currentChoice.Layout,
                _playerPosition,
                MapSurface.Width,
                MapSurface.Height,
                out var point))
            return;
        var heading = _playerHeading is null
            ? 0
            : MapCoordinateTransformer.SurfaceHeading(_currentChoice.Layout, _playerHeading.Value);
        var visual = MapVisualFactory.CreatePlayerMarker(heading);
        PlaceVisual(PlayerCanvas, visual, point);
    }

    private void RenderTrail()
    {
        TrailCanvas.Children.Clear();
        if (!_settings.ShowTrail || _currentChoice is null || _trail.Count < 2)
            return;
        var points = new PointCollection();
        foreach (var world in _trail)
        {
            if (MapCoordinateTransformer.TryWorldToSurface(
                    _currentChoice.Layout,
                    world,
                    MapSurface.Width,
                    MapSurface.Height,
                    out var point))
                points.Add(point);
        }
        if (points.Count > 1)
        {
            TrailCanvas.Children.Add(new Polyline
            {
                Points = points,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 3,
                Opacity = 0.72,
            });
        }
    }

    private void AddOutline(IReadOnlyList<MapOutlinePoint> outline, Brush brush, double opacity)
    {
        if (_currentChoice is null)
            return;
        var points = new PointCollection();
        foreach (var point in outline)
        {
            if (MapCoordinateTransformer.TryWorldToSurface(
                    _currentChoice.Layout,
                    new MapWorldPosition(point.X, 0, point.Z),
                    MapSurface.Width,
                    MapSurface.Height,
                    out var screen))
                points.Add(screen);
        }
        if (points.Count > 2)
        {
            ZoneCanvas.Children.Add(new Polygon
            {
                Points = points,
                Stroke = brush,
                Fill = brush,
                StrokeThickness = 2,
                Opacity = opacity,
            });
        }
    }

    private bool IsMarkerVisible(MapMarkerKind kind) =>
        _settings.MarkerVisibility.TryGetValue(kind, out var visible) ? visible : true;

    private bool IsOnCurrentFloor(double? top, double? bottom, double fallbackHeight)
    {
        if (_currentChoice is null || _currentFloor is null || _currentChoice.Layout.Floors.Count <= 1)
            return true;
        var markerTop = top ?? fallbackHeight;
        var markerBottom = bottom ?? fallbackHeight;
        if (markerTop < markerBottom)
            (markerTop, markerBottom) = (markerBottom, markerTop);
        return markerTop >= _currentFloor.MinHeight && markerBottom < _currentFloor.MaxHeight;
    }

    private static void PlaceVisual(Canvas canvas, FrameworkElement visual, Point point)
    {
        Canvas.SetLeft(visual, point.X - visual.Width / 2);
        Canvas.SetTop(visual, point.Y - visual.Height / 2);
        canvas.Children.Add(visual);
    }

    private void ShowMarkerInfo(string title, string detail)
    {
        MarkerInfoTitle.Text = title;
        MarkerInfoText.Text = detail;
        MarkerInfoPanel.Visibility = Visibility.Visible;
    }

    private static string BuildMarkerInfo(MapMarkerKind kind, string? detail)
    {
        var label = kind switch
        {
            MapMarkerKind.PmcExtract => "PMC 탈출구",
            MapMarkerKind.ScavExtract => "Scav 탈출구",
            MapMarkerKind.SharedExtract => "공용 탈출구",
            MapMarkerKind.Transit => "Transit",
            MapMarkerKind.PmcSpawn => "PMC 스폰",
            MapMarkerKind.ScavSpawn => "Scav 스폰",
            MapMarkerKind.SniperScav => "저격 Scav",
            MapMarkerKind.Boss => "Boss 스폰",
            MapMarkerKind.SpecialAi => "특수 AI",
            MapMarkerKind.Hazard => "위험 구역",
            MapMarkerKind.Lock => "잠금 지점",
            MapMarkerKind.Switch => "스위치",
            MapMarkerKind.StationaryWeapon => "고정 화기",
            MapMarkerKind.BtrStop => "BTR 정류장",
            MapMarkerKind.LootContainer => "루팅 컨테이너",
            MapMarkerKind.LooseLoot => "루즈 루트",
            _ => kind.ToString(),
        };
        return string.IsNullOrWhiteSpace(detail) ? label : $"{label}\n{detail}";
    }

    private void UpdateMiniMap()
    {
        if (_miniMapWindow?.IsVisible != true || _currentChoice is null || _assetCache is null)
            return;
        var svg = _assetCache.GetRenderedSvgPath(_currentChoice.Layout, _currentFloor?.Id);
        if (svg is null)
            return;
        _miniMapWindow.SetState(
            _currentChoice.Layout,
            svg,
            _currentFloor?.Name,
            BuildMiniMapMarkers(),
            _settings.ShowPlayerPosition ? _playerPosition : null,
            _playerHeading,
            _trail,
            _settings.ShowTrail);
    }

    private IReadOnlyList<MiniMapMarker> BuildMiniMapMarkers()
    {
        if (_content is null || _currentChoice is null)
            return [];
        var result = _content.MapMarkers
            .Where(marker => _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                             IsMarkerVisible(marker.Kind) &&
                             IsOnCurrentFloor(marker.Top, marker.Bottom, marker.Position.Y))
            .Select(marker => new MiniMapMarker(marker.Position, marker.Name, marker.Kind, false))
            .ToList();
        if (_settings.ShowQuestMarkers)
        {
            result.AddRange(_mapQuestRows.SelectMany(row => row.Locations)
                .Where(view => IsOnCurrentFloor(view.Location.Top, view.Location.Bottom, view.Location.Position.Y))
                .Select(view => new MiniMapMarker(view.Location.Position, view.Objective.DescriptionKo ?? view.Objective.DescriptionEn ?? view.Objective.Type, null, true)));
        }
        if (_settings.ShowUserMarkers)
        {
            result.AddRange(_userMarkers.Where(marker =>
                    _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                    (string.IsNullOrWhiteSpace(marker.FloorId) || string.Equals(marker.FloorId, _currentFloor?.Id, StringComparison.Ordinal)))
                .Select(marker => new MiniMapMarker(marker.Position, marker.Name, null, false, marker.Color)));
        }
        return result;
    }

    private async void MapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_applyingUi)
            return;
        _currentChoice = MapComboBox.SelectedItem as MapChoice;
        if (_currentChoice is null)
            return;
        _settings.LastMapId = _currentChoice.NormalizedName;
        PopulateFloors();
        _trail.Clear();
        RenderCurrentMap();
        await SaveSettingsAsync();
    }

    private async void FloorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_applyingUi || _currentChoice is null)
            return;
        _currentFloor = FloorComboBox.SelectedItem as MapFloorDefinition;
        if (_currentFloor is not null)
            _settings.LastFloorByMap[_currentChoice.NormalizedName] = _currentFloor.Id;
        RenderCurrentMap();
        await SaveSettingsAsync();
    }

    private async void MarkerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_applyingUi || sender is not CheckBox { Tag: string tag } checkBox)
            return;
        var enabled = checkBox.IsChecked == true;
        switch (tag)
        {
            case "BossGroup":
                _settings.MarkerVisibility[MapMarkerKind.Boss] = enabled;
                _settings.MarkerVisibility[MapMarkerKind.SpecialAi] = enabled;
                break;
            case "LockSwitch":
                _settings.MarkerVisibility[MapMarkerKind.Lock] = enabled;
                _settings.MarkerVisibility[MapMarkerKind.Switch] = enabled;
                break;
            case "Quest": _settings.ShowQuestMarkers = enabled; break;
            case "User": _settings.ShowUserMarkers = enabled; break;
            case "Player": _settings.ShowPlayerPosition = enabled; break;
            case "Trail":
                _settings.ShowTrail = enabled;
                if (enabled && _playerPosition is not null && _trail.Count == 0)
                    _trail.Add(_playerPosition);
                break;
            default:
                if (Enum.TryParse<MapMarkerKind>(tag, out var kind))
                    _settings.MarkerVisibility[kind] = enabled;
                break;
        }
        RenderCurrentMap();
        await SaveSettingsAsync();
    }

    private void ApplySettingsToCheckboxes()
    {
        _applyingUi = true;
        foreach (var checkBox in FindVisualChildren<CheckBox>(this))
        {
            if (checkBox.Tag is not string tag)
                continue;
            checkBox.IsChecked = tag switch
            {
                "BossGroup" => IsMarkerVisible(MapMarkerKind.Boss) && IsMarkerVisible(MapMarkerKind.SpecialAi),
                "LockSwitch" => IsMarkerVisible(MapMarkerKind.Lock) && IsMarkerVisible(MapMarkerKind.Switch),
                "Quest" => _settings.ShowQuestMarkers,
                "User" => _settings.ShowUserMarkers,
                "Player" => _settings.ShowPlayerPosition,
                "Trail" => _settings.ShowTrail,
                _ when Enum.TryParse<MapMarkerKind>(tag, out var kind) => IsMarkerVisible(kind),
                _ => checkBox.IsChecked,
            };
        }
        _applyingUi = false;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;
            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private async void MapQuestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = MapQuestList.SelectedItem as MapQuestRow;
        OpenQuestButton.IsEnabled = row is not null;
        if (row?.Locations.FirstOrDefault() is { } first)
            FocusWorldPosition(first.Location.Position);
        await Task.CompletedTask;
    }

    private void OpenQuestButton_Click(object sender, RoutedEventArgs e)
    {
        if (MapQuestList.SelectedItem is MapQuestRow row)
            QuestNavigationRequested?.Invoke(this, new MapQuestNavigationRequestedEventArgs(row.QuestId));
    }

    private async void ScreenshotFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Escape from Tarkov 스크린샷 폴더 선택",
            InitialDirectory = _settings.ScreenshotFolderPath,
        };
        if (dialog.ShowDialog() != true)
            return;
        _settings.ScreenshotFolderPath = dialog.FolderName;
        StartScreenshotTrackingIfConfigured();
        await SaveSettingsAsync();
    }

    private async void AutoDetectScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var detected = MapScreenshotTracker.TryDetectScreenshotFolder();
        if (detected is null)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "스크린샷 폴더를 자동으로 찾지 못했습니다. '스크린샷 경로'에서 직접 선택해주세요.",
                "지도 위치 추적",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }
        _settings.ScreenshotFolderPath = detected;
        StartScreenshotTrackingIfConfigured();
        await SaveSettingsAsync();
    }

    private void StartScreenshotTrackingIfConfigured()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ScreenshotFolderPath) &&
            _screenshotTracker.Start(_settings.ScreenshotFolderPath))
        {
            TrackingStatusText.Text = "스크린샷 위치 추적 중";
        }
        else
        {
            TrackingStatusText.Text = "스크린샷 경로를 설정하면 현재 위치를 표시합니다.";
        }
    }

    private void ScreenshotTracker_PositionDetected(object? sender, ScreenshotPositionDetected e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _playerPosition = e.Position;
            _playerHeading = e.HeadingDegrees;
            if (_settings.ShowTrail)
            {
                if (_trail.Count == 0 || DistanceSquared(_trail[^1], e.Position) > 0.25)
                    _trail.Add(e.Position);
                if (_trail.Count > 400)
                    _trail.RemoveAt(0);
            }
            TryAutoSelectFloor(e.Position.Y);
            RenderTrail();
            RenderPlayer();
            UpdateMiniMap();
        });
    }

    private void ScreenshotTracker_StatusChanged(object? sender, string e) =>
        Dispatcher.BeginInvoke(() => TrackingStatusText.Text = e);

    private void RaidMapWatcher_MapAliasDetected(object? sender, string alias) =>
        Dispatcher.BeginInvoke(() => SelectMapByAlias(alias));

    private void SelectMapByAlias(string alias)
    {
        var normalized = NormalizeAlias(alias);
        var choices = (MapComboBox.ItemsSource as IEnumerable<MapChoice>)?.ToArray() ?? [];
        var target = choices.FirstOrDefault(choice =>
            AliasMatchesChoice(normalized, choice));
        if (target is not null && !ReferenceEquals(MapComboBox.SelectedItem, target))
        {
            MapComboBox.SelectedItem = target;
            TrackingStatusText.Text = $"레이드 지도 자동 전환 · {target.Name}";
        }
    }

    private static bool AliasMatchesChoice(string alias, MapChoice choice)
    {
        var target = alias switch
        {
            "bigmap" or "customs" => "customs",
            "woods" or "woodspreset" => "woods",
            "shoreline" or "shorelinepreset" => "shoreline",
            "shoppingmall" or "interchange" => "interchange",
            "rezervbase" or "rezervbasepreset" or "reserve" => "reserve",
            "lighthouse" or "lighthousepreset" => "lighthouse",
            "tarkovstreets" or "streets" or "citypreset" => "streetsoftarkov",
            "factory4day" or "factory4night" or "factorydaypreset" or "factorynightpreset" or "factory" => "factory",
            "sandbox" or "sandboxhigh" or "sandboxstart" or "sandboxpreset" or "sandboxhighpreset" or "groundzero" => "groundzero",
            "laboratory" or "laboratorypreset" or "labs" or "lab" => "thelab",
            "labyrinth" or "labyrinthpreset" => "labyrinth",
            _ => alias,
        };
        var choiceKey = NormalizeAlias(choice.NormalizedName);
        return choiceKey.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Contains(choiceKey, StringComparison.OrdinalIgnoreCase) ||
               choice.MapIds.Any(id => NormalizeAlias(id) == target);
    }

    private void TryAutoSelectFloor(double height)
    {
        if (_currentChoice is null || _currentChoice.Layout.Floors.Count <= 1)
            return;
        var floor = MapCoordinateTransformer.FloorForHeight(_currentChoice.Layout, height);
        if (floor is not null && !string.Equals(floor.Id, _currentFloor?.Id, StringComparison.Ordinal))
            FloorComboBox.SelectedItem = floor;
    }

    private async void MapSurface_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_busy || _currentChoice is null || _userDataStore is null)
            return;
        var point = e.GetPosition(MapSurface);
        var floorHeight = _currentFloor is null || !double.IsFinite(_currentFloor.MinHeight) || !double.IsFinite(_currentFloor.MaxHeight)
            ? 0
            : (_currentFloor.MinHeight + _currentFloor.MaxHeight) / 2;
        if (!MapCoordinateTransformer.TrySurfaceToWorld(
                _currentChoice.Layout,
                point,
                MapSurface.Width,
                MapSurface.Height,
                floorHeight,
                out var world))
            return;

        var editor = new CustomMarkerEditorWindow("새 마커", "#FFD700") { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true)
            return;
        var marker = new UserMapMarker(
            Guid.NewGuid().ToString("N"),
            _currentChoice.Layout.MapId,
            _currentFloor?.Id,
            editor.MarkerName,
            editor.MarkerColor,
            world);
        _userMarkers = _userMarkers.Append(marker).ToArray();
        await _userDataStore.SaveMarkersAsync(_userMarkers);
        RenderCurrentMap();
        e.Handled = true;
    }

    private async Task EditUserMarkerAsync(UserMapMarker marker)
    {
        if (_userDataStore is null)
            return;
        var editor = new CustomMarkerEditorWindow(marker.Name, marker.Color) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true)
            return;
        var updated = marker with { Name = editor.MarkerName, Color = editor.MarkerColor };
        _userMarkers = _userMarkers.Select(candidate => candidate.Id == marker.Id ? updated : candidate).ToArray();
        await _userDataStore.SaveMarkersAsync(_userMarkers);
        RenderCurrentMap();
    }

    private async Task DeleteUserMarkerAsync(UserMapMarker marker)
    {
        if (_userDataStore is null)
            return;
        var decision = MessageBox.Show(
            Window.GetWindow(this),
            $"'{marker.Name}' 마커를 삭제할까요?",
            "사용자 마커",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (decision != MessageBoxResult.Yes)
            return;
        _userMarkers = _userMarkers.Where(candidate => candidate.Id != marker.Id).ToArray();
        await _userDataStore.SaveMarkersAsync(_userMarkers);
        RenderCurrentMap();
    }

    private void MiniMapButton_Click(object sender, RoutedEventArgs e)
    {
        if (_miniMapWindow?.IsVisible == true)
        {
            _miniMapWindow.Hide();
            MiniMapButton.Content = "미니맵 켜기";
            return;
        }
        if (_miniMapWindow is null)
        {
            _miniMapWindow = new MiniMapWindow();
            _miniMapWindow.UserClosed += (_, _) => MiniMapButton.Content = "미니맵 켜기";
        }
        _miniMapWindow.Show();
        MiniMapButton.Content = "미니맵 끄기";
        UpdateMiniMap();
    }

    private void ClearTrailButton_Click(object sender, RoutedEventArgs e)
    {
        _trail.Clear();
        RenderTrail();
        UpdateMiniMap();
    }

    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        _zoom = 1;
        _scaleTransform.ScaleX = _zoom;
        _scaleTransform.ScaleY = _zoom;
        MapScrollViewer.ScrollToHorizontalOffset(0);
        MapScrollViewer.ScrollToVerticalOffset(0);
    }

    private void MapScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15), 0.25, 4);
        if (Math.Abs(oldZoom - _zoom) < 0.001)
            return;
        var centerX = (MapScrollViewer.HorizontalOffset + MapScrollViewer.ViewportWidth / 2) / oldZoom;
        var centerY = (MapScrollViewer.VerticalOffset + MapScrollViewer.ViewportHeight / 2) / oldZoom;
        _scaleTransform.ScaleX = _zoom;
        _scaleTransform.ScaleY = _zoom;
        MapScrollViewer.ScrollToHorizontalOffset(centerX * _zoom - MapScrollViewer.ViewportWidth / 2);
        MapScrollViewer.ScrollToVerticalOffset(centerY * _zoom - MapScrollViewer.ViewportHeight / 2);
        e.Handled = true;
    }

    private void MapScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button or CheckBox or ListBoxItem)
            return;
        _dragging = true;
        _dragStart = e.GetPosition(MapScrollViewer);
        _dragHorizontalOffset = MapScrollViewer.HorizontalOffset;
        _dragVerticalOffset = MapScrollViewer.VerticalOffset;
        MapScrollViewer.CaptureMouse();
        Mouse.OverrideCursor = Cursors.Hand;
        e.Handled = true;
    }

    private void MapScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
            return;
        var current = e.GetPosition(MapScrollViewer);
        MapScrollViewer.ScrollToHorizontalOffset(_dragHorizontalOffset - (current.X - _dragStart.X));
        MapScrollViewer.ScrollToVerticalOffset(_dragVerticalOffset - (current.Y - _dragStart.Y));
    }

    private void MapScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;
        _dragging = false;
        MapScrollViewer.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        e.Handled = true;
    }

    private void FocusWorldPosition(MapWorldPosition position)
    {
        if (_currentChoice is null ||
            !MapCoordinateTransformer.TryWorldToSurface(
                _currentChoice.Layout,
                position,
                MapSurface.Width,
                MapSurface.Height,
                out var point))
            return;
        MapScrollViewer.ScrollToHorizontalOffset(point.X * _zoom - MapScrollViewer.ViewportWidth / 2);
        MapScrollViewer.ScrollToVerticalOffset(point.Y * _zoom - MapScrollViewer.ViewportHeight / 2);
    }

    private void CloseMarkerInfoButton_Click(object sender, RoutedEventArgs e) =>
        MarkerInfoPanel.Visibility = Visibility.Collapsed;

    private async Task SaveSettingsAsync()
    {
        if (_userDataStore is null)
            return;
        try { await _userDataStore.SaveSettingsAsync(_settings); }
        catch { }
    }

    private static double DistanceSquared(MapWorldPosition a, MapWorldPosition b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return dx * dx + dz * dz;
    }

    private static string NormalizeAlias(string value) =>
        new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string PhysicalMapName(string normalizedName, MapReference? map) =>
        NormalizeAlias(normalizedName) switch
        {
            var key when key.Contains("factory", StringComparison.Ordinal) => "Factory",
            var key when key.Contains("groundzero", StringComparison.Ordinal) => "Ground Zero",
            _ => DisplayName(map?.NameKo, map?.NameEn, normalizedName),
        };

    private static int MapOrder(string normalizedName)
    {
        var key = NormalizeAlias(normalizedName);
        var order = new[] { "groundzero", "factory", "customs", "woods", "shoreline", "interchange", "reserve", "lighthouse", "streetsoftarkov", "thelab", "labyrinth" };
        var index = Array.FindIndex(order, item => key.Contains(item, StringComparison.Ordinal));
        return index < 0 ? int.MaxValue : index;
    }

    private static string DisplayName(string? korean, string? english, string fallback) =>
        !string.IsNullOrWhiteSpace(korean) ? korean : !string.IsNullOrWhiteSpace(english) ? english : fallback;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _screenshotTracker.Dispose();
        _raidMapWatcher.Dispose();
        if (_miniMapWindow is not null)
        {
            _miniMapWindow.Close();
            _miniMapWindow = null;
        }
        _initializeGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record MapChoice(
        string NormalizedName,
        string Name,
        MapLayoutDefinition Layout,
        IReadOnlyList<string> MapIds)
    {
        public override string ToString() => Name;
    }

    private sealed record QuestLocationView(QuestObjective Objective, QuestMapLocation Location);

    private sealed record MapQuestRow(
        string QuestId,
        string Name,
        string LocationSummary,
        IReadOnlyList<QuestLocationView> Locations);
}
