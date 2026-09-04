using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using JunhyunHelper.Desktop.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private bool _productUsabilityInitialized;
    private Border? _neededSourcesHost;
    private StackPanel? _neededSourcesItems;
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

        BuildNeededSourcesPresentation();
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

    private void BuildNeededSourcesPresentation()
    {
        if (_neededSourcesHost is not null)
            return;
        _neededSourcesItems = new StackPanel();
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "필요한 곳", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        stack.Children.Add(_neededSourcesItems);
        _neededSourcesHost = new Border
        {
            Background = TryFindResource("BackgroundDarkBrush") as System.Windows.Media.Brush,
            BorderBrush = TryFindResource("BorderBrush") as System.Windows.Media.Brush,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(10),
            Margin = new Thickness(0, 18, 0, 0), Visibility = Visibility.Collapsed, Child = stack,
        };
        SelectedItemPanel.Children.Add(_neededSourcesHost);
    }

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

    private void RefreshNeededSources(string itemId)
    {
        if (_neededSourcesHost is null || _neededSourcesItems is null || Window.GetWindow(this) is not MainWindow mainWindow) return;
        var sources = mainWindow.GetScannerNeededSources(itemId);
        _neededSourcesItems.Children.Clear();
        foreach (var source in sources)
        {
            var row = new StackPanel();
            row.Children.Add(new TextBlock { Text = source.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            row.Children.Add(new TextBlock { Text = source.Detail, Foreground = TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush, FontSize = 11, Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap });
            var button = new Button { Tag = source, Content = row, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 6) };
            button.Click += NeededSourceButton_Click;
            _neededSourcesItems.Children.Add(button);
        }
        _neededSourcesHost.Visibility = sources.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NeededSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ScannerNeededSourceRow source } && Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.NavigateFromScannerNeededSource(source);
    }

    private void ClearNeededSources()
    {
        _neededSourcesItems?.Children.Clear();
        if (_neededSourcesHost is not null) _neededSourcesHost.Visibility = Visibility.Collapsed;
    }
}
