using System.Windows;
using JunhyunHelper.Desktop.Items;
using JunhyunHelper.Desktop.Quests;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _contentNavigationAttached;

    private void ItemsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ItemsPage.SetImageCache(_services.Images);
        QuestPage.SetImageCache(_services.Images);

        if (_contentNavigationAttached)
            return;

        QuestPage.ItemNavigationRequested += QuestPage_ItemNavigationRequested;
        QuestPage.QuestNavigationRequested += QuestPage_QuestNavigationRequested;
        ItemsPage.QuestNavigationRequested += ItemsPage_QuestNavigationRequested;
        _contentNavigationAttached = true;
    }

    private void HideoutPage_Loaded(object sender, RoutedEventArgs e) =>
        HideoutPage.SetImageCache(_services.Images);

    private void AmmoPage_Loaded(object sender, RoutedEventArgs e) =>
        AmmoPage.SetImageCache(_services.Images);

    private void QuestPage_ItemNavigationRequested(
        object? sender,
        QuestItemNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Items;
        ShowActiveSection();
        ItemsPage.NavigateToItem(e.ItemId);
    }

    private void QuestPage_QuestNavigationRequested(
        object? sender,
        QuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }

    private void ItemsPage_QuestNavigationRequested(
        object? sender,
        ItemQuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }
}
