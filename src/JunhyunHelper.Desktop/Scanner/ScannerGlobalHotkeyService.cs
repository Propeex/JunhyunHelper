using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace JunhyunHelper.Desktop.Scanner;

internal sealed class ScannerGlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;

    private readonly int _hotkeyId;
    private readonly string _actionLabel;
    private Window? _window;
    private HwndSource? _source;
    private IntPtr _handle;
    private ScannerHotkeyGesture? _gesture;
    private Func<Task>? _handler;
    private bool _registered;
    private bool _disposed;
    private int _handlerRunning;

    public ScannerGlobalHotkeyService(int hotkeyId, string actionLabel)
    {
        _hotkeyId = hotkeyId;
        _actionLabel = string.IsNullOrWhiteSpace(actionLabel) ? "Scanner" : actionLabel.Trim();
        StatusText = $"{_actionLabel} 단축키가 아직 초기화되지 않았습니다.";
    }

    public event Action<string>? RegistrationChanged;
    public event Action? Disposed;

    public string StatusText { get; private set; }

    public void Attach(Window window, ScannerHotkeyGesture? gesture, Func<Task> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(handler);

        if (!ReferenceEquals(_window, window))
        {
            DetachWindow();
            _window = window;
            _window.SourceInitialized += Window_SourceInitialized;
            _window.Closed += Window_Closed;
        }
        _handler = handler;
        _gesture = gesture;

        if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
            InitializeSource();
    }

    public void UpdateGesture(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _gesture = gesture;
        RegisterCurrent();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) => InitializeSource();

    private void Window_Closed(object? sender, EventArgs e) => DetachWindow();

    private void InitializeSource()
    {
        if (_window is null)
            return;
        var handle = new WindowInteropHelper(_window).Handle;
        if (handle == IntPtr.Zero)
            return;

        if (_source is null || _handle != handle)
        {
            Unregister();
            _source?.RemoveHook(WndProc);
            _handle = handle;
            _source = HwndSource.FromHwnd(handle);
            _source?.AddHook(WndProc);
        }
        RegisterCurrent();
    }

    private void RegisterCurrent()
    {
        Unregister();
        if (_handle == IntPtr.Zero)
            return;
        if (_gesture is not { } gesture)
        {
            SetStatus($"{_actionLabel} 단축키 사용 안 함");
            return;
        }

        var modifiers = ModNoRepeat;
        if (gesture.Control)
            modifiers |= ModControl;
        if (gesture.Alt)
            modifiers |= ModAlt;
        if (gesture.Shift)
            modifiers |= ModShift;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);
        if (virtualKey == 0)
        {
            SetStatus($"{_actionLabel} 단축키가 유효하지 않습니다.");
            return;
        }

        _registered = RegisterHotKey(_handle, _hotkeyId, modifiers, virtualKey);
        SetStatus(_registered
            ? $"{_actionLabel} 단축키: {gesture}"
            : $"{_actionLabel}: {gesture} 단축키를 등록하지 못했습니다. 다른 프로그램에서 사용 중일 수 있습니다.");
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmHotkey || wParam.ToInt32() != _hotkeyId)
            return IntPtr.Zero;

        handled = true;
        if (Interlocked.Exchange(ref _handlerRunning, 1) != 0)
            return IntPtr.Zero;

        _ = InvokeHandlerAsync();
        return IntPtr.Zero;
    }

    private async Task InvokeHandlerAsync()
    {
        try
        {
            if (_handler is not null)
                await _handler();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic($"Scanner hotkey handler failed: {_actionLabel}", exception);
        }
        finally
        {
            Volatile.Write(ref _handlerRunning, 0);
        }
    }

    private void SetStatus(string text)
    {
        StatusText = text;
        RegistrationChanged?.Invoke(text);
    }

    private void Unregister()
    {
        if (!_registered || _handle == IntPtr.Zero)
            return;
        _ = UnregisterHotKey(_handle, _hotkeyId);
        _registered = false;
    }

    private void DetachWindow()
    {
        Unregister();
        if (_source is not null)
            _source.RemoveHook(WndProc);
        _source = null;
        _handle = IntPtr.Zero;
        if (_window is not null)
        {
            _window.SourceInitialized -= Window_SourceInitialized;
            _window.Closed -= Window_Closed;
        }
        _window = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        DetachWindow();
        _handler = null;
        var disposed = Disposed;
        Disposed = null;
        disposed?.Invoke();
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

public readonly record struct ScannerHotkeyGesture(bool Control, bool Alt, bool Shift, Key Key)
{
    public static ScannerHotkeyGesture DefaultOneShotTarkov { get; } = new(true, false, true, Key.F10);
    public static ScannerHotkeyGesture DefaultOneShotTest { get; } = new(true, false, true, Key.F11);
    public static ScannerHotkeyGesture DefaultScannerToggle { get; } = new(true, false, true, Key.F12);

    public static ScannerHotkeyGesture Default => DefaultOneShotTarkov;

    public static bool TryParse(string? value, out ScannerHotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var control = false;
        var alt = false;
        var shift = false;
        Key? key = null;
        foreach (var raw in value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || raw.Equals("Control", StringComparison.OrdinalIgnoreCase))
                control = true;
            else if (raw.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                alt = true;
            else if (raw.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                shift = true;
            else if (Enum.TryParse<Key>(raw, true, out var parsed) && !IsModifierKey(parsed))
                key = parsed;
            else
                return false;
        }

        if (key is null)
            return false;
        gesture = new ScannerHotkeyGesture(control, alt, shift, key.Value);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Control)
            parts.Add("Ctrl");
        if (Alt)
            parts.Add("Alt");
        if (Shift)
            parts.Add("Shift");
        parts.Add(Key.ToString());
        return string.Join('+', parts);
    }

    public static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
}
