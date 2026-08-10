namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private DateTime _junhyunTemporaryHideUntilUtc = DateTime.MinValue;
    private bool _junhyunTemporaryHideTickAttached;

    public void JunhyunTemporarilyHide(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Clamp(seconds, 1.0, 15.0));
        _junhyunTemporaryHideUntilUtc = DateTime.UtcNow + duration;
        Opacity = 0.0;

        // The normal product timer also owns hover transparency. Attach after that
        // handler so temporary hiding wins for the configured interval without
        // replacing the existing hover behavior.
        if (!_junhyunTemporaryHideTickAttached && _junhyunProductTimer is not null)
        {
            _junhyunProductTimer.Tick += JunhyunTemporaryHideTimer_Tick;
            _junhyunTemporaryHideTickAttached = true;
        }
    }

    private void JunhyunTemporaryHideTimer_Tick(object? sender, EventArgs e)
    {
        if (DateTime.UtcNow < _junhyunTemporaryHideUntilUtc)
        {
            if (Opacity != 0.0)
                Opacity = 0.0;
            return;
        }

        if (_junhyunProductTimer is not null)
            _junhyunProductTimer.Tick -= JunhyunTemporaryHideTimer_Tick;
        _junhyunTemporaryHideTickAttached = false;
        Opacity = IsCursorInsideMiniMap() ? 0.0 : 1.0;
    }
}
