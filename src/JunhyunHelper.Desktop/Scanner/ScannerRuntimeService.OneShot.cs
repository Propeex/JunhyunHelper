using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerRuntimeService
{
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private CancellationTokenSource? _oneShotDisplayCts;

    public async Task<bool> ScanOnceAsync(
        ScannerCaptureMode mode,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var context = _contextProvider();
        if (context is null)
        {
            Publish(ScannerRuntimeState.NoProfile, "1회 스캔을 사용할 활성 프로필이 없습니다.", captureMode: mode);
            return false;
        }
        if (_catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
        {
            Publish(ScannerRuntimeState.CatalogUnavailable, "현재 게임 모드의 아이템 목록이 준비되지 않았습니다.", captureMode: mode);
            return false;
        }
        if (!_detector.IsAvailable || !_ocr.IsAvailable)
        {
            var message = !_detector.IsAvailable ? _detector.AvailabilityMessage : _ocr.AvailabilityMessage;
            Publish(ScannerRuntimeState.WaitingForVision, message, captureMode: mode);
            return false;
        }

        CancelOneShotDisplayTimer();
        _detector.SetCaptureMode(mode);
        const string readingMessage = "1회 고정밀 스캔 · 상세창과 아이템 이름을 분석하는 중입니다.";
        Publish(ScannerRuntimeState.ReadingTitle, readingMessage, captureMode: mode);

        IReadOnlyList<ScannerInspectCandidate> candidates;
        await _captureGate.WaitAsync(cancellationToken);
        try
        {
            candidates = await ObserveCandidatesCoreAsync(cancellationToken);
        }
        finally
        {
            _captureGate.Release();
        }

        if (candidates.Count == 0)
        {
            _overlay.Hide();
            Publish(
                ScannerRuntimeState.Uncertain,
                "1회 스캔에서 아이템 상세창을 찾지 못했습니다.",
                captureMode: mode);
            return false;
        }

        ScannerDiagnosticLog.Write(
            "one-shot-candidates",
            mode,
            ("count", candidates.Count),
            ("topScore", candidates[0].StructuralScore),
            ("topReason", candidates[0].StructuralReason),
            ("topAnchorScore", candidates[0].TitleAnchorScore),
            ("topAnchorReason", candidates[0].TitleAnchorReason));

        var search = await SearchCandidatesPrecisionAsync(candidates, mode, cancellationToken);
        PublishSearchActivity(search, mode);
        if (!search.Success || search.Candidate is null || string.IsNullOrWhiteSpace(search.Recognition.ItemId))
        {
            ClearVerifiedItem();
            _overlay.Hide();
            var message = string.IsNullOrWhiteSpace(search.OcrText)
                ? "1회 스캔에서 제목 픽셀은 확인했지만 아이템을 확정할 충분한 증거가 없었습니다."
                : $"1회 스캔에서 아이템을 확실하게 식별하지 못했습니다. ({search.Recognition.Reason}, {search.Recognition.Confidence:P0})";
            Publish(ScannerRuntimeState.Uncertain, message, captureMode: mode);
            return false;
        }

        var snapshot = _presentation.CreateSnapshot(search.Recognition.ItemId);
        if (snapshot is null)
        {
            ClearVerifiedItem();
            _overlay.Hide();
            Publish(ScannerRuntimeState.Uncertain, "Item ID는 확정했지만 현재 표시 데이터를 만들 수 없습니다.", captureMode: mode);
            return false;
        }

        _verifiedBounds = search.Candidate.Bounds;
        _verifiedTitleSignature = search.Candidate.TitleSignature;
        _currentSnapshot = snapshot;
        _nextPresentationRefreshAtUtc = DateTimeOffset.UtcNow + PresentationRefreshInterval;
        _overlay.Show(snapshot);
        Publish(ScannerRuntimeState.ShowingItem, $"1회 스캔: {snapshot.OfficialName}", snapshot, mode);

        ScannerDiagnosticLog.Write(
            "one-shot-selected",
            mode,
            ("candidateIndex", search.CandidateIndex),
            ("pass", search.Pass),
            ("officialName", search.Recognition.OfficialName),
            ("confidence", search.Recognition.Confidence),
            ("structure", search.Candidate.StructuralScore),
            ("anchor", search.Candidate.TitleAnchorScore));

        if (ActiveCaptureMode is null)
            ScheduleOneShotAutoHide();
        return true;
    }

    private async Task<CandidateSearchResult> SearchCandidatesPrecisionAsync(
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
            LogCandidateAttempt(mode, index, "ONESHOT_ORIGINAL", candidate, text, recognition);
            var result = CreateSearchResult(candidate, recognition, text, "ONESHOT_ORIGINAL", index, 0.82, 0.18);
            bestFailure = PickBetterFailure(bestFailure, result);
            if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                bestSuccess = result;
        }

        if (bestSuccess is not null)
            return bestSuccess;

        if (_ocr is IScannerDeepOcrEngine deepOcr)
        {
            // Unlike the 350ms continuous loop, one-shot mode intentionally spends
            // more CPU once: every structural candidate receives deep OCR plus the
            // OCR-independent visual recovery path.
            for (var index = 0; index < limit; index++)
            {
                var candidate = candidates[index];
                if (candidate.StructuralScore < CandidateStructuralFloor || candidate.TitleImage is null)
                    continue;

                var text = await deepOcr.ReadDeepTextAsync(candidate.TitleImage, cancellationToken);
                var recognition = _catalog.ResolveOcrText(text);
                LogCandidateAttempt(mode, index, "ONESHOT_DEEP", candidate, text, recognition);
                var result = CreateSearchResult(candidate, recognition, text, "ONESHOT_DEEP", index, 0.88, 0.12);
                bestFailure = PickBetterFailure(bestFailure, result);
                if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                    bestSuccess = result;
            }
        }

        return bestSuccess ?? bestFailure ?? new CandidateSearchResult(
            false,
            null,
            ScannerRecognition.Failed("EMPTY_OCR"),
            string.Empty,
            "ONESHOT_NONE",
            -1,
            0);
    }

    private void ScheduleOneShotAutoHide()
    {
        CancelOneShotDisplayTimer();
        _oneShotDisplayCts = new CancellationTokenSource();
        var token = _oneShotDisplayCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(8), token);
                if (token.IsCancellationRequested || _disposed || ActiveCaptureMode is not null)
                    return;
                _overlay.Hide();
                ClearVerifiedItem();
                Publish(ScannerRuntimeState.Disabled, "1회 스캔 표시가 종료되었습니다.");
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
    }

    private void CancelOneShotDisplayTimer()
    {
        var cancellation = Interlocked.Exchange(ref _oneShotDisplayCts, null);
        if (cancellation is null)
            return;
        cancellation.Cancel();
        cancellation.Dispose();
    }

    private async Task<IReadOnlyList<ScannerInspectCandidate>> ObserveCandidatesCoreAsync(CancellationToken cancellationToken)
    {
        if (_detector is IScannerCandidateInspectDetector candidateDetector)
            return await candidateDetector.ObserveCandidatesAsync(cancellationToken);
        var candidate = await _detector.ObserveAsync(cancellationToken);
        return candidate is null ? [] : [candidate];
    }
}
