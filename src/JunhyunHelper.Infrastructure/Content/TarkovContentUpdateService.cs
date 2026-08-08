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

    public TarkovContentUpdateService(
        TarkovContentBuildService buildService,
        ContentActivationService activationService,
        ContentSnapshotStore? snapshotStore = null)
    {
        _buildService = buildService ?? throw new ArgumentNullException(nameof(buildService));
        _activationService = activationService ?? throw new ArgumentNullException(nameof(activationService));
        _snapshotStore = snapshotStore ?? new ContentSnapshotStore();
    }

    public async Task<ContentUpdateResult> UpdateAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default,
        IProgress<ContentUpdateProgress>? progress = null)
    {
        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Preparing,
            "기존 정상 데이터를 보존하고 업데이트를 준비하는 중...",
            0));

        _activationService.DiscardCandidate(gameMode);

        var build = await _buildService.BuildAsync(gameMode, cancellationToken, progress);
        if (!build.IsValid)
        {
            progress?.Report(new ContentUpdateProgress(
                ContentUpdateStage.Failed,
                "새 데이터 검증에 실패했습니다. 기존 정상 데이터를 유지합니다.",
                80));

            return new ContentUpdateResult(
                Applied: false,
                build.Validation,
                build.Warnings);
        }

        var paths = _activationService.GetPaths(gameMode);
        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.WritingCandidate,
            "검증된 candidate 데이터베이스를 작성하는 중...",
            88));

        await _snapshotStore.WriteNewAsync(
            paths.CandidatePath,
            gameMode,
            build.Content,
            build.Warnings,
            cancellationToken);

        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Activating,
            "candidate를 다시 검증하고 최신 게임 데이터로 적용하는 중...",
            96));

        await _activationService.ActivateCandidateAsync(gameMode, cancellationToken);

        progress?.Report(new ContentUpdateProgress(
            ContentUpdateStage.Completed,
            "게임 데이터 업데이트 완료",
            100));

        return new ContentUpdateResult(
            Applied: true,
            build.Validation,
            build.Warnings);
    }
}
