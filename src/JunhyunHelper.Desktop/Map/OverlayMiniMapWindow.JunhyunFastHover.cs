using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private DispatcherTimer? _junhyunFastHoverTimer;

    [ModuleInitializer]
    internal static void RegisterJunhyunFastHover()
    {
        EventManager.RegisterClassHandler(
            typeof(OverlayMiniMapWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(JunhyunFastHoverLoaded));
    }

    private static void JunhyunFastHoverLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not OverlayMiniMapWindow window || window._junhyunFastHoverTimer is not null)
            return;

        // Hover feedback is latency-sensitive but does not need any map/marker rendering.
        // Keep it on a dedicated lightweight ~60 Hz timer instead of waiting for the
        // existing 80 ms product synchronization timer (which performs much heavier work).
        window._junhyunFastHoverTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Input,
            window.JunhyunFastHoverTimer_Tick,
            window.Dispatcher);
        window._junhyunFastHoverTimer.Start();
        window.Closed += window.JunhyunFastHoverClosed;
        window.ApplyJunhyunFastHoverOpacity();
    }

    private void JunhyunFastHoverTimer_Tick(object? sender, EventArgs e) =>
        ApplyJunhyunFastHoverOpacity();

    private void ApplyJunhyunFastHoverOpacity()
    {
        var shouldHide = JunhyunTemporaryHideActive || IsCursorInsideMiniMap();
        var targetOpacity = shouldHide ? 0.0 : 1.0;
        if (Math.Abs(Opacity - targetOpacity) > 0.001)
            Opacity = targetOpacity;
    }

    private void JunhyunFastHoverClosed(object? sender, EventArgs e)
    {
        Closed -= JunhyunFastHoverClosed;
        if (_junhyunFastHoverTimer is null)
            return;

        _junhyunFastHoverTimer.Stop();
        _junhyunFastHoverTimer.Tick -= JunhyunFastHoverTimer_Tick;
        _junhyunFastHoverTimer = null;
    }
}
