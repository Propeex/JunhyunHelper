using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Storage;
using JunhyunHelper.Infrastructure.Validation;

namespace JunhyunHelper.Infrastructure.Content;

public sealed record ContentUpdateResult(
    bool Applied,
    ContentValidationResult Validation,
    IReadOnlyList<string> Warnings);

public sealed class TarkovContentUpdateService
{
    private readonly ITarkovContentBuildService _buildService;
    private readonly ContentSnapshotStore _snapshotStore;
    private readonly ContentActivationService _activationService;
    private readonly ContentUpdateCompletenessGuard _completenessGuard;
    private readonly GameContentIntegrityValidator _integrityValidator;
    private readonly ItemRelationshipIntegrityValidator _itemRelationshipValidator;
    private readonly SemaphoreSlim _updateGate = new(1, 1);

    public TarkovContentUpdateService(
        ITarkovContentBuildService buildService,
        ContentActivationService activationService,
        ContentSnapshotStore? snapshotStore = null,
        ContentUpdateCompletenessGuard? completenessGuard = null,
        GameContentIntegrityValidator? integrityValidator = null)
    {
        _buildService = buildService ?? throw new ArgumentNullException(nameof(buildService));
        _activationService = activationService ?? throw new ArgumentNullException(nameof(activationService));
        _snapshotStore = snapshotStore ?? new ContentSnapshotStore();
        _completenessGuard = completenessGuard ?? new ContentUpdateCompletenessGuard();
        _integrityValidator = integrityValidator ?? new GameContentIntegrityValidator();
        _itemRelationshipValidator = new ItemRelationshipIntegrityValidator();
    }

    public async Task<ContentUpdateResult> UpdateAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default,
        IProgress<ContentUpdateProgress>? progress = null)
    {
        var trackedProgress = new TrackingProgress(progress);
        var gateEntered = false;
        var applied = false;

        try
        {
            // Data Update is a transactional product boundary. Never let two callers
            // race candidate deletion/write/activation for the same shared content root.
            await _updateGate.WaitAsync(cancellationToken);
            gateEntered = true;

            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Preparing,
                "기존 정상 데이터를 보존하고 업데이트를 준비하는 중...",
                0));

            var baseline = await TryReadBaselineAsync(gameMode, cancellationToken);
            _activationService.DiscardCandidate(gameMode);

            var build = await _buildService.BuildAsync(
                gameMode,
                cancellationToken,
                trackedProgress);

            var regressionValidation = _completenessGuard.Validate(
                build.Content,
                baseline?.Content);
            var validation = MergeValidation(build.Validation, regressionValidation);
            if (!validation.IsValid)
            {
                trackedProgress.Report(new ContentUpdateProgress(
                    ContentUpdateStage.Failed,
                    "새 데이터의 구성·관계 검증에 실패했습니다. 기존 정상 데이터를 유지합니다.",
                    Math.Max(trackedProgress.LastPercent, 80)));

                return new ContentUpdateResult(
                    Applied: false,
                    validation,
                    build.Warnings);
            }

            var paths = _activationService.GetPaths(gameMode);
            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.WritingCandidate,
                "검증된 candidate 데이터베이스를 작성하는 중...",
                88));

            await _snapshotStore.WriteNewAsync(
                paths.CandidatePath,
                gameMode,
                build.Content,
                build.Warnings,
                cancellationToken);

            // Verify the bytes we actually persisted, not only the in-memory import.
            // This catches storage/serialization regressions before the active snapshot
            // is touched and repeats both semantic relationship validation and the
            // baseline partial-payload guard on read-back.
            var persistedCandidate = await _snapshotStore.ReadAsync(paths.CandidatePath, cancellationToken);
            if (persistedCandidate.GameMode != gameMode)
                throw new InvalidDataException("Persisted candidate belongs to a different game mode.");

            var persistedValidation = MergeValidation(
                MergeValidation(
                    _integrityValidator.Validate(persistedCandidate.Content),
                    _itemRelationshipValidator.Validate(persistedCandidate.Content)),
                _completenessGuard.Validate(persistedCandidate.Content, baseline?.Content));
            if (!persistedValidation.IsValid)
                throw new InvalidDataException("Persisted content candidate failed integrity validation.");

            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Activating,
                "candidate를 다시 검증하고 최신 게임 데이터로 적용하는 중...",
                96));

            await _activationService.ActivateCandidateAsync(gameMode, cancellationToken);

            // Do not report success merely because a file move completed. Load the final
            // active snapshot through the existing recovery/validation boundary once more.
            // If activation produced an invalid file, ContentActivationService restores
            // the previous last-known-good snapshot before this call returns.
            _ = await _activationService.ReadActiveOrRecoverAsync(gameMode, cancellationToken);
            applied = true;

            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Completed,
                "게임 데이터 업데이트 완료",
                100));

            return new ContentUpdateResult(
                Applied: true,
                validation,
                build.Warnings);
        }
        catch (OperationCanceledException)
        {
            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Failed,
                "게임 데이터 업데이트가 중단되었습니다. 기존 정상 데이터를 유지합니다.",
                trackedProgress.LastPercent));
            throw;
        }
        catch
        {
            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Failed,
                "게임 데이터 업데이트에 실패했습니다. 기존 정상 데이터를 유지합니다.",
                trackedProgress.LastPercent));
            throw;
        }
        finally
        {
            if (gateEntered && !applied)
            {
                // A canceled/failed write must not leave a stale candidate that looks
                // actionable to maintenance tooling or a future code path. Active and
                // previous last-known-good snapshots are never touched here.
                try
                {
                    _activationService.DiscardCandidate(gameMode);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Preserve the original update result/error; the next update also
                    // discards candidates before building a new one.
                }
            }

            if (gateEntered)
                _updateGate.Release();
        }
    }

    private async Task<StoredContentSnapshot?> TryReadBaselineAsync(
        GameMode gameMode,
        CancellationToken cancellationToken)
    {
        var paths = _activationService.GetPaths(gameMode);
        if (!File.Exists(paths.ActivePath) && !File.Exists(paths.PreviousPath))
            return null;

        try
        {
            return await _activationService.ReadActiveOrRecoverAsync(gameMode, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            // A broken baseline must not be used to judge a new candidate. The candidate
            // still has to pass complete build + activation validation before replacing it.
            return null;
        }
    }

    private static ContentValidationResult MergeValidation(
        ContentValidationResult first,
        ContentValidationResult second)
    {
        if (second.Issues.Count == 0)
            return first;
        if (first.Issues.Count == 0)
            return second;
        return new ContentValidationResult(first.Issues.Concat(second.Issues).ToArray());
    }

    private sealed class TrackingProgress(IProgress<ContentUpdateProgress>? inner)
        : IProgress<ContentUpdateProgress>
    {
        public int LastPercent { get; private set; }

        public void Report(ContentUpdateProgress value)
        {
            LastPercent = Math.Max(LastPercent, Math.Clamp(value.Percent, 0, 100));
            inner?.Report(value);
        }
    }
}
