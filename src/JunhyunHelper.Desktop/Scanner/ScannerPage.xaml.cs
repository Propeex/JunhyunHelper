using System.Windows;
using System.Windows.Controls;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage : UserControl
{
    private ScannerCoordinator? _coordinator;
    private bool _initialized;
    private bool _updatingUi;
    private bool _positionEditing;

    public ScannerPage()
    {
        InitializeComponent();
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
        ApplySettings(_coordinator.Settings);

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
        UpdateStatus(_coordinator.Status);
    }

    private async void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updatingUi || _coordinator is null)
            return;

        try
        {
            EnabledCheckBox.IsEnabled = false;
            await _coordinator.SetEnabledAsync(EnabledCheckBox.IsChecked == true);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner enable state update failed", exception);
        }
        finally
        {
            EnabledCheckBox.IsEnabled = true;
            ApplySettings(_coordinator.Settings);
            UpdateStatus(_coordinator.Status);
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
            App.WriteDiagnostic("Scanner catalog synchronization failed", exception);
        }
        finally
        {
            SyncCatalogButton.IsEnabled = true;
            UpdateStatus(_coordinator.Status);
        }
    }

    private void PositionEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;

        _positionEditing = !_positionEditing;
        if (_positionEditing)
        {
            _coordinator.PauseForPositionEdit();
            _coordinator.BeginPositionEdit();
            PositionEditButton.Content = "편집 종료";
        }
        else
        {
            _coordinator.EndPositionEdit();
            _ = ResumeAfterPositionEditAsync();
            PositionEditButton.Content = "위치 편집";
        }
    }

    private void PositionResetButton_Click(object sender, RoutedEventArgs e)
    {
        _coordinator?.ResetPosition();
    }

    private async void PreviewItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;
        await ShowPreviewAsync(PreviewItemIdTextBox.Text);
    }

    private async void PreviewDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;
        await ShowPreviewAsync(null);
    }

    private async void PreviewHideButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;
        await _coordinator.HidePreviewAsync();
        UpdateStatus(_coordinator.Status);
    }

    private async Task ShowPreviewAsync(string? itemId)
    {
        if (_coordinator is null)
            return;

        try
        {
            await _coordinator.ShowPreviewAsync(itemId);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner preview failed", exception);
        }
        UpdateStatus(_coordinator.Status);
    }

    private async Task ResumeAfterPositionEditAsync()
    {
        if (_coordinator is null)
            return;
        try
        {
            await _coordinator.ResumeAfterPositionEditAsync();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner position edit resume failed", exception);
        }
        UpdateStatus(_coordinator.Status);
    }

    private void Coordinator_StatusChanged(ScannerRuntimeStatus status)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateStatus(status));
            return;
        }
        UpdateStatus(status);
    }

    private void UpdateStatus(ScannerRuntimeStatus status)
    {
        RuntimeStatusText.Text = status.Message;
        if (_coordinator is null)
        {
            CatalogStatusText.Text = "카탈로그 상태를 확인할 수 없습니다.";
            return;
        }

        var mode = _coordinator.CatalogMode?.ToDataKey() ?? "미로드";
        var generated = _coordinator.CatalogGeneratedAtUtc is { } time
            ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "없음";
        CatalogStatusText.Text = $"카탈로그: {mode} · {_coordinator.CatalogCount:N0}개 · 생성 {generated}";
    }

    private void ApplySettings(ScannerDisplaySettings settings)
    {
        _updatingUi = true;
        try
        {
            EnabledCheckBox.IsChecked = settings.Enabled;
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
}
