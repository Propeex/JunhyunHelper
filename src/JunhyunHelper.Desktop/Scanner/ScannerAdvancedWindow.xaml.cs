using Microsoft.Win32;
using System.Windows;
using JunhyunHelper.Desktop;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerAdvancedWindow : Window, IInAppOverlayDialog
{
    private readonly ScannerCoordinator _coordinator;
    private Action<bool?>? _inAppCloseRequested;

    public ScannerAdvancedWindow(ScannerCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        UpdateTestToggle();
    }

    void IInAppOverlayDialog.AttachInAppOverlay(Action<bool?> closeRequested) =>
        _inAppCloseRequested = closeRequested ?? throw new ArgumentNullException(nameof(closeRequested));

    bool IInAppOverlayDialog.TryDismissInAppOverlay()
    {
        _inAppCloseRequested?.Invoke(true);
        return true;
    }

    private async void TestToggleButton_Click(object sender, RoutedEventArgs e)
    {
        TestToggleButton.IsEnabled = false;
        try
        {
            await _coordinator.SetTestEnabledAsync(!_coordinator.TestEnabled);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner advanced test toggle failed", exception);
            MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                "테스트 스캐너 상태를 변경하지 못했습니다.",
                "Scanner",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            UpdateTestToggle();
            TestToggleButton.IsEnabled = true;
        }
    }

    private void ManageCorrectionsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ScannerDiagnosticCasesWindow(_coordinator)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    private async void ExportDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Scanner 성능 진단 자료 저장",
            Filter = "ZIP 압축 파일 (*.zip)|*.zip",
            DefaultExt = ".zip",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"JunhyunHelper-Scanner-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog(System.Windows.Application.Current.MainWindow) != true)
            return;

        ExportDiagnosticsButton.IsEnabled = false;
        try
        {
            ScannerPerformanceTrace.Mark("support-bundle-export-start");
            var destination = dialog.FileName;
            await Task.Run(() => ScannerSupportBundleExporter.Export(destination));
            MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                "Scanner 성능 진단 자료를 저장했습니다. 이 ZIP 파일만 전달하면 세부 로그를 직접 찾아볼 필요가 없습니다.",
                "Scanner 성능 진단",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner support bundle export failed", exception);
            MessageBox.Show(
                System.Windows.Application.Current.MainWindow,
                "Scanner 성능 진단 자료를 저장하지 못했습니다.",
                "Scanner 성능 진단",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            ExportDiagnosticsButton.IsEnabled = true;
        }
    }

    private void UpdateTestToggle() =>
        TestToggleButton.Content = _coordinator.TestEnabled ? "테스트 스캐너 ON" : "테스트 스캐너 OFF";
}
