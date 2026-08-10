using System.Runtime.InteropServices;
using SysDiag = System.Diagnostics;

namespace TarkovHelper.Services;

/// <summary>
/// JunhyunHelper-owned compatibility replacement for the transplanted Tarkov Helper
/// global keyboard hook. Product overlay hotkeys are dispatched by JunhyunHelper's
/// own runtime. This class exists only for the original Map page's direct NumPad
/// floor-selection contract and for source compatibility with the overlay service.
///
/// Deliberately absent from the old implementation:
/// - hidden S/S+D/D/O command sequence
/// - hidden Ctrl+L overlay-settings shortcut
/// - legacy direct zoom/floor/opacity/view-mode dispatch
/// - keyboard/foreground-process log file
/// - broad process-name substring matching
/// </summary>
public sealed class GlobalKeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyUp = 0x0105;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc callback,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

    public static GlobalKeyboardHookService Instance { get; } = new();

    private readonly LowLevelKeyboardProc _callback;
    private readonly HashSet<int> _pressedKeys = [];
    private IntPtr _hookId;
    private bool _isHooked;
    private bool _isEnabled;

    private GlobalKeyboardHookService()
    {
        _callback = HookCallback;
    }

    /// <summary>
    /// Direct floor-selection event retained for the original Map page.
    /// NumPad0..5 map to floor indexes 0..5.
    /// </summary>
    public event Action<int>? FloorKeyPressed;

    // Compatibility-only events. The transplanted overlay service may attach handlers,
    // but JunhyunHelper never lets this legacy hook dispatch those product actions.
    public event Action? OverlayTogglePressed { add { } remove { } }
    public event Action? OverlaySettingsPressed { add { } remove { } }
    public event Action? OverlayZoomInPressed { add { } remove { } }
    public event Action? OverlayZoomOutPressed { add { } remove { } }
    public event Action? OverlayFloorUpPressed { add { } remove { } }
    public event Action? OverlayFloorDownPressed { add { } remove { } }
    public event Action? OverlayOpacityIncreasePressed { add { } remove { } }
    public event Action? OverlayOpacityDecreasePressed { add { } remove { } }
    public event Action? OverlayCenterPlayerPressed { add { } remove { } }
    public event Action? OverlayToggleViewModePressed { add { } remove { } }
    public event Action? OverlayToggleClickThroughPressed { add { } remove { } }
    public event Action? OverlayResetViewPressed { add { } remove { } }
    public event Action? OverlayResumeAutoFloorPressed { add { } remove { } }

    // Compatibility properties are intentionally inert. Product hotkeys are read from
    // JunhyunMapProductSettingsStore by the JunhyunHelper-owned dispatcher.
    public int ZoomInKey { get; set; }
    public int ZoomOutKey { get; set; }
    public int FloorUpKey { get; set; }
    public int FloorDownKey { get; set; }
    public int OpacityIncreaseKey { get; set; }
    public int OpacityDecreaseKey { get; set; }
    public int CenterPlayerKey { get; set; }
    public int ToggleViewModeKey { get; set; }
    public int ToggleClickThroughKey { get; set; }
    public int ResetViewKey { get; set; }
    public int ResumeAutoFloorKey { get; set; }

    /// <summary>
    /// Used while a key editor is capturing input so the same press is not treated as
    /// a direct floor-selection action.
    /// </summary>
    public bool OverlayHotkeysSuppressed { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;

            _isEnabled = value;
            if (value)
                StartHook();
            else
                StopHook();
        }
    }

    private void StartHook()
    {
        if (_isHooked)
            return;

        _hookId = SetWindowsHookEx(WhKeyboardLl, _callback, IntPtr.Zero, 0);
        _isHooked = _hookId != IntPtr.Zero;
    }

    private void StopHook()
    {
        _pressedKeys.Clear();
        if (!_isHooked || _hookId == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _isHooked = false;
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0)
            return CallNextHookEx(_hookId, code, wParam, lParam);

        var message = wParam.ToInt32();
        var virtualKey = Marshal.ReadInt32(lParam);

        if (message is WmKeyUp or WmSysKeyUp)
        {
            _pressedKeys.Remove(virtualKey);
            return CallNextHookEx(_hookId, code, wParam, lParam);
        }

        if (message is not WmKeyDown and not WmSysKeyDown)
            return CallNextHookEx(_hookId, code, wParam, lParam);

        var firstPress = _pressedKeys.Add(virtualKey);
        if (!firstPress || OverlayHotkeysSuppressed || !IsAllowedForeground())
            return CallNextHookEx(_hookId, code, wParam, lParam);

        var floorIndex = GetFloorIndex(virtualKey);
        if (floorIndex.HasValue)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                FloorKeyPressed?.Invoke(floorIndex.Value);
            });
        }

        return CallNextHookEx(_hookId, code, wParam, lParam);
    }

    private static bool IsAllowedForeground()
    {
        try
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero)
                return false;

            GetWindowThreadProcessId(window, out var processId);
            if (processId == 0)
                return false;

            using var process = SysDiag.Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            return name.Equals("EscapeFromTarkov", StringComparison.OrdinalIgnoreCase)
                || name.Equals("EscapeFromTarkov_BE", StringComparison.OrdinalIgnoreCase)
                || name.Equals("JunhyunHelper", StringComparison.OrdinalIgnoreCase)
                || name.Equals("TarkovHelper", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static int? GetFloorIndex(int virtualKey) => virtualKey switch
    {
        0x60 => 0,
        0x61 => 1,
        0x62 => 2,
        0x63 => 3,
        0x64 => 4,
        0x65 => 5,
        _ => null,
    };

    public void Dispose()
    {
        StopHook();
        GC.SuppressFinalize(this);
    }

    ~GlobalKeyboardHookService()
    {
        StopHook();
    }
}
