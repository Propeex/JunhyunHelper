using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _farmingGuideConfigured;

    private void FarmingGuideTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null || _activeContent is null)
            return;

        _activeSection = DesktopSection.FarmingGuide;
        ShowActiveSection();
    }

    private void EnsureFarmingGuideConfigured()
    {
        if (_farmingGuideConfigured)
            return;

        FarmingGuidePage.Configure(
            _services.Images,
            _services.FarmingGuide,
            () => _activeProfile);
        _farmingGuideConfigured = true;
    }
}
