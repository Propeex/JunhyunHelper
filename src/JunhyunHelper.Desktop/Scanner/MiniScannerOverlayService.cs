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
    private bool _disposed;
    private int _visibilityEpoch;

    public MiniScannerOverlayService(ScannerSettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings.SettingsChanged += OnSettingsChanged;
    }

    internal static bool CanOpenConfirmedItem(bool scannerEnabled, bool foregroundTarkov) =>
        !scannerEnabled || foregroundTarkov;

    public void Show(ScannerItemSnapshot snapshot)
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

        if (!_settings.Current.Enabled)
        {
            ShowVerified(snapshot, epoch);
            return;
        }

        var hasVisibleSnapshot = Invoke(() => _snapshot is not null && _window?.IsVisible == true);
        if (hasVisibleSnapshot)
        {
            ShowVerified(snapshot, epoch);
            return;
        }

        var foregroundTarkov = ScannerInventoryContextDetector.IsForegroundTarkovClient();
        if (!CanOpenConfirmedItem(_settings.Current.Enabled, foregroundTarkov))
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
            var window = EnsureWindow();
            window.Render(snapshot, _settings.Current);
        });
    }

    public void ShowStandby(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Hide();
    }

    public void HoldStandby(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
    }

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
            _window?.Hide();
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
            {
                _window.Render(_snapshot, settings);
            }
        });
    }


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
