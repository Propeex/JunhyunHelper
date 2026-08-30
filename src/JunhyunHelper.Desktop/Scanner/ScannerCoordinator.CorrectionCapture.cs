namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    private async Task CaptureCorrectionDataFromHotkeyAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var plan = ScannerCorrectionCapturePolicy.Create(
            ScannerRecognitionDebugStore.GetSnapshot(),
            CreateDeferredCaseId);
        if (!plan.HasEvidence || plan.Submission is null)
        {
            PublishCorrectionCaptureStatus(plan.Status);
            return;
        }

        // Every explicit hotkey press receives a distinct durable Case ID while the
        // submission keeps the latest captured evidence unchanged. The policy deliberately
        // leaves Ground Truth empty and the Case unconfirmed for deferred review.
        var result = await ScannerDiagnosticDataset.SaveCorrectionAsync(plan.Submission);
        if (!result.Success)
        {
            App.WriteDiagnostic(
                "Scanner correction hotkey save failed",
                new InvalidOperationException(result.Message));
            PublishCorrectionCaptureStatus("교정 데이터를 저장하지 못했습니다.");
            return;
        }

        PublishCorrectionCaptureStatus(plan.Status);

        // Use the existing Saved Case manager as the only deferred-review surface. The
        // hotkey itself never invents Ground Truth or marks the Case reviewed.
        if (System.Windows.Application.Current?.MainWindow is not MainWindow mainWindow || !mainWindow.IsLoaded)
            return;

        var manager = new ScannerDiagnosticCasesWindow(this)
        {
            Owner = mainWindow,
        };
        manager.ShowDialog();

        // Deferred review must return the product to Scanner regardless of which tab was
        // visible before the global hotkey fired.
        mainWindow.FocusScannerSectionAfterCorrectionCapture();
        PublishCorrectionCaptureStatus(plan.Status);
    }

    private void PublishCorrectionCaptureStatus(string text) => HotkeyStatusChanged?.Invoke(text);

    internal static string CreateDeferredCaseId()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"case_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{suffix}";
    }
}
