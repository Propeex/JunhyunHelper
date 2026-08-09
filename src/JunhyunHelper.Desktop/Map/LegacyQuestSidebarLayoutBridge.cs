using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Map;

public static class LegacyQuestSidebarLayoutBridge
{
    public static void Apply(LegacyMapQuestSidebar sidebar)
    {
        ArgumentNullException.ThrowIfNull(sidebar);
        if (sidebar.Child is not StackPanel stack || stack.Children.Count != 3)
            return;

        var title = stack.Children[0] as UIElement;
        var summary = stack.Children[1] as UIElement;
        var list = stack.Children[2] as UIElement;
        if (title is null || summary is null || list is null)
            return;

        stack.Children.Clear();

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(title, 0);
        Grid.SetRow(summary, 1);
        Grid.SetRow(list, 2);
        grid.Children.Add(title);
        grid.Children.Add(summary);
        grid.Children.Add(list);
        sidebar.Child = grid;
    }
}
