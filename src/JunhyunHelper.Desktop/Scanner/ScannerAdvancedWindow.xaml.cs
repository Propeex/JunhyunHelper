using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerAdvancedWindow : Window
{
    private readonly ScannerCoordinator _coordinator;

    public ScannerAdvancedWindow(ScannerCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        UpdateTestToggle();
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
            MessageBox.Show(this, "테스트 스캐너 상태를 변경하지 못했습니다.", "Scanner", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            UpdateTestToggle();
            TestToggleButton.IsEnabled = true;
        }
    }

    private void CurrentCorrectionButton_Click(object sender, RoutedEventArgs e)
    {
        var frame = ScannerRecognitionDebugStore.GetSnapshot();
        if (frame is null)
        {
            MessageBox.Show(
                this,
                "교정할 최신 Scanner 인식 이미지가 없습니다. 상세창을 연 뒤 스캔을 먼저 실행해 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var window = new ScannerCorrectionWindow(frame, _coordinator)
        {
            Owner = this,
        };
        window.ShowDialog();
    }

    private void ManageCorrectionsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ScannerDiagnosticCasesWindow(_coordinator)
        {
            Owner = this,
        };
        window.ShowDialog();
    }

    private void UpdateTestToggle() =>
        TestToggleButton.Content = _coordinator.TestEnabled ? "테스트 스캐너 ON" : "테스트 스캐너 OFF";

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
