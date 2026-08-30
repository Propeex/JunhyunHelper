using System.Runtime.CompilerServices;
using System.Windows;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Hideout;

internal static class HideoutSearchClearLifecycleRegistration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(HideoutPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded),
            handledEventsToo: true);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is HideoutPage page)
            page.EnsureProductSearchClearAttached();
    }
}

public partial class HideoutPage
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        EnsureProductSearchClearAttached();
    }

    internal void EnsureProductSearchClearAttached() =>
        _ = ProductSearchClearButtonBehavior.Attach(SearchBox);
}
