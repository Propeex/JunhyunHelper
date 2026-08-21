using System.Windows;
using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerRuntimeService : IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan SemanticRetryInterval = TimeSpan.FromMilliseconds(1200);
    private const int StableCandidateHitsRequired = 2;
    private const int MissesToHide = 2;
    private const int CandidateLimit = 8;
    private const int DeepOcrCandidateLimit = 3;
    private const double CandidateStructuralFloor = 0.34;
    private const double VerifiedGeometryDistanceLimit = 100;

    private readonly ScannerSettingsService _settings;
    private readonly ScannerCatalogService _catalog;
    private readonly ScannerItemPresentationService _presentation;
    private readonly MiniScannerOverlayService _overlay;
    private readonly IScannerInspectDetector _detector;
    private readonly IScannerOcrEngine _ocr;
    private readonly Func<ScannerDataContext?> _contextProvider;
    private readonly object _loopGate = new();

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;
    private int _loopEpoch;
    private bool _disposed;
    private ScannerCaptureMode? _activeMode;
    private int _candidatePresenceHits;
    private int _consecutiveMisses;
    private DateTimeOffset _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;
    private Rect? _verifiedBounds;
    private string _verifiedTitleSignature = string.Empty;
    private ScannerItemSnapshot? _currentSnapshot;
    private string _lastDiagnosticStatusKey = string.Empty;

    public ScannerRuntimeService(
        ScannerSettingsService settings,
        ScannerCatalogService catalog,
        ScannerItemPresentationService presentation,
        MiniScannerOverlayService overlay,
        IScannerInspectDetector detector,
        IScannerOcrEngine ocr,
        Func<ScannerDataContext?> contextProvider)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _ocr = ocr ?? throw new ArgumentNullException(nameof(ocr));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        Status = new ScannerRuntimeStatus(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public event Action<ScannerRuntimeStatus>? StatusChanged;

    public ScannerRuntimeStatus Status { get; private set; }

    public ScannerCaptureMode? ActiveCaptureMode
    {
        get
        {
            lock (_loopGate)
                return _activeMode;
        }
    }

    public Task StartAsync(ScannerCaptureMode mode, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (mode == ScannerCaptureMode.TarkovWindow && !_settings.Current.Enabled)
        {
            Stop();
            return Task.CompletedTask;
        }

        if (ActiveCaptureMode != mode)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
        }

        lock (_loopGate)
            _activeMode = mode;
        _detector.SetCaptureMode(mode);

        ScannerDiagnosticLog.Write(
            "runtime-start",
            mode,
            ("pipeline", "scanner-lab-3.8-semantic"),
            ("detectorAvailable", _detector.IsAvailable),
            ("ocrAvailable", _ocr.IsAvailable),
            ("catalogCount", _catalog.Count));

        var initialMessage = ModeInitialMessage(mode);
        _overlay.ShowStandby(initialMessage);

        var context = _contextProvider();
        if (context is null)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            const string message = "Scanner를 사용할 활성 프로필이 없습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.NoProfile, message, captureMode: mode);
            return Task.CompletedTask;
        }

        if (_catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            const string message = "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.CatalogUnavailable, message, captureMode: mode);
            return Task.CompletedTask;
        }

        if (!_detector.IsAvailable || !_ocr.IsAvailable)
        {
            StopLoop();
            ResetObservationState(hideOverlay: false);
            var message = !_detector.IsAvailable ? _detector.AvailabilityMessage : _ocr.AvailabilityMessage;
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.WaitingForVision, message, captureMode: mode);
            return Task.CompletedTask;
        }

        lock (_loopGate)
        {
            if (_loopTask is { IsCompleted: false })
                return Task.CompletedTask;

            _loopCts?.Dispose();
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _loopCts.Token;
            var epoch = Interlocked.Increment(ref _loopEpoch);
            _loopTask = Task.Run(() => RunLoopAsync(mode, token, epoch), CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public Task ResumeActiveAsync(CancellationToken cancellationToken = default)
    {
        var mode = ActiveCaptureMode;
        return mode is null ? Task.CompletedTask : StartAsync(mode.Value, cancellationToken);
    }

    public void Stop()
    {
        var stoppedMode = ActiveCaptureMode;
        StopLoop();
        lock (_loopGate)
            _activeMode = null;
        ResetObservationState(hideOverlay: true);
        ScannerDiagnosticLog.Write("runtime-stop", stoppedMode);
        Publish(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public void Suspend(ScannerRuntimeState state, string message)
    {
        StopLoop();
        ResetObservationState(hideOverlay: false);
        var mode = ActiveCaptureMode;
        if (mode is not null)
            _overlay.ShowStandby(message);
        else
            _overlay.Hide();
        Publish(state, message, captureMode: mode);
    }

    public void PauseForPositionEdit()
    {
        StopLoop();
        Publish(ScannerRuntimeState.Stabilizing, "Mini Scanner 위치를 편집하는 중입니다.", captureMode: ActiveCaptureMode);
    }

    public void ShowPreview(ScannerItemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopLoop();
        ResetObservationState(hideOverlay: false);
        _currentSnapshot = snapshot;
        _overlay.Show(snapshot, preview: true);
        Publish(ScannerRuntimeState.ShowingItem, $"미리보기: {snapshot.OfficialName}", snapshot, ActiveCaptureMode);
    }

    public async Task HidePreviewAsync(CancellationToken cancellationToken = default)
    {
        _overlay.Hide();
        _currentSnapshot = null;
        var mode = ActiveCaptureMode;
        if (mode is not null)
            await StartAsync(mode.Value, cancellationToken);
        else
            Publish(ScannerRuntimeState.Disabled, "Scanner가 꺼져 있습니다.");
    }

    public void PublishExternalState(ScannerRuntimeState state, string message) =>
        Publish(state, message, captureMode: ActiveCaptureMode);

    private async Task RunLoopAsync(ScannerCaptureMode mode, CancellationToken cancellationToken, int epoch)
    {
        var waitingMessage = ModeInitialMessage(mode);
        _overlay.ShowStandby(waitingMessage);
        Publish(ScannerRuntimeState.WaitingForInspectWindow, waitingMessage, captureMode: mode);

        try
        {
            using var timer = new PeriodicTimer(TickInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                var context = _contextProvider();
                if (context is null)
                {
                    ResetObservationState(hideOverlay: false);
                    const string message = "Scanner를 사용할 활성 프로필이 없습니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.NoProfile, message, captureMode: mode);
                    continue;
                }

                if (_catalog.LoadedMode != context.GameMode)
                {
                    ResetObservationState(hideOverlay: false);
                    await _catalog.LoadCacheAsync(context.GameMode, cancellationToken);
                    if (!_catalog.HasHealthyCatalog)
                    {
                        const string message = "현재 게임 모드의 전체 아이템 카탈로그가 준비되지 않았습니다.";
                        _overlay.ShowStandby(message);
                        Publish(ScannerRuntimeState.CatalogUnavailable, message, captureMode: mode);
                        continue;
                    }
                }

                if (!_detector.IsAvailable || !_ocr.IsAvailable)
                {
                    var message = !_detector.IsAvailable ? _detector.AvailabilityMessage : _ocr.AvailabilityMessage;
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.WaitingForVision, message, captureMode: mode);
                    continue;
                }

                var candidates = await ObserveCandidatesAsync(cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                if (candidates.Count == 0)
                {
                    HandleMiss(mode, _detector.StatusMessage);
                    continue;
                }

                _consecutiveMisses = 0;
                _candidatePresenceHits++;

                ScannerDiagnosticLog.Write(
                    "geometry-candidates",
                    mode,
                    ("count", candidates.Count),
                    ("topScore", candidates[0].StructuralScore),
                    ("topReason", candidates[0].StructuralReason),
                    ("topBounds", FormatBounds(candidates[0].Bounds)));

                if (_currentSnapshot is not null && _verifiedBounds is { } verifiedBounds)
                {
                    var closest = candidates
                        .Select(candidate => (Candidate: candidate, Distance: GeometryDistance(candidate.Bounds, verifiedBounds)))
                        .OrderBy(item => item.Distance)
                        .First();

                    if (closest.Distance <= VerifiedGeometryDistanceLimit &&
                        string.Equals(closest.Candidate.TitleSignature, _verifiedTitleSignature, StringComparison.Ordinal))
                    {
                        _overlay.Show(_currentSnapshot);
                        continue;
                    }

                    ClearVerifiedItem();
                    const string changedMessage = "아이템 제목 변화를 확인하는 중입니다.";
                    _overlay.ShowStandby(changedMessage);
                    Publish(ScannerRuntimeState.Stabilizing, changedMessage, captureMode: mode);
                }

                if (_candidatePresenceHits < StableCandidateHitsRequired)
                {
                    const string message = "상세창 후보를 확인하는 중입니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Stabilizing, message, captureMode: mode);
                    continue;
                }

                if (DateTimeOffset.UtcNow < _nextSemanticAttemptAtUtc)
                    continue;

                _nextSemanticAttemptAtUtc = DateTimeOffset.UtcNow + SemanticRetryInterval;
                const string readingMessage = "아이템 이름을 읽는 중입니다.";
                _overlay.ShowStandby(readingMessage);
                Publish(ScannerRuntimeState.ReadingTitle, readingMessage, captureMode: mode);

                var search = await SearchCandidatesAsync(candidates, mode, cancellationToken);
                if (epoch != Volatile.Read(ref _loopEpoch))
                    return;

                PublishSearchActivity(search, mode);

                if (!search.Success || search.Candidate is null ||
                    string.IsNullOrWhiteSpace(search.Recognition.ItemId))
                {
                    _currentSnapshot = null;
                    _verifiedBounds = null;
                    _verifiedTitleSignature = string.Empty;
                    var message = string.IsNullOrWhiteSpace(search.OcrText)
                        ? "아이템 이름을 읽지 못해 식별을 보류했습니다."
                        : $"아이템을 확실하게 식별하지 못했습니다. ({search.Recognition.Reason}, {search.Recognition.Confidence:P0})";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Uncertain, message, captureMode: mode);
                    continue;
                }

                var snapshot = _presentation.CreateSnapshot(search.Recognition.ItemId);
                if (snapshot is null)
                {
                    _currentSnapshot = null;
                    const string message = "Item ID는 확정했지만 현재 표시 데이터를 만들 수 없습니다.";
                    _overlay.ShowStandby(message);
                    Publish(ScannerRuntimeState.Uncertain, message, captureMode: mode);
                    continue;
                }

                _verifiedBounds = search.Candidate.Bounds;
                _verifiedTitleSignature = search.Candidate.TitleSignature;
                _currentSnapshot = snapshot;
                ScannerDiagnosticLog.Write(
                    "semantic-selected",
                    mode,
                    ("candidateIndex", search.CandidateIndex),
                    ("pass", search.Pass),
                    ("structure", search.Candidate.StructuralScore),
                    ("structureReason", search.Candidate.StructuralReason),
                    ("officialName", search.Recognition.OfficialName),
                    ("confidence", search.Recognition.Confidence),
                    ("bounds", FormatBounds(search.Candidate.Bounds)));
                _overlay.Show(snapshot);
                Publish(ScannerRuntimeState.ShowingItem, snapshot.OfficialName, snapshot, mode);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Scanner runtime loop failed", exception);
            ScannerDiagnosticLog.Write(
                "runtime-error",
                mode,
                ("type", exception.GetType().Name),
                ("message", exception.Message));
            _currentSnapshot = null;
            const string message = "Scanner 런타임 오류가 발생했습니다.";
            _overlay.ShowStandby(message);
            Publish(ScannerRuntimeState.Error, message, captureMode: mode);
        }
    }

    private async Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesAsync(CancellationToken cancellationToken)
    {
        if (_detector is IScannerCandidateInspectDetector candidateDetector)
            return await candidateDetector.ObserveCandidatesAsync(cancellationToken);

        var candidate = await _detector.ObserveAsync(cancellationToken);
        return candidate is null ? [] : [candidate];
    }

    private async Task<CandidateSearchResult> SearchCandidatesAsync(
        IReadOnlyList<ScannerInspectCandidate> candidates,
        ScannerCaptureMode mode,
        CancellationToken cancellationToken)
    {
        var limit = Math.Min(CandidateLimit, candidates.Count);
        CandidateSearchResult? bestSuccess = null;
        CandidateSearchResult? bestFailure = null;

        for (var index = 0; index < limit; index++)
        {
            var candidate = candidates[index];
            if (candidate.StructuralScore < CandidateStructuralFloor || candidate.TitleImage is null)
                continue;

            var text = await _ocr.ReadTextAsync(candidate.TitleImage, cancellationToken);
            var recognition = _catalog.ResolveOcrText(text);
            LogCandidateAttempt(mode, index, "ORIGINAL", candidate, text, recognition);
            var result = new CandidateSearchResult(
                recognition.Success,
                candidate,
                recognition,
                text,
                "ORIGINAL",
                index,
                recognition.Confidence * 0.82 + candidate.StructuralScore * 0.18);

            bestFailure = BetterFailure(bestFailure, result);
            if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                bestSuccess = result;
        }

        if (bestSuccess is not null)
            return bestSuccess;

        if (_ocr is IScannerDeepOcrEngine deepOcr)
        {
            var deepLimit = Math.Min(DeepOcrCandidateLimit, limit);
            for (var index = 0; index < deepLimit; index++)
            {
                var candidate = candidates[index];
                if (candidate.StructuralScore < CandidateStructuralFloor || candidate.TitleImage is null)
                    continue;

                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text);
                LogCandidateAttempt(mode, index, "DEEP", candidate, text, recognition);
                var result = new CandidateSearchResult(
                    recognition.Success,
                    candidate,
                    recognition,
                    text,
                    "DEEP",
                    index,
                    recognition.Confidence * 0.86 + candidate.StructuralScore * 0.14);

                bestFailure = BetterFailure(bestFailure, result);
                if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                    bestSuccess = result;
            }
        }

        if (bestSuccess is not null)
            return bestSuccess;

        return bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            "NONE",
            -1,
            0);
    }

    private static CandidateSearchResult BetterFailure(CandidateSearchResult? current, CandidateSearchResult candidate)
    {
        if (current is null)
            return candidate;
        if (!string.IsNullOrWhiteSpace(candidate.OcrText) && string.IsNullOrWhiteSpace(current.OcrText))
            return candidate;
        if (candidate.Recognition.Confidence > current.Recognition.Confidence)
            return candidate;
        if (Math.Abs(candidate.Recognition.Confidence - current.Recognition.Confidence) < 0.0001 &&
            candidate.CombinedScore > current.CombinedScore)
            return candidate;
        return current;
    }

    private static void LogCandidateAttempt(
        ScannerCaptureMode mode,
        int index,
        string pass,
        ScannerInspectCandidate candidate,
        string text,
        ScannerRecognition recognition)
    {
        ScannerDiagnosticLog.Write(
            "candidate-semantic",
            mode,
            ("index", index),
            ("pass", pass),
            ("structure", candidate.StructuralScore),
            ("structureReason", candidate.StructuralReason),
            ("bounds", FormatBounds(candidate.Bounds)),
            ("ocr", text),
            ("match", recognition.Reason),
            ("success", recognition.Success),
            ("officialName", recognition.OfficialName),
            ("confidence", recognition.Confidence),
            ("secondScore", recognition.SecondScore));
    }

    private static CandidateSearchResult? BetterFailure(CandidateSearchResult? current, CandidateSearchResult? candidate) =>
        candidate is null ? current : BetterFailure(current, candidate);

    private static double GeometryDistance(Rect left, Rect right) =>
        Math.Abs(left.X - right.X) +
        Math.Abs(left.Y - right.Y) +
        Math.Abs(left.Width - right.Width) +
        Math.Abs(left.Height - right.Height);

    private static string FormatBounds(Rect bounds) =>
        $"{bounds.X:F0},{bounds.Y:F0},{bounds.Width:F0},{bounds.Height:F0}";

    private static void PublishSearchActivity(CandidateSearchResult search, ScannerCaptureMode mode)
    {
        ScannerDiagnosticLog.Write(
            "ocr-result",
            mode,
            ("candidateIndex", search.CandidateIndex),
            ("pass", search.Pass),
            ("text", search.OcrText));
        ScannerDiagnosticLog.Write(
            "match-result",
            mode,
            ("success", search.Recognition.Success),
            ("reason", search.Recognition.Reason),
            ("itemId", search.Recognition.ItemId),
            ("officialName", search.Recognition.OfficialName),
            ("confidence", search.Recognition.Confidence),
            ("secondScore", search.Recognition.SecondScore));
    }

    private void HandleMiss(ScannerCaptureMode mode, string detectorMessage)
    {
        _consecutiveMisses++;
        _candidatePresenceHits = 0;
        if (_consecutiveMisses < MissesToHide)
            return;

        ClearVerifiedItem();
        var message = string.IsNullOrWhiteSpace(detectorMessage) ? ModeInitialMessage(mode) : detectorMessage;
        _overlay.ShowStandby(message);
        Publish(ScannerRuntimeState.WaitingForInspectWindow, message, captureMode: mode);
    }

    private void ClearVerifiedItem()
    {
        _verifiedBounds = null;
        _verifiedTitleSignature = string.Empty;
        _currentSnapshot = null;
    }

    private void ResetObservationState(bool hideOverlay)
    {
        _candidatePresenceHits = 0;
        _consecutiveMisses = 0;
        _nextSemanticAttemptAtUtc = DateTimeOffset.MinValue;
        ClearVerifiedItem();
        _lastDiagnosticStatusKey = string.Empty;
        if (hideOverlay)
            _overlay.Hide();
    }

    private void StopLoop()
    {
        Interlocked.Increment(ref _loopEpoch);

        CancellationTokenSource? cancellation;
        Task? task;
        lock (_loopGate)
        {
            cancellation = _loopCts;
            task = _loopTask;
            _loopCts = null;
            _loopTask = null;
        }

        if (cancellation is null)
            return;

        cancellation.Cancel();
        if (task is { IsCompleted: false })
        {
            _ = task.ContinueWith(
                _ => cancellation.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else
        {
            cancellation.Dispose();
        }
    }

    private static string ModeInitialMessage(ScannerCaptureMode mode) =>
        mode == ScannerCaptureMode.DisplayTest
            ? "테스트 모드 · 전체 디스플레이에서 상세창을 찾는 중입니다."
            : "Tarkov 게임 창을 찾는 중입니다. (Borderless 지원)";

    private void Publish(
        ScannerRuntimeState state,
        string message,
        ScannerItemSnapshot? snapshot = null,
        ScannerCaptureMode? captureMode = null)
    {
        Status = new ScannerRuntimeStatus(
            state,
            message,
            snapshot?.ItemId,
            snapshot?.OfficialName,
            DateTimeOffset.Now,
            captureMode);

        var diagnosticKey = $"{captureMode}:{state}:{message}:{snapshot?.ItemId}";
        if (!string.Equals(_lastDiagnosticStatusKey, diagnosticKey, StringComparison.Ordinal))
        {
            _lastDiagnosticStatusKey = diagnosticKey;
            ScannerDiagnosticLog.Write(
                "status",
                captureMode,
                ("state", state),
                ("message", message),
                ("itemId", snapshot?.ItemId),
                ("officialName", snapshot?.OfficialName));
        }

        StatusChanged?.Invoke(Status);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        StopLoop();
        lock (_loopGate)
            _activeMode = null;
        _overlay.Hide();
        ScannerDiagnosticLog.Write("runtime-dispose");
        GC.SuppressFinalize(this);
    }

    private sealed record CandidateSearchResult(
        bool Success,
        ScannerInspectCandidate? Candidate,
        ScannerRecognition Recognition,
        string OcrText,
        string Pass,
        int CandidateIndex,
        double CombinedScore);
}
