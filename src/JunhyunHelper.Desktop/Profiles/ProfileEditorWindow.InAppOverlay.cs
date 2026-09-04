namespace JunhyunHelper.Desktop.Profiles;

public partial class ProfileEditorWindow : IInAppOverlayDialog
{
    private Action<bool?>? _inAppCloseRequested;

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested) =>
        _inAppCloseRequested = closeRequested ?? throw new ArgumentNullException(nameof(closeRequested));

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        if (_editingExistingProfile && !DeleteRequested)
        {
            if (!TryBuildResult(out var result))
                return false;

            Result = result;
            _inAppCloseRequested?.Invoke(true);
            return true;
        }

        _inAppCloseRequested?.Invoke(false);
        return true;
    }
}
