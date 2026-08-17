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
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly ComboBox? _mapSelector;
    private readonly MapTrackerService _tracker = MapTrackerService.Instance;
    private bool _disposed;
    private bool _syncQueued;

    public LegacyMapSelectionConsistencyBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _mapSelector = _page.FindName("CmbMapSelect") as ComboBox;

        _page.Loaded += Page_Loaded;
        _tracker.MapChanged += Tracker_MapChanged;
        if (_mapSelector is not null)
            _mapSelector.SelectionChanged += MapSelector_SelectionChanged;

        QueueSynchronize();
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
                Synchronize();
            },
            DispatcherPriority.ContextIdle);
    }

    private void Synchronize()
    {
        if (_disposed || _mapSelector is null)
            return;

        NormalizeInterchangeLabel();

        if (_mapSelector.SelectedItem is not ComboBoxItem selected ||
            selected.Tag is not string selectedKey ||
            string.IsNullOrWhiteSpace(selectedKey))
        {
            return;
        }

        var canonicalKey = _tracker.ResolveMapKey(selectedKey) ?? selectedKey;
        if (!string.Equals(_tracker.CurrentMapKey, canonicalKey, StringComparison.OrdinalIgnoreCase))
            _tracker.SetCurrentMap(canonicalKey);
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
    }
}
