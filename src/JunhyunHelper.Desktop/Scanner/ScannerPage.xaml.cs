using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage : UserControl
{
    private const int MaximumVisibleActivities = 40;

    private readonly ObservableCollection<ScannerActivityEntry> _activities = [];
    private ScannerCoordinator? _coordinator;
    private bool _initialized;
    private bool _updatingUi;
    private bool _activitySubscribed;
    private string? _selectedWikiUrl;

    public ScannerPage()
    {
        InitializeComponent();
        ActivityItems.ItemsSource = _activities;
    }

    private async void ScannerPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;
        if (Window.GetWindow(this) is not MainWindow mainWindow)
            return;

        _initialized = true;
        _coordinator = mainWindow.ScannerCoordinator;
        _coordinator.StatusChanged += Coordinator_StatusChanged;
        _coordinator.HotkeyStatusChanged += Coordinator_HotkeyStatusChanged;
        SubscribeActivityFeed();
        UpdateToggleButton();

        try
        {
            await _coordinator.InitializeAsync();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner initialization failed", exception);
        }
        UpdateStatus(_coordinator.Status);
    }

    private async void ScannerPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_initialized || _coordinator is null || !IsVisible)
            return;

        try
        {
            await _coordinator.RefreshContextAsync();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner context refresh failed", exception);
        }
        UpdateToggleButton();
        UpdateStatus(_coordinator.Status);
        RefreshSearchResults();
    }

    private async void ScannerToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updatingUi || _coordinator is null)
            return;

        try
        {
            _updatingUi = true;
            ScannerToggleButton.IsEnabled = false;
            await _coordinator.SetEnabledAsync(!_coordinator.Settings.Enabled);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner enable state update failed", exception);
        }
        finally
        {
            _updatingUi = false;
            ScannerToggleButton.IsEnabled = true;
            UpdateToggleButton();
            UpdateStatus(_coordinator.Status);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;

        var window = new ScannerSettingsWindow(_coordinator)
        {
            Owner = Window.GetWindow(this),
        };
        window.ShowDialog();
        UpdateStatus(_coordinator.Status);
    }

    private void AdvancedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;

        var window = new ScannerAdvancedWindow(_coordinator)
        {
            Owner = Window.GetWindow(this),
        };
        window.ShowDialog();
        UpdateToggleButton();
        UpdateStatus(_coordinator.Status);
    }

    private void ItemSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSearchResults();

    private void RefreshSearchResults()
    {
        if (_coordinator is null || ItemSearchBox is null || SearchResultList is null || SearchResultsPopup is null)
            return;

        var query = ItemSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            SearchResultList.ItemsSource = null;
            SearchResultsPopup.IsOpen = false;
            return;
        }

        var hits = _coordinator.SearchItems(query, 20);
        SearchResultList.ItemsSource = hits;
        SearchResultsPopup.IsOpen = hits.Count > 0;
        if (hits.Count > 0)
            SearchResultList.SelectedIndex = 0;
    }

    private void ItemSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchResultsPopup.IsOpen = false;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down && SearchResultsPopup.IsOpen && SearchResultList.Items.Count > 0)
        {
            SearchResultList.Focus();
            if (SearchResultList.SelectedIndex < 0)
                SearchResultList.SelectedIndex = 0;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && SearchResultList.SelectedItem is ScannerItemSearchHit hit)
        {
            SelectSearchHit(hit);
            e.Handled = true;
        }
    }

    private void SearchResultList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (SearchResultList.SelectedItem is ScannerItemSearchHit hit)
            SelectSearchHit(hit);
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        ItemSearchBox.Clear();
        SearchResultsPopup.IsOpen = false;
        ItemSearchBox.Focus();
    }

    private void SelectSearchHit(ScannerItemSearchHit hit)
    {
        if (_coordinator is null)
            return;

        var details = _coordinator.GetSearchItemDetails(hit.ItemId);
        if (details is null)
        {
            RuntimeStatusText.Text = "선택한 아이템 정보를 현재 데이터에서 불러올 수 없습니다.";
            return;
        }

        SearchResultsPopup.IsOpen = false;
        ItemSearchBox.Text = hit.OfficialName;
        ItemSearchBox.CaretIndex = ItemSearchBox.Text.Length;
        RenderSearchDetails(details);
    }

    private void RenderSearchDetails(ScannerItemSearchDetails details)
    {
        var snapshot = details.Snapshot;
        SelectedItemIcon.Source = snapshot.Icon;
        SelectedItemNameText.Text = snapshot.OfficialName;
        FleaAverageText.Text = snapshot.FleaAveragePrice is { } flea
            ? FormatRoubles(flea)
            : "정보 없음";
        BestTraderText.Text = snapshot.TraderSellPrice is { } trader
            ? string.IsNullOrWhiteSpace(snapshot.BestTraderName)
                ? FormatRoubles(trader)
                : $"{snapshot.BestTraderName} · {FormatRoubles(trader)}"
            : "정보 없음";
        NeededCountText.Text = snapshot.CurrentNeeded.ToString("N0", CultureInfo.InvariantCulture);

        _selectedWikiUrl = NormalizeWikiUrl(details.WikiUrl);
        WikiButton.IsEnabled = _selectedWikiUrl is not null;
        EmptyItemText.Visibility = Visibility.Collapsed;
        SelectedItemPanel.Visibility = Visibility.Visible;
    }

    private void WikiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWikiUrl is null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(_selectedWikiUrl) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            App.WriteDiagnostic("Scanner item wiki open failed", exception);
            RuntimeStatusText.Text = "아이템 위키를 열지 못했습니다.";
        }
    }

    private static string? NormalizeWikiUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return null;
        }
        return uri.AbsoluteUri;
    }

    private static string FormatRoubles(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + "₽";

    private void Coordinator_StatusChanged(ScannerRuntimeStatus status)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() =>
            {
                UpdateToggleButton();
                UpdateStatus(status);
            });
            return;
        }
        UpdateToggleButton();
        UpdateStatus(status);
    }

    private void Coordinator_HotkeyStatusChanged(string status)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(() => RuntimeStatusText.Text = status);
            return;
        }
        RuntimeStatusText.Text = status;
    }

    private void SubscribeActivityFeed()
    {
        if (_activitySubscribed)
            return;

        _activitySubscribed = true;
        foreach (var activity in ScannerDiagnosticLog.GetRecentActivities().Take(MaximumVisibleActivities))
            _activities.Add(activity);
        ScannerDiagnosticLog.ActivityAdded += ScannerDiagnosticLog_ActivityAdded;
        ScannerDiagnosticLog.ActivitiesCleared += ScannerDiagnosticLog_ActivitiesCleared;
        UpdateEmptyActivityState();
    }

    private void ScannerDiagnosticLog_ActivityAdded(ScannerActivityEntry activity)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(() => AddActivity(activity));
            return;
        }
        AddActivity(activity);
    }

    private void ScannerDiagnosticLog_ActivitiesCleared()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(ClearActivities);
            return;
        }
        ClearActivities();
    }

    private void AddActivity(ScannerActivityEntry activity)
    {
        _activities.Insert(0, activity);
        while (_activities.Count > MaximumVisibleActivities)
            _activities.RemoveAt(_activities.Count - 1);
        UpdateEmptyActivityState();
    }

    private void ClearActivities()
    {
        _activities.Clear();
        UpdateEmptyActivityState();
    }

    private void UpdateEmptyActivityState()
    {
        EmptyActivityText.Visibility = _activities.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateStatus(ScannerRuntimeStatus status) => RuntimeStatusText.Text = status.Message;

    private void UpdateToggleButton()
    {
        if (_coordinator is null)
            return;
        ScannerToggleButton.Content = _coordinator.Settings.Enabled ? "스캐너 ON" : "스캐너 OFF";
    }
}
