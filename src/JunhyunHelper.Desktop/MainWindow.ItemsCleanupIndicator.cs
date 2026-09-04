using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private void RefreshItemsCleanupIndicator()
    {
        ItemsCleanupIndicator.Visibility =
            (_activeItemsWorkspace?.Plan.CleanupItems.Count ?? 0) > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
}
