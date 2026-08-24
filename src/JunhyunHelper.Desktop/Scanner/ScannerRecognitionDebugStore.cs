using System.Windows;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Keeps exactly one latest Scanner diagnostic frame in memory. The frame carries a
/// stable Case ID so runtime logs, user correction and persisted diagnostic data can be
/// joined without guessing. Persistence remains owned by ScannerDiagnosticDataset.
/// </summary>
public static class ScannerRecognitionDebugStore
{
    private static readonly object Gate = new();
    private static ScannerRecognitionDebugFrame? _frame;
    private static DateTimeOffset _lastCaptureUtc = DateTimeOffset.MinValue;
    private static string _lastSignature = string.Empty;
    private static long _caseSequence;

    public static event Action? Changed;

    public static bool ShouldCapture(string? signature, bool hasCandidate)
    {
        lock (Gate)
        {
            var now = DateTimeOffset.UtcNow;
            var interval = hasCandidate ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(1500);
            if (!string.IsNullOrWhiteSpace(signature) && !string.Equals(signature, _lastSignature, StringComparison.Ordinal))
                return true;
            return now - _lastCaptureUtc >= interval;
        }
    }

    public static void PublishCapture(ScannerRecognitionDebugFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ScannerRecognitionDebugFrame published;
        lock (Gate)
        {
            published = string.IsNullOrWhiteSpace(frame.CaseId)
                ? frame with { CaseId = CreateCaseId() }
                : frame;
            _frame = published;
            _lastCaptureUtc = DateTimeOffset.UtcNow;
            _lastSignature = published.TitleSignature ?? string.Empty;
        }
        Changed?.Invoke();

        // Structural/detail and header-lock failures happen before OCR. Give those
        // observations an explicit diagnostic reason only for persistence; identity still
        // remains NOT_RUN in the live frame. The dataset fingerprint gate prevents the
        // same stationary failure from being written every capture tick.
        var retentionFrame = BuildPreOcrRetentionFrame(published);
        if (retentionFrame is not null)
            ScannerDiagnosticDataset.QueueAutomaticObservation(retentionFrame);
    }

    public static void UpdateAnalysis(
        ScannerInspectCandidate? candidate,
        string pass,
        string ocrText,
        string matcherText,
        ScannerRecognition recognition) =>
        UpdateAnalysis(candidate, pass, ocrText, ocrText, matcherText, recognition);

    public static void UpdateAnalysis(
        ScannerInspectCandidate? candidate,
        string pass,
        string ocrText,
        string userSubstitutedOcrText,
        string matcherText,
        ScannerRecognition recognition)
    {
        ScannerCaptureMode mode;
        lock (Gate)
        {
            mode = _frame?.CaptureMode ??
                (_frame?.Source.StartsWith("display:", StringComparison.OrdinalIgnoreCase) == true
                    ? ScannerCaptureMode.DisplayTest
                    : ScannerCaptureMode.TarkovWindow);
        }
        UpdateAnalysis(
            candidate,
            mode,
            pass,
            ocrText,
            userSubstitutedOcrText,
            matcherText,
            recognition);
        ScannerDiagnosticDataset.QueueAutomaticObservation(GetSnapshot());
    }

    public static void UpdateAnalysis(
        ScannerInspectCandidate? candidate,
        ScannerCaptureMode mode,
        string pass,
        string ocrText,
        string matcherText,
        ScannerRecognition recognition) =>
        UpdateAnalysis(candidate, mode, pass, ocrText, ocrText, matcherText, recognition);

    public static void UpdateAnalysis(
        ScannerInspectCandidate? candidate,
        ScannerCaptureMode mode,
        string pass,
        string ocrText,
        string userSubstitutedOcrText,
        string matcherText,
        ScannerRecognition recognition)
    {
        lock (Gate)
        {
            if (_frame is null)
                return;

            var selected = candidate is null
                ? _frame.SelectedBounds
                : ToLocal(candidate.Bounds, _frame.CaptureOriginX, _frame.CaptureOriginY);
            var title = candidate is null || candidate.TitleBounds.Width <= 0
                ? _frame.TitleBounds
                : ToLocal(candidate.TitleBounds, _frame.CaptureOriginX, _frame.CaptureOriginY);
            var magnifier = candidate?.MagnifierBounds is { } magnifierBounds && magnifierBounds.Width > 0
                ? ToLocal(magnifierBounds, _frame.CaptureOriginX, _frame.CaptureOriginY)
                : _frame.MagnifierBounds;
            var close = candidate?.CloseBounds is { } closeBounds && closeBounds.Width > 0
                ? ToLocal(closeBounds, _frame.CaptureOriginX, _frame.CaptureOriginY)
                : _frame.CloseBounds;
            var now = DateTimeOffset.Now;

            // The initial frame is captured from the detector's current top candidate.
            // Semantic/visual matching may ultimately select another candidate, so the
            // diagnostics must move both rectangles and scores to the selected one.
            // Matcher failures can still carry their nearest catalog candidate for
            // diagnostics. Only a successful recognition may publish ItemId as the final
            // identity; failed candidate IDs remain available through TopCandidates.
            _frame = _frame with
            {
                SelectedBounds = selected,
                TitleBounds = title,
                MagnifierBounds = magnifier,
                CloseBounds = close,
                StructuralScore = candidate?.StructuralScore ?? _frame.StructuralScore,
                StructuralReason = candidate?.StructuralReason ?? _frame.StructuralReason,
                TitleAnchorScore = candidate?.TitleAnchorScore ?? _frame.TitleAnchorScore,
                TitleAnchorReason = candidate?.TitleAnchorReason ?? _frame.TitleAnchorReason,
                CaptureMode = mode,
                Pass = pass,
                OcrText = ocrText,
                UserSubstitutedOcrText = userSubstitutedOcrText,
                MatcherText = matcherText,
                ItemId = recognition.Success ? recognition.ItemId : null,
                CandidateName = recognition.OfficialName,
                RecognitionReason = recognition.Reason,
                Confidence = recognition.Confidence,
                SecondScore = recognition.SecondScore,
                TopCandidates = recognition.TopCandidates,
                UpdatedAt = now,
                Timestamp = now,
            };
        }
        Changed?.Invoke();
    }

    public static ScannerRecognitionDebugFrame? GetSnapshot()
    {
        lock (Gate)
            return _frame;
    }

    public static void Clear()
    {
        lock (Gate)
        {
            _frame = null;
            _lastSignature = string.Empty;
            _lastCaptureUtc = DateTimeOffset.MinValue;
        }
        Changed?.Invoke();
    }

    private static ScannerRecognitionDebugFrame? BuildPreOcrRetentionFrame(ScannerRecognitionDebugFrame frame)
    {
        if (!string.Equals(frame.RecognitionReason, "NOT_RUN", StringComparison.Ordinal))
            return null;
        if (string.Equals(frame.StructuralReason, "NO_DETAIL_CANDIDATE", StringComparison.Ordinal))
            return frame with { RecognitionReason = "DETAIL_WINDOW_NOT_DETECTED" };
        if (!string.Equals(frame.TitleAnchorReason, "HEADER_FRAME_LOCKED", StringComparison.Ordinal))
            return frame with { RecognitionReason = "TITLE_ANCHOR_NOT_LOCKED" };
        return null;
    }

    private static Rect ToLocal(Rect absolute, int originX, int originY) =>
        new(absolute.X - originX, absolute.Y - originY, absolute.Width, absolute.Height);

    private static string CreateCaseId()
    {
        var sequence = Interlocked.Increment(ref _caseSequence) % 1000000;
        return $"case_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{sequence:D6}";
    }
}

public sealed record ScannerRecognitionDebugFrame(
    BitmapSource Image,
    int CaptureOriginX,
    int CaptureOriginY,
    string Source,
    Rect? SelectedBounds,
    Rect? TitleBounds,
    Rect? MagnifierBounds,
    Rect? CloseBounds,
    double StructuralScore,
    string StructuralReason,
    double TitleAnchorScore,
    string TitleAnchorReason,
    string? TitleSignature = null,
    string Pass = "NONE",
    string OcrText = "",
    string UserSubstitutedOcrText = "",
    string MatcherText = "",
    string? ItemId = null,
    string? CandidateName = null,
    string RecognitionReason = "NOT_RUN",
    double Confidence = 0,
    double SecondScore = 0,
    IReadOnlyList<ScannerMatchCandidate>? TopCandidates = null,
    DateTimeOffset? UpdatedAt = null,
    ScannerCaptureMode? CaptureMode = null,
    string CaseId = "")
{
    public DateTimeOffset Timestamp { get; init; } = UpdatedAt ?? DateTimeOffset.Now;
}
