using System.Runtime.InteropServices;
using System.Windows;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Observes the same global MiniMap keys used by the legacy Tarkov-Helper without
/// suppressing the keystroke. The hook is alive only while the MiniMap window is alive.
/// </summary>
internal sealed class LegacyMiniMapHotkeyService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private readonly LowLevelKeyboardProc _callback;
    private readonly Dictionary<int, long> _lastDispatchTicks = new();
    private Action<int>? _onKey;
    private IntPtr _hook;
    private bool _disposed;

    public LegacyMiniMapHotkeyService()
    {
        _callback = HookCallback;
    }

    public bool Start(Action<int> onKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(onKey);
        _onKey = onKey;
        if (_hook != IntPtr.Zero)
            return true;

        _hook = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(null), 0);
        return _hook != IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown))
        {
            var virtualKey = Marshal.ReadInt32(lParam);
            var now = Environment.TickCount64;
            lock (_lastDispatchTicks)
            {
                if (!_lastDispatchTicks.TryGetValue(virtualKey, out var previous) || now - previous >= 80)
                {
                    _lastDispatchTicks[virtualKey] = now;
                    var handler = _onKey;
                    if (handler is not null)
                    {
                        var dispatcher = Application.Current?.Dispatcher;
                        if (dispatcher is not null)
                            _ = dispatcher.BeginInvoke(() => handler(virtualKey));
                    }
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _onKey = null;
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc callback,
        IntPtr module,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hook,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}