using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using JunhyunHelper.Application.Quests;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Quests;

public partial class QuestPage
{
    public void SetDataPreservingScroll(GameContentCatalog content, QuestWorkspace workspace)
    {
        var scrollViewer = FindScrollViewer(QuestList);
        var offset = scrollViewer?.VerticalOffset ?? 0;
        SetData(content, workspace);

        if (scrollViewer is null)
            return;

        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () => scrollViewer.ScrollToVerticalOffset(offset));
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer)
            return viewer;

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var found = FindScrollViewer(VisualTreeHelper.GetChild(root, index));
            if (found is not null)
                return found;
        }

        return null;
    }
}
