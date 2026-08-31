using System.Windows;
using System.Windows.Input;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop;

public partial class MainWindow
{
    private bool _kimTaeyoungDiagnosticRunning;

    private async void AppIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_kimTaeyoungDiagnosticRunning)
            return;

        var confirmed = MessageBox.Show(
            this,
            "김태영 본인이 맞습니까?\n\n" +
            "예를 누르면 Scanner와 화면 캡처 문제를 진단하기 위해 디스플레이·HDR·GPU·드라이버·Scanner 설정과 관련 프로그램 상태를 확인하고, 현재 화면 캡처 증거를 ZIP에 포함합니다.\n\n" +
            "ZIP은 자동 전송되지 않고 바탕화면에만 저장됩니다.",
            "김태영 PC 진단",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes)
            return;

        e.Handled = true;
        _kimTaeyoungDiagnosticRunning = true;
        var previousStatus = StatusText.Text;
        StatusText.Text = "김태영 PC 진단 중...";

        try
        {
            var archivePath = await KimTaeyoungPcDiagnosticExporter.ExportAsync(ScannerCoordinator);
            StatusText.Text = "김태영 PC 진단 완료";
            MessageBox.Show(
                this,
                "진단이 완료되었습니다.\n\n" +
                $"바탕화면에 다음 파일을 만들었습니다.\n{System.IO.Path.GetFileName(archivePath)}\n\n" +
                "이 ZIP 파일을 hyune4784@naver.com 으로 보내주세요.",
                "김태영 PC 진단 완료",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Kim Taeyoung PC diagnostic failed", exception);
            StatusText.Text = previousStatus;
            MessageBox.Show(
                this,
                "PC 진단 파일을 만들지 못했습니다. 준현 헬퍼를 다시 실행한 뒤 한 번 더 시도해 주세요.\n\n" +
                $"오류: {exception.GetType().Name}",
                "김태영 PC 진단",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _kimTaeyoungDiagnosticRunning = false;
        }
    }
}
