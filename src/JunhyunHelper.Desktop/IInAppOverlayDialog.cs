namespace JunhyunHelper.Desktop;

internal interface IInAppOverlayDialog
{
    void AttachInAppOverlay(Action<bool?> close);
    bool TryDismissInAppOverlay();
}
