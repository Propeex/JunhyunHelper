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

        var store = JunhyunMapProductSettingsStore.Instance;
        window.ApplyJunhyunInputPolicy();
        window.ApplyJunhyunBaseOpacity(store.MiniMapOpacity);
        window.ApplyJunhyunMarkerScale(store.MiniMapMarkerScale);
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

    public static void ZoomIn() => WithActive(window => window.ZoomIn());

    public static void ZoomOut() => WithActive(window => window.ZoomOut());

    public static void MoveFloorUp() => _ = MoveFloorUpAsync();

    public static void MoveFloorDown() => _ = MoveFloorDownAsync();

    public static Task MoveFloorUpAsync() =>
        WithActiveAsync(window => window.JunhyunMoveFloorUpAsync());

    public static Task MoveFloorDownAsync() =>
        WithActiveAsync(window => window.JunhyunMoveFloorDownAsync());

    public static Task SelectFloorIndexAsync(int floorIndex) =>
        WithActiveAsync(window => window.JunhyunSelectFloorIndexAsync(floorIndex));

    public static void IncreaseSize() => WithActive(window => window.IncreaseAnchoredSize());

    public static void DecreaseSize() => WithActive(window => window.DecreaseAnchoredSize());

    public static void ApplyPlayerMarkerSize(double mapPixelSize) =>
        WithActive(window => window.ApplySharedPlayerMarkerSize(mapPixelSize));

    public static void ApplyBaseOpacity(double opacity) =>
        WithActive(window => window.ApplyJunhyunBaseOpacity(opacity));

    public static void ApplyMarkerScale(double scale) =>
        WithActive(window => window.ApplyJunhyunMarkerScale(scale));

    public static void TemporarilyHide(double seconds) =>
        WithActive(window => window.JunhyunTemporarilyHide(seconds));

    private static TarkovHelper.Windows.OverlayMiniMapWindow? ActiveWindow()
    {
        lock (Gate)
        {
            return _active?.TryGetTarget(out var current) == true
                ? current
                : null;
        }
    }

    private static void WithActive(Action<TarkovHelper.Windows.OverlayMiniMapWindow> action)
    {
        var window = ActiveWindow();
        if (window is null)
            return;

        if (window.Dispatcher.CheckAccess())
            action(window);
        else
            window.Dispatcher.BeginInvoke(() => action(window));
    }

    private static Task WithActiveAsync(Func<TarkovHelper.Windows.OverlayMiniMapWindow, Task> action)
    {
        var window = ActiveWindow();
        if (window is null)
            return Task.CompletedTask;

        if (window.Dispatcher.CheckAccess())
            return action(window);

        return window.Dispatcher.InvokeAsync(() => action(window)).Task.Unwrap();
    }
}
