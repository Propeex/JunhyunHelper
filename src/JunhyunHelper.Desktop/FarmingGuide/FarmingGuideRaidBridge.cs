using System.Windows.Threading;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

/// <summary>
/// Narrow desktop bridge between Scanner presentation and the Farming Guide page. Scanner
/// publishes confirmed item presentation data; Farming Guide owns inventory decisions.
/// Scanner runtime events may originate on worker threads, so all page-facing callbacks
/// cross the WPF Dispatcher boundary here.
/// </summary>
public sealed class FarmingGuideRaidBridge
{
    private readonly object _gate = new();
    private readonly Dispatcher _dispatcher;
    private Action<ScannerItemSnapshot>? _scanHandler;
    private Func<bool>? _acceptHandler;
    private Action<int>? _quantityHandler;
    private Func<string, ScannerItemSnapshot?>? _snapshotResolver;
    private Func<string, CancellationToken, Task<ScannerItemSnapshot?>>? _simulatedSnapshotResolver;
    private Action<string?>? _miniScannerInstructionHandler;
    private Action? _miniScannerQuantityRequester;
    private Action? _miniScannerQuantityCanceller;
    private Action<ScannerItemSnapshot>? _simulatedScanPresenter;
    private Action<string>? _transientStatusHandler;
    private string? _lastScannerItemId;

    public FarmingGuideRaidBridge()
    {
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public void SetScannerSnapshotResolver(Func<string, ScannerItemSnapshot?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        lock (_gate)
            _snapshotResolver = resolver;
    }

    public void SetSimulatedSnapshotResolver(
        Func<string, CancellationToken, Task<ScannerItemSnapshot?>> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        lock (_gate)
            _simulatedSnapshotResolver = resolver;
    }

    public void SetMiniScannerInstructionHandler(Action<string?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
            _miniScannerInstructionHandler = handler;
    }

    public void SetMiniScannerQuantityHandlers(Action requester, Action canceller)
    {
        ArgumentNullException.ThrowIfNull(requester);
        ArgumentNullException.ThrowIfNull(canceller);
        lock (_gate)
        {
            _miniScannerQuantityRequester = requester;
            _miniScannerQuantityCanceller = canceller;
        }
    }

    public void SetSimulatedScanPresenter(Action<ScannerItemSnapshot> presenter)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        lock (_gate)
            _simulatedScanPresenter = presenter;
    }

    public void SetTransientStatusHandler(Action<string> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
            _transientStatusHandler = handler;
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
        Action<int>? quantityHandler = null)
    {
        ArgumentNullException.ThrowIfNull(scanHandler);
        ArgumentNullException.ThrowIfNull(acceptHandler);
        lock (_gate)
        {
            _scanHandler = scanHandler;
            _acceptHandler = acceptHandler;
            _quantityHandler = quantityHandler;
        }
    }

    // Compatibility overload for the page created in the original raid-advisor feature.
    public void Bind(
        Action<ScannerItemSnapshot> scanHandler,
        Func<bool> acceptHandler,
        Action<string> legacyStatusHandler) =>
        Bind(scanHandler, acceptHandler, quantityHandler: null);

    public void Unbind()
    {
        lock (_gate)
        {
            _scanHandler = null;
            _acceptHandler = null;
            _quantityHandler = null;
            _lastScannerItemId = null;
        }
        CancelMiniScannerQuantity();
        SetMiniScannerInstruction(null);
    }

    public void ObserveScannerStatus(ScannerRuntimeStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.State != ScannerRuntimeState.ShowingItem || string.IsNullOrWhiteSpace(status.ItemId))
        {
            lock (_gate)
                _lastScannerItemId = null;
            return;
        }

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
        if (snapshot is null || handler is null)
            return;

        InvokePageCallback(() => handler(snapshot));
    }

    public void ResetScannerIdentity()
    {
        lock (_gate)
            _lastScannerItemId = null;
        CancelMiniScannerQuantity();
        SetMiniScannerInstruction(null);
    }

    public bool PublishSimulatedScan(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        Action<ScannerItemSnapshot>? handler;
        Action<ScannerItemSnapshot>? presenter;
        var snapshot = ResolveSnapshot(itemId);
        lock (_gate)
        {
            handler = _scanHandler;
            presenter = _simulatedScanPresenter;
        }
        if (snapshot is null)
            return false;

        PublishSimulatedSnapshot(snapshot, presenter, handler);
        return true;
    }

    public async Task<bool> PublishSimulatedScanAsync(
        string itemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        Func<string, CancellationToken, Task<ScannerItemSnapshot?>>? asyncResolver;
        Action<ScannerItemSnapshot>? handler;
        Action<ScannerItemSnapshot>? presenter;
        lock (_gate)
        {
            asyncResolver = _simulatedSnapshotResolver;
            handler = _scanHandler;
            presenter = _simulatedScanPresenter;
        }

        var normalized = itemId.Trim();
        var snapshot = asyncResolver is null
            ? ResolveSnapshot(normalized)
            : await asyncResolver(normalized, cancellationToken);
        if (snapshot is null)
            return false;

        PublishSimulatedSnapshot(snapshot, presenter, handler);
        return true;
    }

    private void PublishSimulatedSnapshot(
        ScannerItemSnapshot snapshot,
        Action<ScannerItemSnapshot>? presenter,
        Action<ScannerItemSnapshot>? handler)
    {
        if (_dispatcher.CheckAccess())
        {
            presenter?.Invoke(snapshot);
            handler?.Invoke(snapshot);
            return;
        }

        InvokePageCallback(() =>
        {
            presenter?.Invoke(snapshot);
            handler?.Invoke(snapshot);
        });
    }

    public bool TryAccept()
    {
        Func<bool>? handler;
        lock (_gate)
            handler = _acceptHandler;
        if (handler is null || _dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return false;
        if (_dispatcher.CheckAccess())
            return handler();

        try
        {
            return _dispatcher.Invoke(handler);
        }
        catch (Exception exception) when (exception is TaskCanceledException or InvalidOperationException)
        {
            return false;
        }
    }

    public void SubmitMiniScannerQuantity(int quantity)
    {
        Action<int>? handler;
        lock (_gate)
            handler = _quantityHandler;
        if (handler is null)
            return;
        InvokePageCallback(() => handler(quantity));
    }

    public void RequestMiniScannerQuantity()
    {
        Action? handler;
        lock (_gate)
            handler = _miniScannerQuantityRequester;
        handler?.Invoke();
    }

    public void CancelMiniScannerQuantity()
    {
        Action? handler;
        lock (_gate)
            handler = _miniScannerQuantityCanceller;
        handler?.Invoke();
    }

    public void SetMiniScannerInstruction(string? message)
    {
        Action<string?>? handler;
        lock (_gate)
            handler = _miniScannerInstructionHandler;
        handler?.Invoke(string.IsNullOrWhiteSpace(message) ? null : message.Trim());
    }

    public void ShowTransientStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        Action<string>? handler;
        lock (_gate)
            handler = _transientStatusHandler;
        handler?.Invoke(message.Trim());
    }

    public void ShowMiniScannerStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var normalized = message.Trim();
        if (string.Equals(normalized, "반영 완료", StringComparison.Ordinal) ||
            string.Equals(normalized, "수락 완료", StringComparison.Ordinal))
        {
            SetMiniScannerInstruction(null);
            ShowTransientStatus(normalized);
            return;
        }

        SetMiniScannerInstruction(normalized);
    }

    private void InvokePageCallback(Action callback)
    {
        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return;
        if (_dispatcher.CheckAccess())
        {
            callback();
            return;
        }

        _ = _dispatcher.BeginInvoke(callback, DispatcherPriority.Normal);
    }
}
