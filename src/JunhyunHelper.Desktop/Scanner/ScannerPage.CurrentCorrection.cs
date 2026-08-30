using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private void CurrentCorrectionButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        if (_coordinator is null)
            return;

        var frame = ScannerRecognitionDebugStore.GetCorrectionSnapshot();
        if (frame is null)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "교정할 최신 Scanner 인식 이미지가 없습니다. 상세창을 연 뒤 스캔을 먼저 실행해 주세요.",
                "Scanner 교정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var window = new ScannerCorrectionWindow(frame, _coordinator)
        {
            Owner = Window.GetWindow(this),
        };
        window.ShowDialog();
        if (window.DatasetChanged)
            RuntimeStatusText.Text = "Scanner 교정 데이터를 저장했습니다.";
        RefreshActivityCorrectionAvailability();
    }
}
