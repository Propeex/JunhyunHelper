using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _farmingGuideHooksInstalled;
    private bool _farmingGuideConfigured;

    private void FarmingGuideTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        EnsureFarmingGuideHooks();
        EnsureFarmingGuideConfigured();
        FarmingGuidePage.SetData(_activeContent, _activeProfile.ProfileId);

        QuestPage.Visibility = Visibility.Collapsed;
        HideoutPage.Visibility = Visibility.Collapsed;
        ItemsPage.Visibility = Visibility.Collapsed;
        AmmoPage.Visibility = Visibility.Collapsed;
        MapPlaceholder.Visibility = Visibility.Collapsed;
        ScannerPlaceholder.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        FarmingGuidePage.Visibility = Visibility.Visible;

        SetFarmingGuideHeaderSelected(true);
    }

    private void EnsureFarmingGuideConfigured()
    {
        if (_farmingGuideConfigured)
            return;
        FarmingGuidePage.Configure(_services.Images, _services.FarmingGuide);
        _farmingGuideConfigured = true;
    }

    private void EnsureFarmingGuideHooks()
    {
        if (_farmingGuideHooksInstalled)
            return;
        _farmingGuideHooksInstalled = true;

        QuestTabButton.Click += ExistingSectionButton_Click;
        HideoutTabButton.Click += ExistingSectionButton_Click;
        ItemsTabButton.Click += ExistingSectionButton_Click;
        AmmoTabButton.Click += ExistingSectionButton_Click;
        MapTabButton.Click += ExistingSectionButton_Click;
        ScannerTabButton.Click += ExistingSectionButton_Click;
        ProfileComboBox.SelectionChanged += FarmingGuideProfileComboBox_SelectionChanged;
    }

    private void ExistingSectionButton_Click(object sender, RoutedEventArgs e)
    {
        FarmingGuidePage.Visibility = Visibility.Collapsed;
        SetFarmingGuideHeaderSelected(false);
    }

    private async void FarmingGuideProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FarmingGuidePage.Visibility != Visibility.Visible)
            return;

        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        if (_activeProfile is null || _activeContent is null)
        {
            FarmingGuidePage.Visibility = Visibility.Collapsed;
            SetFarmingGuideHeaderSelected(false);
            return;
        }

        EnsureFarmingGuideConfigured();
        FarmingGuidePage.SetData(_activeContent, _activeProfile.ProfileId);
    }

    private void SetFarmingGuideHeaderSelected(bool selected)
    {
        if (selected)
        {
            FarmingGuideTabButton.Background = (Brush)FindResource("BackgroundHoverBrush");
            FarmingGuideTabButton.BorderBrush = (Brush)FindResource("AccentBrush");
        }
        else
        {
            FarmingGuideTabButton.ClearValue(Control.BackgroundProperty);
            FarmingGuideTabButton.ClearValue(Control.BorderBrushProperty);
        }
    }
}
