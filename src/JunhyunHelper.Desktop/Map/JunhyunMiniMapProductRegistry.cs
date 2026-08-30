using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Narrow runtime bridge for product actions layered on the exact Tarkov Helper
/// MiniMap window.
/// </summary>
public static class JunhyunMiniMapProductRegistry
{
    private static readonly object Gate = new();
    private static WeakReference<TarkovHelper.Windows.OverlayMiniMapWindow>? _active;
    private static string? _latestMapKey;

    public static void Register(TarkovHelper.Windows.OverlayMiniMapWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Capture the visible Main Map before registration. SynchronizeMapSelection retains
        // the selection even while no MiniMap window exists, so first-show state cannot be
        // lost between the Main Map combo and the donor overlay lifecycle.
        _ = LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow();

        string? latestMapKey;
        lock (Gate)
        {
            _active = new WeakReference<TarkovHelper.Windows.OverlayMiniMapWindow>(window);
            latestMapKey = _latestMapKey;
        }

        var store = JunhyunMapProductSettingsStore.Instance;
        window.InitializeJunhyunWindowState();
        window.ApplyJunhyunInputPolicy();
        window.ApplyJunhyunBaseOpacity(store.MiniMapOpacity);
        window.ApplyJunhyunMarkerScale(store.MiniMapMarkerScale);
        window.InitializeQuestV2();

        // SourceInitialized occurs before donor Loaded. The window-side synchronization
        // bridge retains this replay until Loaded, after the donor tracker has initialized.
        if (!string.IsNullOrWhiteSpace(latestMapKey))
            window.SynchronizeJunhyunMapSelection(latestMapKey);

        // Re-read once the Loaded queue settles as a final consistency boundary in case
        // the Main Map changed while the overlay was being created.
        window.Dispatcher.BeginInvoke(
            () => _ = LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow(),
            DispatcherPriority.ContextIdle);
    }

    public static void Unregister(TarkovHelper.Windows.OverlayMiniMapWindow window)
    {
        window.DisposeJunhyunWindowState();
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
        WithActive(window =>
        {
            window.ApplySharedPlayerMarkerSize(mapPixelSize);
            window.ReapplyJunhyunMarkerPresentationAfterDonorMapView();
        });

    public static void ApplyBaseOpacity(double opacity) =>
        WithActive(window => window.ApplyJunhyunBaseOpacity(opacity));

    public static void ApplyMarkerScale(double scale) =>
        WithActive(window => window.ApplyJunhyunMarkerScale(scale));

    public static void TemporarilyHide(double seconds) =>
        WithActive(window => window.JunhyunTemporarilyHide(seconds));

    /// <summary>
    /// Retains the canonical Main Map selection even when no MiniMap exists and pushes it
    /// into the active window when one is present. This makes first-show synchronization
    /// stateful rather than dependent on WPF lifecycle timing.
    /// </summary>
    public static void SynchronizeMapSelection(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return;

        lock (Gate)
            _latestMapKey = mapKey;

        WithActive(window => window.SynchronizeJunhyunMapSelection(mapKey));
    }

    public static bool IsActiveMapSelectionSynchronized(string mapKey)
    {
        if (string.IsNullOrWhiteSpace(mapKey))
            return false;

        var window = ActiveWindow();
        if (window is null || !window.IsLoaded)
            return false;

        bool Read() => string.Equals(
            window.JunhyunCurrentMapKey,
            mapKey,
            StringComparison.OrdinalIgnoreCase);

        return window.Dispatcher.CheckAccess()
            ? Read()
            : window.Dispatcher.Invoke(Read);
    }

    public static bool HasLoadedActiveWindow
    {
        get
        {
            var window = ActiveWindow();
            if (window is null)
                return false;

            return window.Dispatcher.CheckAccess()
                ? window.IsLoaded
                : window.Dispatcher.Invoke(() => window.IsLoaded);
        }
    }

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
