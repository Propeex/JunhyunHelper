using System.Windows;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private void ItemsPage_Loaded(object sender, RoutedEventArgs e) =>
        ItemsPage.SetImageCache(_services.Images);

    private void HideoutPage_Loaded(object sender, RoutedEventArgs e) =>
        HideoutPage.SetImageCache(_services.Images);

    private void AmmoPage_Loaded(object sender, RoutedEventArgs e) =>
        AmmoPage.SetImageCache(_services.Images);
}
