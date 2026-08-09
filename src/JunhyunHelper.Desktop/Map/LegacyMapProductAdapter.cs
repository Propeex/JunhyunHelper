using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Product delta applied on top of the exact Tarkov Helper MapPage transplant.
///
/// Map remains an independent subsystem. Quest is the only JunhyunHelper product
/// boundary read here, as explicitly required by the product contract.
/// </summary>
public sealed class LegacyMapProductAdapter : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly Func<GameContentCatalog?> _contentProvider;
    private readonly Func<QuestWorkspace?> _workspaceProvider;
    private readonly LegacyMapQuestSidebar _sidebar;
    private readonly Canvas _questMarkerLayer;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly ComboBox? _floorSelector;
    private bool _disposed;

    public LegacyMapProductAdapter(
        TarkovHelper.Pages.Map.MapPage page,
        LegacyMapQuestSidebar sidebar,
        Func<GameContentCatalog?> contentProvider,
        Func<QuestWorkspace?> workspaceProvider)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _sidebar = sidebar ?? throw new ArgumentNullException(nameof(sidebar));
        _contentProvider = contentProvider ?? throw new ArgumentNullException(nameof(contentProvider));
        _workspaceProvider = workspaceProvider ?? throw new ArgumentNullException(nameof(workspaceProvider));

        ApplyUiDelta();

        var mapCanvas = Find<Canvas>("MapCanvas")
            ?? throw new InvalidOperationException("Legacy MapCanvas was not found.");
        _questMarkerLayer = new Canvas
        {
            IsHitTestVisible = true,
            ClipToBounds = false,
        };
        Panel.SetZIndex(_questMarkerLayer, 500);
        mapCanvas.Children.Add(_questMarkerLayer);

        _floorSelector = Find<ComboBox>("CmbFloorSelect");
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;

        _tracker.MapChanged += Tracker_MapChanged;
        _sidebar.RefreshRequested += Sidebar_RefreshRequested;
        _page.Loaded += Page_Loaded;
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
            _sidebar.SetState(mapKey, Array.Empty<LegacyMapQuestEntry>());
            _questMarkerLayer.Children.Clear();
            JunhyunMapQuestProjection.Publish(mapKey, Array.Empty<JunhyunQuestMarkerProjection>());
            return;
        }

        var entries = BuildEntries(content, workspace, mapKey);
        _sidebar.SetState(mapKey, entries);
        RenderQuestMarkers(entries, mapKey);
    }

    private void ApplyUiDelta()
    {
        Collapse("BtnFullScreen");
        Collapse("BtnExitFullScreen");
        Collapse("ChkShowExtractMarkers");
        Collapse("ChkFixedView");
        Collapse("BtnMinimapHelp");

        // The original Tarkov Helper quest drawer is tied to its own legacy quest DB.
        // Disable it completely; JunhyunHelper's current Quest workspace is projected
        // through the dedicated sidebar created by the host.
        Collapse("QuestDrawerPanel");
        if (_page.FindName("QuestDrawerColumn") is ColumnDefinition questDrawerColumn)
        {
            questDrawerColumn.MinWidth = 0;
            questDrawerColumn.Width = new GridLength(0);
        }

        // Prevent the legacy quest DB marker manager from drawing stale Quest markers.
        Collapse("QuestMarkersContainer");

        MergeExtractFiltersIntoMapMarkers();
    }

    private void MergeExtractFiltersIntoMapMarkers()
    {
        var markerContent = Find<StackPanel>("MapMarkersContent");
        if (markerContent is null)
            return;

        Collapse("TxtExtractSettingsLabel");

        var divider = new Border
        {
            Height = 1,
            Background = BrushFromResource("BorderBrush", Brushes.DimGray),
            Margin = new Thickness(0, 7, 0, 7),
        };
        markerContent.Children.Add(divider);

        MoveExistingCheckBox("ChkShowPmcExtracts", markerContent);
        MoveExistingCheckBox("ChkShowScavExtracts", markerContent);
        MoveExistingCheckBox("ChkShowTransitExtracts", markerContent);
    }

    private void MoveExistingCheckBox(string name, Panel destination)
    {
        var checkBox = Find<CheckBox>(name);
        if (checkBox is null || ReferenceEquals(checkBox.Parent, destination))
            return;

        if (checkBox.Parent is Panel parent)
            parent.Children.Remove(checkBox);
        else if (checkBox.Parent is Decorator decorator)
            decorator.Child = null;
        else
            return;

        checkBox.Margin = new Thickness(0, 3, 0, 3);
        destination.Children.Add(checkBox);
    }

    private IReadOnlyList<LegacyMapQuestEntry> BuildEntries(
        GameContentCatalog content,
        QuestWorkspace workspace,
        string mapKey)
    {
        var mapReferences = content.Maps.ToDictionary(map => map.Id, StringComparer.Ordinal);
        var objectivesByQuest = content.QuestObjectives
            .GroupBy(objective => objective.QuestId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var result = new List<LegacyMapQuestEntry>();

        foreach (var catalogEntry in workspace.Quests)
        {
            if (catalogEntry.Availability.State != QuestAvailabilityState.Current)
                continue;

            var quest = catalogEntry.Quest;
            objectivesByQuest.TryGetValue(quest.Id, out var objectives);
            objectives ??= Array.Empty<QuestObjective>();

            var questMapMatches = MapIdMatches(content, mapReferences, quest.MapId, mapKey);
            var objectiveMapMatches = objectives.Any(objective =>
                objective.MapIds.Any(mapId => MapIdMatches(content, mapReferences, mapId, mapKey)));

            if (!questMapMatches && !objectiveMapMatches)
                continue;

            var markers = objectives
                .SelectMany(objective => objective.MapLocations.Select(location => (objective, location)))
                .Where(pair => MapIdMatches(content, mapReferences, pair.location.MapId, mapKey))
                .Select(pair => CreateProjection(quest, pair.objective, pair.location, mapKey))
                .Where(static marker => marker is not null)
                .Cast<JunhyunQuestMarkerProjection>()
                .ToArray();

            result.Add(new LegacyMapQuestEntry(
                quest.Id,
                DisplayName(quest.NameKo, quest.NameEn, quest.Id),
                markers.Length,
                markers));
        }

        return result
            .OrderBy(entry => entry.Name, StringComparer.CurrentCulture)
            .ToArray();
    }

    private JunhyunQuestMarkerProjection? CreateProjection(
        Core.Quests.QuestDefinition quest,
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

        var detectedFloor = FloorDetectionService.Instance.DetectFloor(
            mapKey,
            location.Position.X,
            location.Position.Y,
            location.Position.Z);

        var objectiveName = DisplayName(
            objective.DescriptionKo,
            objective.DescriptionEn,
            objective.Type);

        return new JunhyunQuestMarkerProjection(
            quest.Id,
            DisplayName(quest.NameKo, quest.NameEn, quest.Id),
            objective.ObjectiveId,
            objectiveName,
            transformed.X,
            transformed.Y,
            detectedFloor);
    }

    private void RenderQuestMarkers(
        IReadOnlyList<LegacyMapQuestEntry> entries,
        string mapKey)
    {
        _questMarkerLayer.Children.Clear();
        var selectedFloor = SelectedFloorId();
        var visible = new List<JunhyunQuestMarkerProjection>();

        foreach (var marker in entries.SelectMany(entry => entry.Markers))
        {
            if (!FloorMatches(marker.FloorId, selectedFloor))
                continue;

            var visual = JunhyunQuestMarkerVisualFactory.Create(
                marker,
                MapSettings.Instance.QuestMarkerSize,
                MapSettings.Instance.QuestNameSize);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            _questMarkerLayer.Children.Add(visual);
            visible.Add(marker);
        }

        JunhyunMapQuestProjection.Publish(mapKey, visible);
    }

    private string? SelectedFloorId()
    {
        if (_floorSelector?.Visibility != Visibility.Visible)
            return null;

        return (_floorSelector.SelectedItem as ComboBoxItem)?.Tag as string;
    }

    private static bool FloorMatches(string? markerFloor, string? selectedFloor)
    {
        if (string.IsNullOrWhiteSpace(selectedFloor) || string.IsNullOrWhiteSpace(markerFloor))
            return true;

        return string.Equals(markerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    private bool MapIdMatches(
        GameContentCatalog content,
        IReadOnlyDictionary<string, Core.Reference.MapReference> references,
        string? mapId,
        string legacyMapKey)
    {
        if (string.IsNullOrWhiteSpace(mapId))
            return false;

        if (references.TryGetValue(mapId, out var map))
        {
            foreach (var candidate in new[] { map.NameEn, map.NameKo, map.Id })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                var resolved = _tracker.ResolveMapKey(candidate);
                if (string.Equals(resolved, legacyMapKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        var direct = _tracker.ResolveMapKey(mapId);
        return string.Equals(direct, legacyMapKey, StringComparison.OrdinalIgnoreCase);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => Refresh();

    private void Tracker_MapChanged(string mapKey) =>
        _page.Dispatcher.BeginInvoke(Refresh);

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(Refresh);

    private void Sidebar_RefreshRequested(object? sender, EventArgs e) => Refresh();

    private T? Find<T>(string name) where T : class => _page.FindName(name) as T;

    private void Collapse(string name)
    {
        if (_page.FindName(name) is FrameworkElement element)
        {
            element.Visibility = Visibility.Collapsed;
            element.IsEnabled = false;
        }
    }

    private Brush BrushFromResource(string key, Brush fallback) =>
        _page.TryFindResource(key) as Brush ?? fallback;

    private static string DisplayName(string? ko, string? en, string fallback) =>
        !string.IsNullOrWhiteSpace(ko) ? ko :
        !string.IsNullOrWhiteSpace(en) ? en : fallback;

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        _sidebar.RefreshRequested -= Sidebar_RefreshRequested;
        _page.Loaded -= Page_Loaded;
        JunhyunMapQuestProjection.Publish(null, Array.Empty<JunhyunQuestMarkerProjection>());
    }
}

public sealed record LegacyMapQuestEntry(
    string QuestId,
    string Name,
    int MarkerCount,
    IReadOnlyList<JunhyunQuestMarkerProjection> Markers);

public sealed record JunhyunQuestMarkerProjection(
    string QuestId,
    string QuestName,
    string ObjectiveId,
    string ObjectiveName,
    double X,
    double Y,
    string? FloorId);

public static class JunhyunMapQuestProjection
{
    private static string? _mapKey;
    private static IReadOnlyList<JunhyunQuestMarkerProjection> _markers = Array.Empty<JunhyunQuestMarkerProjection>();

    public static event EventHandler? Changed;

    public static string? MapKey => _mapKey;
    public static IReadOnlyList<JunhyunQuestMarkerProjection> Markers => _markers;

    public static void Publish(string? mapKey, IReadOnlyList<JunhyunQuestMarkerProjection> markers)
    {
        _mapKey = mapKey;
        _markers = markers;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}

public static class JunhyunQuestMarkerVisualFactory
{
    public static FrameworkElement Create(
        JunhyunQuestMarkerProjection marker,
        double markerSize,
        double labelSize)
    {
        markerSize = Math.Clamp(markerSize, 12, 32);
        labelSize = Math.Clamp(labelSize, 10, 32);

        var root = new Grid
        {
            Width = 0,
            Height = 0,
            ToolTip = $"{marker.QuestName}\n{marker.ObjectiveName}",
        };

        var badge = new Border
        {
            Width = markerSize,
            Height = markerSize,
            CornerRadius = new CornerRadius(markerSize / 2),
            Background = new SolidColorBrush(Color.FromArgb(225, 197, 168, 74)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
            RenderTransform = new TranslateTransform(-markerSize / 2, -markerSize / 2),
            Child = new TextBlock
            {
                Text = "Q",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = Math.Max(9, markerSize * 0.55),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            },
        };
        root.Children.Add(badge);

        var label = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 2, 4, 2),
            RenderTransform = new TranslateTransform(markerSize / 2 + 4, -labelSize * 0.8),
            Child = new TextBlock
            {
                Text = marker.QuestName,
                Foreground = Brushes.White,
                FontSize = labelSize,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.NoWrap,
            },
        };
        root.Children.Add(label);

        return root;
    }
}

public sealed class LegacyMapQuestSidebar : Border
{
    private readonly TextBlock _title;
    private readonly TextBlock _summary;
    private readonly StackPanel _items;

    public event EventHandler? RefreshRequested;

    public LegacyMapQuestSidebar()
    {
        Width = 300;
        Background = new SolidColorBrush(Color.FromRgb(31, 31, 31));
        BorderBrush = new SolidColorBrush(Color.FromRgb(69, 69, 69));
        BorderThickness = new Thickness(0, 0, 1, 0);
        Padding = new Thickness(12);

        _title = new TextBlock
        {
            Text = "진행 중 퀘스트",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
        };
        _summary = new TextBlock
        {
            Margin = new Thickness(0, 4, 0, 10),
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            FontSize = 11,
        };
        _items = new StackPanel();

        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_summary);
        stack.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _items,
        });
        Child = stack;
    }

    public void SetState(string? mapKey, IReadOnlyList<LegacyMapQuestEntry> entries)
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
        {
            var markerText = entry.MarkerCount > 0
                ? $"좌표 {entry.MarkerCount}개"
                : "정확한 좌표 없음";

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = entry.Name,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });
            panel.Children.Add(new TextBlock
            {
                Text = markerText,
                Foreground = entry.MarkerCount > 0
                    ? new SolidColorBrush(Color.FromRgb(197, 168, 74))
                    : new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                FontSize = 10,
                Margin = new Thickness(0, 3, 0, 0),
            });

            _items.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 63)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(9),
                Margin = new Thickness(0, 0, 0, 7),
                Child = panel,
            });
        }
    }

    public void RequestRefresh() => RefreshRequested?.Invoke(this, EventArgs.Empty);
}
