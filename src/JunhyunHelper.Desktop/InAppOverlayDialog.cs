namespace JunhyunHelper.Desktop;

/// <summary>
/// Optional adapter for existing Window-backed editors that are hosted inside the
/// MainWindow product overlay. Implementations keep their existing data-entry logic
/// but replace Win32 dialog close semantics with an in-app completion callback.
/// </summary>
internal interface IInAppOverlayDialog
{
    void AttachInAppOverlay(Action<bool?> closeRequested);

    /// <summary>
    /// Called for backdrop/X dismissal. Return false only when validation must keep the
    /// editor open. Implementations normally invoke the attached close callback.
    /// </summary>
    bool TryDismissInAppOverlay();
}
