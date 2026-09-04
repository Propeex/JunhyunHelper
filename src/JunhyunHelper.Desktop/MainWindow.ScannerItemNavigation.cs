using JunhyunHelper.Desktop.Items;

namespace JunhyunHelper.Desktop;

internal enum ScannerItemNavigationKind
{
    Quest,
    Hideout,
}

internal sealed record ScannerItemNavigationTarget(
    ScannerItemNavigationKind Kind,
    string TargetId);

public partial class MainWindow
{
    internal void NavigateFromScannerItemUsage(ScannerItemNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Kind == ScannerItemNavigationKind.Quest)
        {
            ItemsPage_QuestNavigationRequested(
                this,
                new ItemQuestNavigationRequestedEventArgs(target.TargetId));
            return;
        }

        ItemsPage_HideoutNavigationRequested(
            this,
            new ItemHideoutNavigationRequestedEventArgs(target.TargetId));
    }
}
