using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Reuses the visible legacy "퀘스트 마커" checkbox as the single display switch
/// for JunhyunHelper's current-Quest projection on both Main Map and MiniMap.
/// </summary>
public sealed class LegacyQuestMarkerToggleBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly CheckBox? _toggle;
    private readonly Canvas? _mainQuestLayer;
    private IReadOnlyList<JunhyunQuestMarkerProjection> _lastAvailable =
        Array.Empty<JunhyunQuestMarkerProjection>();
    private string? _lastMapKey;
    private bool _republishing;
    private bool _disposed;

    public LegacyQuestMarkerToggleBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _toggle = _page.FindName("ChkShowQuestMarkers") as CheckBox;

        if (_page.FindName("MapCanvas") is Canvas mapCanvas)
        {
            _mainQuestLayer = mapCanvas.Children
                .OfType<Canvas>()
                .FirstOrDefault(canvas => Panel.GetZIndex(canvas) == 500);
        }

        if (_toggle is not null)
        {
            _toggle.Checked += Toggle_Changed;
            _toggle.Unchecked += Toggle_Changed;
        }

        JunhyunMapQuestProjection.Changed += Projection_Changed;
        ApplyVisibility();
    }

    private bool IsEnabled => _toggle?.IsChecked != false;

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        ApplyVisibility();
        if (IsEnabled)
            Republish(_lastMapKey, _lastAvailable);
        else
            Republish(_lastMapKey, Array.Empty<JunhyunQuestMarkerProjection>());
    }

    private void Projection_Changed(object? sender, EventArgs e)
    {
        if (_disposed || _republishing)
            return;

        var incoming = JunhyunMapQuestProjection.Markers;
        var mapKey = JunhyunMapQuestProjection.MapKey;

        // Every external publication is the newest current-Quest state, including
        // a legitimate empty result after the last Quest on this map is completed.
        _lastMapKey = mapKey;
        _lastAvailable = incoming;

        ApplyVisibility();
        if (!IsEnabled && incoming.Count > 0)
            Republish(mapKey, Array.Empty<JunhyunQuestMarkerProjection>());
    }

    private void ApplyVisibility()
    {
        if (_mainQuestLayer is not null)
            _mainQuestLayer.Visibility = IsEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Republish(
        string? mapKey,
        IReadOnlyList<JunhyunQuestMarkerProjection> markers)
    {
        _republishing = true;
        try
        {
            JunhyunMapQuestProjection.Publish(mapKey, markers);
        }
        finally
        {
            _republishing = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_toggle is not null)
        {
            _toggle.Checked -= Toggle_Changed;
            _toggle.Unchecked -= Toggle_Changed;
        }
        JunhyunMapQuestProjection.Changed -= Projection_Changed;
    }
}
