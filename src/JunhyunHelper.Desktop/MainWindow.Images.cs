using System.Windows;
using JunhyunHelper.Desktop.Ammo;
using JunhyunHelper.Desktop.Hideout;
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
        QuestPage.RefreshNavigationLinks();
        AttachContentNavigation();
    }

    private void HideoutPage_Loaded(object sender, RoutedEventArgs e)
    {
        HideoutPage.SetImageCache(_services.Images);
        AttachContentNavigation();
    }

    private void AmmoPage_Loaded(object sender, RoutedEventArgs e)
    {
        AmmoPage.SetImageCache(_services.Images);
        AmmoPage.SetFavoriteStore(_services.AmmoFavorites);
        AttachContentNavigation();
    }

    private void AttachContentNavigation()
    {
        if (_contentNavigationAttached)
            return;

        QuestPage.ItemNavigationRequested += QuestPage_ItemNavigationRequested;
        QuestPage.QuestNavigationRequested += QuestPage_QuestNavigationRequested;
        ItemsPage.QuestNavigationRequested += ItemsPage_QuestNavigationRequested;
        ItemsPage.HideoutNavigationRequested += ItemsPage_HideoutNavigationRequested;
        HideoutPage.ItemNavigationRequested += HideoutPage_ItemNavigationRequested;
        AmmoPage.QuestNavigationRequested += AmmoPage_QuestNavigationRequested;
        _contentNavigationAttached = true;
    }

    private void QuestPage_ItemNavigationRequested(object? sender, QuestItemNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Items;
        ShowActiveSection();
        ItemsPage.NavigateToAnyItem(e.ItemId);
    }

    private void QuestPage_QuestNavigationRequested(object? sender, QuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }

    private void ItemsPage_QuestNavigationRequested(object? sender, ItemQuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }

    private void ItemsPage_HideoutNavigationRequested(object? sender, ItemHideoutNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Hideout;
        ShowActiveSection();
        HideoutPage.NavigateToStation(e.StationId);
    }

    private void HideoutPage_ItemNavigationRequested(object? sender, HideoutItemNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Items;
        ShowActiveSection();
        ItemsPage.NavigateToAnyItem(e.ItemId);
    }

    private void AmmoPage_QuestNavigationRequested(object? sender, AmmoQuestNavigationRequestedEventArgs e)
    {
        _activeSection = DesktopSection.Quest;
        ShowActiveSection();
        QuestPage.NavigateToQuest(e.QuestId);
    }
}
