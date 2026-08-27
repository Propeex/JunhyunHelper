using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop;

public partial class App
{
    static App()
    {
        // Product policy: standard WPF explanatory ToolTips are not part of the UI.
        // Handling the routed opening event at FrameworkElement level is stronger than
        // theme styling alone and does not affect functional custom Popup windows.
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            ToolTipService.ToolTipOpeningEvent,
            new ToolTipEventHandler(BlockStandardToolTipOpening),
            handledEventsToo: true);
    }

    private static void BlockStandardToolTipOpening(object sender, ToolTipEventArgs e) =>
        e.Handled = true;
}
