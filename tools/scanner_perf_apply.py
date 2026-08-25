from pathlib import Path


def replace(path, old, new, count=1):
    p = Path(path)
    text = p.read_text(encoding="utf-8")
    actual = text.count(old)
    if actual != count:
        raise SystemExit(f"{path}: expected {count} occurrences, found {actual}: {old[:100]!r}")
    p.write_text(text.replace(old, new, count), encoding="utf-8")


# Runtime cadence + conservative adaptive retry. Thresholds/caps stay untouched.
runtime = "src/JunhyunHelper.Desktop/Scanner/ScannerRuntimeService.cs"
replace(
    runtime,
    "    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(350);\n    private static readonly TimeSpan SemanticRetryInterval = TimeSpan.FromMilliseconds(1200);",
    "    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);",
)
replace(
    runtime,
    "    private int _candidatePresenceHits;\n    private int _consecutiveMisses;\n    private DateTimeOffset _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;",
    "    private int _candidatePresenceHits;\n    private int _consecutiveMisses;\n    private int _semanticFailureCount;\n    private DateTimeOffset _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;",
)
replace(
    runtime,
    "    private Rect? _verifiedBounds;\n    private string _verifiedTitleSignature = string.Empty;\n    private ScannerItemSnapshot? _currentSnapshot;",
    "    private Rect? _verifiedBounds;\n    private ScannerInspectCandidate? _verifiedCandidate;\n    private string _verifiedTitleSignature = string.Empty;\n    private ScannerItemSnapshot? _currentSnapshot;",
)

old = '''                using var latencyCycle = ScannerLatencyTelemetry.BeginCycle(mode, "continuous");
                var candidates = await ObserveCandidatesAsync(cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;
'''
new = '''                using var latencyCycle = ScannerLatencyTelemetry.BeginCycle(mode, "continuous");

                // Once an Item has been fully verified, first revalidate only that known
                // detail-window rectangle with the same close-X/magnifier/header lock and
                // a freshly computed title signature. This is never allowed to identify a
                // new Item: any miss or title change falls through to the original full
                // client capture + proposal pipeline in this same cycle.
                if (_currentSnapshot is not null && _verifiedCandidate is not null)
                {
                    var tracked = await ObserveTrackedCandidateAsync(cancellationToken);
                    if (epoch != Volatile.Read(ref _loopEpoch))
                        return;

                    if (tracked is not null &&
                        HasTrustedTitleAnchors(tracked) &&
                        GeometryDistance(tracked.Bounds, _verifiedCandidate.Bounds) <= VerifiedGeometryDistanceLimit &&
                        string.Equals(tracked.TitleSignature, _verifiedTitleSignature, StringComparison.Ordinal))
                    {
                        _verifiedCandidate = tracked;
                        _verifiedBounds = tracked.Bounds;
                        if (DateTimeOffset.UtcNow >= _nextPresentationRefreshAtUtc)
                        {
                            _nextPresentationRefreshAtUtc = DateTimeOffset.UtcNow + PresentationRefreshInterval;
                            var refreshed = _presentation.CreateSnapshot(_currentSnapshot.ItemId);
                            if (refreshed is null)
                            {
                                ScheduleSemanticRetry();
                                ClearVerifiedItem();
                                const string refreshMessage = "현재 아이템 표시 데이터를 갱신할 수 없어 다시 확인합니다.";
                                _overlay.ReportTransientMiss(refreshMessage);
                                Publish(ScannerRuntimeState.Uncertain, refreshMessage, captureMode: mode);
                                continue;
                            }
                            _currentSnapshot = refreshed;
                        }

                        _overlay.Show(_currentSnapshot);
                        continue;
                    }
                }

                var candidates = await ObserveCandidatesAsync(cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;
'''
replace(runtime, old, new)
replace(
    runtime,
    '''                    if (closest.Distance <= VerifiedGeometryDistanceLimit &&
                        string.Equals(closest.Candidate.TitleSignature, _verifiedTitleSignature, StringComparison.Ordinal))
                    {
                        if (DateTimeOffset.UtcNow >= _nextPresentationRefreshAtUtc)''',
    '''                    if (closest.Distance <= VerifiedGeometryDistanceLimit &&
                        string.Equals(closest.Candidate.TitleSignature, _verifiedTitleSignature, StringComparison.Ordinal))
                    {
                        _verifiedCandidate = closest.Candidate;
                        _verifiedBounds = closest.Candidate.Bounds;
                        if (DateTimeOffset.UtcNow >= _nextPresentationRefreshAtUtc)''',
)
replace(
    runtime,
    '''                    ClearVerifiedItem();
                    _candidatePresenceHits = 1;
                    const string changedMessage = "아이템 제목 변화를 확인하는 중입니다.";''',
    '''                    ClearVerifiedItem();
                    ResetSemanticRetry();
                    _candidatePresenceHits = 1;
                    const string changedMessage = "아이템 제목 변화를 확인하는 중입니다.";''',
)
replace(
    runtime,
    '''                _nextSemanticAttemptAtUtc = DateTimeOffset.UtcNow + SemanticRetryInterval;
                const string readingMessage = "아이템 이름을 읽는 중입니다.";''',
    '''                const string readingMessage = "아이템 이름을 읽는 중입니다.";''',
)
replace(
    runtime,
    '''                if (!search.Success || search.Candidate is null ||
                    string.IsNullOrWhiteSpace(search.Recognition.ItemId))
                {
                    ClearVerifiedItem();''',
    '''                if (!search.Success || search.Candidate is null ||
                    string.IsNullOrWhiteSpace(search.Recognition.ItemId))
                {
                    ScheduleSemanticRetry();
                    ClearVerifiedItem();''',
)
replace(
    runtime,
    '''                if (snapshot is null)
                {
                    ClearVerifiedItem();''',
    '''                if (snapshot is null)
                {
                    ScheduleSemanticRetry();
                    ClearVerifiedItem();''',
    count=1,
)
replace(
    runtime,
    '''                _verifiedBounds = search.Candidate.Bounds;
                _verifiedTitleSignature = search.Candidate.TitleSignature;
                _currentSnapshot = snapshot;''',
    '''                ResetSemanticRetry();
                _verifiedBounds = search.Candidate.Bounds;
                _verifiedCandidate = search.Candidate;
                _verifiedTitleSignature = search.Candidate.TitleSignature;
                _currentSnapshot = snapshot;''',
    count=1,
)
replace(
    runtime,
    '''    private async Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken)
    {
        await _captureGate.WaitAsync(cancellationToken);
        try
        {
            return await ObserveCandidatesCoreAsync(cancellationToken);
        }
        finally
        {
            _captureGate.Release();
        }
    }
''',
    '''    private async Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken)
    {
        await _captureGate.WaitAsync(cancellationToken);
        try
        {
            return await ObserveCandidatesCoreAsync(cancellationToken);
        }
        finally
        {
            _captureGate.Release();
        }
    }

    private async Task<ScannerInspectCandidate?> ObserveTrackedCandidateAsync(CancellationToken cancellationToken)
    {
        if (_verifiedCandidate is null || _detector is not IScannerTrackedInspectDetector trackedDetector)
            return null;

        await _captureGate.WaitAsync(cancellationToken);
        try
        {
            var tracked = await trackedDetector.ObserveTrackedAsync(_verifiedCandidate, cancellationToken);
            return tracked is null ? null : NormalizeTitleIdentitySignature(tracked);
        }
        finally
        {
            _captureGate.Release();
        }
    }
''',
)
replace(
    runtime,
    '''        var overlapsPrevious = current.Count > 0 && _previousCandidateGeometrySignatures.Overlaps(current);
        _candidatePresenceHits = overlapsPrevious
            ? Math.Min(_candidatePresenceHits + 1, StableCandidateHitsRequired)
            : 1;
''',
    '''        var overlapsPrevious = current.Count > 0 && _previousCandidateGeometrySignatures.Overlaps(current);
        if (!overlapsPrevious)
            ResetSemanticRetry();
        _candidatePresenceHits = overlapsPrevious
            ? Math.Min(_candidatePresenceHits + 1, StableCandidateHitsRequired)
            : 1;
''',
)
replace(
    runtime,
    '''    private void HandleMiss(ScannerCaptureMode mode, string detectorMessage)
    {
        _consecutiveMisses++;
        _candidatePresenceHits = 0;''',
    '''    private void HandleMiss(ScannerCaptureMode mode, string detectorMessage)
    {
        _consecutiveMisses++;
        _candidatePresenceHits = 0;
        ResetSemanticRetry();''',
)
replace(
    runtime,
    '''    private void ClearVerifiedItem()
    {
        _verifiedBounds = null;
        _verifiedTitleSignature = string.Empty;''',
    '''    private void ClearVerifiedItem()
    {
        _verifiedBounds = null;
        _verifiedCandidate = null;
        _verifiedTitleSignature = string.Empty;''',
)
replace(
    runtime,
    '''        _previousCandidateGeometrySignatures.Clear();
        _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;
        ClearVerifiedItem();''',
    '''        _previousCandidateGeometrySignatures.Clear();
        ResetSemanticRetry();
        ClearVerifiedItem();''',
)
replace(
    runtime,
    '''    private void StopLoop()
    {''',
    '''    private void ResetSemanticRetry()
    {
        _semanticFailureCount = 0;
        _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;
    }

    private void ScheduleSemanticRetry()
    {
        _semanticFailureCount = Math.Min(_semanticFailureCount + 1, 32);
        _nextSemanticAttemptAtUtc = DateTimeOffset.UtcNow +
            ScannerSemanticRetryPolicy.DelayAfterFailure(_semanticFailureCount);
    }

    private void StopLoop()
    {''',
)

# One-shot stores the fully verified candidate too; no cadence/cap changes.
oneshot = "src/JunhyunHelper.Desktop/Scanner/ScannerRuntimeService.OneShot.cs"
replace(
    oneshot,
    '''        _verifiedBounds = search.Candidate.Bounds;
        _verifiedTitleSignature = search.Candidate.TitleSignature;
        _currentSnapshot = snapshot;''',
    '''        _verifiedBounds = search.Candidate.Bounds;
        _verifiedCandidate = search.Candidate;
        _verifiedTitleSignature = search.Candidate.TitleSignature;
        _currentSnapshot = snapshot;''',
)
replace(
    oneshot,
    "// Unlike the 350ms continuous loop, one-shot mode intentionally spends",
    "// Unlike the continuous loop, one-shot mode intentionally spends",
)

# Optional tracked-detector boundary. It never returns an Item identity.
interfaces = "src/JunhyunHelper.Desktop/Scanner/ScannerVisionInterfaces.cs"
replace(
    interfaces,
    '''public interface IScannerCandidateInspectDetector : IScannerInspectDetector
{
    Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken);
}
''',
    '''public interface IScannerCandidateInspectDetector : IScannerInspectDetector
{
    Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Optional fast path for revalidating a previously fully verified detail window.
/// Implementations must return fresh pixels and the same semantic header evidence.
/// A tracked candidate is never an Item identity proof by itself; runtime falls back to
/// the full detector whenever the fresh title signature is different or validation fails.
/// </summary>
public interface IScannerTrackedInspectDetector : IScannerCandidateInspectDetector
{
    Task<ScannerInspectCandidate?> ObserveTrackedAsync(
        ScannerInspectCandidate previous,
        CancellationToken cancellationToken);
}
''',
)

# Detector fast-path + direct BGRA SoftwareBitmap OCR.
vision = "src/JunhyunHelper.Desktop/Scanner/ScannerLab38WindowsVision.cs"
replace(
    vision,
    "using Windows.Globalization;\nusing Windows.Media.Ocr;\nusing Windows.Storage.Streams;",
    "using Windows.Globalization;\nusing Windows.Graphics.Imaging;\nusing Windows.Media.Ocr;\nusing Windows.Security.Cryptography;",
)
replace(
    vision,
    "public sealed class ScannerLab38InspectDetector : IScannerCandidateInspectDetector",
    "public sealed class ScannerLab38InspectDetector : IScannerTrackedInspectDetector",
)
replace(
    vision,
    "    private const int CandidateLimit = 12;\n    private static readonly TimeSpan WindowDiscoveryInterval = TimeSpan.FromSeconds(1.5);",
    "    private const int CandidateLimit = 12;\n    private const int TrackedCaptureMargin = 24;\n    private static readonly TimeSpan WindowDiscoveryInterval = TimeSpan.FromSeconds(1.5);",
)
marker = '''    private IReadOnlyList<ScannerInspectCandidate> ObserveTarkovWindow(CancellationToken cancellationToken)
    {'''
tracked_methods = r'''    public Task<ScannerInspectCandidate?> ObserveTrackedAsync(
        ScannerInspectCandidate previous,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CaptureMode == ScannerCaptureMode.TarkovWindow
            ? ObserveTrackedTarkovWindow(previous, cancellationToken)
            : ObserveTrackedScreen(previous, cancellationToken));
    }

    private ScannerInspectCandidate? ObserveTrackedTarkovWindow(
        ScannerInspectCandidate previous,
        CancellationToken cancellationToken)
    {
        var window = GetTarkovWindow();
        if (window == IntPtr.Zero || IsIconic(window) || !IsWindowVisible(window) ||
            !TryGetClientScreenRect(window, out var clientRect) ||
            !TryCreateTrackedCaptureRect(previous.Bounds, clientRect, out var captureRect))
        {
            return null;
        }

        return CaptureAndValidateTracked(previous, captureRect, cancellationToken);
    }

    private ScannerInspectCandidate? ObserveTrackedScreen(
        ScannerInspectCandidate previous,
        CancellationToken cancellationToken)
    {
        if (!TryCreateTrackedCaptureRect(previous.Bounds, null, out var captureRect))
            return null;
        return CaptureAndValidateTracked(previous, captureRect, cancellationToken);
    }

    private static ScannerInspectCandidate? CaptureAndValidateTracked(
        ScannerInspectCandidate previous,
        NativeRect captureRect,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Bitmap bitmap;
        try
        {
            using (ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.Capture))
                bitmap = CaptureScreenRectangle(captureRect);
        }
        catch (Exception exception) when (
            exception is ExternalException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            return null;
        }

        using (bitmap)
            return DetectTrackedCandidate(bitmap, captureRect.Left, captureRect.Top, previous);
    }

    private static ScannerInspectCandidate? DetectTrackedCandidate(
        Bitmap bitmap,
        int screenLeft,
        int screenTop,
        ScannerInspectCandidate previous)
    {
        (byte[] Pixels, int Stride) data;
        using (ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.Capture))
            data = ReadBgra(bitmap);

        var localWindow = ToLocalRegion(
            previous.Bounds,
            screenLeft,
            screenTop,
            bitmap.Width,
            bitmap.Height,
            previous.StructuralScore);
        var localTitle = ToLocalRegion(
            previous.TitleBounds,
            screenLeft,
            screenTop,
            bitmap.Width,
            bitmap.Height,
            previous.TitleAnchorScore);
        var localClose = previous.CloseBounds is { } close
            ? ToLocalRegion(close, screenLeft, screenTop, bitmap.Width, bitmap.Height, previous.TitleAnchorScore)
            : default;

        if (localWindow.Width < 2 || localWindow.Height < 2 || localTitle.Width < 1 || localClose.Width < 1)
            return null;

        ScannerTitleAnchorRefinement anchors;
        using (ScannerLatencyTelemetry.Measure(ScannerLatencyTelemetry.SemanticHeader))
        {
            anchors = ScannerTitleAnchorRefiner.Refine(
                data.Pixels,
                bitmap.Width,
                bitmap.Height,
                data.Stride,
                new ScannerDetectedCandidate(localWindow, localTitle, localClose, "TRACKED_VERIFIED_BOUNDS"));
        }

        if (anchors.Reason != "HEADER_FRAME_LOCKED" ||
            anchors.Score < 0.68 ||
            anchors.Magnifier.Width <= 0 ||
            anchors.CloseButton.Width <= 0)
        {
            return null;
        }

        var lockedWindow = RefineLockedWindow(localWindow, anchors, bitmap.Width, bitmap.Height);
        var titlePixels = CropBgra(
            data.Pixels,
            data.Stride,
            anchors.Title,
            bitmap.Width,
            bitmap.Height,
            out var titleStride);
        if (titlePixels.Length == 0)
            return null;

        var titleImage = BitmapSource.Create(
            anchors.Title.Width,
            anchors.Title.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            titlePixels,
            titleStride);
        titleImage.Freeze();

        var windowBounds = ToScreenRect(lockedWindow, screenLeft, screenTop);
        var titleBounds = ToScreenRect(anchors.Title, screenLeft, screenTop);
        var magnifierBounds = ToScreenRect(anchors.Magnifier, screenLeft, screenTop);
        var closeBounds = ToScreenRect(anchors.CloseButton, screenLeft, screenTop);
        var geometrySignature = $"tracked:{Quantize(lockedWindow.X + screenLeft)}:{Quantize(lockedWindow.Y + screenTop)}:{Quantize(lockedWindow.Width)}:{Quantize(lockedWindow.Height)}";
        var titleSignature = $"{HashPixels(titlePixels):X16}";

        return new ScannerInspectCandidate(
            windowBounds,
            geometrySignature,
            titleSignature,
            titleImage,
            previous.StructuralScore,
            "TRACKED_VERIFIED_BOUNDS",
            titleBounds,
            magnifierBounds,
            closeBounds,
            anchors.Score,
            anchors.Reason);
    }

    private static ScannerDetectedRegion ToLocalRegion(
        Rect screenBounds,
        int screenLeft,
        int screenTop,
        int captureWidth,
        int captureHeight,
        double score)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0)
            return default;

        var left = Math.Clamp((int)Math.Round(screenBounds.Left) - screenLeft, 0, Math.Max(0, captureWidth - 1));
        var top = Math.Clamp((int)Math.Round(screenBounds.Top) - screenTop, 0, Math.Max(0, captureHeight - 1));
        var right = Math.Clamp((int)Math.Round(screenBounds.Right) - screenLeft, left + 1, captureWidth);
        var bottom = Math.Clamp((int)Math.Round(screenBounds.Bottom) - screenTop, top + 1, captureHeight);
        return new ScannerDetectedRegion(left, top, right - left, bottom - top, score);
    }

    private static bool TryCreateTrackedCaptureRect(
        Rect previousBounds,
        NativeRect? containingRect,
        out NativeRect captureRect)
    {
        captureRect = default;
        if (previousBounds.Width < 150 || previousBounds.Height < 80)
            return false;

        var left = (int)Math.Floor(previousBounds.Left) - TrackedCaptureMargin;
        var top = (int)Math.Floor(previousBounds.Top) - TrackedCaptureMargin;
        var right = (int)Math.Ceiling(previousBounds.Right) + TrackedCaptureMargin;
        var bottom = (int)Math.Ceiling(previousBounds.Bottom) + TrackedCaptureMargin;

        if (containingRect is { } container)
        {
            if (previousBounds.Right < container.Left || previousBounds.Left > container.Right ||
                previousBounds.Bottom < container.Top || previousBounds.Top > container.Bottom)
            {
                return false;
            }
            left = Math.Max(left, container.Left);
            top = Math.Max(top, container.Top);
            right = Math.Min(right, container.Right);
            bottom = Math.Min(bottom, container.Bottom);
        }

        if (right - left < 150 || bottom - top < 80)
            return false;

        captureRect = new NativeRect { Left = left, Top = top, Right = right, Bottom = bottom };
        return true;
    }

'''
replace(vision, marker, tracked_methods + marker)

old_recognize = r'''    private async Task<OcrResult> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        byte[] png;
        using (var memory = new MemoryStream())
        {
            encoder.Save(memory);
            png = memory.ToArray();
        }

        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _engine!.RecognizeAsync(softwareBitmap);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
'''
new_recognize = r'''    private async Task<OcrResult> RecognizeAsync(BitmapSource image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        BitmapSource bgra = image.Format == PixelFormats.Bgra32
            ? image
            : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        if (!bgra.IsFrozen && bgra is Freezable freezable && freezable.CanFreeze)
            freezable.Freeze();

        var stride = checked(bgra.PixelWidth * 4);
        var pixels = new byte[checked(stride * bgra.PixelHeight)];
        bgra.CopyPixels(pixels, stride, 0);

        var buffer = CryptographicBuffer.CreateFromByteArray(pixels);
        using var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
            buffer,
            BitmapPixelFormat.Bgra8,
            bgra.PixelWidth,
            bgra.PixelHeight,
            BitmapAlphaMode.Premultiplied);

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _engine!.RecognizeAsync(softwareBitmap);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
'''
replace(vision, old_recognize, new_recognize)

# Deterministic retry policy in Core.
Path("src/JunhyunHelper.Core/Scanner/ScannerSemanticRetryPolicy.cs").write_text(
    '''namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Conservative retry pacing for repeated semantic/OCR failures on the same stable
/// detail-window geometry. A new candidate lineage is eligible immediately; persistent
/// failures back off to the previous 1200 ms ceiling to bound CPU usage.
/// </summary>
public static class ScannerSemanticRetryPolicy
{
    public static TimeSpan DelayAfterFailure(int consecutiveFailures) =>
        consecutiveFailures switch
        {
            <= 0 => TimeSpan.Zero,
            1 => TimeSpan.FromMilliseconds(250),
            2 => TimeSpan.FromMilliseconds(500),
            3 => TimeSpan.FromMilliseconds(800),
            _ => TimeSpan.FromMilliseconds(1200),
        };
}
''',
    encoding="utf-8",
)
Path("tests/JunhyunHelper.Tests/Scanner/ScannerSemanticRetryPolicyTests.cs").write_text(
    '''using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerSemanticRetryPolicyTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 250)]
    [InlineData(2, 500)]
    [InlineData(3, 800)]
    [InlineData(4, 1200)]
    [InlineData(20, 1200)]
    public void DelayAfterFailure_UsesFastRetryThenPreviousCeiling(int failures, int expectedMilliseconds)
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(expectedMilliseconds),
            ScannerSemanticRetryPolicy.DelayAfterFailure(failures));
    }
}
''',
    encoding="utf-8",
)

# Release candidate identity/docs.
csproj = "src/JunhyunHelper.Desktop/JunhyunHelper.Desktop.csproj"
replace(csproj, "<Version>1.7.2</Version>", "<Version>1.7.3</Version>")
Path("packaging/FIRST_RUN_KO.txt").write_text(
    '''준현 헬퍼 v1.7.3 — Windows x64

실행 방법
1. 받은 'Junhyun-Helper.zip'을 원하는 위치에 압축 해제합니다.
2. 압축을 풀면 생성되는 '준현 헬퍼' 폴더 안의 '준현 헬퍼.exe'를 실행합니다.
3. 처음 실행하면 프로필을 만들고 필요한 최신 게임 데이터를 온라인에서 내려받습니다.
4. 프로그램 실행 시 GitHub의 최신 정식 준현 헬퍼 릴리즈를 확인합니다. 더 최신 버전이 있으면 업데이트 여부를 묻습니다.

v1.7.3 주요 변경 — Scanner 인식 지연 최적화
- Scanner 관측 주기를 350 ms에서 200 ms로 단축해 상세창 변화 감지를 더 촘촘하게 수행합니다.
- 새 상세창의 첫 식별 시도는 기존 안전 게이트를 통과하는 즉시 수행하고, 실패한 동일 후보만 250 → 500 → 800 → 1200 ms로 보수적으로 backoff합니다.
- Windows OCR 입력에서 PNG 인코딩/디코딩 왕복을 제거하고 동일 BGRA 픽셀을 SoftwareBitmap으로 직접 전달합니다.
- 이미 확정된 상세창은 이전 위치의 좁은 화면 영역만 먼저 다시 캡처해 close-X, 돋보기, HEADER_FRAME_LOCKED와 새 제목 픽셀을 재검증합니다.
- fast-path에서 제목 변화나 검증 실패가 감지되면 같은 cycle에서 기존 전체 Tarkov 창 탐지로 즉시 되돌아갑니다.
- 후보 선택, deep OCR, Tarkov-font visual recovery의 판정 로직은 성능을 이유로 생략하거나 완화하지 않았습니다.

Scanner 인식 안전 계약
- structural floor 0.34, HEADER_FRAME_LOCKED 0.68, continuous candidate cap 8, one-shot candidate cap 12는 그대로 유지합니다.
- false positive보다 miss를 선호하며, fast-path는 이전 Item ID를 새 identity 증거로 사용하지 않습니다.
- cross-frame OCR 결과를 재사용하지 않습니다. 매 인식 판단의 OCR/제목 픽셀은 현재 화면에서 새로 획득합니다.
- Item ID가 확정되기 전에는 가격·필요 개수 같은 mapped data를 identity 증거로 사용하지 않습니다.
- Scanner 인식 중 네트워크 요청, 게임 메모리 읽기, DLL injection, packet interception을 사용하지 않습니다.

사용자 데이터
- 프로필, 진행도, Scanner 설정, 교정 데이터, 로그 등 변경 가능한 사용자 데이터는 프로그램 폴더가 아니라 %LocalAppData%\\JunhyunHelper 아래에 보관됩니다.
- 프로그램 업데이트로 기존 사용자 진행도와 Scanner Ground Truth를 덮어쓰지 않습니다.
''',
    encoding="utf-8",
)
Path("docs/DECISION_V1.7.3_SCANNER_PERFORMANCE_2026-08-25.md").write_text(
    '''# v1.7.3 Scanner Performance Pass

기준일: 2026-08-25
상태: IMPLEMENTED — RELEASE CANDIDATE VALIDATION

## 목표

실사용에서 체감한 약 1초 단위 semantic recognition cadence를 줄이되 Scanner의 false-positive 방지 계약을 변경하지 않는다.

## 고정 안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
false positive보다 miss 선호
current official Korean catalog authority
ambiguity / low confidence fail closed
cross-frame OCR identity cache 금지
scan-time network / game memory / injection / packet interception 금지
```

## 적용

- Windows OCR PNG encode/stream/decode round-trip 제거. 동일 BGRA 픽셀을 SoftwareBitmap으로 직접 전달.
- continuous observation 350 ms → 200 ms.
- 동일 안정 후보 반복 실패만 250 / 500 / 800 / 1200 ms adaptive backoff. 1200 ms ceiling 유지.
- verified detail rectangle을 24px margin의 좁은 fresh capture로 먼저 재검증.
- tracked path도 close-X + magnifier + HEADER_FRAME_LOCKED >= 0.68 + fresh title signature를 모두 요구.
- tracked path는 새 Item ID를 결정하지 않으며, 실패/제목 변화 시 같은 cycle에서 full detector로 fallback.

## 의도적으로 제외

- 첫 OCR 성공 후 나머지 candidate 무조건 생략
- deep OCR candidate cap 축소
- full-catalog visual recovery 생략/threshold 완화
- cross-frame OCR cache
- structural/header/matcher threshold 완화

위 항목들은 결과 선택/정확도에 영향을 줄 수 있으므로 이번 accuracy-neutral pass에서 적용하지 않는다.
''',
    encoding="utf-8",
)
Path("docs/RELEASE_NOTES_V1.7.3.md").write_text(
    '''# 준현 헬퍼 v1.7.3

## Scanner performance

- continuous 화면 관측: 350 ms → 200 ms
- 동일 안정 후보 semantic 실패 retry: 고정 1200 ms → 250/500/800/1200 ms adaptive backoff
- Windows OCR 입력의 PNG encode → stream → PNG decode 왕복 제거
- verified detail window의 좁은 영역 fresh revalidation fast-path 추가
- fast-path 불확실/제목 변화 시 같은 cycle에서 기존 full detector로 fail-safe fallback

## Accuracy contract unchanged

- structural floor 0.34
- HEADER_FRAME_LOCKED 0.68
- continuous candidate cap 8
- one-shot candidate cap 12
- OCR 확대/deep variant, catalog matcher, visual corroboration/recovery 판정 기준 변경 없음
- cross-frame OCR identity cache 없음
- false-positive보다 miss 선호 유지
''',
    encoding="utf-8",
)
current = Path("docs/CURRENT_SCANNER_WORK.md")
text = current.read_text(encoding="utf-8")
text += '''

## v1.7.3 Scanner Performance Pass — 2026-08-25

accuracy-neutral latency pass:

```text
continuous observation: 350 ms -> 200 ms
semantic retry: fixed 1200 ms -> 250 / 500 / 800 / 1200 ms adaptive backoff
OCR transport: PNG encode/decode round-trip -> direct BGRA SoftwareBitmap copy
verified detail: fresh small-rectangle semantic/title revalidation fast-path
```

fast-path는 새 identity를 결정하지 않는다. fresh close-X + magnifier + HEADER_FRAME_LOCKED + title signature가 모두 기존 verified frame과 일치할 때만 presentation을 유지하며, 불일치/실패 시 같은 cycle에서 full detector로 fallback한다.

변경 금지/유지: structural floor 0.34, trusted header floor 0.68, continuous candidate cap 8, one-shot candidate cap 12, matcher/deep OCR/visual recovery acceptance semantics, cross-frame OCR identity cache 금지.

결과 선택을 바꿀 수 있는 candidate early-exit, deep candidate 축소, visual recovery 생략은 이번 pass에서 의도적으로 제외한다.
'''
current.write_text(text, encoding="utf-8")
