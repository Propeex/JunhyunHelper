using System.Windows.Media;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Applies the normal MiniMap opacity without competing with the existing product
    /// presentation loop. That loop still owns Window.Opacity for hover/timed full-hide;
    /// this mask only controls the visible-state baseline.
    /// </summary>
    public void ApplyJunhyunBaseOpacity(double opacity)
    {
        opacity = Math.Clamp(opacity, 0.10, 1.0);
        var alpha = (byte)Math.Round(opacity * byte.MaxValue);
        var mask = new SolidColorBrush(Color.FromArgb(alpha, 255, 255, 255));
        mask.Freeze();
        MainBorder.OpacityMask = mask;
    }
}
