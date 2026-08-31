using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private const string KimTaeyoungNaverComposeUrl = "https://mail.naver.com/v2/new";

    private bool _kimTaeyoungDiagnosticRunning;

    private async void AppIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_kimTaeyoungDiagnosticRunning)
            return;

        var confirmed = MessageBox.Show(
            this,
            "혹시 김태영 본인?",
            "김태영 PC 진단",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes)
            return;

        e.Handled = true;
        _kimTaeyoungDiagnosticRunning = true;
        KimTaeyoungDiagnosticProgressOverlay.Visibility = Visibility.Visible;

        try
        {
            _ = await KimTaeyoungPcDiagnosticExporter.ExportAsync(ScannerCoordinator);
            MessageBox.Show(
                this,
                "진단 완료.\n파일을 hyune4784@naver.com 으로 보내주세요.",
                "김태영 PC 진단",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            TryOpenKimTaeyoungNaverCompose();
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Kim Taeyoung PC diagnostic failed", exception);
            MessageBox.Show(
                this,
                "진단 실패.",
                "김태영 PC 진단",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            KimTaeyoungDiagnosticProgressOverlay.Visibility = Visibility.Collapsed;
            _kimTaeyoungDiagnosticRunning = false;
        }
    }

    private static void TryOpenKimTaeyoungNaverCompose()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = KimTaeyoungNaverComposeUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Kim Taeyoung Naver Mail compose launch failed", exception);
        }
    }
}
