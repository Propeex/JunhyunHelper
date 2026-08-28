using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Ammo;

public partial class AmmoPage
{
    private bool _productVisibleDropdownActivatedFromInitialization;

    internal void EnsureProductVisibleDropdownInitialization()
    {
        if (_productVisibleDropdownActivatedFromInitialization)
            return;

        // AmmoPage.ProductGridFixes owns OnInitialized and calls this method after the
        // XAML control tree has been initialized. Keep this method idempotent so any later
        // verification/render pass cannot duplicate the runtime ComboBox or event hooks.
        ApplyProductCaliberDropdownPolish();
        _productVisibleDropdownActivatedFromInitialization = true;
    }

    internal void VerifyProductVisibleDropdownInitialization()
    {
        if (!_productVisibleDropdownActivatedFromInitialization || !_productCaliberDropdownApplied)
            throw new InvalidOperationException("Ammo visible dropdown polish was not activated during page initialization.");
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

internal static class AmmoVisibleDropdownInitializationSmokeGate
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

        // Inspect the product-owned initialization flag after the window has loaded. The
        // older rendered Ammo smoke can still exercise icons later, but it cannot set this
        // flag and therefore cannot hide a missing real product initialization again.
        window.Dispatcher.BeginInvoke(
            () =>
            {
                try
                {
                    window.AmmoPage.VerifyProductVisibleDropdownInitialization();
                }
                catch (Exception exception)
                {
                    try
                    {
                        var diagnostic = Path.Combine(Path.GetTempPath(), "junhyun-map-smoke-error.txt");
                        File.WriteAllText(diagnostic, "Ammo visible dropdown initialization failed.\n" + exception);
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
