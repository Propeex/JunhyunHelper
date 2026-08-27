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
    /// MiniMap calls this during SourceInitialized, before its donor Loaded handler reads
    /// CurrentMapKey, so the very first frame cannot inherit an older tracker selection.
    /// </summary>
    public static bool SynchronizeCurrentSelectionNow()
    {
        LegacyMapSelectionConsistencyBridge? bridge;
        lock (ActiveGate)
        {
            if (_active?.TryGetTarget(out bridge) != true)
                return false;
        }

        if (bridge._disposed)
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
        return true;
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
