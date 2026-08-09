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
    private readonly TarkovContentBuildService _buildService;
    private readonly ContentSnapshotStore _snapshotStore;
    private readonly ContentActivationService _activationService;
    private readonly Func<GameContentCatalog, CancellationToken, Task<IReadOnlyList<string>>>? _supplementalUpdater;

    public TarkovContentUpdateService(
        TarkovContentBuildService buildService,
        ContentActivationService activationService,
        ContentSnapshotStore? snapshotStore = null,
        Func<GameContentCatalog, CancellationToken, Task<IReadOnlyList<string>>>? supplementalUpdater = null)
    {
        _buildService = buildService ?? throw new ArgumentNullException(nameof(buildService));
        _activationService = activationService ?? throw new ArgumentNullException(nameof(activationService));
        _snapshotStore = snapshotStore ?? new ContentSnapshotStore();
        _supplementalUpdater = supplementalUpdater;
    }

    public event Action<GameContentCatalog>? ContentActivated;

    public async Task<ContentUpdateResult> UpdateAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default,
        IProgress<ContentUpdateProgress>? progress = null)
    {
        var trackedProgress = new TrackingProgress(progress);

        try
        {
            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Preparing,
                "기존 정상 데이터를 보존하고 업데이트를 준비하는 중...",
                0));

            _activationService.DiscardCandidate(gameMode);

            var build = await _buildService.BuildAsync(
                gameMode,
                cancellationToken,
                trackedProgress);
            if (!build.IsValid)
            {
                trackedProgress.Report(new ContentUpdateProgress(
                    ContentUpdateStage.Failed,
                    "새 데이터 검증에 실패했습니다. 기존 정상 데이터를 유지합니다.",
                    Math.Max(trackedProgress.LastPercent, 80)));

                return new ContentUpdateResult(
                    Applied: false,
                    build.Validation,
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

            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Activating,
                "candidate를 다시 검증하고 최신 게임 데이터로 적용하는 중...",
                94));

            await _activationService.ActivateCandidateAsync(gameMode, cancellationToken);

            var warnings = build.Warnings.ToList();
            if (_supplementalUpdater is not null)
            {
                trackedProgress.Report(new ContentUpdateProgress(
                    ContentUpdateStage.Activating,
                    "지도 레이아웃과 지도 자산을 업데이트하는 중...",
                    97));
                try
                {
                    warnings.AddRange(await _supplementalUpdater(build.Content, cancellationToken));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Supplemental presentation assets are independently recoverable.
                    // A temporary map-artwork failure must not roll back otherwise valid
                    // canonical game content; the previous validated map-cache stays active.
                    warnings.Add($"Map asset update failed; previous validated map assets were kept: {exception.Message}");
                }
            }

            ContentActivated?.Invoke(build.Content);

            trackedProgress.Report(new ContentUpdateProgress(
                ContentUpdateStage.Completed,
                "게임 데이터 및 지도 업데이트 완료",
                100));

            return new ContentUpdateResult(
                Applied: true,
                build.Validation,
                warnings.ToArray());
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
