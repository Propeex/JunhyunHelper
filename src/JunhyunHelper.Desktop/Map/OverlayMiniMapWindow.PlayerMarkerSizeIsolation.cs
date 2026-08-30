namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Updates only the MiniMap player marker. The donor whole-view refresh also rewrites
    /// unrelated marker transforms, so a Player Marker Size change must not use that
    /// broad path or it can visually reset Name Size / MiniMap Marker Size.
    /// </summary>
    internal void ApplyJunhyunPlayerMarkerSizeOnly(double mapPixelSize)
    {
        var markerSize = Math.Clamp(mapPixelSize / 18.0, 0.5, 3.0);
        _settings.PlayerMarkerSize = markerSize;

        if (PlayerMarkerScale is not null)
        {
            PlayerMarkerScale.ScaleX = markerSize;
            PlayerMarkerScale.ScaleY = markerSize;
        }

        SaveSettings();
    }
}
