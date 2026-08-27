using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using JunhyunHelper.Core.Hotkeys;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Scanner global-hotkey registration facade. The shared broker expands each configured
/// gesture across compatible Ctrl/Alt/Shift supersets and assigns every concrete Windows
/// registration to the most specific configured Scanner gesture. This preserves the
/// WM_HOTKEY/UI-thread execution model while allowing harmless extra modifiers.
/// </summary>
internal sealed class ScannerGlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const int BrokerHotkeyIdBase = 0x5200;

    private readonly int _hotkeyId;
    private readonly string _actionLabel;
    private Window? _window;
    private ScannerHotkeyGesture? _gesture;
    private Func<Task>? _handler;
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

    private ScannerHotkeyGesture? Gesture => _gesture;
    private int StableOrder => _hotkeyId;

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

        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
            Broker.Attach(this, window, handle);
        else
            SetStatus(gesture is null
                ? $"{_actionLabel} 단축키 사용 안 함"
                : $"{_actionLabel} 단축키 초기화 중: {gesture}");
    }

    public void UpdateGesture(ScannerHotkeyGesture? gesture)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _gesture = gesture;
        if (_window is not null && new WindowInteropHelper(_window).Handle != IntPtr.Zero)
            Broker.Refresh(this);
        else
            SetStatus(gesture is null
                ? $"{_actionLabel} 단축키 사용 안 함"
                : $"{_actionLabel} 단축키 초기화 중: {gesture}");
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        if (_window is null)
            return;
        var handle = new WindowInteropHelper(_window).Handle;
        if (handle != IntPtr.Zero)
            Broker.Attach(this, _window, handle);
    }

    private void Window_Closed(object? sender, EventArgs e) => DetachWindow();

    private void InvokeFromBroker()
    {
        if (_disposed || _handler is null)
            return;
        if (Interlocked.Exchange(ref _handlerRunning, 1) != 0)
            return;

        _ = InvokeHandlerAsync();
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

    private void SetBrokerStatus(bool sourceReady, bool baseRegistered, int extraRegistrationFailures)
    {
        if (_gesture is not { } gesture)
        {
            SetStatus($"{_actionLabel} 단축키 사용 안 함");
            return;
        }

        if (!sourceReady)
        {
            SetStatus($"{_actionLabel} 단축키 초기화 중: {gesture}");
            return;
        }

        if (!baseRegistered)
        {
            SetStatus($"{_actionLabel}: {gesture} 단축키를 등록하지 못했습니다. 다른 프로그램에서 사용 중일 수 있습니다.");
            return;
        }

        SetStatus(extraRegistrationFailures == 0
            ? $"{_actionLabel} 단축키: {gesture}"
            : $"{_actionLabel} 단축키: {gesture} (일부 추가 modifier 조합은 다른 프로그램에서 사용 중입니다.)");
    }

    private void SetStatus(string text)
    {
        if (string.Equals(StatusText, text, StringComparison.Ordinal))
            return;
        StatusText = text;
        RegistrationChanged?.Invoke(text);
    }

    private void DetachWindow()
    {
        Broker.Detach(this);
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

    private static class Broker
    {
        private static readonly object Gate = new();
        private static readonly HashSet<ScannerGlobalHotkeyService> Services = [];
        private static readonly Dictionary<int, ScannerGlobalHotkeyService> OwnersByRegistrationId = [];
        private static readonly List<int> RegisteredIds = [];
        private static Window? _window;
        private static HwndSource? _source;
        private static IntPtr _handle;

        public static void Attach(ScannerGlobalHotkeyService service, Window window, IntPtr handle)
        {
            lock (Gate)
            {
                if (_handle != handle || !ReferenceEquals(_window, window))
                    AttachSourceCore(window, handle);
                Services.Add(service);
                RebuildCore();
            }
        }

        public static void Refresh(ScannerGlobalHotkeyService service)
        {
            lock (Gate)
            {
                if (Services.Contains(service))
                    RebuildCore();
            }
        }

        public static void Detach(ScannerGlobalHotkeyService service)
        {
            lock (Gate)
            {
                if (!Services.Remove(service))
                    return;

                RebuildCore();
                if (Services.Count == 0)
                    DetachSourceCore();
            }
        }

        private static void AttachSourceCore(Window window, IntPtr handle)
        {
            UnregisterAllCore();
            if (_source is not null)
                _source.RemoveHook(WndProc);

            _window = window;
            _handle = handle;
            _source = HwndSource.FromHwnd(handle);
            _source?.AddHook(WndProc);
        }

        private static void DetachSourceCore()
        {
            UnregisterAllCore();
            if (_source is not null)
                _source.RemoveHook(WndProc);
            _source = null;
            _window = null;
            _handle = IntPtr.Zero;
        }

        private static void RebuildCore()
        {
            UnregisterAllCore();

            var services = Services
                .Where(service => !service._disposed && service.Gesture is not null)
                .OrderBy(service => service.StableOrder)
                .ToArray();
            var baseRegistered = Services.ToDictionary(service => service, _ => false);
            var extraFailures = Services.ToDictionary(service => service, _ => 0);

            if (_handle != IntPtr.Zero)
            {
                var registrationId = BrokerHotkeyIdBase;
                var virtualKeys = services
                    .Select(service => service.Gesture!.Value.VirtualKey)
                    .Where(value => value != 0)
                    .Distinct()
                    .ToArray();

                foreach (var virtualKey in virtualKeys)
                {
                    foreach (var pressed in EnumerateModifierMasks())
                    {
                        var owner = services
                            .Where(service =>
                                service.Gesture!.Value.VirtualKey == virtualKey &&
                                HotkeyModifierMatchPolicy.IsCompatible(
                                    service.Gesture.Value.RequiredModifiers,
                                    pressed))
                            .OrderByDescending(service => HotkeyModifierMatchPolicy.Specificity(
                                service.Gesture!.Value.RequiredModifiers))
                            .ThenBy(service => service.StableOrder)
                            .FirstOrDefault();
                        if (owner is null)
                            continue;

                        var id = registrationId++;
                        var registered = RegisterHotKey(
                            _handle,
                            id,
                            ModNoRepeat | ToNativeModifiers(pressed),
                            (uint)virtualKey);

                        var required = owner.Gesture!.Value.RequiredModifiers;
                        if (pressed == required)
                        {
                            baseRegistered[owner] = registered;
                        }
                        else if (!registered)
                        {
                            extraFailures[owner]++;
                        }

                        if (!registered)
                            continue;

                        RegisteredIds.Add(id);
                        OwnersByRegistrationId[id] = owner;
                    }
                }
            }

            foreach (var service in Services)
            {
                service.SetBrokerStatus(
                    _handle != IntPtr.Zero && _source is not null,
                    baseRegistered.GetValueOrDefault(service),
                    extraFailures.GetValueOrDefault(service));
            }
        }

        private static IEnumerable<HotkeyModifierMask> EnumerateModifierMasks()
        {
            for (var value = 0; value <= (int)HotkeyModifierMask.All; value++)
                yield return (HotkeyModifierMask)value;
        }

        private static uint ToNativeModifiers(HotkeyModifierMask modifiers)
        {
            var native = 0u;
            if ((modifiers & HotkeyModifierMask.Control) != 0)
                native |= ModControl;
            if ((modifiers & HotkeyModifierMask.Alt) != 0)
                native |= ModAlt;
            if ((modifiers & HotkeyModifierMask.Shift) != 0)
                native |= ModShift;
            return native;
        }

        private static IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmHotkey)
                return IntPtr.Zero;

            ScannerGlobalHotkeyService? owner;
            lock (Gate)
                OwnersByRegistrationId.TryGetValue(wParam.ToInt32(), out owner);
            if (owner is null)
                return IntPtr.Zero;

            handled = true;
            owner.InvokeFromBroker();
            return IntPtr.Zero;
        }

        private static void UnregisterAllCore()
        {
            if (_handle != IntPtr.Zero)
            {
                foreach (var id in RegisteredIds)
                    _ = UnregisterHotKey(_handle, id);
            }
            RegisteredIds.Clear();
            OwnersByRegistrationId.Clear();
        }
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

    public int VirtualKey => KeyInterop.VirtualKeyFromKey(Key);

    public HotkeyModifierMask RequiredModifiers
    {
        get
        {
            var modifiers = HotkeyModifierMask.None;
            if (Control)
                modifiers |= HotkeyModifierMask.Control;
            if (Alt)
                modifiers |= HotkeyModifierMask.Alt;
            if (Shift)
                modifiers |= HotkeyModifierMask.Shift;
            return modifiers;
        }
    }

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
