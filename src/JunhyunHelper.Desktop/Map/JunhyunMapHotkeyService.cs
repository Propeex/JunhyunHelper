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
    private readonly OverlayMiniMapService _overlay = OverlayMiniMapService.Instance;
    private readonly LowLevelKeyboardProc _callback;
    private readonly HashSet<int> _pressed = [];
    private IntPtr _hook;
    private bool _disposed;

    public JunhyunMapHotkeyService(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _callback = HookCallback;

        _overlay.OverlayVisibilityChanged += Overlay_VisibilityChanged;
        _overlay.SettingsChanged += Overlay_SettingsChanged;
        SuppressLegacyDirectMapHotkeys();

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

        var action = GetProductActionForHotkey(virtualKey);
        if (action is null)
            return CallNextHookEx(_hook, code, wParam, lParam);

        var repeatable = action is OverlayMiniMapHotkeyAction.ZoomIn or OverlayMiniMapHotkeyAction.ZoomOut;
        if (!firstPress && !repeatable)
            return CallNextHookEx(_hook, code, wParam, lParam);

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(async () =>
            await ExecuteAsync(action.Value));
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

    private async Task ExecuteAsync(OverlayMiniMapHotkeyAction action)
    {
        SuppressLegacyDirectMapHotkeys();

        switch (action)
        {
            case OverlayMiniMapHotkeyAction.ToggleOverlay:
                _overlay.ToggleOverlay();
                break;
            case OverlayMiniMapHotkeyAction.ZoomIn:
                _page.JunhyunZoomIn();
                _overlay.ZoomIn();
                break;
            case OverlayMiniMapHotkeyAction.ZoomOut:
                _page.JunhyunZoomOut();
                _overlay.ZoomOut();
                break;
            case OverlayMiniMapHotkeyAction.FloorUp:
                await _page.JunhyunFloorUpAsync();
                _overlay.MoveFloorUp();
                break;
            case OverlayMiniMapHotkeyAction.FloorDown:
                await _page.JunhyunFloorDownAsync();
                _overlay.MoveFloorDown();
                break;
            case OverlayMiniMapHotkeyAction.SizeIncrease:
                if (_overlay.IsOverlayVisible)
                    JunhyunMiniMapProductRegistry.IncreaseSize();
                break;
            case OverlayMiniMapHotkeyAction.SizeDecrease:
                if (_overlay.IsOverlayVisible)
                    JunhyunMiniMapProductRegistry.DecreaseSize();
                break;
        }
    }

    private void Overlay_VisibilityChanged(bool visible)
    {
        if (_disposed)
            return;
        SuppressLegacyDirectMapHotkeys();
    }

    private void Overlay_SettingsChanged(OverlayMiniMapSettings settings)
    {
        if (_disposed)
            return;
        SuppressLegacyDirectMapHotkeys();
    }

    internal static void SuppressLegacyDirectMapHotkeys()
    {
        var hook = GlobalKeyboardHookService.Instance;
        hook.ZoomInKey = 0;
        hook.ZoomOutKey = 0;
        hook.FloorUpKey = 0;
        hook.FloorDownKey = 0;
        hook.ResumeAutoFloorKey = 0;
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

        _overlay.OverlayVisibilityChanged -= Overlay_VisibilityChanged;
        _overlay.SettingsChanged -= Overlay_SettingsChanged;
        SuppressLegacyDirectMapHotkeys();

        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
