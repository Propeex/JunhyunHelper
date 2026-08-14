using System.Windows;
using System.Windows.Media;

namespace TarkovHelper.Pages.Map;

public partial class MapPage
{
    private static readonly bool JunhyunSmokeLayoutHandlerRegistered = RegisterJunhyunSmokeLayoutHandler();

    private static bool RegisterJunhyunSmokeLayoutHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(MapPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnJunhyunSmokeMapLoaded));
        return true;
    }

    private static void OnJunhyunSmokeMapLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MapPage page ||
            !string.Equals(
                Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        // CI can start with no user profile, which intentionally collapses the product
        // section host. The floor-hotkey regression check needs real viewport geometry,
        // so reveal only the existing visual ancestry for the smoke process. This path
        // is environment-gated and cannot affect a normal product run.
        DependencyObject? current = page;
        while (current is not null)
        {
            if (current is FrameworkElement element)
                element.Visibility = Visibility.Visible;

            current = VisualTreeHelper.GetParent(current);
        }

        page.Dispatcher.BeginInvoke(() =>
        {
            page.Measure(new Size(1200, 820));
            page.Arrange(new Rect(0, 0, 1200, 820));
            page.UpdateLayout();
        });
    }
}
