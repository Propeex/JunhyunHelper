using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage : UserControl
{
    private const int MaximumVisibleActivities = 40;

    private readonly ObservableCollection<ScannerActivityEntry> _activities = [];
    private ScannerCoordinator? _coordinator;
    private bool _initialized;
    private bool _updatingUi;
    private bool _activitySubscribed;

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
        _coordinator.AttachContextProvider(mainWindow.GetScannerDataContext);
        _coordinator.StatusChanged += Coordinator_StatusChanged;
        SubscribeActivityFeed();
        ApplySettings(_coordinator.Settings);
        UpdateToggleButtons();

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
        ApplySettings(_coordinator.Settings);
        UpdateToggleButtons();
        UpdateStatus(_coordinator.Status);
    }

    private async void ScannerToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updatingUi || _coordinator is null)
            return;

        try
        {
            SetToggleButtonsEnabled(false);
            await _coordinator.SetEnabledAsync(!_coordinator.Settings.Enabled);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner enable state update failed", exception);
        }
        finally
        {
            ApplySettings(_coordinator.Settings);
            UpdateToggleButtons();
            UpdateStatus(_coordinator.Status);
            SetToggleButtonsEnabled(true);
        }
    }

    private async void TestToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updatingUi || _coordinator is null)
            return;

        try
        {
            SetToggleButtonsEnabled(false);
            await _coordinator.SetTestEnabledAsync(!_coordinator.TestEnabled);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner display test state update failed", exception);
        }
        finally
        {
            ApplySettings(_coordinator.Settings);
            UpdateToggleButtons();
            UpdateStatus(_coordinator.Status);
            SetToggleButtonsEnabled(true);
        }
    }

    private void DisplayOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_updatingUi || _coordinator is null)
            return;

        _coordinator.UpdateDisplaySettings(settings =>
        {
            settings.ShowItemName = ShowItemNameCheckBox.IsChecked == true;
            settings.ShowItemIcon = ShowItemIconCheckBox.IsChecked == true;
            settings.ShowTraderSellPrice = ShowTraderSellPriceCheckBox.IsChecked == true;
            settings.ShowFleaAveragePrice = ShowFleaAveragePriceCheckBox.IsChecked == true;
            settings.ShowTraderPricePerSlot = ShowTraderPricePerSlotCheckBox.IsChecked == true;
            settings.ShowFleaPricePerSlot = ShowFleaPricePerSlotCheckBox.IsChecked == true;
            settings.ShowCurrentNeeded = ShowCurrentNeededCheckBox.IsChecked == true;
        });
    }

    private async void SyncCatalogButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;

        try
        {
            SyncCatalogButton.IsEnabled = false;
            await _coordinator.SyncCatalogAsync();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner item list refresh failed", exception);
        }
        finally
        {
            SyncCatalogButton.IsEnabled = true;
            UpdateStatus(_coordinator.Status);
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        ClearLogButton.IsEnabled = false;
        try
        {
            var success = ScannerDiagnosticLog.Clear();
            RuntimeStatusText.Text = success
                ? "Scanner 로그를 삭제했습니다."
                : "일부 Scanner 로그 파일을 삭제하지 못했습니다.";
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner log clear failed", exception);
            RuntimeStatusText.Text = "Scanner 로그 삭제 중 오류가 발생했습니다.";
        }
        finally
        {
            ClearLogButton.IsEnabled = true;
        }
    }

    private void Coordinator_StatusChanged(ScannerRuntimeStatus status)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() =>
            {
                UpdateToggleButtons();
                UpdateStatus(status);
            });
            return;
        }
        UpdateToggleButtons();
        UpdateStatus(status);
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
            _ = Dispatcher.BeginInvoke(() => ClearActivities());
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

    private void UpdateStatus(ScannerRuntimeStatus status)
    {
        RuntimeStatusText.Text = status.Message;
    }

    private void ApplySettings(ScannerDisplaySettings settings)
    {
        _updatingUi = true;
        try
        {
            ShowItemNameCheckBox.IsChecked = settings.ShowItemName;
            ShowItemIconCheckBox.IsChecked = settings.ShowItemIcon;
            ShowTraderSellPriceCheckBox.IsChecked = settings.ShowTraderSellPrice;
            ShowFleaAveragePriceCheckBox.IsChecked = settings.ShowFleaAveragePrice;
            ShowTraderPricePerSlotCheckBox.IsChecked = settings.ShowTraderPricePerSlot;
            ShowFleaPricePerSlotCheckBox.IsChecked = settings.ShowFleaPricePerSlot;
            ShowCurrentNeededCheckBox.IsChecked = settings.ShowCurrentNeeded;
        }
        finally
        {
            _updatingUi = false;
        }
    }

    private void UpdateToggleButtons()
    {
        if (_coordinator is null)
            return;

        ScannerToggleButton.Content = _coordinator.Settings.Enabled ? "스캐너 ON" : "스캐너 OFF";
        TestToggleButton.Content = _coordinator.TestEnabled ? "테스트 ON" : "테스트 OFF";
    }

    private void SetToggleButtonsEnabled(bool enabled)
    {
        ScannerToggleButton.IsEnabled = enabled;
        TestToggleButton.IsEnabled = enabled;
    }
}
