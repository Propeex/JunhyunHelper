using System.Windows;
using JunhyunHelper.Desktop.Map;
using TarkovHelper.Models.Map;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    private bool _junhyunWindowStateAttached;
    private bool _junhyunApplyingWindowState;

    internal void InitializeJunhyunWindowState()
    {
        if (_junhyunWindowStateAttached)
            return;

        var store = JunhyunMiniMapWindowStateStore.Instance;
        if (store.TryGetSize(out var width, out var height))
        {
            _junhyunApplyingWindowState = true;
            try
            {
                Width = Math.Clamp(width, OverlayMiniMapSettings.MinWidth, OverlayMiniMapSettings.MaxWidth);
                Height = Math.Clamp(height, OverlayMiniMapSettings.MinHeight, OverlayMiniMapSettings.MaxHeight);
                PositionToTopRight();
            }
            finally
            {
                _junhyunApplyingWindowState = false;
            }
        }

        SizeChanged += JunhyunPersistedWindowSizeChanged;
        _junhyunWindowStateAttached = true;
    }

    internal void DisposeJunhyunWindowState()
    {
        if (!_junhyunWindowStateAttached)
            return;

        SizeChanged -= JunhyunPersistedWindowSizeChanged;
        _junhyunWindowStateAttached = false;
    }

    private void JunhyunPersistedWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_junhyunApplyingWindowState)
            return;

        var width = e.NewSize.Width > 0 ? e.NewSize.Width : Width;
        var height = e.NewSize.Height > 0 ? e.NewSize.Height : Height;
        JunhyunMiniMapWindowStateStore.Instance.SetSize(width, height);
    }
}
