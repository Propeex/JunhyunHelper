namespace TarkovHelper.Services;

/// <summary>
/// Compatibility shell for transplanted Map code. JunhyunHelper-owned configurable Map
/// hotkeys are dispatched exclusively by JunhyunMapHotkeyService. The old bare NumPad0..5
/// direct-floor path was removed in v1.16.0 so those keys remain available for ordinary
/// configurable product hotkeys.
/// </summary>
public sealed class GlobalKeyboardHookService : IDisposable
{
    public static GlobalKeyboardHookService Instance { get; } = new();

    private bool _isEnabled;

    private GlobalKeyboardHookService()
    {
    }

    public event Action<int>? FloorKeyPressed { add { } remove { } }

    // Kept only for binary/source compatibility. No direct-floor event is ever raised.
    public event Action<int>? DirectFloorSelectionPressed { add { } remove { } }

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

    /// <summary>
    /// Shared capture guard used by JunhyunMapHotkeyService while a key editor is active.
    /// </summary>
    public bool OverlayHotkeysSuppressed { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public void Dispose()
    {
        _isEnabled = false;
        GC.SuppressFinalize(this);
    }
}
