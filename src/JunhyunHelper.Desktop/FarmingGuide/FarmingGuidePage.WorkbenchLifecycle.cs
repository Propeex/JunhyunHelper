using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (!IsWorkbenchOpen || e.ClickCount != 1)
            return;

        DependencyObject? current = e.OriginalSource as DependencyObject;
        while (current is not null && !ReferenceEquals(current, this))
        {
            if (current is FrameworkElement { Tag: EquipmentDropTarget })
            {
                // The equipment board remains visible while the center workbench is
                // open. Close the workbench before any equipment drag can begin so its
                // apply callback can never write a later slot edit back into an owner
                // item that has already been moved or removed.
                CloseWorkbench();
                return;
            }

            current = VisualTreeHelper.GetParent(current);
        }
    }
}
