using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private void ItemsPage_Loaded(object sender, RoutedEventArgs e) =>
        ItemsPage.SetImageCache(_services.Images);
}
