using System.Windows;
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
            var heading = playerHeading is null ? 0 : MapCoordinateTransformer.SurfaceHeading(layout, playerHeading.Value);
            var player = MapVisualFactory.CreatePlayerMarker(heading, 34);
            Canvas.SetLeft(player, playerPoint.X - player.Width / 2);
            Canvas.SetTop(player, playerPoint.Y - player.Height / 2);
            MiniPlayerCanvas.Children.Add(player);
        }
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
        if (Application.Current?.MainWindow?.IsVisible == true)
        {
            e.Cancel = true;
            Hide();
            UserClosed?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.OnClosing(e);
    }
}
