using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JunhyunHelper.Desktop.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    internal int JunhyunRenderedTransitMarkerCountForSmoke =>
        ExtractMarkersContainer.Children
            .OfType<Canvas>()
            .Count(IsJunhyunTransitExtractVisualForSmoke);

    internal int JunhyunRenderedStandardMarkerCountForSmoke =>
        MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Count(static marker => marker.Tag is not JunhyunAdditionalMapMarker);

    internal double? JunhyunFirstStandardMarkerScaleForSmoke =>
        MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Where(static marker => marker.Tag is not JunhyunAdditionalMapMarker)
            .Select(static marker => marker.RenderTransform as ScaleTransform)
            .Where(static transform => transform is not null)
            .Select(static transform => (double?)transform!.ScaleX)
            .FirstOrDefault();

    internal double JunhyunPlayerMarkerScaleForSmoke => PlayerMarkerScale.ScaleX;

    internal void JunhyunClearStandardMarkersForSmoke()
    {
        var standard = MapMarkersContainer.Children
            .OfType<FrameworkElement>()
            .Where(static marker => marker.Tag is not JunhyunAdditionalMapMarker)
            .ToArray();
        foreach (var marker in standard)
            MapMarkersContainer.Children.Remove(marker);
    }

    private static bool IsJunhyunTransitExtractVisualForSmoke(Canvas canvas)
    {
        var expected = Color.FromRgb(255, 152, 0);
        return canvas.Children
            .OfType<Border>()
            .Select(static border => border.Child)
            .OfType<TextBlock>()
            .Any(text => text.Foreground is SolidColorBrush brush && brush.Color == expected);
    }
}
