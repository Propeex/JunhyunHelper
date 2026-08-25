using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerRuntimeService
{
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private CancellationTokenSource? _oneShotDisplayCts;

    /// <summary>
    /// Cancels the continuous Scanner loop and waits until it has actually exited.
    /// One-shot recognition mutates the same verified-item/overlay state, so merely
    /// issuing cancellation is not sufficient: the old loop must be quiescent first.
    /// </summary>
    public async Task PauseForOneShotAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Task? loopTask;
        lock (_loopGate)
            loopTask = _loopTask;

        StopLoop();
        if (loopTask is { IsCompleted: false })
            await loopTask.WaitAsync(cancellationToken);
    }

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

        using var latencyCycle = ScannerLatencyTelemetry.BeginCycle(mode, "one-shot");
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
            ScannerDiagnosticLog.Write(
                "one-shot-no-structural-candidate",
                mode,
                ("detectorStatus", _detector.StatusMessage));
            _overlay.Hide();
            Publish(
                ScannerRuntimeState.Uncertain,
                "1회 스캔에서 아이템 상세창 구조 후보를 찾지 못했습니다.",
                captureMode: mode);
            return false;
        }

        // Persist exactly the proposal set used by this precision pass. Correction can
        // then select detector evidence instead of redrawing boxes from memory.
        ScannerRecognitionDebugStore.UpdateCandidates(candidates);

        ScannerDiagnosticLog.Write(
            "one-shot-candidates",
            mode,
            ("count", candidates.Count),
            ("topScore", candidates[0].StructuralScore),
            ("topReason", candidates[0].StructuralReason),
            ("topAnchorScore", candidates[0].TitleAnchorScore),
            ("topAnchorReason", candidates[0].TitleAnchorReason),
            ("topHasTitleImage", candidates[0].TitleImage is not null),
            ("topHasMagnifier", candidates[0].MagnifierBounds is not null),
            ("topHasClose", candidates[0].CloseBounds is not null));

        var search = await SearchCandidatesPrecisionAsync(candidates, mode, cancellationToken);
        PublishSearchActivity(search, mode);
        if (!search.Success || search.Candidate is null || string.IsNullOrWhiteSpace(search.Recognition.ItemId))
        {
            ClearVerifiedItem();
            _overlay.Hide();
            var message = search.Recognition.Reason == "TITLE_ANCHOR_INCOMPLETE"
                ? "1회 스캔에서 상세창 구조는 찾았지만 제목 헤더 잠금에 실패해 식별을 보류했습니다."
                : string.IsNullOrWhiteSpace(search.OcrText)
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
        _verifiedCandidate = search.Candidate;
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
        // One-shot is an explicit precision action. Inspect the full detector candidate
        // set while continuous scanning keeps its existing eight-candidate CPU budget.
        var limit = Math.Min(12, candidates.Count);
        CandidateSearchResult? bestSuccess = null;
        CandidateSearchResult? bestFailure = null;

        for (var index = 0; index < limit; index++)
        {
            var candidate = candidates[index];
            if (candidate.StructuralScore < CandidateStructuralFloor)
                continue;

            if (!HasTrustedTitleAnchors(candidate))
            {
                LogAnchorRejection(mode, index, "ONESHOT_ORIGINAL", candidate);
                var rejected = CreateAnchorFailure(candidate, index, "ONESHOT_ORIGINAL");
                bestFailure = PickBetterFailure(bestFailure, rejected);
                continue;
            }

            var rawText = await _ocr.ReadTextAsync(candidate.TitleImage!, cancellationToken);
            var substitution = ApplyUserOcrSubstitutions(rawText);
            var (recognition, assessment) = ResolveCatalogTextMeasured(substitution.Text);
            LogCandidateAttempt(
                mode,
                index,
                "ONESHOT_ORIGINAL",
                candidate,
                rawText,
                substitution.Text,
                assessment.FilteredText,
                recognition);
            var result = CreateSearchResult(
                candidate,
                recognition,
                rawText,
                assessment.FilteredText,
                "ONESHOT_ORIGINAL",
                index,
                0.82,
                0.18);
            bestFailure = PickBetterFailure(bestFailure, result);
            if (recognition.Success && (bestSuccess is null || result.CombinedScore > bestSuccess.CombinedScore))
                bestSuccess = result;
        }

        if (bestSuccess is not null)
            return bestSuccess;

        if (_ocr is IScannerDeepOcrEngine deepOcr)
        {
            // Unlike the continuous loop, one-shot mode intentionally spends
            // more CPU once: every semantic-ready candidate receives deep OCR plus the
            // OCR-independent visual recovery path. Diagnostic-only candidates never do.
            for (var index = 0; index < limit; index++)
            {
                var candidate = candidates[index];
                if (candidate.StructuralScore < CandidateStructuralFloor || !HasTrustedTitleAnchors(candidate))
                    continue;

                var rawText = await deepOcr.ReadDeepTextAsync(candidate.TitleImage!, cancellationToken);
                var substitution = ApplyUserOcrSubstitutions(rawText);
                var (recognition, assessment) = ResolveCatalogTextMeasured(substitution.Text);
                LogCandidateAttempt(
                    mode,
                    index,
                    "ONESHOT_DEEP",
                    candidate,
                    rawText,
                    substitution.Text,
                    assessment.FilteredText,
                    recognition);
                var result = CreateSearchResult(
                    candidate,
                    recognition,
                    rawText,
                    assessment.FilteredText,
                    "ONESHOT_DEEP",
                    index,
                    0.88,
                    0.12);
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
        {
            var candidates = await candidateDetector.ObserveCandidatesAsync(cancellationToken);
            return NormalizeTitleIdentitySignatures(candidates);
        }

        var candidate = await _detector.ObserveAsync(cancellationToken);
        return candidate is null
            ? []
            : NormalizeTitleIdentitySignatures([candidate]);
    }
}
