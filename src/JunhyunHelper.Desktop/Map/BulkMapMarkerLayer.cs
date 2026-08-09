using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Map;

public sealed record BulkMapMarkerPoint(
    Point Position,
    string Name,
    string Detail);

public sealed class BulkMapMarkerLayer : FrameworkElement
{
    private readonly IReadOnlyList<BulkMapMarkerPoint> _points;
    private readonly ImageSource? _icon;
    private readonly Brush _fallbackBrush;
    private readonly double _markerSize;

    public BulkMapMarkerLayer(
        IReadOnlyList<BulkMapMarkerPoint> points,
        double width,
        double height,
        string? iconPath,
        Brush fallbackBrush,
        double markerSize = 18)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(fallbackBrush);

        _points = points;
        _fallbackBrush = fallbackBrush;
        _markerSize = markerSize;
        Width = width;
        Height = height;
        Cursor = Cursors.Hand;
        _icon = TryLoadIcon(iconPath);
    }

    public event EventHandler<BulkMapMarkerPoint>? MarkerClicked;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var half = _markerSize / 2;
        foreach (var point in _points)
        {
            var rect = new Rect(
                point.Position.X - half,
                point.Position.Y - half,
                _markerSize,
                _markerSize);
            if (_icon is not null)
            {
                drawingContext.DrawImage(_icon, rect);
            }
            else
            {
                drawingContext.DrawEllipse(
                    _fallbackBrush,
                    null,
                    point.Position,
                    half * 0.62,
                    half * 0.62);
            }
        }
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        var point = hitTestParameters.HitPoint;
        return point.X >= 0 && point.Y >= 0 && point.X <= ActualWidth && point.Y <= ActualHeight
            ? new PointHitTestResult(this, point)
            : null;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_points.Count == 0)
            return;

        var click = e.GetPosition(this);
        var radiusSquared = Math.Pow(Math.Max(10, _markerSize), 2);
        BulkMapMarkerPoint? nearest = null;
        var nearestDistance = double.MaxValue;
        foreach (var point in _points)
        {
            var dx = point.Position.X - click.X;
            var dy = point.Position.Y - click.Y;
            var distance = dx * dx + dy * dy;
            if (distance <= radiusSquared && distance < nearestDistance)
            {
                nearest = point;
                nearestDistance = distance;
            }
        }

        if (nearest is null)
            return;

        e.Handled = true;
        MarkerClicked?.Invoke(this, nearest);
    }

    private static ImageSource? TryLoadIcon(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
