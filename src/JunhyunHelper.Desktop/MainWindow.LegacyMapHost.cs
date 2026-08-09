using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Desktop.Map;

namespace JunhyunHelper.Desktop;

/// <summary>
/// Host boundary for the exact Tarkov Helper Map subsystem plus the explicitly
/// approved JunhyunHelper product delta. Quest is the only cross-feature dependency.
/// </summary>
public partial class MainWindow : TarkovHelper.MainWindow
{
    private TarkovHelper.Pages.Map.MapPage? _legacyMapPage;
    private LegacyMapProductAdapter? _legacyMapProductAdapter;
    private LegacyMapQuestSidebar? _legacyMapQuestSidebar;
    private bool _legacyMapTabHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_legacyMapTabHooked)
            return;

        _legacyMapTabHooked = true;
        MapTabButton.Click += LegacyMapTabButton_Click;
    }

    private void LegacyMapTabButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureLegacyMapPage();
        _legacyMapProductAdapter?.Refresh();
    }

    private void EnsureLegacyMapPage()
    {
        if (_legacyMapPage is not null)
            return;

        try
        {
            var page = new TarkovHelper.Pages.Map.MapPage();
            var sidebar = new LegacyMapQuestSidebar();

            var host = new Grid();
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            host.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(sidebar, 0);
            Grid.SetColumn(page, 1);
            host.Children.Add(sidebar);
            host.Children.Add(page);

            MapPlaceholder.Children.Clear();
            MapPlaceholder.Children.Add(host);

            _legacyMapPage = page;
            _legacyMapQuestSidebar = sidebar;
            _legacyMapProductAdapter = new LegacyMapProductAdapter(
                page,
                sidebar,
                () => QuestPage.CurrentContentForMap,
                () => QuestPage.CurrentWorkspaceForMap);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"기존 Tarkov Helper 지도 초기화에 실패했습니다.\n\n{exception.Message}",
                "지도",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// The product no longer exposes Map full-screen mode. The method remains only
    /// as a compatibility contract required by the pinned upstream MapPage type.
    /// </summary>
    public void SetFullScreenMode(bool enabled)
    {
        // Intentionally disabled.
    }
}
