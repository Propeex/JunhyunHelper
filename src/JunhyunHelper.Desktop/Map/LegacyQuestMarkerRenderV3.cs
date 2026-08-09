using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Renders JunhyunHelper Quest projections on the exact Tarkov Helper map using the
/// same zero-size Canvas anchor pattern as the proven legacy marker renderers.
/// </summary>
public sealed class LegacyQuestMarkerRenderV3 : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private readonly Canvas _layer;
    private readonly ComboBox? _floorSelector;
    private readonly ScaleTransform? _mapScale;
    private readonly DispatcherTimer _scaleTimer;
    private bool _disposed;

    public LegacyQuestMarkerRenderV3(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        var mapCanvas = _page.FindName("MapCanvas") as Canvas
            ?? throw new InvalidOperationException("Legacy MapCanvas was not found.");

        _layer = new Canvas
        {
            IsHitTestVisible = false,
            ClipToBounds = false,
        };
        Panel.SetZIndex(_layer, 540);
        mapCanvas.Children.Add(_layer);

        _floorSelector = _page.FindName("CmbFloorSelect") as ComboBox;
        _mapScale = _page.FindName("MapScale") as ScaleTransform;

        JunhyunMapQuestProjectionV2.Changed += Projection_Changed;
        _tracker.MapChanged += Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged += FloorSelector_SelectionChanged;

        _scaleTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(120),
            DispatcherPriority.Background,
            (_, _) => UpdateScale(),
            _page.Dispatcher);
        _scaleTimer.Start();

        Render();
    }

    private void Projection_Changed(object? sender, EventArgs e) =>
        _page.Dispatcher.BeginInvoke(Render);

    private void Tracker_MapChanged(string mapKey) =>
        _page.Dispatcher.BeginInvoke(Render);

    private void FloorSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _page.Dispatcher.BeginInvoke(Render);

    private void Render()
    {
        if (_disposed)
            return;

        _layer.Children.Clear();

        var projectionMap = JunhyunMapQuestProjectionV2.MapKey;
        var currentMap = _tracker.CurrentMapKey;
        if (string.IsNullOrWhiteSpace(projectionMap) ||
            string.IsNullOrWhiteSpace(currentMap) ||
            !string.Equals(projectionMap, currentMap, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var selectedFloor = SelectedFloorId();
        foreach (var marker in JunhyunMapQuestProjectionV2.Markers)
        {
            if (!FloorMatches(marker.FloorId, selectedFloor))
                continue;

            var visual = JunhyunQuestMarkerVisualFactoryV3.Create(marker);
            Canvas.SetLeft(visual, marker.X);
            Canvas.SetTop(visual, marker.Y);
            _layer.Children.Add(visual);
        }

        UpdateScale();
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
        if (string.IsNullOrWhiteSpace(markerFloor) || string.IsNullOrWhiteSpace(selectedFloor))
            return true;
        return string.Equals(markerFloor, selectedFloor, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _scaleTimer.Stop();
        JunhyunMapQuestProjectionV2.Changed -= Projection_Changed;
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_floorSelector is not null)
            _floorSelector.SelectionChanged -= FloorSelector_SelectionChanged;

        if (_layer.Parent is Panel parent)
            parent.Children.Remove(_layer);
    }
}

public static class JunhyunQuestMarkerVisualFactoryV3
{
    private const double MarkerSize = 24;

    public static FrameworkElement Create(JunhyunQuestMarkerProjectionV2 marker)
    {
        var root = new Canvas
        {
            Width = 0,
            Height = 0,
            IsHitTestVisible = false,
            ToolTip = $"{marker.MarkerCode} · {marker.QuestName}\n{marker.ObjectiveName}",
            Tag = marker,
        };

        var badge = new Border
        {
            Width = MarkerSize,
            Height = MarkerSize,
            CornerRadius = new CornerRadius(MarkerSize / 2),
            Background = new SolidColorBrush(Color.FromArgb(235, 197, 168, 74)),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1.5),
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
        };

        Canvas.SetLeft(badge, -MarkerSize / 2);
        Canvas.SetTop(badge, -MarkerSize / 2);
        root.Children.Add(badge);
        return root;
    }
}
