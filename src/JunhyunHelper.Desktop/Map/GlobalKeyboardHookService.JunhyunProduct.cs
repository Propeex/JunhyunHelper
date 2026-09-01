using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TarkovHelper.Services;

/// <summary>
/// JunhyunHelper-owned compatibility replacement for the transplanted Tarkov Helper
/// global keyboard hook. Configurable product hotkeys are dispatched by
/// JunhyunMapHotkeyService, while this class preserves the donor service lifecycle expected
/// by the Map/MiniMap runtime. v1.16 removes only the old bare NumPad0..5 direct-floor
/// command: the compatibility hook never consumes or dispatches those keys.
/// </summary>
public sealed class GlobalKeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;

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

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

    public static GlobalKeyboardHookService Instance { get; } = new();

    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hookId;
    private bool _isHooked;
    private bool _isEnabled;

    private GlobalKeyboardHookService()
    {
        _callback = HookCallback;
    }

    /// <summary>
    /// Legacy source compatibility only. JunhyunHelper does not dispatch the donor direct
    /// floor path because product floor movement is owned by configurable up/down hotkeys.
    /// </summary>
    public event Action<int>? FloorKeyPressed { add { } remove { } }

    /// <summary>
    /// Retained as a source-compatible endpoint for LegacyMapProductRuntime. No key is
    /// translated into this event in v1.16, so NumPad0..5 are fully available to normal
    /// configurable hotkeys.
    /// </summary>
    public event Action<int>? DirectFloorSelectionPressed { add { } remove { } }

    // Donor compatibility-only events. Product hotkeys are dispatched by the JunhyunHelper
    // owned hotkey service; these endpoints deliberately never fire from this hook.
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

    // Donor compatibility properties are inert. Product values live in
    // JunhyunMapProductSettingsStore.
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
    /// Shared capture guard used by JunhyunMapHotkeyService while a key editor is active.
    /// The compatibility hook itself never dispatches product actions.
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
        if (!_isHooked)
        {
            JunhyunHelper.Desktop.App.WriteDiagnostic(
                "Failed to install Map compatibility keyboard hook",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }
    }

    private void StopHook()
    {
        if (!_isHooked || _hookId == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _isHooked = false;
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam) =>
        CallNextHookEx(_hookId, code, wParam, lParam);

    public void Dispose()
    {
        StopHook();
        _isEnabled = false;
        GC.SuppressFinalize(this);
    }

    ~GlobalKeyboardHookService()
    {
        StopHook();
    }
}
