using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    internal void VerifyProductCaliberSelectorInitialization()
    {
        if (!_productCaliberDropdownApplied)
            throw new InvalidOperationException("Ammo caliber selector initialization was not applied during page initialization.");

        if (CaliberComboBox.ItemTemplate is null ||
            !ReferenceEquals(CaliberComboBox.ItemTemplate, FavoriteCaliberComboBox.ItemTemplate))
        {
            throw new InvalidOperationException("Ammo caliber and favorite dropdowns do not share the animated icon template.");
        }
    }
}

internal static class AmmoCaliberSelectorInitializationSmokeGate
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window ||
            !ReferenceEquals(e.OriginalSource, window) ||
            !string.Equals(Environment.GetEnvironmentVariable("JUNHYUNHELPER_MAP_SMOKE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        // Verification inspects product-owned initialization only; it never calls the
        // initializer and therefore cannot repair a missing real product path.
        window.Dispatcher.BeginInvoke(
            () =>
            {
                try
                {
                    window.AmmoPage.VerifyProductCaliberSelectorInitialization();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Ammo caliber selector initialization failed.\n" + exception);
                    }
                    catch
                    {
                    }

                    Environment.Exit(88);
                }
            },
            DispatcherPriority.Loaded);
    }
}
