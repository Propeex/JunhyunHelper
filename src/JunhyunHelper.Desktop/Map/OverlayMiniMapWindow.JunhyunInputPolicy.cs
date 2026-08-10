using System.Windows;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private bool _junhyunInputPolicyAttached;

    /// <summary>
    /// JunhyunHelper MiniMap is controlled by hotkeys. Mouse resizing and every
    /// legacy bottom-right resize affordance are removed from the product UI.
    /// </summary>
    public void ApplyJunhyunInputPolicy()
    {
        ApplyJunhyunNoResizeState();

        // SourceInitialized can occur before the visual has fully settled. Re-apply
        // once at Loaded so both WPF's native resize grip and the transplanted custom
        // Path are guaranteed to be gone in the visible window.
        if (_junhyunInputPolicyAttached)
            return;

        _junhyunInputPolicyAttached = true;
        Loaded += JunhyunInputPolicy_Loaded;
        Closed += JunhyunInputPolicy_Closed;
    }

    private void JunhyunInputPolicy_Loaded(object sender, RoutedEventArgs e) =>
        ApplyJunhyunNoResizeState();

    private void ApplyJunhyunNoResizeState()
    {
        ResizeMode = ResizeMode.NoResize;

        for (var i = MapContainer.Children.Count - 1; i >= 0; i--)
        {
            if (MapContainer.Children[i] is System.Windows.Shapes.Path)
                MapContainer.Children.RemoveAt(i);
        }
    }

    private void JunhyunInputPolicy_Closed(object? sender, EventArgs e)
    {
        Loaded -= JunhyunInputPolicy_Loaded;
        Closed -= JunhyunInputPolicy_Closed;
        _junhyunInputPolicyAttached = false;
    }
}
