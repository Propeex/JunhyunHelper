using System.ComponentModel;
using System.Runtime.InteropServices;
using SysDiag = System.Diagnostics;

namespace TarkovHelper.Services;

/// <summary>
/// JunhyunHelper-owned compatibility replacement for the transplanted Tarkov Helper
/// global keyboard hook. Product overlay hotkeys are dispatched by JunhyunHelper's
/// own runtime. v1.16 preserves the established donor-compatible hook lifecycle while
/// disabling only the old bare-NumPad direct floor-selection mapping.
/// </summary>
public sealed class GlobalKeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyUp = 0x0105;

    private static readonly int[] ModifierVirtualKeys =
    [
        0x10, 0x11, 0x12,
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5,
        0x5B, 0x5C,
    ];

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

    public event Action<int>? FloorKeyPressed { add { } remove { } }
    public event Action<int>? DirectFloorSelectionPressed;

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
        if (!_isHooked)
        {
            JunhyunHelper.Desktop.App.WriteDiagnostic(
                "Failed to install direct Map floor-selection keyboard hook",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }
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

        var floorIndex = HasModifierPressed() ? null : GetFloorIndex(virtualKey);
        if (floorIndex.HasValue)
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                DirectFloorSelectionPressed?.Invoke(floorIndex.Value);
            });
        }

        return CallNextHookEx(_hookId, code, wParam, lParam);
    }

    private bool HasModifierPressed() => ModifierVirtualKeys.Any(_pressedKeys.Contains);

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
                || name.Equals("준현 헬퍼", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Junhyun Helper", StringComparison.OrdinalIgnoreCase)
                || name.Equals("JunhyunHelper", StringComparison.OrdinalIgnoreCase)
                || name.Equals("TarkovHelper", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // v1.16: bare NumPad0..5 no longer own direct floor-selection actions. Keep the
    // established hook lifecycle intact so Map/MiniMap donor behavior remains unchanged.
    private static int? GetFloorIndex(int virtualKey) => null;

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
