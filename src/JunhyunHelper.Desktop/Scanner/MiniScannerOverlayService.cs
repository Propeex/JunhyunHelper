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
    private string? _farmingGuideInstruction;
    private bool _farmingGuideQuantityPending;
    private bool _editMode;
    private bool _disposed;
    private int _visibilityEpoch;
    private int _temporaryPreviewEpoch;

    public MiniScannerOverlayService(ScannerSettingsService settings, IScannerOcrEngine ocr)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(ocr);
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings.SettingsChanged += OnSettingsChanged;
    }

    public bool IsEditing => Invoke(() => _editMode);
    public bool IsFarmingGuideQuantityPending => Invoke(() => _farmingGuideQuantityPending);
    public event Action<int>? FarmingGuideQuantitySubmitted;

    internal static bool CanOpenConfirmedItem(bool preview, bool scannerEnabled, bool foregroundTarkov) =>
        preview || !scannerEnabled || foregroundTarkov;

    public void Show(ScannerItemSnapshot snapshot, bool preview = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!preview)
            Interlocked.Increment(ref _temporaryPreviewEpoch);

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

        if (preview || !_settings.Current.Enabled)
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

    public void ShowTemporaryPreview(ScannerItemSnapshot snapshot, TimeSpan lifetime)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var previewEpoch = Interlocked.Increment(ref _temporaryPreviewEpoch);
        Show(snapshot, preview: true);
        var visibilityEpoch = Volatile.Read(ref _visibilityEpoch);
        _ = HideTemporaryPreviewAsync(snapshot.ItemId, previewEpoch, visibilityEpoch, lifetime);
    }

    private async Task HideTemporaryPreviewAsync(
        string itemId,
        int previewEpoch,
        int visibilityEpoch,
        TimeSpan lifetime)
    {
        try
        {
            await Task.Delay(lifetime).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (_disposed || previewEpoch != Volatile.Read(ref _temporaryPreviewEpoch))
            return;

        bool isSamePresentation;
        lock (_requestGate)
        {
            isSamePresentation =
                visibilityEpoch == Volatile.Read(ref _visibilityEpoch) &&
                string.Equals(_requestedItemId, itemId, StringComparison.Ordinal);
        }
        if (isSamePresentation)
            Hide();
    }

    private void ShowVerified(ScannerItemSnapshot snapshot, int epoch)
    {
        Invoke(() =>
        {
            if (_disposed || epoch != Volatile.Read(ref _visibilityEpoch))
                return;
            _snapshot = snapshot;
            var window = EnsureWindow();
            window.Render(snapshot, _settings.Current, _editMode);
            window.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
            if (_farmingGuideQuantityPending)
                window.BeginFarmingGuideQuantityInput(_settings.Current);
        });
    }

    public void SetFarmingGuideInstruction(string? instruction)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var normalized = string.IsNullOrWhiteSpace(instruction) ? null : instruction.Trim();
        Invoke(() =>
        {
            if (_disposed)
                return;
            _farmingGuideInstruction = normalized;
            if (normalized is not null)
                CancelFarmingGuideQuantityInputCore();
            _window?.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
        });
    }

    public void RequestFarmingGuideQuantityInput()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Invoke(() =>
        {
            if (_disposed || _snapshot is null)
                return;
            _farmingGuideInstruction = null;
            _farmingGuideQuantityPending = true;
            var window = EnsureWindow();
            window.SetFarmingGuideInstruction(null, _settings.Current);
            window.BeginFarmingGuideQuantityInput(_settings.Current);
        });
    }

    public void CancelFarmingGuideQuantityInput()
    {
        if (_disposed)
            return;
        Invoke(CancelFarmingGuideQuantityInputCore);
    }

    private void CancelFarmingGuideQuantityInputCore()
    {
        _farmingGuideQuantityPending = false;
        _window?.CancelFarmingGuideQuantityInput(_settings.Current);
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
            CancelFarmingGuideQuantityInputCore();
            _snapshot = null;
            if (_editMode)
                return;
            _window?.Hide();
        });
    }

    public void BeginPositionEdit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClearRequestedItem();
        Invoke(() =>
        {
            CancelFarmingGuideQuantityInputCore();
            _editMode = true;
            if (_snapshot is not null)
            {
                var existingWindow = EnsureWindow();
                existingWindow.Render(_snapshot, _settings.Current, editMode: true);
                existingWindow.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
                return;
            }

            var preview = CreatePositionPreview();
            _snapshot = preview;
            lock (_requestGate)
            {
                _presentationRetention.Confirm(preview.ItemId);
                _requestedItemId = preview.ItemId;
            }
            var window = EnsureWindow();
            window.Render(preview, _settings.Current, editMode: true);
            window.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
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
            _window.FarmingGuideQuantitySubmitted -= OnFarmingGuideQuantitySubmitted;
            _window.Close();
            _window = null;
            if (snapshot is not null)
            {
                var window = EnsureWindow();
                window.Render(snapshot, _settings.Current, editMode);
                window.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
                if (_farmingGuideQuantityPending)
                    window.BeginFarmingGuideQuantityInput(_settings.Current);
            }
        });
    }

    private void ClearRequestedItem()
    {
        Interlocked.Increment(ref _temporaryPreviewEpoch);
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
        _window.FarmingGuideQuantitySubmitted += OnFarmingGuideQuantitySubmitted;
        _window.SetFarmingGuideInstruction(_farmingGuideInstruction, _settings.Current);
        return _window;
    }

    private void OnFarmingGuideQuantitySubmitted(int quantity)
    {
        _farmingGuideQuantityPending = false;
        FarmingGuideQuantitySubmitted?.Invoke(quantity);
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
                _window.Render(_snapshot, settings, _editMode);
                _window.SetFarmingGuideInstruction(_farmingGuideInstruction, settings);
                if (_farmingGuideQuantityPending)
                    _window.BeginFarmingGuideQuantityInput(settings);
            }
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
            _window.FarmingGuideQuantitySubmitted -= OnFarmingGuideQuantitySubmitted;
            _window.Close();
            _window = null;
            _snapshot = null;
            _farmingGuideInstruction = null;
            _farmingGuideQuantityPending = false;
        });
        GC.SuppressFinalize(this);
    }
}
