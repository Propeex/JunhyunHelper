namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Narrow runtime bridge for product actions layered on the exact Tarkov Helper
/// MiniMap window.
/// </summary>
public static class JunhyunMiniMapProductRegistry
{
    private static readonly object Gate = new();
    private static WeakReference<TarkovHelper.Windows.OverlayMiniMapWindow>? _active;

    public static void Register(TarkovHelper.Windows.OverlayMiniMapWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        lock (Gate)
            _active = new WeakReference<TarkovHelper.Windows.OverlayMiniMapWindow>(window);

        window.InitializeQuestV2();
    }

    public static void Unregister(TarkovHelper.Windows.OverlayMiniMapWindow window)
    {
        window.DisposeQuestV2();
        lock (Gate)
        {
            if (_active is null || !_active.TryGetTarget(out var current) || ReferenceEquals(current, window))
                _active = null;
        }
    }

    public static void IncreaseSize() => WithActive(window => window.IncreaseAnchoredSize());

    public static void DecreaseSize() => WithActive(window => window.DecreaseAnchoredSize());

    public static void ApplyPlayerMarkerSize(double mapPixelSize) =>
        WithActive(window => window.ApplySharedPlayerMarkerSize(mapPixelSize));

    private static void WithActive(Action<TarkovHelper.Windows.OverlayMiniMapWindow> action)
    {
        TarkovHelper.Windows.OverlayMiniMapWindow? window = null;
        lock (Gate)
        {
            if (_active?.TryGetTarget(out var current) == true)
                window = current;
        }

        if (window is null)
            return;

        if (window.Dispatcher.CheckAccess())
            action(window);
        else
            window.Dispatcher.BeginInvoke(() => action(window));
    }
}
