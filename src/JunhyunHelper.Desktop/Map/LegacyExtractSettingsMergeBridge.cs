using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Completes the product merge of extract settings into the visible "지도 마커"
/// group. The adapter already moves PMC/Scav/Transit toggles; this bridge moves the
/// remaining extract-label-size control so no orphan extract settings section remains.
/// </summary>
public sealed class LegacyExtractSettingsMergeBridge
{
    public LegacyExtractSettingsMergeBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (page.FindName("MapMarkersContent") is not StackPanel markerSettings ||
            page.FindName("SliderExtractTextSize") is not Slider extractTextSize ||
            extractTextSize.Parent is not StackPanel sizeRow)
        {
            return;
        }

        if (ReferenceEquals(sizeRow.Parent, markerSettings))
            return;

        if (sizeRow.Parent is not Panel oldParent)
            return;

        oldParent.Children.Remove(sizeRow);
        sizeRow.Margin = new Thickness(0, 7, 0, 3);
        markerSettings.Children.Add(sizeRow);
    }
}
