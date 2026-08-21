using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Independent lifecycle for the Scanner overlay. It does not share a Window or state
/// with the existing MiniMap overlay. All Window access is marshalled to the WPF UI
/// dispatcher because future detector/OCR implementations run outside that thread.
/// </summary>
public sealed class MiniScannerOverlayService : IDisposable
{
    private readonly ScannerSettingsService _settings;
    private readonly Dispatcher _dispatcher;
    private MiniScannerWindow? _window;
    private ScannerItemSnapshot? _snapshot;
    private bool _editMode;
    private bool _disposed;

    public MiniScannerOverlayService(ScannerSettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings.SettingsChanged += OnSettingsChanged;
    }

    public bool IsEditing => Invoke(() => _editMode);

    public void Show(ScannerItemSnapshot snapshot, bool preview = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(snapshot);
        _ = preview;

        Invoke(() =>
        {
            _snapshot = snapshot;
            var window = EnsureWindow();
            window.Render(snapshot, _settings.Current, _editMode);
        });
    }

    public void Hide()
    {
        if (_disposed)
            return;

        Invoke(() =>
        {
            _snapshot = null;
            if (_editMode)
                return;
            _window?.Hide();
        });
    }

    public void BeginPositionEdit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Invoke(() =>
        {
            _editMode = true;
            var preview = _snapshot ?? CreatePositionPreview();
            _snapshot ??= preview;
            EnsureWindow().Render(preview, _settings.Current, editMode: true);
        });
    }

    public void EndPositionEdit(bool keepVisible)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Invoke(() =>
        {
            if (_window is null)
            {
                _editMode = false;
                return;
            }

            var position = _window.GetPosition();
            _editMode = false;
            _window.SetEditMode(false);
            _settings.Update(settings =>
            {
                // WPF screen coordinates are device-independent pixels and may be
                // negative on monitors located left/above the primary monitor.
                settings.PositionX = position.X;
                settings.PositionY = position.Y;
            });

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

            // Recreate only the lightweight Scanner overlay so default placement is
            // applied with a fresh SizeToContent measurement. MiniMap remains untouched.
            var snapshot = _snapshot;
            var editMode = _editMode;
            _window.Close();
            _window = null;
            if (snapshot is not null)
                EnsureWindow().Render(snapshot, _settings.Current, editMode);
        });
    }

    private MiniScannerWindow EnsureWindow()
    {
        if (_window is null)
            _window = new MiniScannerWindow();
        return _window;
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
        _settings.SettingsChanged -= OnSettingsChanged;

        Invoke(() =>
        {
            if (_window is null)
                return;
            _window.Close();
            _window = null;
            _snapshot = null;
        });
        GC.SuppressFinalize(this);
    }
}
