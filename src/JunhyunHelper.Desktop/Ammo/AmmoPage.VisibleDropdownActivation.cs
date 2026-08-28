using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private bool _productVisibleDropdownActivatedFromLoaded;

    internal static void RegisterProductVisibleDropdownActivation()
    {
        EventManager.RegisterClassHandler(
            typeof(AmmoPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnProductVisibleDropdownLoaded));
    }

    private static void OnProductVisibleDropdownLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not AmmoPage page)
            return;

        // The Loaded routed event can originate from a descendant while the class-handler
        // sender is the AmmoPage itself. Do not gate product activation on OriginalSource.
        // Apply synchronously so the visible toolbar is correct before user interaction can
        // observe the legacy XAML fallback.
        page.ApplyProductCaliberDropdownPolish();
        page._productVisibleDropdownActivatedFromLoaded = true;
    }

    internal void VerifyProductVisibleDropdownLoadedActivation()
    {
        if (!_productVisibleDropdownActivatedFromLoaded || !_productCaliberDropdownApplied)
            throw new InvalidOperationException("Ammo visible dropdown polish was not activated by the real Loaded route.");
        if (_productFavoriteCaliberComboBox is null)
            throw new InvalidOperationException("Ammo favorite caliber selector is not a ComboBox in the visible runtime toolbar.");
        if (FavoriteCaliberMenuButton.Visibility != Visibility.Collapsed || FavoriteCaliberMenuButton.IsHitTestVisible)
            throw new InvalidOperationException("Ammo legacy favorite menu is still visible or interactive.");
        if (CaliberComboBox.ItemTemplate is null ||
            !ReferenceEquals(CaliberComboBox.ItemTemplate, _productFavoriteCaliberComboBox.ItemTemplate))
        {
            throw new InvalidOperationException("Ammo caliber and favorite dropdowns do not share the animated icon template.");
        }
    }
}

internal static class AmmoVisibleDropdownActivationModule
{
    [ModuleInitializer]
    internal static void Initialize() => AmmoPage.RegisterProductVisibleDropdownActivation();
}

internal static class AmmoVisibleDropdownLoadedSmokeGate
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

        // Wait until the complete child tree has emitted Loaded, then check the dedicated
        // activation flag. The older published Ammo smoke may run before or after this
        // callback, but its direct initializer cannot set this flag, so it cannot repair
        // a missing real Loaded activation and hide the regression again.
        window.Dispatcher.BeginInvoke(
            () =>
            {
                try
                {
                    window.AmmoPage.VerifyProductVisibleDropdownLoadedActivation();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Ammo visible dropdown Loaded activation failed.\n" + exception);
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
