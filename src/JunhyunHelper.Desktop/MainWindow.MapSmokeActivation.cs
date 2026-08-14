using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private static readonly bool MapSmokeActivationRegistered = RegisterMapSmokeActivation();

    private static bool RegisterMapSmokeActivation()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoadedForMapSmoke));
        return true;
    }

    private static void OnMainWindowLoadedForMapSmoke(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || !IsMapSmokeEnabled())
            return;

        // The normal product starts on Quest. The Map regression smoke now measures
        // viewport coordinates, so it must exercise the Map exactly as a user sees it
        // rather than while MapPlaceholder is Collapsed. Preserve the requested section
        // while the asynchronous profile/content startup finishes, then reveal it.
        window._activeSection = DesktopSection.Map;
        window.Dispatcher.BeginInvoke(
            async () =>
            {
                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(8);
                while (DateTime.UtcNow < deadline)
                {
                    if (window._activeProfile is not null && window._activeContent is not null)
                    {
                        window._activeSection = DesktopSection.Map;
                        window.ShowActiveSection();
                        return;
                    }

                    await Task.Delay(100);
                }
            },
            DispatcherPriority.Background);
    }
}
