using System.Runtime.CompilerServices;
using System.Windows;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Items;

internal static class ItemsSearchClearLifecycleRegistration
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(ItemsPage),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded),
            handledEventsToo: true);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsPage page)
            page.EnsureProductSearchClearAttached();
    }
}

public partial class ItemsPage
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        EnsureProductSearchClearAttached();
    }

    internal void EnsureProductSearchClearAttached() =>
        _ = ProductSearchClearButtonBehavior.Attach(SearchBox);
}
