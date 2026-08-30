using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerCorrectionCaptureSmoke
{
    public static void VerifyProductContract()
    {
        var noEvidence = ScannerCorrectionCapturePolicy.Create(null, static () => "case_unused");
        if (noEvidence.HasEvidence ||
            noEvidence.Submission is not null ||
            noEvidence.Status != ScannerCorrectionCapturePolicy.NoEvidenceStatus)
        {
            throw new InvalidOperationException("Scanner correction hotkey no-evidence contract failed.");
        }

        var image = BitmapSource.Create(
            2,
            2,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            new byte[16],
            8);
        image.Freeze();

        var completeFrame = new ScannerRecognitionDebugFrame(
            Image: image,
            CaptureOriginX: 100,
            CaptureOriginY: 200,
            Source: "tarkov-window",
            SelectedBounds: new Rect(1, 2, 30, 40),
            TitleBounds: new Rect(3, 4, 20, 8),
            MagnifierBounds: new Rect(5, 6, 7, 8),
            CloseBounds: new Rect(9, 10, 11, 12),
            StructuralScore: 0.91,
            StructuralReason: "complete",
            TitleAnchorScore: 0.88,
            TitleAnchorReason: "anchor",
            TitleSignature: "sig",
            Pass: "OCR",
            OcrText: "M855",
            UserSubstitutedOcrText: "M855",
            MatcherText: "M855",
            ItemId: "ammo-id",
            CandidateName: "5.56x45mm M855",
            RecognitionReason: "MATCHED",
            Confidence: 0.98);

        var complete = ScannerCorrectionCapturePolicy.Create(completeFrame, static () => "case_complete");
        if (!complete.HasEvidence ||
            complete.Submission is null ||
            complete.Submission.GroundTruthItemName is not null ||
            complete.Submission.UserConfirmed ||
            complete.Submission.Frame.CaseId != "case_complete" ||
            !ReferenceEquals(complete.Submission.Frame.Image, completeFrame.Image) ||
            complete.Submission.Frame.SelectedBounds != completeFrame.SelectedBounds ||
            complete.Submission.Frame.TitleBounds != completeFrame.TitleBounds ||
            complete.Submission.Frame.OcrText != completeFrame.OcrText ||
            complete.Submission.Frame.ItemId != completeFrame.ItemId ||
            complete.Status != "교정 데이터를 저장했습니다: 5.56x45mm M855")
        {
            throw new InvalidOperationException("Scanner correction hotkey complete-evidence contract failed.");
        }

        var incompleteFrame = completeFrame with
        {
            SelectedBounds = null,
            TitleBounds = null,
            ItemId = null,
            CandidateName = null,
            OcrText = "???",
            RecognitionReason = "UNRESOLVED",
        };
        var incomplete = ScannerCorrectionCapturePolicy.Create(incompleteFrame, static () => "case_incomplete");
        if (!incomplete.HasEvidence ||
            incomplete.Submission is null ||
            incomplete.Submission.Frame.SelectedBounds is not null ||
            incomplete.Submission.Frame.TitleBounds is not null ||
            incomplete.Submission.GroundTruthItemName is not null ||
            incomplete.Submission.UserConfirmed ||
            incomplete.Status != "교정 데이터를 저장했습니다: 인식되지 않은 결과")
        {
            throw new InvalidOperationException("Scanner correction hotkey incomplete-evidence contract failed.");
        }

        var duplicate = ScannerCorrectionCapturePolicy.Create(completeFrame, static () => "case_duplicate");
        if (duplicate.Submission is null ||
            duplicate.Submission.Frame.CaseId == complete.Submission.Frame.CaseId)
        {
            throw new InvalidOperationException("Scanner correction hotkey duplicate-save contract failed.");
        }
    }
}
