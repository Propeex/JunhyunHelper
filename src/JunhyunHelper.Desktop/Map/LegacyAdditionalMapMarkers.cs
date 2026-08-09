using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TarkovHelper.Models;
using TarkovHelper.Services;
using TarkovHelper.Services.Map;
using TarkovHelper.Services.Settings;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Adds only marker types that are present in the exact bundled Tarkov Helper DB
/// but were not exposed by its original MapMarkersManager. The current DB contains
/// RaiderSpawn entries; ScavSpawn and Keys are intentionally not surfaced because
/// their data set is empty.
/// </summary>
public sealed class LegacyAdditionalMapMarkerController : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly MapMarkerDbService _db = MapMarkerDbService.Instance;
    private readonly Canvas _layer;
    private readonly StackPanel? _markerSettingsPanel;
    private readonly ComboBox? _floorSelector;
    private readonly ScaleTransform? _mapScale;
    private readonly DispatcherTimer _scaleTimer;
    private CheckBox? _raiderToggle;
    private bool _disposed;

    public LegacyAdditionalMapMarkerController(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        var mapCanvas = _page.FindName("MapCanvas") as Canvas
            ?? throw new InvalidOperationException("Legacy MapCanvas was not found.");

        _layer = new Canvas { IsHitTestVisible = false, ClipToBounds = false };
        Panel.SetZIndex(_layer, 450);
        mapCanvas.Children.Add(_layer);

        _markerSettingsPanel = _page.FindName("MapMarkersContent") as StackPanel;
        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;
        _mapScale = _page.FindName("MapScale") as ScaleTransform;

        _page.Loaded += Page_Loaded;
        _tracker.MapChanged += Tracker_MapChanged;
        _db.DataRefreshed += Db_DataRefreshed;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;

        _scaleTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(120),
            DispatcherPriority.Background,
            (_, _) => UpdateScale(),
            _page.Dispatcher);
        _scaleTimer.Start();
    }

    public void Refresh() => _ = RefreshAsync();

    private async void Page_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void Tracker_MapChanged(string mapKey) => _page.Dispatcher.BeginInvoke(Refresh);

    private void Db_DataRefreshed(object? sender, EventArgs e) => _page.Dispatcher.BeginInvoke(Refresh);

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(Refresh);

    private async Task RefreshAsync()
    {
        if (_disposed)
            return;

        if (!_db.IsLoaded && !await _db.LoadMarkersAsync())
            return;

        EnsureRaiderToggle();
        RenderRaiders();
    }

    private void EnsureRaiderToggle()
    {
        if (_raiderToggle is not null || _markerSettingsPanel is null)
            return;

        if (!_db.AllMarkers.Any(marker => marker.Type == MarkerType.RaiderSpawn))
            return;

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2, 0, 2),
        };

        _raiderToggle = new CheckBox
        {
            IsChecked = MapSettings.Instance.ShowSpawns,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _raiderToggle.Checked += RaiderToggle_Changed;
        _raiderToggle.Unchecked += RaiderToggle_Changed;
        row.Children.Add(_raiderToggle);

        row.Children.Add(JunhyunAdditionalMarkerVisualFactory.CreateLegendIcon(MarkerType.RaiderSpawn));
        row.Children.Add(new TextBlock
        {
            Text = "레이더",
            FontSize = 11,
            Foreground = _page.TryFindResource("TextSecondaryBrush") as Brush ?? Brushes.LightGray,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
        });

        var divider = _markerSettingsPanel.Children
            .OfType<Border>()
            .FirstOrDefault(border => Math.Abs(border.Height - 1.0) < 0.01);
        var insertIndex = divider is null
            ? _markerSettingsPanel.Children.Count
            : _markerSettingsPanel.Children.IndexOf(divider);
        _markerSettingsPanel.Children.Insert(Math.Max(0, insertIndex), row);
    }

    private void RaiderToggle_Changed(object sender, RoutedEventArgs e)
    {
        MapSettings.Instance.ShowSpawns = _raiderToggle?.IsChecked == true;
        RenderRaiders();
    }

    private void RenderRaiders()
    {
        _layer.Children.Clear();

        var mapKey = _tracker.CurrentMapKey;
        if (string.IsNullOrWhiteSpace(mapKey) || _raiderToggle?.IsChecked != true)
        {
            JunhyunAdditionalMapMarkerProjection.Publish(mapKey, Array.Empty<JunhyunAdditionalMapMarker>());
            return;
        }

        var config = _tracker.GetMapConfig(mapKey);
        if (config is null)
            return;

        var selectedFloor = SelectedFloorId();
        var projections = new List<JunhyunAdditionalMapMarker>();

        foreach (var marker in _db.GetMarkersForMapByType(mapKey, MarkerType.RaiderSpawn))
        {
            if (!FloorMatches(marker.FloorId, selectedFloor))
                continue;

            var (x, y) = config.GameToScreenForPlayer(marker.X, marker.Z);
            var projection = new JunhyunAdditionalMapMarker(
                marker.Id,
                marker.Type,
                marker.NameKo ?? marker.Name,
                x,
                y,
                marker.FloorId,
                config.MarkerScale);
            projections.Add(projection);

            var visual = JunhyunAdditionalMarkerVisualFactory.Create(projection, baseSize: 24);
            Canvas.SetLeft(visual, x);
            Canvas.SetTop(visual, y);
            _layer.Children.Add(visual);
        }

        UpdateScale();
        JunhyunAdditionalMapMarkerProjection.Publish(mapKey, projections);
    }

    private void UpdateScale()
    {
        var zoom = _mapScale?.ScaleX ?? 1.0;
        var inverse = 1.0 / Math.Max(zoom, 0.01);
        foreach (FrameworkElement child in _layer.Children)
        {
            child.RenderTransform = new ScaleTransform(inverse, inverse);
            child.RenderTransformOrigin = new Point(0, 0);
        }
    }

    private string? SelectedFloorId() =>
        (_floorSelector?.SelectedItem as ComboBoxItem)?.Tag as string;

    private static bool FloorMatches(string? markerFloor, string? selectedFloor)
    {
        if (string.IsNullOrWhiteSpace(selectedFloor))
            return true;
        var effective = string.IsNullOrWhiteSpace(markerFloor) ? "main" : markerFloor;
        return string.Equals(effective, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _scaleTimer.Stop();
        _page.Loaded -= Page_Loaded;
        _tracker.MapChanged -= Tracker_MapChanged;
        _db.DataRefreshed -= Db_DataRefreshed;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;
        if (_raiderToggle is not null)
        {
            _raiderToggle.Checked -= RaiderToggle_Changed;
            _raiderToggle.Unchecked -= RaiderToggle_Changed;
        }
        JunhyunAdditionalMapMarkerProjection.Publish(null, Array.Empty<JunhyunAdditionalMapMarker>());
    }
}

public sealed record JunhyunAdditionalMapMarker(
    string Id,
    MarkerType Type,
    string Name,
    double X,
    double Y,
    string? FloorId,
    double MapScale);

public static class JunhyunAdditionalMapMarkerProjection
{
    private static string? _mapKey;
    private static IReadOnlyList<JunhyunAdditionalMapMarker> _markers = Array.Empty<JunhyunAdditionalMapMarker>();

    public static event EventHandler? Changed;
    public static string? MapKey => _mapKey;
    public static IReadOnlyList<JunhyunAdditionalMapMarker> Markers => _markers;

    public static void Publish(string? mapKey, IReadOnlyList<JunhyunAdditionalMapMarker> markers)
    {
        _mapKey = mapKey;
        _markers = markers;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}

public static class JunhyunAdditionalMarkerVisualFactory
{
    public static FrameworkElement Create(JunhyunAdditionalMapMarker marker, double baseSize)
    {
        var size = baseSize * marker.MapScale;
        var root = new Canvas
        {
            Width = 0,
            Height = 0,
            IsHitTestVisible = false,
            Tag = marker,
        };

        var diamond = new Polygon
        {
            Points = new PointCollection
            {
                new(0, -size / 2),
                new(size / 2, 0),
                new(0, size / 2),
                new(-size / 2, 0),
            },
            Fill = new SolidColorBrush(Color.FromRgb(220, 65, 65)),
            Stroke = Brushes.White,
            StrokeThickness = 1.6,
        };
        root.Children.Add(diamond);

        var text = new TextBlock
        {
            Text = "R",
            Width = size,
            Height = size,
            FontSize = size * 0.55,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Canvas.SetLeft(text, -size / 2);
        Canvas.SetTop(text, -size * 0.38);
        root.Children.Add(text);
        root.ToolTip = marker.Name;
        return root;
    }

    public static FrameworkElement CreateLegendIcon(MarkerType type)
    {
        var projection = new JunhyunAdditionalMapMarker("legend", type, "", 0, 0, null, 1.0);
        var canvas = Create(projection, 18);
        return new Grid
        {
            Width = 18,
            Height = 18,
            Margin = new Thickness(4, 0, 0, 0),
            ClipToBounds = false,
            Children = { canvas },
        };
    }
}
