using System.Windows;

namespace TarkovHelper.Windows;

public partial class OverlayMiniMapWindow
{
    /// <summary>
    /// Donor UpdateMapView reapplies its inverse zoom transform to every marker container.
    /// Re-project JunhyunHelper's independent MiniMap marker scale immediately afterward
    /// so changing Player Marker Size cannot reset unrelated marker/name presentation.
    /// </summary>
    internal void ReapplyJunhyunMarkerPresentationAfterDonorMapView()
    {
        _junhyunLastGeneralMarkerSignature = int.MinValue;
        SynchronizeGeneralMarkerScale(force: true);

        foreach (FrameworkElement child in MapMarkersContainer.Children)
        {
            if (child.Tag is JunhyunHelper.Desktop.Map.JunhyunAdditionalMapMarker)
                ApplyJunhyunAdditionalMarkerScale(child);
        }

        foreach (FrameworkElement child in QuestMarkersContainer.Children)
            ApplyJunhyunMarkerVisualScale(child);

        if (_junhyunQuestV2Layer is not null)
        {
            var inverse = 1.0 / Math.Max(
                _settings.ZoomLevel,
                TarkovHelper.Models.Map.OverlayMiniMapSettings.MinZoom);
            var scale = inverse * _junhyunMarkerScale;
            foreach (FrameworkElement child in _junhyunQuestV2Layer.Children)
            {
                child.RenderTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                child.RenderTransformOrigin = new Point(0, 0);
            }
        }

        foreach (FrameworkElement child in ExtractMarkersContainer.Children)
        {
            if (child.Tag is JunhyunSynchronizedExtractTag)
                ApplyJunhyunMarkerVisualScale(child);
        }
    }
}
