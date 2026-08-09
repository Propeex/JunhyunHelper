using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Keeps JunhyunHelper Quest markers at a stable screen size while the exact legacy
/// Map canvas zooms. This matches the original marker managers and MiniMap behavior.
/// </summary>
public sealed class LegacyQuestMarkerScaleBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly Canvas? _mapCanvas;
    private readonly ScaleTransform? _mapScale;
    private bool _disposed;

    public LegacyQuestMarkerScaleBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _mapCanvas = _page.FindName("MapCanvas") as Canvas;
        _mapScale = _page.FindName("MapScale") as ScaleTransform;

        if (_mapScale is not null)
            _mapScale.Changed += MapScale_Changed;
        JunhyunMapQuestProjection.Changed += Projection_Changed;
    }

    private void MapScale_Changed(object? sender, EventArgs e) => Apply();

    private void Projection_Changed(object? sender, EventArgs e) =>
        _page.Dispatcher.BeginInvoke(Apply);

    private void Apply()
    {
        if (_disposed || _mapCanvas is null)
            return;

        var zoom = _mapScale?.ScaleX ?? 1.0;
        var inverse = 1.0 / Math.Max(zoom, 0.01);

        var questLayer = _mapCanvas.Children
            .OfType<Canvas>()
            .FirstOrDefault(canvas => Panel.GetZIndex(canvas) == 500);
        if (questLayer is null)
            return;

        foreach (FrameworkElement marker in questLayer.Children)
        {
            marker.RenderTransform = new ScaleTransform(inverse, inverse);
            marker.RenderTransformOrigin = new Point(0, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_mapScale is not null)
            _mapScale.Changed -= MapScale_Changed;
        JunhyunMapQuestProjection.Changed -= Projection_Changed;
    }
}
