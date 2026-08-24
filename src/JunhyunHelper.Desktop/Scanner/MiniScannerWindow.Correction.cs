using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class MiniScannerWindow
{
    private void CurrentCorrectionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var frame = ScannerRecognitionDebugStore.GetSnapshot();
        var owner = System.Windows.Application.Current?.MainWindow;
        if (frame is null)
        {
            MessageBox.Show(
                owner,
                "교정할 최신 Scanner 인식 이미지가 없습니다.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var coordinator = (owner as MainWindow)?.ScannerCoordinator;
        var correction = new ScannerCorrectionWindow(frame, coordinator)
        {
            Owner = owner,
        };
        correction.ShowDialog();
    }
}
