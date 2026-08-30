using System.Windows;
using System.Windows.Threading;
using JunhyunHelper.Core.Scanner;

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
    private readonly ScannerPresentationRetention _presentationRetention = new();
    private readonly object _requestGate = new();
    private MiniScannerWindow? _window;
    private ScannerItemSnapshot? _snapshot;
    private string? _requestedItemId;
    private bool _editMode;
    private bool _disposed;
    private int _visibilityEpoch;

    public MiniScannerOverlayService(ScannerSettingsService settings, IScannerOcrEngine ocr)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(ocr);
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings.SettingsChanged += OnSettingsChanged;
    }

    public bool IsEditing => Invoke(() => _editMode);

    internal static bool CanOpenConfirmedItem(bool preview, bool scannerEnabled, bool foregroundTarkov) =>
        preview || !scannerEnabled || foregroundTarkov;

    public void Show(ScannerItemSnapshot snapshot, bool preview = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);

        int epoch;
        lock (_requestGate)
        {
            _presentationRetention.Confirm(snapshot.ItemId);
            if (!string.Equals(_requestedItemId, snapshot.ItemId, StringComparison.Ordinal))
            {
                _requestedItemId = snapshot.ItemId;
                epoch = Interlocked.Increment(ref _visibilityEpoch);
            }
            else
            {
                epoch = Volatile.Read(ref _visibilityEpoch);
            }
        }

        // Display-test and explicit preview remain deterministic development/test tools.
        if (preview || !_settings.Current.Enabled)
        {
            ShowVerified(snapshot, epoch);
            return;
        }

        // Once an Item has already been presented, an authoritative later Scanner match
        // updates it directly. Recognition owns liveness; Mini Scanner must not run an
        // independent OCR gate for every presentation refresh.
        var hasVisibleSnapshot = Invoke(() => _snapshot is not null && _window?.IsVisible == true);
        if (hasVisibleSnapshot)
        {
            ShowVerified(snapshot, epoch);
            return;
        }

        // A hidden Mini Scanner still opens only while the real Tarkov client owns the
        // foreground. The Scanner has already verified the detail window and Item ID at
        // this point, so auxiliary top-band inventory OCR is weaker evidence and must not
        // veto a confirmed result. This fixes successful Scanner recognition being logged
        // while the Mini Scanner remained hidden because that second OCR missed raid UI.
        var foregroundTarkov = ScannerInventoryContextDetector.IsForegroundTarkovClient();
        if (!CanOpenConfirmedItem(preview, _settings.Current.Enabled, foregroundTarkov))
        {
            ScannerDiagnosticLog.Write(
                "mini-scanner-show-blocked",
                ScannerCaptureMode.TarkovWindow,
                ("reason", "TARKOV_NOT_FOREGROUND"),
                ("itemId", snapshot.ItemId));
            return;
        }

        ShowVerified(snapshot, epoch);
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
    /// Hard non-item state. Scanner stop/suspend/unavailable states use this path and
    /// clear the Mini Scanner immediately rather than consuming the transient miss budget.
    /// </summary>
    public void ShowStandby(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Hide();
    }

    /// <summary>
    /// Progress-only state such as candidate stabilization or OCR work. The Scanner page
    /// may report the status, while the last confirmed Mini Scanner item stays untouched.
    /// </summary>
    public void HoldStandby(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }

    /// <summary>
    /// Shows a short user-facing confirmation without replacing Scanner evidence or the
    /// current item snapshot. When no item is currently visible, the Mini Scanner opens as
    /// a status-only card and closes itself after the transient message expires.
    /// </summary>
    public void ShowTransientStatus(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Invoke(() =>
        {
            if (_disposed)
                return;
            EnsureWindow().ShowTransientStatus(message, _settings.Current, _snapshot is not null);
        });
    }

    /// <summary>
    /// Records one completed continuous-recognition miss. The last confirmed item remains
    /// visible through two misses and is hidden on the third consecutive miss. Any later
    /// successful Show call resets this budget, including an immediate switch to a new item.
    /// </summary>
    public void ReportTransientMiss(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        bool shouldHide;
        lock (_requestGate)
            shouldHide = _presentationRetention.ReportMiss();

        if (shouldHide)
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
                _presentationRetention.Confirm(preview.ItemId);
                _requestedItemId = preview.ItemId;
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
        lock (_requestGate)
        {
            _presentationRetention.Reset();
            _requestedItemId = null;
            Interlocked.Increment(ref _visibilityEpoch);
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
