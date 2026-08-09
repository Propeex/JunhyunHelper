using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// Removes settings that belonged to the old Tarkov Helper Quest DB presentation and
/// keeps the two presentation controls that are meaningful for JunhyunHelper's current
/// Quest projection: marker size and quest-name text size.
/// </summary>
public sealed class LegacyQuestPresentationSettingsBridge : IDisposable
{
    private readonly TarkovHelper.Pages.Map.MapPage _page;
    private readonly Action _refresh;
    private readonly Slider? _markerSize;
    private readonly Slider? _nameSize;
    private bool _disposed;

    public LegacyQuestPresentationSettingsBridge(
        TarkovHelper.Pages.Map.MapPage page,
        Action refresh)
    {
        _page = page ?? throw new ArgumentNullException(nameof(page));
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

        Collapse("ChkHideCompletedObjectives");
        CollapseParent("CmbQuestMarkerStyle");
        Collapse("TxtMarkerColorsLabel");
        CollapseParent("ColorVisit");
        CollapseParent("ColorMark");
        CollapseParent("ColorPlant");
        CollapseParent("ColorExtract");
        CollapseParent("ColorFind");
        Collapse("BtnResetColors");

        _markerSize = _page.FindName("SliderMarkerSize") as Slider;
        _nameSize = _page.FindName("SliderQuestNameTextSize") as Slider;
        if (_markerSize is not null)
            _markerSize.ValueChanged += Size_ValueChanged;
        if (_nameSize is not null)
            _nameSize.ValueChanged += Size_ValueChanged;
    }

    private void Size_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_disposed)
            return;

        // Run after the exact MapPage handler has persisted the new MapSettings value.
        _page.Dispatcher.BeginInvoke(_refresh);
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
        if (_disposed)
            return;
        _disposed = true;

        if (_markerSize is not null)
            _markerSize.ValueChanged -= Size_ValueChanged;
        if (_nameSize is not null)
            _nameSize.ValueChanged -= Size_ValueChanged;
    }
}
