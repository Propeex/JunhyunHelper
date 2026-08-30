using System.Windows;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    private async Task CaptureCorrectionDataFromHotkeyAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var latest = ScannerRecognitionDebugStore.GetSnapshot();
        if (latest is null)
        {
            PublishCorrectionCaptureStatus("저장할 스캔 결과가 없습니다.");
            return;
        }

        // A diagnostic frame's runtime Case ID identifies that recognition attempt. A user
        // may intentionally press the save hotkey more than once for the same latest result,
        // so every explicit save gets a distinct durable Case ID while all evidence remains
        // byte-for-byte/logically tied to the same captured frame.
        var savedFrame = latest with { CaseId = CreateDeferredCaseId() };
        var result = await ScannerDiagnosticDataset.SaveCorrectionAsync(
            new ScannerCorrectionSubmission(
                savedFrame,
                CorrectedDetailBounds: null,
                CorrectedTitleBounds: null,
                GroundTruthItemName: null,
                UserConfirmed: false));

        if (!result.Success)
        {
            App.WriteDiagnostic(
                "Scanner correction hotkey save failed",
                new InvalidOperationException(result.Message));
            PublishCorrectionCaptureStatus("교정 데이터를 저장하지 못했습니다.");
            return;
        }

        var savedLabel = string.IsNullOrWhiteSpace(latest.CandidateName)
            ? "인식되지 않은 결과"
            : latest.CandidateName.Trim();
        var status = $"교정 데이터를 저장했습니다: {savedLabel}";
        PublishCorrectionCaptureStatus(status);

        // Use the existing Saved Case manager as the only deferred-review surface. The
        // hotkey itself never invents Ground Truth or marks the Case reviewed.
        var mainWindow = System.Windows.Application.Current?.MainWindow;
        if (mainWindow is null || !mainWindow.IsLoaded)
            return;

        var manager = new ScannerDiagnosticCasesWindow(this)
        {
            Owner = mainWindow,
        };
        manager.ShowDialog();

        // Closing the manager returns focus to the product window. When the Scanner page
        // was the active product page, this preserves that exact working context.
        mainWindow.Activate();
        mainWindow.Focus();
        PublishCorrectionCaptureStatus(status);
    }

    private void PublishCorrectionCaptureStatus(string text) => HotkeyStatusChanged?.Invoke(text);

    internal static string CreateDeferredCaseId()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"case_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{suffix}";
    }
}
