using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

/// <summary>
/// Narrow desktop bridge between Scanner presentation and the Farming Guide page. Scanner
/// publishes confirmed item presentation data; Farming Guide owns inventory decisions.
/// </summary>
public sealed class FarmingGuideRaidBridge
{
    private readonly object _gate = new();
    private Action<ScannerItemSnapshot>? _scanHandler;
    private Func<bool>? _acceptHandler;
    private Action<string>? _miniScannerStatusHandler;
    private Func<string, ScannerItemSnapshot?>? _snapshotResolver;
    private string? _lastScannerItemId;

    public void SetScannerSnapshotResolver(Func<string, ScannerItemSnapshot?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        lock (_gate)
            _snapshotResolver = resolver;
    }

    public ScannerItemSnapshot? ResolveSnapshot(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;
        Func<string, ScannerItemSnapshot?>? resolver;
        lock (_gate)
            resolver = _snapshotResolver;
        return resolver?.Invoke(itemId.Trim());
    }

    public void Bind(
        Action<ScannerItemSnapshot> scanHandler,
        Func<bool> acceptHandler,
        Action<string> miniScannerStatusHandler)
    {
        ArgumentNullException.ThrowIfNull(scanHandler);
        ArgumentNullException.ThrowIfNull(acceptHandler);
        ArgumentNullException.ThrowIfNull(miniScannerStatusHandler);
        lock (_gate)
        {
            _scanHandler = scanHandler;
            _acceptHandler = acceptHandler;
            _miniScannerStatusHandler = miniScannerStatusHandler;
        }
    }

    public void Unbind()
    {
        lock (_gate)
        {
            _scanHandler = null;
            _acceptHandler = null;
            _miniScannerStatusHandler = null;
            _lastScannerItemId = null;
        }
    }

    public void ObserveScannerStatus(ScannerRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.State != ScannerRuntimeState.ShowingItem || string.IsNullOrWhiteSpace(status.ItemId))
            return;

        Action<ScannerItemSnapshot>? handler;
        Func<string, ScannerItemSnapshot?>? resolver;
        lock (_gate)
        {
            if (string.Equals(_lastScannerItemId, status.ItemId, StringComparison.Ordinal))
                return;
            _lastScannerItemId = status.ItemId;
            handler = _scanHandler;
            resolver = _snapshotResolver;
        }

        var snapshot = resolver?.Invoke(status.ItemId);
        if (snapshot is not null)
            handler?.Invoke(snapshot);
    }

    public void ResetScannerIdentity()
    {
        lock (_gate)
            _lastScannerItemId = null;
    }

    public bool PublishSimulatedScan(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        Action<ScannerItemSnapshot>? handler;
        var snapshot = ResolveSnapshot(itemId);
        lock (_gate)
            handler = _scanHandler;
        if (snapshot is null)
            return false;
        handler?.Invoke(snapshot);
        return true;
    }

    public bool TryAccept()
    {
        Func<bool>? handler;
        lock (_gate)
            handler = _acceptHandler;
        return handler?.Invoke() == true;
    }

    public void ShowMiniScannerStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        Action<string>? handler;
        lock (_gate)
            handler = _miniScannerStatusHandler;
        handler?.Invoke(message.Trim());
    }
}
