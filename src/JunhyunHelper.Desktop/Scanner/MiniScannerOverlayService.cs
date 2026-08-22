using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Independent lifecycle for the Scanner overlay. It does not share a Window or state
/// with the existing MiniMap overlay. All Window access is marshalled to the WPF UI
/// dispatcher because detector/OCR work runs outside that thread.
/// </summary>
public sealed class MiniScannerOverlayService : IDisposable
{
    private readonly ScannerSettingsService _settings;
    private readonly Dispatcher _dispatcher;
    private readonly ScannerInventoryContextDetector _inventoryContext;
    private readonly object _requestGate = new();
    private MiniScannerWindow? _window;
    private ScannerItemSnapshot? _snapshot;
    private ScannerItemSnapshot? _pendingVisibilitySnapshot;
    private CancellationTokenSource? _visibilityProbeCts;
    private string? _requestedItemId;
    private int _pendingVisibilityEpoch;
    private bool _visibilityProbeRunning;
    private bool _editMode;
    private bool _disposed;
    private int _visibilityEpoch;

    public MiniScannerOverlayService(ScannerSettingsService settings, IScannerOcrEngine ocr)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(ocr);
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _inventoryContext = new ScannerInventoryContextDetector(ocr);
        _settings.SettingsChanged += OnSettingsChanged;
    }

    public bool IsEditing => Invoke(() => _editMode);

    public void Show(ScannerItemSnapshot snapshot, bool preview = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);

        CancellationTokenSource? staleProbe = null;
        int epoch;
        lock (_requestGate)
        {
            if (!string.Equals(_requestedItemId, snapshot.ItemId, StringComparison.Ordinal))
            {
                _requestedItemId = snapshot.ItemId;
                epoch = Interlocked.Increment(ref _visibilityEpoch);
                staleProbe = _visibilityProbeCts;
            }
            else
            {
                epoch = Volatile.Read(ref _visibilityEpoch);
            }

            _pendingVisibilitySnapshot = snapshot;
            _pendingVisibilityEpoch = epoch;
        }
        TryCancel(staleProbe);

        // Display-test and explicit preview remain deterministic development/test tools.
        // A real Scanner has Enabled=true and is visually gated to the foreground Tarkov
        // inventory/stash before the overlay is allowed to appear.
        if (preview || !_settings.Current.Enabled)
        {
            CancelVisibilityProbe(clearPending: false);
            ShowVerified(snapshot, epoch);
            return;
        }

        RequestInventoryProbe();
    }

    /// <summary>
    /// At most one inventory/stash OCR probe is active for this overlay. The continuous
    /// Scanner can call Show every 350 ms, but those calls only replace the pending
    /// snapshot while a probe is running instead of queueing more OCR work behind the
    /// serialized WinRT engine.
    /// </summary>
    private void RequestInventoryProbe()
    {
        CancellationTokenSource? cancellation = null;
        lock (_requestGate)
        {
            if (_disposed || _pendingVisibilitySnapshot is null || _visibilityProbeRunning)
                return;

            _visibilityProbeRunning = true;
            cancellation = new CancellationTokenSource();
            _visibilityProbeCts = cancellation;
        }

        _ = RunInventoryProbeAsync(cancellation);
    }

    private async Task RunInventoryProbeAsync(CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        ScannerItemSnapshot? requested;
        int epoch;
        lock (_requestGate)
        {
            requested = _pendingVisibilitySnapshot;
            epoch = _pendingVisibilityEpoch;
        }

        try
        {
            if (requested is null)
                return;

            var allowed = await _inventoryContext.IsInventoryOrStashAsync(token);
            token.ThrowIfCancellationRequested();

            ScannerItemSnapshot? latest;
            lock (_requestGate)
            {
                latest = !_disposed &&
                         epoch == Volatile.Read(ref _visibilityEpoch) &&
                         _pendingVisibilityEpoch == epoch
                    ? _pendingVisibilitySnapshot
                    : null;
            }

            if (latest is null)
                return;

            if (!allowed)
            {
                Invoke(() =>
                {
                    if (_disposed || epoch != Volatile.Read(ref _visibilityEpoch))
                        return;
                    _snapshot = null;
                    if (!_editMode)
                        _window?.Hide();
                });
                return;
            }

            ShowVerified(latest, epoch);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Mini Scanner inventory context detection failed", exception);
            if (!_disposed && epoch == Volatile.Read(ref _visibilityEpoch))
            {
                Invoke(() =>
                {
                    _snapshot = null;
                    if (!_editMode)
                        _window?.Hide();
                });
            }
        }
        finally
        {
            var restart = false;
            lock (_requestGate)
            {
                if (ReferenceEquals(_visibilityProbeCts, cancellation))
                    _visibilityProbeCts = null;
                _visibilityProbeRunning = false;

                // A new item may have arrived while the old probe was being cancelled.
                // Start exactly one replacement probe for that latest epoch.
                restart = !_disposed &&
                          _pendingVisibilitySnapshot is not null &&
                          _pendingVisibilityEpoch == Volatile.Read(ref _visibilityEpoch) &&
                          _pendingVisibilityEpoch != epoch;
            }
            cancellation.Dispose();
            if (restart)
                RequestInventoryProbe();
        }
    }

    private void ShowVerified(ScannerItemSnapshot snapshot, int epoch)
    {
        Invoke(() =>
        {
            if (_disposed || epoch != Volatile.Read(ref _visibilityEpoch))
                return;
            _snapshot = snapshot;
            EnsureWindow().Render(snapshot, _settings.Current, _editMode);
        });
    }

    /// <summary>
    /// Runtime status belongs to the Scanner page/activity log, never to the overlay.
    /// Any non-item state clears the current match and hides Mini Scanner.
    /// </summary>
    public void ShowStandby(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Hide();
    }

    public void Hide()
    {
        if (_disposed)
            return;

        ClearRequestedItem();
        Invoke(() =>
        {
            _snapshot = null;
            if (_editMode)
                return;
            _window?.Hide();
        });
    }

    // Legacy Foundation hooks are retained for developer use, but the product UI no
    // longer exposes an edit mode. The window itself is always directly draggable.
    public void BeginPositionEdit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClearRequestedItem();
        Invoke(() =>
        {
            _editMode = true;
            if (_snapshot is not null)
            {
                EnsureWindow().Render(_snapshot, _settings.Current, editMode: true);
                return;
            }

            var preview = CreatePositionPreview();
            _snapshot = preview;
            lock (_requestGate)
            {
                _requestedItemId = preview.ItemId;
                _pendingVisibilitySnapshot = preview;
                _pendingVisibilityEpoch = Volatile.Read(ref _visibilityEpoch);
            }
            EnsureWindow().Render(preview, _settings.Current, editMode: true);
        });
    }

    public void EndPositionEdit(bool keepVisible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClearRequestedItem();
        Invoke(() =>
        {
            if (_window is null)
            {
                _editMode = false;
                return;
            }

            SavePosition(_window.Left, _window.Top);
            _editMode = false;
            _window.SetEditMode(false);

            if (!keepVisible)
            {
                _snapshot = null;
                _window.Hide();
            }
        });
    }

    public void ResetPosition()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Invoke(() =>
        {
            _settings.ResetPosition();
            if (_window is null || !_window.IsVisible)
                return;

            var snapshot = _snapshot;
            var editMode = _editMode;
            _window.Close();
            _window = null;
            if (snapshot is not null)
                EnsureWindow().Render(snapshot, _settings.Current, editMode);
        });
    }

    private void ClearRequestedItem()
    {
        CancellationTokenSource? cancellation;
        lock (_requestGate)
        {
            _requestedItemId = null;
            _pendingVisibilitySnapshot = null;
            _pendingVisibilityEpoch = Interlocked.Increment(ref _visibilityEpoch);
            cancellation = _visibilityProbeCts;
        }
        TryCancel(cancellation);
    }

    private void CancelVisibilityProbe(bool clearPending)
    {
        CancellationTokenSource? cancellation;
        lock (_requestGate)
        {
            if (clearPending)
                _pendingVisibilitySnapshot = null;
            cancellation = _visibilityProbeCts;
        }
        TryCancel(cancellation);
    }

    private static void TryCancel(CancellationTokenSource? cancellation)
    {
        if (cancellation is null)
            return;
        try
        {
            cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private MiniScannerWindow EnsureWindow()
    {
        if (_window is not null)
            return _window;

        _window = new MiniScannerWindow();
        _window.PositionCommitted += OnPositionCommitted;
        return _window;
    }

    private void OnPositionCommitted(double x, double y) => SavePosition(x, y);

    private void SavePosition(double x, double y)
    {
        _settings.Update(settings =>
        {
            // Negative WPF coordinates are valid for monitors left/above primary.
            settings.PositionX = x;
            settings.PositionY = y;
        });
    }

    private void OnSettingsChanged(ScannerDisplaySettings settings)
    {
        if (_disposed || _dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return;

        Invoke(() =>
        {
            if (_window is null)
                return;

            _window.ApplySettings(settings);
            if (_snapshot is not null && _window.IsVisible)
                _window.Render(_snapshot, settings, _editMode);
        });
    }

    private static ScannerItemSnapshot CreatePositionPreview() => new(
        "preview",
        "Mini Scanner 위치",
        null,
        42000,
        57000,
        21000,
        28500,
        2,
        3);

    private void Invoke(Action action)
    {
        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return;
        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }
        _dispatcher.Invoke(action);
    }

    private T Invoke<T>(Func<T> action)
    {
        if (_dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
            return default!;
        return _dispatcher.CheckAccess() ? action() : _dispatcher.Invoke(action);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ClearRequestedItem();
        _settings.SettingsChanged -= OnSettingsChanged;

        Invoke(() =>
        {
            if (_window is null)
                return;
            _window.PositionCommitted -= OnPositionCommitted;
            _window.Close();
            _window = null;
            _snapshot = null;
        });
        GC.SuppressFinalize(this);
    }
}
