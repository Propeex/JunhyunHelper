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

    private bool JunhyunTemporaryHideActive =>
        DateTime.UtcNow < _junhyunTemporaryHideUntilUtc;
}
