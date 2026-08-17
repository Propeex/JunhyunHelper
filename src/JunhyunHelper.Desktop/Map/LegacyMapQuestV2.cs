using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Quests;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

public sealed record LegacyMapQuestEntryV2(
    string QuestId,
    string Name,
    IReadOnlyList<JunhyunQuestMarkerProjectionV2> Markers,
    bool MarkerEnabled,
    string? MarkerCode);

public sealed record JunhyunQuestMarkerProjectionV2(
    string QuestId,
    string QuestName,
    string ObjectiveId,
    string ObjectiveName,
    string MarkerCode,
    double X,
    double Y,
    string? FloorId);

public sealed class QuestSidebarQuestEventArgs(string questId) : EventArgs
{
    public string QuestId { get; } = questId;
}

public sealed class QuestSidebarMarkerEventArgs(string questId, bool enabled) : EventArgs
{
    public string QuestId { get; } = questId;
    public bool Enabled { get; } = enabled;
}

/// <summary>
/// Current-Quest projection owned by JunhyunHelper. Each enabled Quest receives a
/// visible A/B/C identity shared by Main Map and MiniMap.
/// </summary>
public sealed class LegacyMapQuestV2Controller : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly LegacyMapQuestSidebarV2 _sidebar;
    private readonly Func<GameContentCatalog?> _contentProvider;
    private readonly Func<QuestWorkspace?> _workspaceProvider;
    private readonly Action<string> _openQuest;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly JunhyunMapProductSettingsStore _settingsStore = JunhyunMapProductSettingsStore.Instance;
    private readonly Canvas _layer;
    private readonly ScaleTransform? _mapScale;
    private readonly ComboBox? _floorSelector;
    private readonly CheckBox? _globalToggle;
    private readonly DispatcherTimer _scaleTimer;
    private readonly Dictionary<string, bool> _questMarkerEnabled = new(StringComparer.Ordinal);
    private bool _disposed;

    public LegacyMapQuestV2Controller(
        TarkovHelper.Pages.Map.MapPage page,
        LegacyMapQuestSidebarV2 sidebar,
        Func<GameContentCatalog?> contentProvider,
        Func<QuestWorkspace?> workspaceProvider,
        Action<string> openQuest)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _sidebar = sidebar ?? throw new ArgumentNullException(nameof(sidebar));
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _workspaceProvider = workspaceProvider ?? throw new ArgumentNullException(nameof(workspaceProvider));
        _openQuest = openQuest ?? throw new ArgumentNullException(nameof(openQuest));

        var mapCanvas = _page.FindName("MapCanvas") as Canvas
            ?? throw new InvalidOperationException("Legacy MapCanvas was not found.");
        _layer = new Canvas { IsHitTestVisible = false, ClipToBounds = false };
        Panel.SetZIndex(_layer, 520);
        mapCanvas.Children.Add(_layer);

        _mapScale = _page.FindName("MapScale") as ScaleTransform;
        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;
        _globalToggle = _page.FindName("ChkShowQuestMarkers") as CheckBox;

        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;
        if (_globalToggle is not null)
        {
            _globalToggle.Content = "퀘스트 마커 표시";
            _globalToggle.Checked += GlobalToggle_Changed;
            _globalToggle.Unchecked += GlobalToggle_Changed;
        }

        _tracker.MapChanged += Tracker_MapChanged;
        _sidebar.QuestRequested += Sidebar_QuestRequested;
        _sidebar.MarkerVisibilityChanged += Sidebar_MarkerVisibilityChanged;
        _page.Loaded += Page_Loaded;

        _scaleTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(120),
            DispatcherPriority.Background,
            (_, _) => UpdateMarkerScale(),
            _page.Dispatcher);
        _scaleTimer.Start();
    }

    public void Refresh()
    {
        if (_disposed)
            return;

        var content = _contentProvider();
        var workspace = _workspaceProvider();
        var mapKey = _tracker.CurrentMapKey;
        if (content is null || workspace is null || string.IsNullOrWhiteSpace(mapKey))
        {
            _sidebar.SetState(mapKey, Array.Empty<LegacyMapQuestEntryV2>());
            _layer.Children.Clear();
            PublishEmpty(mapKey);
            return;
        }

        var rawEntries = BuildEntries(content, workspace, mapKey);
        foreach (var entry in rawEntries.Where(entry => entry.Markers.Count > 0))
        {
            if (!_questMarkerEnabled.ContainsKey(entry.QuestId))
            {
                _questMarkerEnabled[entry.QuestId] =
                    _settingsStore.GetQuestMarkerEnabled(entry.QuestId) ?? true;
            }
        }

        var codeByQuest = rawEntries
            .Where(entry => entry.Markers.Count > 0 && IsQuestMarkerEnabled(entry.QuestId))
            .Select((entry, index) => (entry.QuestId, Code: MarkerCode(index)))
            .ToDictionary(pair => pair.QuestId, pair => pair.Code, StringComparer.Ordinal);

        var entries = rawEntries
            .Select(entry => new LegacyMapQuestEntryV2(
                entry.QuestId,
                entry.Name,
                entry.Markers,
                entry.Markers.Count > 0 && IsQuestMarkerEnabled(entry.QuestId),
                codeByQuest.GetValueOrDefault(entry.QuestId)))
            .ToArray();

        _sidebar.SetState(mapKey, entries);
        Render(entries, mapKey);
    }

    private IReadOnlyList<LegacyMapQuestEntryV2> BuildEntries(
        GameContentCatalog content,
        QuestWorkspace workspace,
        string mapKey)
    {
        var maps = content.Maps.ToDictionary(map => map.Id, StringComparer.Ordinal);
        var objectivesByQuest = content.QuestObjectives
            .GroupBy(objective => objective.QuestId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var result = new List<LegacyMapQuestEntryV2>();

        foreach (var catalogEntry in workspace.Quests)
        {
            if (catalogEntry.Availability.State != QuestAvailabilityState.Current)
                continue;

            var quest = catalogEntry.Quest;
            objectivesByQuest.TryGetValue(quest.Id, out var objectives);
            objectives ??= Array.Empty<QuestObjective>();

            var questMapMatches = MapIdMatches(maps, quest.MapId, mapKey);
            var objectiveMapMatches = objectives.Any(objective =>
                objective.MapIds.Any(mapId => MapIdMatches(maps, mapId, mapKey)));
            if (!questMapMatches && !objectiveMapMatches)
                continue;

            var markers = objectives
                .SelectMany(objective => objective.MapLocations.Select(location => (objective, location)))
                .Where(pair => MapIdMatches(maps, pair.location.MapId, mapKey))
                .Select(pair => CreateProjection(quest, pair.objective, pair.location, mapKey))
                .Where(static marker => marker is not null)
                .Cast<JunhyunQuestMarkerProjectionV2>()
                .ToArray();

            result.Add(new LegacyMapQuestEntryV2(
                quest.Id,
                DisplayName(quest.NameKo, quest.NameEn, quest.Id),
                markers,
                false,
                null));
        }

        return result.OrderBy(entry => entry.Name, StringComparer.CurrentCulture).ToArray();
    }

    private JunhyunQuestMarkerProjectionV2? CreateProjection(
        QuestDefinition quest,
        QuestObjective objective,
        QuestMapLocation location,
        string mapKey)
    {
        var transformed = _tracker.TransformGameCoordinate(
            mapKey,
            location.Position.X,
            location.Position.Z);
        if (transformed is null)
            return null;

        string? floorId = null;
        if (location.Position.Height.HasValue)
        {
            floorId = FloorDetectionService.Instance.DetectFloor(
                mapKey,
                location.Position.X,
                location.Position.Height.Value,
                location.Position.Z);
        }

        return new JunhyunQuestMarkerProjectionV2(
            quest.Id,
            DisplayName(quest.NameKo, quest.NameEn, quest.Id),
            objective.ObjectiveId,
            DisplayName(objective.DescriptionKo, objective.DescriptionEn, objective.Type),
            string.Empty,
            transformed.X,
            transformed.Y,
            floorId);
    }

    private void Render(IReadOnlyList<LegacyMapQuestEntryV2> entries, string mapKey)
    {
        _layer.Children.Clear();
        var miniMapMarkers = new List<JunhyunQuestMarkerProjectionV2>();
        var selectedFloor = SelectedFloorId();

        if (_globalToggle?.IsChecked != false)
        {
            foreach (var entry in entries)
            {
                if (!entry.MarkerEnabled || string.IsNullOrWhiteSpace(entry.MarkerCode))
                    continue;

                foreach (var marker in entry.Markers)
                {
                    var coded = marker with { MarkerCode = entry.MarkerCode };
                    miniMapMarkers.Add(coded);
                    if (!FloorMatches(coded.FloorId, selectedFloor))
                        continue;

                    var visual = JunhyunQuestMarkerVisualFactoryV2.Create(coded);
                    Canvas.SetLeft(visual, coded.X);
                    Canvas.SetTop(visual, coded.Y);
                    _layer.Children.Add(visual);
                }
            }
        }

        UpdateMarkerScale();
        JunhyunMapQuestProjectionV2.Publish(mapKey, miniMapMarkers);
        JunhyunMapQuestProjection.Publish(mapKey, Array.Empty<JunhyunQuestMarkerProjection>());
    }

    private void UpdateMarkerScale()
    {
        var zoom = _mapScale?.ScaleX ?? 1.0;
        var inverse = 1.0 / Math.Max(zoom, 0.01);
        foreach (FrameworkElement child in _layer.Children)
        {
            child.RenderTransform = new ScaleTransform(inverse, inverse);
            child.RenderTransformOrigin = new Point(0, 0);
        }
    }

    private void PublishEmpty(string? mapKey)
    {
        JunhyunMapQuestProjectionV2.Publish(mapKey, Array.Empty<JunhyunQuestMarkerProjectionV2>());
        JunhyunMapQuestProjection.Publish(mapKey, Array.Empty<JunhyunQuestMarkerProjection>());
    }

    private bool IsQuestMarkerEnabled(string questId) =>
        !_questMarkerEnabled.TryGetValue(questId, out var enabled) || enabled;

    private void Sidebar_QuestRequested(object? sender, QuestSidebarQuestEventArgs e) => _openQuest(e.QuestId);

    private void Sidebar_MarkerVisibilityChanged(object? sender, QuestSidebarMarkerEventArgs e)
    {
        _questMarkerEnabled[e.QuestId] = e.Enabled;
        _settingsStore.SetQuestMarkerEnabled(e.QuestId, e.Enabled);
        Refresh();
    }

    private void GlobalToggle_Changed(object sender, RoutedEventArgs e) => Refresh();
    private void Page_Loaded(object sender, RoutedEventArgs e) => Refresh();
    private void Tracker_MapChanged(string mapKey) => _page.Dispatcher.BeginInvoke(Refresh);
    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(Refresh);

    private string? SelectedFloorId() =>
        (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;

    private static bool FloorMatches(string? markerFloor, string? selectedFloor)
    {
        if (string.IsNullOrWhiteSpace(markerFloor) || string.IsNullOrWhiteSpace(selectedFloor))
            return true;
        return string.Equals(markerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    private bool MapIdMatches(
        IReadOnlyDictionary<string, Core.Reference.MapReference> maps,
        string? mapId,
        string legacyMapKey)
    {
        if (string.IsNullOrWhiteSpace(mapId))
            return false;

        if (maps.TryGetValue(mapId, out var map))
        {
            foreach (var candidate in new[] { map.NameEn, map.NameKo, map.Id })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                if (string.Equals(
                        _tracker.ResolveMapKey(candidate),
                        legacyMapKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return string.Equals(
            _tracker.ResolveMapKey(mapId),
            legacyMapKey,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string MarkerCode(int index)
    {
        index++;
        var chars = new Stack<char>();
        while (index > 0)
        {
            index--;
            chars.Push((char)('A' + index % 26));
            index /= 26;
        }
        return new string(chars.ToArray());
    }

    private static string DisplayName(string? ko, string? en, string fallback) =>
        !string.IsNullOrWhiteSpace(ko) ? ko :
        !string.IsNullOrWhiteSpace(en) ? en : fallback;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _scaleTimer.Stop();
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        if (_globalToggle is not null)
        {
            _globalToggle.Checked -= GlobalToggle_Changed;
            _globalToggle.Unchecked -= GlobalToggle_Changed;
        }
        _sidebar.QuestRequested -= Sidebar_QuestRequested;
        _sidebar.MarkerVisibilityChanged -= Sidebar_MarkerVisibilityChanged;
        _page.Loaded -= Page_Loaded;
        PublishEmpty(null);
    }
}

public static class JunhyunMapQuestProjectionV2
{
    private static string? _mapKey;
    private static IReadOnlyList<JunhyunQuestMarkerProjectionV2> _markers = Array.Empty<JunhyunQuestMarkerProjectionV2>();

    public static event EventHandler? Changed;
    public static string? MapKey => _mapKey;
    public static IReadOnlyList<JunhyunQuestMarkerProjectionV2> Markers => _markers;

    public static void Publish(string? mapKey, IReadOnlyList<JunhyunQuestMarkerProjectionV2> markers)
    {
        _mapKey = mapKey;
        _markers = markers;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}

public static class JunhyunQuestMarkerVisualFactoryV2
{
    private const double MarkerSize = 24;

    public static FrameworkElement Create(JunhyunQuestMarkerProjectionV2 marker)
    {
        var root = new Grid
        {
            Width = 0,
            Height = 0,
            IsHitTestVisible = false,
            ToolTip = $"{marker.MarkerCode} · {marker.QuestName}\n{marker.ObjectiveName}",
        };
        root.Children.Add(new Border
        {
            Width = MarkerSize,
            Height = MarkerSize,
            CornerRadius = new CornerRadius(MarkerSize / 2),
            Background = new SolidColorBrush(Color.FromArgb(235, 197, 168, 74)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
            RenderTransform = new TranslateTransform(-MarkerSize / 2, -MarkerSize / 2),
            Child = new TextBlock
            {
                Text = marker.MarkerCode,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = marker.MarkerCode.Length > 1 ? 9 : 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            },
        });
        return root;
    }
}

/// <summary>
/// Collapsible current-Quest sidebar. It starts collapsed to preserve Map width.
/// </summary>
public sealed class LegacyMapQuestSidebarV2 : Border
{
    public const double CollapsedWidth = 34;
    public const double ExpandedWidth = 300;

    private readonly Grid _root;
    private readonly Grid _expandedContent;
    private readonly Button _toggle;
    private readonly TextBlock _summary;
    private readonly StackPanel _items;
    private bool _expanded;

    public event EventHandler<QuestSidebarQuestEventArgs>? QuestRequested;
    public event EventHandler<QuestSidebarMarkerEventArgs>? MarkerVisibilityChanged;

    public LegacyMapQuestSidebarV2()
    {
        Width = CollapsedWidth;
        Background = new SolidColorBrush(Color.FromRgb(31, 31, 31));
        BorderBrush = new SolidColorBrush(Color.FromRgb(69, 69, 69));
        BorderThickness = new Thickness(0, 0, 1, 0);

        _root = new Grid();
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
        _root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CollapsedWidth) });

        _toggle = new Button
        {
            Content = "▶",
            Width = 28,
            Height = 56,
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "진행 중 퀘스트 펼치기",
            Cursor = Cursors.Hand,
        };
        _toggle.Click += Toggle_Click;
        Grid.SetColumn(_toggle, 1);
        _root.Children.Add(_toggle);

        _summary = new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 10),
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            FontSize = 11,
        };
        _items = new StackPanel();

        _expandedContent = new Grid
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(8, 12, 12, 12),
        };
        _expandedContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _expandedContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _expandedContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _expandedContent.Children.Add(new TextBlock
        {
            Text = "진행 중 퀘스트",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
        });
        Grid.SetRow(_summary, 1);
        _expandedContent.Children.Add(_summary);
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _items,
        };
        Grid.SetRow(scroll, 2);
        _expandedContent.Children.Add(scroll);
        Grid.SetColumn(_expandedContent, 0);
        _root.Children.Add(_expandedContent);
        Child = _root;
    }

    public void SetState(string? mapKey, IReadOnlyList<LegacyMapQuestEntryV2> entries)
    {
        _summary.Text = string.IsNullOrWhiteSpace(mapKey)
            ? "지도를 선택하면 현재 진행 중 퀘스트를 표시합니다."
            : $"{mapKey} · {entries.Count}개";

        _items.Children.Clear();
        if (entries.Count == 0)
        {
            _items.Children.Add(new TextBlock
            {
                Text = "이 지도에서 진행 중인 퀘스트가 없습니다.",
                Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 145)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0),
            });
            return;
        }

        foreach (var entry in entries)
            _items.Children.Add(CreateQuestRow(entry));
    }

    private FrameworkElement CreateQuestRow(LegacyMapQuestEntryV2 entry)
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        if (entry.Markers.Count > 0)
        {
            var markerToggle = new CheckBox
            {
                IsChecked = entry.MarkerEnabled,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0),
                Tag = entry.QuestId,
                ToolTip = "이 퀘스트의 지도 마커 표시",
            };
            markerToggle.Checked += MarkerToggle_Changed;
            markerToggle.Unchecked += MarkerToggle_Changed;
            Grid.SetColumn(markerToggle, 0);
            grid.Children.Add(markerToggle);
        }

        if (!string.IsNullOrWhiteSpace(entry.MarkerCode))
        {
            var badge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(197, 168, 74)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = entry.MarkerCode,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    FontSize = entry.MarkerCode.Length > 1 ? 8 : 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                },
            };
            Grid.SetColumn(badge, 1);
            grid.Children.Add(badge);
        }

        var button = new Button
        {
            Tag = entry.QuestId,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Cursor = Cursors.Hand,
            Content = null,
        };
        button.Click += QuestButton_Click;
        Grid.SetColumn(button, 2);
        grid.Children.Add(button);

        var content = CreateQuestContent(entry);
        content.IsHitTestVisible = false;
        content.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(content, 2);
        grid.Children.Add(content);

        return new Border
        {
            Height = 68,
            MinHeight = 68,
            MaxHeight = 68,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 63)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 7),
            Child = grid,
        };
    }

    private static FrameworkElement CreateQuestContent(LegacyMapQuestEntryV2 entry)
    {
        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
        content.Children.Add(new TextBlock
        {
            Text = entry.Name,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        });
        content.Children.Add(new TextBlock
        {
            Text = entry.Markers.Count > 0 ? $"좌표 {entry.Markers.Count}개" : "정확한 좌표 없음",
            Foreground = entry.Markers.Count > 0
                ? new SolidColorBrush(Color.FromRgb(197, 168, 74))
                : new SolidColorBrush(Color.FromRgb(130, 130, 130)),
            FontSize = 10,
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left,
        });
        return content;
    }

    private void MarkerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is string questId)
        {
            MarkerVisibilityChanged?.Invoke(
                this,
                new QuestSidebarMarkerEventArgs(questId, checkBox.IsChecked == true));
        }
    }

    private void QuestButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string questId)
            QuestRequested?.Invoke(this, new QuestSidebarQuestEventArgs(questId));
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        _expanded = !_expanded;
        Width = _expanded ? ExpandedWidth : CollapsedWidth;
        _expandedContent.Visibility = _expanded ? Visibility.Visible : Visibility.Collapsed;
        _root.ColumnDefinitions[0].Width = _expanded
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);
        _toggle.Content = _expanded ? "◀" : "▶";
        _toggle.ToolTip = _expanded ? "진행 중 퀘스트 접기" : "진행 중 퀘스트 펼치기";
    }
}