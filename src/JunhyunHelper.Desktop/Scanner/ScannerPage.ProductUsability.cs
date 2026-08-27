using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private bool _productUsabilityInitialized;
    private Border? _neededSourcesHost;
    private StackPanel? _neededSourcesItems;

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

        SettingsButton.Click -= SettingsButton_Click;
        SettingsButton.Click += ProductSettingsButton_Click;

        ItemSearchBox.TextChanged += ProductItemSearchBox_TextChanged;
        ItemSearchBox.AddHandler(
            Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(ProductItemSearchBox_PreviewKeyDown),
            handledEventsToo: true);
        SearchResultList.AddHandler(
            Mouse.PreviewMouseUpEvent,
            new MouseButtonEventHandler(ProductSearchResultList_PreviewMouseUp),
            handledEventsToo: true);
    }

    private void RebuildToolbarLayout()
    {
        if (ScannerToggleButton.Parent is not Grid toolbar)
            return;

        while (toolbar.ColumnDefinitions.Count < 6)
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(CurrentCorrectionButton, 5);
        CurrentCorrectionButton.Margin = new Thickness(8, 0, 0, 0);

        var hotkeyButton = new Button
        {
            Content = "단축키",
            MinWidth = 92,
            Margin = new Thickness(0, 0, 8, 0),
        };
        hotkeyButton.Click += ProductHotkeyButton_Click;
        Grid.SetColumn(hotkeyButton, 3);
        toolbar.Children.Add(hotkeyButton);
    }

    private void BuildNeededSourcesPresentation()
    {
        if (_neededSourcesHost is not null)
            return;

        _neededSourcesItems = new StackPanel();
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "필요한 곳",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        });
        stack.Children.Add(_neededSourcesItems);

        _neededSourcesHost = new Border
        {
            Background = TryFindResource("BackgroundDarkBrush") as System.Windows.Media.Brush,
            BorderBrush = TryFindResource("BorderBrush") as System.Windows.Media.Brush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 18, 0, 0),
            Visibility = Visibility.Collapsed,
            Child = stack,
        };
        SelectedItemPanel.Children.Add(_neededSourcesHost);
    }

    private async void ProductSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null || Window.GetWindow(this) is not MainWindow mainWindow)
            return;

        var settings = new ScannerSettingsWindow(_coordinator);
        await mainWindow.ToggleInAppWindowAsync("scanner-settings", settings);
        UpdateStatus(_coordinator.Status);
    }

    private async void ProductHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null || Window.GetWindow(this) is not MainWindow mainWindow)
            return;

        var dialog = new ScannerHotkeySettingsWindow(
            _coordinator.Settings.OneShotTarkovHotkey,
            _coordinator.Settings.OneShotTestHotkey,
            _coordinator.Settings.ScannerToggleHotkey);
        if (await mainWindow.ToggleInAppWindowAsync("scanner-hotkeys", dialog) != true)
            return;

        _coordinator.SetOneShotTarkovHotkey(dialog.OneShotTarkovGesture);
        _coordinator.SetOneShotTestHotkey(dialog.OneShotTestGesture);
        _coordinator.SetScannerToggleHotkey(dialog.ScannerToggleGesture);
        UpdateToggleButton();
        UpdateStatus(_coordinator.Status);
    }

    private void ProductItemSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressSearchRefresh)
            ClearNeededSources();
    }

    private void ProductItemSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && SearchResultList.SelectedItem is ScannerItemSearchHit hit)
            RefreshNeededSources(hit.ItemId);
    }

    private void ProductSearchResultList_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && SearchResultList.SelectedItem is ScannerItemSearchHit hit)
            RefreshNeededSources(hit.ItemId);
    }

    private void RefreshNeededSources(string itemId)
    {
        if (_neededSourcesHost is null || _neededSourcesItems is null ||
            Window.GetWindow(this) is not MainWindow mainWindow)
        {
            return;
        }

        var sources = mainWindow.GetScannerNeededSources(itemId);
        _neededSourcesItems.Children.Clear();
        foreach (var source in sources)
        {
            var row = new StackPanel();
            row.Children.Add(new TextBlock
            {
                Text = source.Title,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });
            row.Children.Add(new TextBlock
            {
                Text = source.Detail,
                Foreground = TryFindResource("TextSecondaryBrush") as System.Windows.Media.Brush,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });

            var button = new Button
            {
                Tag = source,
                Content = row,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6),
            };
            button.Click += NeededSourceButton_Click;
            _neededSourcesItems.Children.Add(button);
        }

        _neededSourcesHost.Visibility = sources.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NeededSourceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ScannerNeededSourceRow source } ||
            Window.GetWindow(this) is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.NavigateFromScannerNeededSource(source);
    }

    private void ClearNeededSources()
    {
        if (_neededSourcesItems is not null)
            _neededSourcesItems.Children.Clear();
        if (_neededSourcesHost is not null)
            _neededSourcesHost.Visibility = Visibility.Collapsed;
    }
}
