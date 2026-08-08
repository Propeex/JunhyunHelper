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
        CancellationToken cancellationToken = default)
    {
        _activationService.DiscardCandidate(gameMode);

        var build = await _buildService.BuildAsync(gameMode, cancellationToken);
        if (!build.IsValid)
        {
            return new ContentUpdateResult(
                Applied: false,
                build.Validation,
                build.Warnings);
        }

        var paths = _activationService.GetPaths(gameMode);
        await _snapshotStore.WriteNewAsync(
            paths.CandidatePath,
            gameMode,
            build.Content,
            build.Warnings,
            cancellationToken);

        await _activationService.ActivateCandidateAsync(gameMode, cancellationToken);

        return new ContentUpdateResult(
            Applied: true,
            build.Validation,
            build.Warnings);
    }
}
