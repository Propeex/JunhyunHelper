using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Keeps extract visibility inside the Map marker panel while moving the extract
/// label-size control into the main Settings panel.
/// </summary>
public sealed class LegacyExtractSettingsMergeBridge
{
    public LegacyExtractSettingsMergeBridge(TarkovHelper.Pages.Map.MapPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (page.FindName("SettingsPanel") is not Border settingsPanel ||
            settingsPanel.Child is not ScrollViewer scrollViewer ||
            scrollViewer.Content is not StackPanel settingsStack ||
            page.FindName("SliderExtractTextSize") is not Slider extractTextSize ||
            extractTextSize.Parent is not StackPanel sizeRow)
        {
            return;
        }

        if (page.FindName("TxtExtractNameSizeLabel") is TextBlock label)
            label.Text = "탈출구 이름:";

        if (sizeRow.Parent is Panel oldParent && !ReferenceEquals(oldParent, settingsStack))
            oldParent.Children.Remove(sizeRow);

        sizeRow.Margin = new Thickness(0, 0, 0, 18);

        var insertIndex = settingsStack.Children.Count;
        if (page.FindName("SliderPlayerMarkerSize") is Slider playerSlider &&
            playerSlider.Parent is FrameworkElement playerRow)
        {
            var playerIndex = settingsStack.Children.IndexOf(playerRow);
            if (playerIndex >= 0)
                insertIndex = playerIndex + 1;
        }

        if (!settingsStack.Children.Contains(sizeRow))
            settingsStack.Children.Insert(Math.Min(insertIndex, settingsStack.Children.Count), sizeRow);

        if (page.FindName("TxtExtractSettingsLabel") is FrameworkElement orphanHeader)
            orphanHeader.Visibility = Visibility.Collapsed;
    }
}
