using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TarkovHelper.Services.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Keeps the visible Main Map selection and the shared MapTrackerService key identical.
/// The MiniMap consumes MapTrackerService directly, so a stale tracker key can otherwise
/// display a different map even while the Main Map combo box shows the intended map.
/// </summary>
public sealed class LegacyMapSelectionConsistencyBridge : IDisposable
{
    private static readonly object ActiveGate = new();
    private static WeakReference<LegacyMapSelectionConsistencyBridge>? _active;

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly ComboBox? _mapSelector;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private bool _disposed;
    private bool _syncQueued;

    public LegacyMapSelectionConsistencyBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _mapSelector = _page.FindName("CmbMapSelect") as ComboBox;

        lock (ActiveGate)
            _active = new WeakReference<LegacyMapSelectionConsistencyBridge>(this);

        _page.Loaded += Page_Loaded;
        _tracker.MapChanged += Tracker_MapChanged;
        if (_mapSelector is not null)
            _mapSelector.SelectionChanged += MapSelector_SelectionChanged;

        QueueSynchronize();
    }

    /// <summary>
    /// Forces the active Main Map selection into MapTrackerService synchronously. The
    /// MiniMap calls this during SourceInitialized, after Loaded, and whenever a hidden
    /// donor MiniMap window is shown again, so all entry paths share the same canonical
    /// visible-selection boundary.
    /// </summary>
    public static bool SynchronizeCurrentSelectionNow()
    {
        LegacyMapSelectionConsistencyBridge? bridge = null;
        lock (ActiveGate)
        {
            if (_active?.TryGetTarget(out bridge) != true)
                return false;
        }

        if (bridge is null || bridge._disposed)
            return false;

        if (bridge._page.Dispatcher.CheckAccess())
            return bridge.SynchronizeCore();

        return bridge._page.Dispatcher.Invoke(bridge.SynchronizeCore);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e) => QueueSynchronize();

    private void MapSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        QueueSynchronize();

    private void Tracker_MapChanged(string mapKey) =>
        _page.Dispatcher.BeginInvoke(QueueSynchronize, DispatcherPriority.Loaded);

    private void QueueSynchronize()
    {
        if (_disposed || _syncQueued)
            return;

        _syncQueued = true;
        _page.Dispatcher.BeginInvoke(
            () =>
            {
                _syncQueued = false;
                _ = SynchronizeCore();
            },
            DispatcherPriority.ContextIdle);
    }

    private bool SynchronizeCore()
    {
        if (_disposed || _mapSelector is null)
            return false;

        NormalizeInterchangeLabel();

        if (_mapSelector.SelectedItem is not ComboBoxItem selected ||
            selected.Tag is not string selectedKey ||
            string.IsNullOrWhiteSpace(selectedKey))
        {
            return false;
        }

        var canonicalKey = _tracker.ResolveMapKey(selectedKey) ?? selectedKey;
        if (!string.Equals(_tracker.CurrentMapKey, canonicalKey, StringComparison.OrdinalIgnoreCase))
            _tracker.SetCurrentMap(canonicalKey);

        // SetCurrentMap remains the shared authoritative state. This direct product bridge
        // additionally closes the visible timing gap while an OverlayMiniMapWindow is open.
        JunhyunMiniMapProductRegistry.SynchronizeMapSelection(canonicalKey);
        VerifyMiniMapSynchronizationIfRequested(canonicalKey);
        return true;
    }

    private void VerifyMiniMapSynchronizationIfRequested(string canonicalKey)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal) ||
            !JunhyunMiniMapProductRegistry.HasLoadedActiveWindow)
        {
            return;
        }

        try
        {
            if (!JunhyunMiniMapProductRegistry.IsActiveMapSelectionSynchronized(canonicalKey))
            {
                throw new InvalidOperationException(
                    $"MiniMap did not immediately synchronize to Main Map selection '{canonicalKey}'.");
            }

            // Do not publish success evidence here. v1.9.1 proved that checking only
            // current service/window state can miss the real A -> B -> reopen rendering
            // regression. LegacyMapProductRuntime writes the CI marker only after the
            // reused Window has been shown and MapSvg has actually changed to map B.
        }
        catch (Exception exception)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt"),
                    "Map/MiniMap selection synchronization smoke failed.\n" + exception);
            }
            catch
            {
            }

            Environment.Exit(89);
        }
    }

    private void NormalizeInterchangeLabel()
    {
        if (_mapSelector is null)
            return;

        foreach (var item in _mapSelector.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is not string mapKey)
                continue;

            var canonical = _tracker.ResolveMapKey(mapKey) ?? mapKey;
            if (string.Equals(canonical, "Interchange", StringComparison.OrdinalIgnoreCase))
                item.Content = "인터체인지";
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _page.Loaded -= Page_Loaded;
        _tracker.MapChanged -= Tracker_MapChanged;
        if (_mapSelector is not null)
            _mapSelector.SelectionChanged -= MapSelector_SelectionChanged;

        lock (ActiveGate)
        {
            if (_active?.TryGetTarget(out var current) != true || ReferenceEquals(current, this))
                _active = null;
        }
    }
}
