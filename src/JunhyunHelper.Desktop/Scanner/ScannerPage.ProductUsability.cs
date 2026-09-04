using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private bool _productUsabilityInitialized;
    private ScrollViewer? _selectedItemScrollViewer;

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
        EnsureSelectedItemScrolling();
        AttachSearchClearAffordance();
        InitializeScannerUserItemCollections();
        ApplyV191DetailActionAlignment();

    }

    private void EnsureSelectedItemScrolling()
    {
        if (_selectedItemScrollViewer is not null || SelectedItemPanel.Parent is not Grid parent)
            return;
        var index = parent.Children.IndexOf(SelectedItemPanel);
        parent.Children.RemoveAt(index);
        _selectedItemScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = SelectedItemPanel,
        };
        parent.Children.Insert(index, _selectedItemScrollViewer);
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
