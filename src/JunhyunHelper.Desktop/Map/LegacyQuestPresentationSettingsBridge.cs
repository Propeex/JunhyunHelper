using System.Windows;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Removes legacy Tarkov Helper Quest presentation controls that are not connected
/// to JunhyunHelper's current Quest marker product.
/// </summary>
public sealed class LegacyQuestPresentationSettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;

    public LegacyQuestPresentationSettingsBridge(
        TarkovHelper.Pages.Map.MapPage page,
        Action refreshQuestProjection)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        ArgumentNullException.ThrowIfNull(refreshQuestProjection);

        Collapse("ChkHideCompletedObjectives");
        CollapseParent("CmbQuestMarkerStyle");
        CollapseParent("SliderQuestNameTextSize");
        CollapseParent("SliderMarkerSize");
        Collapse("TxtMarkerColorsLabel");
        CollapseParent("ColorVisit");
        CollapseParent("ColorMark");
        CollapseParent("ColorPlant");
        CollapseParent("ColorExtract");
        CollapseParent("ColorFind");
        Collapse("BtnResetColors");
    }

    private void Collapse(string name)
    {
        if (_page.FindName(name) is FrameworkElement element)
            element.Visibility = Visibility.Collapsed;
    }

    private void CollapseParent(string childName)
    {
        if (_page.FindName(childName) is FrameworkElement child &&
            child.Parent is FrameworkElement parent)
        {
            parent.Visibility = Visibility.Collapsed;
        }
    }

    public void Dispose()
    {
    }
}
