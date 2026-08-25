using System.Windows;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Keeps exactly one latest Scanner diagnostic frame in memory. The frame carries a
/// stable Case ID so runtime logs and explicit user correction can be joined without
/// guessing. Durable correction/Ground Truth persistence happens only when the user
/// explicitly saves a reviewed case through ScannerDiagnosticDataset.
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
        lock (Gate)
        {
            var published = string.IsNullOrWhiteSpace(frame.CaseId)
                ? frame with { CaseId = CreateCaseId() }
                : frame;
            _frame = published;
            _lastCaptureUtc = DateTimeOffset.UtcNow;
            _lastSignature = published.TitleSignature ?? string.Empty;
        }
        Changed?.Invoke();
    }

    /// <summary>
    /// Stores the exact proposal set consumed by the runtime for this capture. Candidate
    /// geometry is converted to capture-local coordinates so correction UI and persisted
    /// Ground Truth use the same coordinate system as full.png.
    /// </summary>
    public static void UpdateCandidates(IReadOnlyList<ScannerInspectCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        lock (Gate)
        {
            if (_frame is null)
                return;

            _frame = _frame with
            {
                Candidates = candidates
                    .Select((candidate, index) => new ScannerDiagnosticCandidateEvidence(
                        Id: $"candidate-{index + 1:D2}",
                        Rank: index + 1,
                        Bounds: ToLocal(candidate.Bounds, _frame.CaptureOriginX, _frame.CaptureOriginY),
                        StructuralScore: candidate.StructuralScore,
                        StructuralReason: candidate.StructuralReason,
                        TitleBounds: candidate.TitleBounds.Width > 0 && candidate.TitleBounds.Height > 0
                            ? ToLocal(candidate.TitleBounds, _frame.CaptureOriginX, _frame.CaptureOriginY)
                            : null,
                        MagnifierBounds: candidate.MagnifierBounds is { Width: > 0, Height: > 0 } magnifier
                            ? ToLocal(magnifier, _frame.CaptureOriginX, _frame.CaptureOriginY)
                            : null,
                        CloseBounds: candidate.CloseBounds is { Width: > 0, Height: > 0 } close
                            ? ToLocal(close, _frame.CaptureOriginX, _frame.CaptureOriginY)
                            : null,
                        TitleAnchorScore: candidate.TitleAnchorScore,
                        TitleAnchorReason: candidate.TitleAnchorReason))
                    .ToArray(),
            };
        }
        Changed?.Invoke();
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

    private static Rect ToLocal(Rect absolute, int originX, int originY) =>
        new(absolute.X - originX, absolute.Y - originY, absolute.Width, absolute.Height);

    private static string CreateCaseId()
    {
        var sequence = Interlocked.Increment(ref _caseSequence) % 1000000;
        return $"case_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{sequence:D6}";
    }
}

public sealed record ScannerDiagnosticCandidateEvidence(
    string Id,
    int Rank,
    Rect Bounds,
    double StructuralScore,
    string StructuralReason,
    Rect? TitleBounds,
    Rect? MagnifierBounds,
    Rect? CloseBounds,
    double TitleAnchorScore,
    string TitleAnchorReason);

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
    string CaseId = "",
    IReadOnlyList<ScannerDiagnosticCandidateEvidence>? Candidates = null)
{
    public DateTimeOffset Timestamp { get; init; } = UpdatedAt ?? DateTimeOffset.Now;
}
