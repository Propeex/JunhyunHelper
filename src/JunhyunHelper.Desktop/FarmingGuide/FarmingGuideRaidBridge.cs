using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

/// <summary>
/// Narrow desktop bridge between Scanner presentation and the Farming Guide page. Scanner
/// publishes only confirmed Item IDs; Farming Guide owns all inventory decisions/state.
/// </summary>
public sealed class FarmingGuideRaidBridge
{
    private readonly object _gate = new();
    private Action<string>? _scanHandler;
    private Func<bool>? _acceptHandler;
    private Action<string>? _miniScannerStatusHandler;
    private string? _lastScannerItemId;

    public void Bind(
        Action<string> scanHandler,
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

        Action<string>? handler;
        lock (_gate)
        {
            // Scanner refreshes the presentation of an already-confirmed item. Those
            // refreshes must not create a second pickup recommendation for the same open
            // Tarkov detail window.
            if (string.Equals(_lastScannerItemId, status.ItemId, StringComparison.Ordinal))
                return;
            _lastScannerItemId = status.ItemId;
            handler = _scanHandler;
        }
        handler?.Invoke(status.ItemId);
    }

    public void ResetScannerIdentity()
    {
        lock (_gate)
            _lastScannerItemId = null;
    }

    public void PublishSimulatedScan(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        Action<string>? handler;
        lock (_gate)
            handler = _scanHandler;
        handler?.Invoke(itemId.Trim());
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
