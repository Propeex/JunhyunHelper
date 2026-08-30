namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    internal const string CorrectionSaveCompletedStatus = "저장 완료";

    private async Task CaptureCorrectionDataFromHotkeyAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var plan = ScannerCorrectionCapturePolicy.Create(
            ScannerRecognitionDebugStore.GetCorrectionSnapshot(),
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

        // A raid-time global hotkey is capture-only. Keep the durable evidence and the
        // short Mini Scanner confirmation, but never steal focus or open a review window.
        // Saved Cases remain available for explicit deferred review from the Scanner UI.
        PublishCorrectionCaptureStatus(plan.Status);
        _overlay.ShowTransientStatus(CorrectionSaveCompletedStatus);
    }

    private void PublishCorrectionCaptureStatus(string text) => HotkeyStatusChanged?.Invoke(text);

    internal static string CreateDeferredCaseId()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"case_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{suffix}";
    }
}
