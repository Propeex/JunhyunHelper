namespace JunhyunHelper.Desktop;

/// <summary>
/// Host-only glue for the unmodified Tarkov Helper MapPage.
/// </summary>
public partial class MainWindow : TarkovHelper.MainWindow
{
    public void SetFullScreenMode(bool enabled)
    {
        if (Content is not System.Windows.Controls.Grid root || root.RowDefinitions.Count == 0)
            return;

        root.RowDefinitions[0].Height = enabled
            ? new System.Windows.GridLength(0)
            : System.Windows.GridLength.Auto;
    }
}
