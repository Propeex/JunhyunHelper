namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private DateTime _junhyunTemporaryHideUntilUtc = DateTime.MinValue;

    public void JunhyunTemporarilyHide(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Clamp(seconds, 1.0, 15.0));
        _junhyunTemporaryHideUntilUtc = DateTime.UtcNow + duration;
        Opacity = 0.0;
    }

    // OverlayMiniMapWindow.JunhyunProduct owns the single 80ms presentation loop.
    // Keeping only the expiry state here avoids competing Tick handlers: timed hide
    // and hover hide are composed by that one loop.
    private bool JunhyunTemporaryHideActive =>
        DateTime.UtcNow < _junhyunTemporaryHideUntilUtc;
}
