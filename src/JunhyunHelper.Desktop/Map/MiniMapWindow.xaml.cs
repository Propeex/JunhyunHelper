using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

public sealed record MiniMapMarker(
    MapWorldPosition Position,
    string Name,
    MapMarkerKind? Kind,
    bool IsQuest,
    string? UserColor = null);

public partial class MiniMapWindow : Window
{
    private const double SurfaceWidth = 1600;
    private const double MinTrackingZoom = 1.0;
    private const double MaxTrackingZoom = 6.0;

    private MapLayoutDefinition? _layout;
    private MapWorldPosition? _playerPosition;
    private double _trackingZoom = 2.35;
    private double _effectiveScale = 1;

    public MiniMapWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? UserClosed;

    public void SetState(
        MapLayoutDefinition layout,
        string svgPath,
        string? floorName,
        IReadOnlyList<MiniMapMarker> markers,
        MapWorldPosition? playerPosition,
        double? playerHeading,
        IReadOnlyList<MapWorldPosition> trail,
        bool showTrail)
    {
        _layout = layout;
        _playerPosition = playerPosition;

        var aspect = MapCoordinateTransformer.SurfaceAspectRatio(layout);
        MiniSurface.Width = SurfaceWidth;
        MiniSurface.Height = SurfaceWidth * aspect;
        MiniSvg.Source = new Uri(svgPath, UriKind.Absolute);
        TitleText.Text = string.IsNullOrWhiteSpace(floorName)
            ? "준현 미니맵"
            : $"준현 미니맵 · {floorName}";

        MiniMarkerCanvas.Children.Clear();
        foreach (var marker in markers)
        {
            if (!MapCoordinateTransformer.TryWorldToSurface(
                    layout,
                    marker.Position,
                    MiniSurface.Width,
                    MiniSurface.Height,
                    out var point))
                continue;

            FrameworkElement visual = marker.IsQuest
                ? MapVisualFactory.CreateQuestMarker(marker.Name, 32)
                : marker.UserColor is not null
                    ? MapVisualFactory.CreateUserMarker(marker.UserColor, marker.Name, 30)
                    : MapVisualFactory.CreateMarker(marker.Kind ?? MapMarkerKind.Hazard, marker.Name, 28);
            Canvas.SetLeft(visual, point.X - visual.Width / 2);
            Canvas.SetTop(visual, point.Y - visual.Height / 2);
            MiniMarkerCanvas.Children.Add(visual);
        }

        MiniTrailCanvas.Children.Clear();
        if (showTrail && trail.Count > 1)
        {
            var points = new PointCollection();
            foreach (var world in trail)
            {
                if (MapCoordinateTransformer.TryWorldToSurface(
                        layout,
                        world,
                        MiniSurface.Width,
                        MiniSurface.Height,
                        out var point))
                    points.Add(point);
            }
            if (points.Count > 1)
            {
                MiniTrailCanvas.Children.Add(new Polyline
                {
                    Points = points,
                    Stroke = Brushes.DeepSkyBlue,
                    StrokeThickness = 3,
                    Opacity = 0.75,
                });
            }
        }

        MiniPlayerCanvas.Children.Clear();
        if (playerPosition is not null &&
            MapCoordinateTransformer.TryWorldToSurface(
                layout,
                playerPosition,
                MiniSurface.Width,
                MiniSurface.Height,
                out var playerPoint))
        {
            var heading = playerHeading is null
                ? 0
                : MapCoordinateTransformer.SurfaceHeading(layout, playerHeading.Value);
            var player = MapVisualFactory.CreatePlayerMarker(heading, 34);
            Canvas.SetLeft(player, playerPoint.X - player.Width / 2);
            Canvas.SetTop(player, playerPoint.Y - player.Height / 2);
            MiniPlayerCanvas.Children.Add(player);
        }

        UpdateViewport();
    }

    private void UpdateViewport()
    {
        if (_layout is null || MiniViewport.ActualWidth <= 0 || MiniViewport.ActualHeight <= 0 ||
            MiniSurface.Width <= 0 || MiniSurface.Height <= 0)
            return;

        var fitScale = Math.Min(
            MiniViewport.ActualWidth / MiniSurface.Width,
            MiniViewport.ActualHeight / MiniSurface.Height);
        if (!double.IsFinite(fitScale) || fitScale <= 0)
            return;

        var tracking = _playerPosition is not null;
        _effectiveScale = fitScale * (tracking ? _trackingZoom : 0.96);
        var offsetX = (MiniViewport.ActualWidth - MiniSurface.Width * _effectiveScale) / 2;
        var offsetY = (MiniViewport.ActualHeight - MiniSurface.Height * _effectiveScale) / 2;

        if (tracking && _playerPosition is not null &&
            MapCoordinateTransformer.TryWorldToSurface(
                _layout,
                _playerPosition,
                MiniSurface.Width,
                MiniSurface.Height,
                out var playerPoint))
        {
            offsetX = MiniViewport.ActualWidth / 2 - playerPoint.X * _effectiveScale;
            offsetY = MiniViewport.ActualHeight / 2 - playerPoint.Y * _effectiveScale;
        }

        MiniSurface.RenderTransform = new MatrixTransform(
            _effectiveScale,
            0,
            0,
            _effectiveScale,
            offsetX,
            offsetY);
        ApplyInverseScale(MiniMarkerCanvas);
        ApplyInverseScale(MiniPlayerCanvas);
        ZoomHintText.Text = tracking
            ? $"플레이어 추적 · {_trackingZoom:0.0}×"
            : "전체 지도";
    }

    private void ApplyInverseScale(Canvas canvas)
    {
        if (_effectiveScale <= 0)
            return;
        var inverse = 1 / _effectiveScale;
        foreach (var child in canvas.Children.OfType<FrameworkElement>())
        {
            child.RenderTransformOrigin = new Point(0.5, 0.5);
            child.RenderTransform = new ScaleTransform(inverse, inverse);
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) =>
        Dispatcher.BeginInvoke(UpdateViewport);

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _trackingZoom = Math.Clamp(
            _trackingZoom * (e.Delta > 0 ? 1.12 : 1 / 1.12),
            MinTrackingZoom,
            MaxTrackingZoom);
        UpdateViewport();
        e.Handled = true;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        UserClosed?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (System.Windows.Application.Current?.MainWindow?.IsVisible == true)
        {
            e.Cancel = true;
            Hide();
            UserClosed?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.OnClosing(e);
    }
}
