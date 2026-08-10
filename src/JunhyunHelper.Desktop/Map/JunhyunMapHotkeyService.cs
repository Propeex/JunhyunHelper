using System.Diagnostics;
using System.Runtime.InteropServices;
using TarkovHelper.Models.Map;
using TarkovHelper.Services;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// JunhyunHelper-owned Map keyboard hook. Configured Map actions must work while
/// Escape from Tarkov or JunhyunHelper has focus, regardless of whether MiniMap is
/// currently visible. The transplanted hook remains only for its direct NumPad floor
/// selection compatibility path.
/// </summary>
public sealed class JunhyunMapHotkeyService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyUp = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc callback,
        IntPtr module,
        uint threadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly LowLevelKeyboardProc _callback;
    private readonly HashSet<int> _pressed = [];
    private IntPtr _hook;
    private bool _disposed;

    public JunhyunMapHotkeyService(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _callback = HookCallback;
        _hook = SetWindowsHookEx(WhKeyboardLl, _callback, IntPtr.Zero, 0);
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0)
            return CallNextHookEx(_hook, code, wParam, lParam);

        var message = wParam.ToInt32();
        var virtualKey = Marshal.ReadInt32(lParam);

        if (message is WmKeyUp or WmSysKeyUp)
        {
            _pressed.Remove(virtualKey);
            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        if (message is not WmKeyDown and not WmSysKeyDown)
            return CallNextHookEx(_hook, code, wParam, lParam);

        var firstPress = _pressed.Add(virtualKey);
        if (GlobalKeyboardHookService.Instance.OverlayHotkeysSuppressed ||
            !IsTarkovOrHelperForeground())
        {
            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        var productSettings = JunhyunMapProductSettingsStore.Instance;
        if (virtualKey != 0 && virtualKey == productSettings.TemporaryHideKey)
        {
            if (firstPress)
            {
                var seconds = productSettings.TemporaryHideSeconds;
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    JunhyunMiniMapProductRegistry.TemporarilyHide(seconds));
            }
            return CallNextHookEx(_hook, code, wParam, lParam);
        }

        // Resolve against the JunhyunHelper-owned persisted values directly. The legacy
        // overlay settings object is only the fallback for users who have never saved a
        // product value. This prevents late legacy initialization from making floor keys
        // appear configured in the UI while the runtime dispatcher reads another value.
        var action = GetProductActionForHotkey(virtualKey);
        if (action is null)
            return CallNextHookEx(_hook, code, wParam, lParam);

        var repeatable = action is OverlayMiniMapHotkeyAction.ZoomIn or OverlayMiniMapHotkeyAction.ZoomOut;
        if (!firstPress && !repeatable)
            return CallNextHookEx(_hook, code, wParam, lParam);

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() => Execute(action.Value));
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static OverlayMiniMapHotkeyAction? GetProductActionForHotkey(int virtualKey)
    {
        if (virtualKey == 0)
            return null;

        var overlaySettings = OverlayMiniMapService.Instance.Settings;
        var productSettings = JunhyunMapProductSettingsStore.Instance;
        foreach (var action in Enum.GetValues<OverlayMiniMapHotkeyAction>())
        {
            var configuredKey = productSettings.GetHotkey(action, overlaySettings.GetHotkey(action));
            if (configuredKey == virtualKey)
                return action;
        }

        return null;
    }

    private void Execute(OverlayMiniMapHotkeyAction action)
    {
        switch (action)
        {
            case OverlayMiniMapHotkeyAction.ToggleOverlay:
                OverlayMiniMapService.Instance.ToggleOverlay();
                break;
            case OverlayMiniMapHotkeyAction.ZoomIn:
                _page.JunhyunZoomIn();
                JunhyunMiniMapProductRegistry.ZoomIn();
                break;
            case OverlayMiniMapHotkeyAction.ZoomOut:
                _page.JunhyunZoomOut();
                JunhyunMiniMapProductRegistry.ZoomOut();
                break;
            case OverlayMiniMapHotkeyAction.FloorUp:
                _page.JunhyunFloorUp();
                JunhyunMiniMapProductRegistry.MoveFloorUp();
                break;
            case OverlayMiniMapHotkeyAction.FloorDown:
                _page.JunhyunFloorDown();
                JunhyunMiniMapProductRegistry.MoveFloorDown();
                break;
            case OverlayMiniMapHotkeyAction.SizeIncrease:
                if (OverlayMiniMapService.Instance.IsOverlayVisible)
                    JunhyunMiniMapProductRegistry.IncreaseSize();
                break;
            case OverlayMiniMapHotkeyAction.SizeDecrease:
                if (OverlayMiniMapService.Instance.IsOverlayVisible)
                    JunhyunMiniMapProductRegistry.DecreaseSize();
                break;
        }
    }

    private static bool IsTarkovOrHelperForeground()
    {
        try
        {
            var window = GetForegroundWindow();
            if (window == IntPtr.Zero)
                return false;

            GetWindowThreadProcessId(window, out var processId);
            if (processId == 0)
                return false;

            using var process = Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            return name.Equals("EscapeFromTarkov", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("EscapeFromTarkov_BE", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("JunhyunHelper", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("TarkovHelper", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pressed.Clear();

        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
