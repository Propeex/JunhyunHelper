using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public partial class ScannerPage
{
    private void OcrSubstitutionSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_coordinator is null)
            return;

        var dialog = new ScannerOcrSubstitutionSettingsWindow(_coordinator.OcrSubstitutions)
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true)
            return;

        _coordinator.ReplaceOcrSubstitutions(dialog.ResultRules);
        RuntimeStatusText.Text = dialog.ResultRules.Count == 0
            ? "사용자 OCR 문자 치환 규칙을 사용하지 않습니다."
            : $"사용자 OCR 문자 치환 규칙 {dialog.ResultRules.Count}개를 저장했습니다.";
    }
}
