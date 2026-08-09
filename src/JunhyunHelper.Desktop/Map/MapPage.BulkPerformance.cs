using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

internal static class MapBulkPerformanceBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MapPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => ((MapPage)sender).EnsureBulkPerformanceHooks()));
    }
}

public partial class MapPage
{
    private readonly Canvas _bulkMarkerCanvas = new() { IsHitTestVisible = true };
    private bool _bulkHooksInitialized;
    private bool _bulkApplying;
    private bool _bulkMiniRefreshScheduled;
    private bool _showLootContainers;
    private bool _showLooseLoot;

    private static string BulkPreferencePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JunhyunHelper",
        "map-bulk-marker-settings.json");

    internal void EnsureBulkPerformanceHooks()
    {
        if (_bulkHooksInitialized)
            return;
        _bulkHooksInitialized = true;

        // Keep the legacy per-marker path permanently disabled for the two
        // potentially enormous categories. Their desired visibility is tracked
        // separately and rendered by one lightweight DrawingContext layer.
        _settings.MarkerVisibility[MapMarkerKind.LootContainer] = false;
        _settings.MarkerVisibility[MapMarkerKind.LooseLoot] = false;

        _bulkMarkerCanvas.Width = MapSurface.Width;
        _bulkMarkerCanvas.Height = MapSurface.Height;
        Panel.SetZIndex(_bulkMarkerCanvas, 2);
        var markerIndex = MapSurface.Children.IndexOf(MarkerCanvas);
        MapSurface.Children.Insert(Math.Max(0, markerIndex), _bulkMarkerCanvas);

        foreach (var checkBox in FindVisualChildren<CheckBox>(this))
        {
            if (checkBox.Tag is not string tag ||
                tag is not ("LootContainer" or "LooseLoot"))
                continue;

            checkBox.Checked -= MarkerToggle_Changed;
            checkBox.Unchecked -= MarkerToggle_Changed;
            checkBox.Checked += BulkMarkerToggle_Changed;
            checkBox.Unchecked += BulkMarkerToggle_Changed;
        }

        MapComboBox.SelectionChanged += (_, _) => ScheduleBulkRefresh();
        FloorComboBox.SelectionChanged += (_, _) => ScheduleBulkRefresh();
        UserMarkerCanvas.LayoutUpdated += (_, _) => ScheduleBulkMiniMapRefresh();
        _screenshotTracker.PositionDetected += (_, _) =>
            Dispatcher.BeginInvoke(ScheduleBulkMiniMapRefresh, DispatcherPriority.Background);

        LoadBulkPreferences();
        ApplyBulkCheckboxes();
        ScheduleBulkRefresh();
    }

    private async void BulkMarkerToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_bulkApplying || sender is not CheckBox { Tag: string tag } checkBox)
            return;

        var enabled = checkBox.IsChecked == true;
        if (tag == "LootContainer")
            _showLootContainers = enabled;
        else if (tag == "LooseLoot")
            _showLooseLoot = enabled;

        await SaveBulkPreferencesAsync();
        RenderBulkPerformanceLayer();
        RefreshMiniMapWithBulkMarkers();
    }

    private void ScheduleBulkRefresh()
    {
        _ = Dispatcher.BeginInvoke(
            () =>
            {
                _settings.MarkerVisibility[MapMarkerKind.LootContainer] = false;
                _settings.MarkerVisibility[MapMarkerKind.LooseLoot] = false;
                _bulkMarkerCanvas.Width = MapSurface.Width;
                _bulkMarkerCanvas.Height = MapSurface.Height;
                RenderBulkPerformanceLayer();
                RefreshMiniMapWithBulkMarkers();
            },
            DispatcherPriority.ContextIdle);
    }

    private void ScheduleBulkMiniMapRefresh()
    {
        if (_bulkMiniRefreshScheduled)
            return;
        _bulkMiniRefreshScheduled = true;
        _ = Dispatcher.BeginInvoke(
            () =>
            {
                _bulkMiniRefreshScheduled = false;
                RefreshMiniMapWithBulkMarkers();
            },
            DispatcherPriority.Background);
    }

    private void RenderBulkPerformanceLayer()
    {
        _bulkMarkerCanvas.Children.Clear();
        if (_content is null || _currentChoice is null)
            return;

        RenderBulkCategory(MapMarkerKind.LootContainer, _showLootContainers, Brushes.DarkCyan);
        RenderBulkCategory(MapMarkerKind.LooseLoot, _showLooseLoot, Brushes.CadetBlue);
    }

    private void RenderBulkCategory(MapMarkerKind kind, bool enabled, Brush fallbackBrush)
    {
        if (!enabled || _content is null || _currentChoice is null)
            return;

        var points = new List<BulkMapMarkerPoint>();
        foreach (var marker in _content.MapMarkers.Where(marker =>
                     marker.Kind == kind &&
                     _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                     IsBulkMarkerOnCurrentFloor(marker.Position, marker.Top, marker.Bottom)))
        {
            if (!MapCoordinateTransformer.TryWorldToSurface(
                    _currentChoice.Layout,
                    marker.Position,
                    MapSurface.Width,
                    MapSurface.Height,
                    out var point))
                continue;

            points.Add(new BulkMapMarkerPoint(
                point,
                marker.Name,
                BuildMarkerInfo(marker.Kind, marker.Detail)));
        }

        if (points.Count == 0)
            return;

        var layer = new BulkMapMarkerLayer(
            points,
            MapSurface.Width,
            MapSurface.Height,
            _assetCache?.GetMarkerIconPath(kind),
            fallbackBrush,
            markerSize: 18);
        layer.MarkerClicked += (_, marker) => ShowMarkerInfo(marker.Name, marker.Detail);
        layer.MouseRightButtonUp += (_, args) => args.Handled = true;
        _bulkMarkerCanvas.Children.Add(layer);
    }

    private void RefreshMiniMapWithBulkMarkers()
    {
        if (_miniMapWindow?.IsVisible != true ||
            _currentChoice is null ||
            _assetCache is null)
            return;

        var svg = _assetCache.GetRenderedSvgPath(_currentChoice.Layout, _currentFloor?.Id);
        if (svg is null)
            return;

        var markers = BuildMiniMapMarkers().ToList();
        if (_content is not null)
        {
            foreach (var marker in _content.MapMarkers.Where(marker =>
                         _currentChoice.MapIds.Contains(marker.MapId, StringComparer.Ordinal) &&
                         ((marker.Kind == MapMarkerKind.LootContainer && _showLootContainers) ||
                          (marker.Kind == MapMarkerKind.LooseLoot && _showLooseLoot)) &&
                         IsBulkMarkerOnCurrentFloor(marker.Position, marker.Top, marker.Bottom)))
            {
                markers.Add(new MiniMapMarker(
                    marker.Position,
                    marker.Name,
                    marker.Kind,
                    IsQuest: false));
            }
        }

        _miniMapWindow.SetState(
            _currentChoice.Layout,
            svg,
            _currentFloor?.Name,
            markers,
            _settings.ShowPlayerPosition ? _playerPosition : null,
            _playerHeading,
            _trail,
            _settings.ShowTrail);
    }

    private bool IsBulkMarkerOnCurrentFloor(
        MapWorldPosition position,
        double? top,
        double? bottom)
    {
        if (_currentChoice is null || _currentFloor is null || _currentChoice.Layout.Floors.Count <= 1)
            return true;

        var markerTop = top ?? position.Y;
        var markerBottom = bottom ?? position.Y;
        if (markerTop < markerBottom)
            (markerTop, markerBottom) = (markerBottom, markerTop);

        return _currentFloor.Extents.Any(extent =>
            markerTop >= extent.MinHeight &&
            markerBottom < extent.MaxHeight &&
            (extent.Bounds.Count == 0 || extent.Bounds.Any(bounds => bounds.Contains(position.X, position.Z))));
    }

    private void LoadBulkPreferences()
    {
        try
        {
            if (!File.Exists(BulkPreferencePath))
                return;
            var json = File.ReadAllText(BulkPreferencePath);
            var preferences = JsonSerializer.Deserialize<BulkMarkerPreferences>(json);
            if (preferences is null)
                return;
            _showLootContainers = preferences.ShowLootContainers;
            _showLooseLoot = preferences.ShowLooseLoot;
        }
        catch (JsonException)
        {
            _showLootContainers = false;
            _showLooseLoot = false;
        }
        catch (IOException)
        {
            _showLootContainers = false;
            _showLooseLoot = false;
        }
    }

    private void ApplyBulkCheckboxes()
    {
        _bulkApplying = true;
        try
        {
            foreach (var checkBox in FindVisualChildren<CheckBox>(this))
            {
                if (checkBox.Tag is not string tag)
                    continue;
                if (tag == "LootContainer")
                    checkBox.IsChecked = _showLootContainers;
                else if (tag == "LooseLoot")
                    checkBox.IsChecked = _showLooseLoot;
            }
        }
        finally
        {
            _bulkApplying = false;
        }
    }

    private async Task SaveBulkPreferencesAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(BulkPreferencePath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(new BulkMarkerPreferences(
                _showLootContainers,
                _showLooseLoot));
            var temp = BulkPreferencePath + ".tmp";
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, BulkPreferencePath, overwrite: true);
        }
        catch (IOException)
        {
            // UI preference persistence failure is non-fatal.
        }
    }

    private sealed record BulkMarkerPreferences(
        bool ShowLootContainers,
        bool ShowLooseLoot);
}
