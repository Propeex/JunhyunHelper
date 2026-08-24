using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private async void OneShotScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null || !OneShotScanButton.IsEnabled)
            return;

        OneShotScanButton.IsEnabled = false;
        try
        {
            await _coordinator.TriggerOneShotTarkovAsync();
            UpdateStatus(_coordinator.Status);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner one-shot button failed", exception);
            RuntimeStatusText.Text = "1회 스캔 중 오류가 발생했습니다.";
        }
        finally
        {
            OneShotScanButton.IsEnabled = true;
        }
    }
}
