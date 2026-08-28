using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        RebuildToolbarLayout();
        BuildNeededSourcesPresentation();
        BuildItemRelationshipPresentation();
        EnsureSelectedItemScrolling();
        NormalizeSearchClearAffordance();
        InitializeScannerUserItemCollections();

        SettingsButton.Click -= SettingsButton_Click;
        SettingsButton.Click += ProductSettingsButton_Click;
        AdvancedButton.Click -= AdvancedButton_Click;
        AdvancedButton.Click += ProductAdvancedButton_Click;

        ItemSearchBox.TextChanged += ProductItemSearchBox_TextChanged;
        ItemSearchBox.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(ProductItemSearchBox_PreviewKeyDown), handledEventsToo: true);
        SearchResultList.AddHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(ProductSearchResultList_PreviewMouseUp), handledEventsToo: true);
    }

    private void RebuildToolbarLayout()
    {
        if (ScannerToggleButton.Parent is not Grid toolbar)
            return;
        while (toolbar.ColumnDefinitions.Count < 6)
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        toolbar.ColumnDefinitions[3].Width = new GridLength(1, GridUnitType.Star);
        toolbar.ColumnDefinitions[4].Width = GridLength.Auto;
        toolbar.ColumnDefinitions[5].Width = GridLength.Auto;
        Grid.SetColumn(RuntimeStatusText, 4);
        Grid.SetColumn(CurrentCorrectionButton, 5);
        CurrentCorrectionButton.Margin = new Thickness(8, 0, 0, 0);
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

    private void NormalizeSearchClearAffordance()
    {
        if (ItemSearchBox.Parent is not Grid searchGrid)
            return;
        foreach (var button in searchGrid.Children.OfType<Button>())
        {
            if (string.Equals(button.Content?.ToString(), "×", StringComparison.Ordinal))
                button.Visibility = Visibility.Collapsed;
        }
        if (searchGrid.ColumnDefinitions.Count > 1)
            searchGrid.ColumnDefinitions[1].Width = new GridLength(0);
        ProductSearchClearButtonBehavior.Attach(ItemSearchBox);
    }

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
    }

    private void ProductItemSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Search text owns only the popup/results state. The currently opened item detail
        // has an independent identity and remains visible until another item is opened.
    }

    private void ProductItemSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SearchResultList.SelectedItem is ScannerItemSearchHit hit)
            RefreshProductItemExtensions(hit.ItemId);
    }

    private void ProductSearchResultList_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && SearchResultList.SelectedItem is ScannerItemSearchHit hit)
            RefreshProductItemExtensions(hit.ItemId);
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
