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
    private Func<string, ScannerItemSnapshot?>? _snapshotResolver;
    private Action<string?>? _miniScannerInstructionHandler;
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

    public void SetMiniScannerInstructionHandler(Action<string?> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
            _miniScannerInstructionHandler = handler;
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

    public void Bind(Action<ScannerItemSnapshot> scanHandler, Func<bool> acceptHandler)
    {
        ArgumentNullException.ThrowIfNull(scanHandler);
        ArgumentNullException.ThrowIfNull(acceptHandler);
        lock (_gate)
        {
            _scanHandler = scanHandler;
            _acceptHandler = acceptHandler;
        }
    }

    // Compatibility overload for the page created in the same feature branch. The third
    // callback used to be supplied by the page; presentation ownership now lives here.
    public void Bind(
        Action<ScannerItemSnapshot> scanHandler,
        Func<bool> acceptHandler,
        Action<string> legacyStatusHandler) =>
        Bind(scanHandler, acceptHandler);

    public void Unbind()
    {
        lock (_gate)
        {
            _scanHandler = null;
            _acceptHandler = null;
            _lastScannerItemId = null;
        }
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

        if (_dispatcher.CheckAccess())
        {
            presenter?.Invoke(snapshot);
            handler?.Invoke(snapshot);
        }
        else
        {
            InvokePageCallback(() =>
            {
                presenter?.Invoke(snapshot);
                handler?.Invoke(snapshot);
            });
        }
        return true;
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

    /// <summary>
    /// Active instructions are persistent. Acceptance/cancellation messages clear the
    /// active instruction first and use the existing short-lived Scanner status badge.
    /// </summary>
    public void ShowMiniScannerStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var normalized = message.Trim();
        if (string.Equals(normalized, "수락 완료", StringComparison.Ordinal) ||
            normalized.StartsWith("상태 변경으로", StringComparison.Ordinal))
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
