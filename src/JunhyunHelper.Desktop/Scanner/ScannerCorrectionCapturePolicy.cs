namespace JunhyunHelper.Desktop.Scanner;

internal sealed record ScannerCorrectionCapturePlan(
    ScannerCorrectionSubmission? Submission,
    string Status)
{
    public bool HasEvidence => Submission is not null;
}

internal static class ScannerCorrectionCapturePolicy
{
    internal const string NoEvidenceStatus = "저장할 스캔 결과가 없습니다.";

    public static ScannerCorrectionCapturePlan Create(
        ScannerRecognitionDebugFrame? latest,
        Func<string> createCaseId)
    {
        ArgumentNullException.ThrowIfNull(createCaseId);

        if (latest is null)
            return new ScannerCorrectionCapturePlan(null, NoEvidenceStatus);

        var savedFrame = latest with { CaseId = createCaseId() };
        var submission = new ScannerCorrectionSubmission(
            savedFrame,
            CorrectedDetailBounds: null,
            CorrectedTitleBounds: null,
            GroundTruthItemName: null,
            UserConfirmed: false);

        var savedLabel = string.IsNullOrWhiteSpace(latest.CandidateName)
            ? "인식되지 않은 결과"
            : latest.CandidateName.Trim();
        return new ScannerCorrectionCapturePlan(
            submission,
            $"교정 데이터를 저장했습니다: {savedLabel}");
    }
}
