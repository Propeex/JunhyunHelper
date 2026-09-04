using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private bool _productUsabilityInitialized;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Dispatcher.BeginInvoke(InitializeProductUsability, DispatcherPriority.Loaded);
    }

    private void InitializeProductUsability()
    {
        if (_productUsabilityInitialized)
            return;
        _productUsabilityInitialized = true;

        BuildItemRelationshipPresentation();
        AttachSearchClearAffordance();
        InitializeScannerUserItemCollections();
        ApplyV191DetailActionAlignment();

    }


    private void AttachSearchClearAffordance() =>
        ProductSearchClearButtonBehavior.Attach(ItemSearchBox);


    private async void ProductSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null || Window.GetWindow(this) is not MainWindow mainWindow) return;
        var settings = new ScannerSettingsWindow(_coordinator);
        await mainWindow.ToggleInAppWindowAsync("scanner-settings", settings);
        UpdateToggleButton();
        UpdateStatus(_coordinator.Status);
    }

    private async void ProductAdvancedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null || Window.GetWindow(this) is not MainWindow mainWindow) return;
        var advanced = new ScannerAdvancedWindow(_coordinator);
        await mainWindow.ToggleInAppWindowAsync("scanner-advanced", advanced);
        UpdateToggleButton();
        UpdateStatus(_coordinator.Status);
        RefreshActivityCorrectionAvailability();
    }



}
