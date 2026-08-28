using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.Validation;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed record ContentModePaths(
    string Directory,
    string ActivePath,
    string CandidatePath,
    string PreviousPath);

public sealed class ContentActivationService
{
    private readonly ContentSnapshotStore _store;
    private readonly GameContentValidator _validator;
    private readonly ItemRelationshipIntegrityValidator _itemRelationshipValidator;

    public ContentActivationService(
        string rootDirectory,
        ContentSnapshotStore? store = null,
        GameContentValidator? validator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        RootDirectory = Path.GetFullPath(rootDirectory);
        _store = store ?? new ContentSnapshotStore();
        _validator = validator ?? new GameContentValidator();
        _itemRelationshipValidator = new ItemRelationshipIntegrityValidator();
    }

    public string RootDirectory { get; }

    public ContentModePaths GetPaths(GameMode gameMode)
    {
        var directory = Path.Combine(RootDirectory, gameMode.ToDataKey());
        return new ContentModePaths(
            directory,
            Path.Combine(directory, "content.db"),
            Path.Combine(directory, "content.candidate.db"),
            Path.Combine(directory, "content.previous.db"));
    }

    public async Task ActivateCandidateAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default)
    {
        var paths = GetPaths(gameMode);
        Directory.CreateDirectory(paths.Directory);

        _ = await ReadAndValidateAsync(
            paths.CandidatePath,
            gameMode,
            cancellationToken);

        if (File.Exists(paths.PreviousPath))
            File.Delete(paths.PreviousPath);

        var hadPreviousActive = File.Exists(paths.ActivePath);
        if (hadPreviousActive)
        {
            File.Replace(
                paths.CandidatePath,
                paths.ActivePath,
                paths.PreviousPath,
                ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(paths.CandidatePath, paths.ActivePath);
        }

        try
        {
            _ = await ReadAndValidateAsync(
                paths.ActivePath,
                gameMode,
                cancellationToken);
        }
        catch
        {
            if (hadPreviousActive)
                RestorePreviousAfterFailedActivation(paths);
            else if (File.Exists(paths.ActivePath))
                File.Delete(paths.ActivePath);
            throw;
        }
    }

    public async Task<StoredContentSnapshot> ReadActiveOrRecoverAsync(
        GameMode gameMode,
        CancellationToken cancellationToken = default)
    {
        var paths = GetPaths(gameMode);
        Directory.CreateDirectory(paths.Directory);

        try
        {
            return await ReadAndValidateAsync(
                paths.ActivePath,
                gameMode,
                cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException && File.Exists(paths.PreviousPath))
        {
            var previous = await ReadAndValidateAsync(
                paths.PreviousPath,
                gameMode,
                cancellationToken);

            if (File.Exists(paths.ActivePath))
                File.Delete(paths.ActivePath);
            File.Move(paths.PreviousPath, paths.ActivePath);

            return previous;
        }
    }

    public void DiscardCandidate(GameMode gameMode)
    {
        var candidatePath = GetPaths(gameMode).CandidatePath;
        if (File.Exists(candidatePath))
            File.Delete(candidatePath);
    }

    private async Task<StoredContentSnapshot> ReadAndValidateAsync(
        string path,
        GameMode expectedGameMode,
        CancellationToken cancellationToken)
    {
        var snapshot = await _store.ReadAsync(path, cancellationToken);
        if (snapshot.GameMode != expectedGameMode)
        {
            throw new InvalidDataException(
                $"Content at '{path}' belongs to '{snapshot.GameMode}', expected '{expectedGameMode}'.");
        }

        var validation = _validator.Validate(snapshot.Content);
        var relationshipValidation = _itemRelationshipValidator.Validate(snapshot.Content);
        if (!validation.IsValid || !relationshipValidation.IsValid)
            throw new InvalidDataException($"Content at '{path}' failed canonical validation.");
        return snapshot;
    }

    private static void RestorePreviousAfterFailedActivation(ContentModePaths paths)
    {
        if (!File.Exists(paths.PreviousPath))
            return;

        try
        {
            if (File.Exists(paths.ActivePath))
            {
                File.Replace(
                    paths.PreviousPath,
                    paths.ActivePath,
                    destinationBackupFileName: null,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(paths.PreviousPath, paths.ActivePath);
            }
        }
        catch
        {
            // Keep the original activation error. Startup recovery can retry later.
        }
    }
}
