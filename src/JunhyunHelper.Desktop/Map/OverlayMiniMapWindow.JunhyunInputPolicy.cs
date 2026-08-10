using System.Windows;
using System.Windows.Shapes;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// JunhyunHelper MiniMap is controlled by hotkeys. Mouse resizing and the legacy
    /// bottom-right resize affordance are intentionally removed from the product UI.
    /// </summary>
    public void ApplyJunhyunInputPolicy()
    {
        ResizeMode = ResizeMode.NoResize;

        // The transplanted XAML exposes its custom resize affordance as the only Path
        // directly under MapContainer. Remove it without changing the upstream source.
        for (var i = MapContainer.Children.Count - 1; i >= 0; i--)
        {
            if (MapContainer.Children[i] is Path)
                MapContainer.Children.RemoveAt(i);
        }
    }
}
