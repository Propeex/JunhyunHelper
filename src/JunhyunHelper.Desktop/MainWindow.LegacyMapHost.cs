namespace JunhyunHelper.Desktop;

/// <summary>
/// Host-only glue for the unmodified Tarkov Helper MapPage.
/// </summary>
public partial class MainWindow : TarkovHelper.MainWindow
{
    private TarkovHelper.Pages.Map.MapPage? _legacyMapPage;
    private bool _legacyMapTabHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_legacyMapTabHooked)
            return;

        _legacyMapTabHooked = true;
        MapTabButton.Click += LegacyMapTabButton_Click;
    }

    private void LegacyMapTabButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        EnsureLegacyMapPage();
    }

    private void EnsureLegacyMapPage()
    {
        if (_legacyMapPage is not null)
            return;

        try
        {
            var page = new TarkovHelper.Pages.Map.MapPage();
            MapPlaceholder.Children.Clear();
            MapPlaceholder.Children.Add(page);
            _legacyMapPage = page;
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                this,
                $"기존 Tarkov Helper 지도 초기화에 실패했습니다.\n\n{exception.Message}",
                "지도",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void SetFullScreenMode(bool enabled)
    {
        if (Content is not System.Windows.Controls.Grid root || root.RowDefinitions.Count == 0)
            return;

        root.RowDefinitions[0].Height = enabled
            ? new System.Windows.GridLength(0)
            : System.Windows.GridLength.Auto;
    }
}
