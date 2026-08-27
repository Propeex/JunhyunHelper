using JunhyunHelper.Desktop;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerSettingsWindow : IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested) =>
        _inAppCloseRequested = closeRequested ?? throw new ArgumentNullException(nameof(closeRequested));

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        // Display settings are persisted on every mutation, so dismissing never needs
        // a separate Save/Cancel decision.
        _inAppCloseRequested?.Invoke(true);
        return true;
    }
}
