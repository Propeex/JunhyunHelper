using JunhyunHelper.Infrastructure.Validation;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed class ContentActivationService
{
    private readonly ContentSnapshotStore _store;
    private readonly GameContentValidator _validator;

    public ContentActivationService(
        string rootDirectory,
        ContentSnapshotStore? store = null,
        GameContentValidator? validator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        RootDirectory = Path.GetFullPath(rootDirectory);
        ActivePath = Path.Combine(RootDirectory, "content.db");
        CandidatePath = Path.Combine(RootDirectory, "content.candidate.db");
        PreviousPath = Path.Combine(RootDirectory, "content.previous.db");

        _store = store ?? new ContentSnapshotStore();
        _validator = validator ?? new GameContentValidator();
    }

    public string RootDirectory { get; }

    public string ActivePath { get; }

    public string CandidatePath { get; }

    public string PreviousPath { get; }

    public async Task ActivateCandidateAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(RootDirectory);

        var candidate = await _store.ReadAsync(CandidatePath, cancellationToken);
        var candidateValidation = _validator.Validate(candidate.Content);
        if (!candidateValidation.IsValid)
        {
            throw new InvalidDataException(
                "Candidate content failed canonical reference validation and was not activated.");
        }

        if (File.Exists(PreviousPath))
            File.Delete(PreviousPath);

        if (File.Exists(ActivePath))
        {
            File.Replace(
                CandidatePath,
                ActivePath,
                PreviousPath,
                ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(CandidatePath, ActivePath);
        }

        try
        {
            var activated = await _store.ReadAsync(ActivePath, cancellationToken);
            var activatedValidation = _validator.Validate(activated.Content);
            if (!activatedValidation.IsValid)
                throw new InvalidDataException("Activated content failed post-activation validation.");
        }
        catch
        {
            RestorePreviousAfterFailedActivation();
            throw;
        }
    }

    public async Task<StoredContentSnapshot> ReadActiveOrRecoverAsync(
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(RootDirectory);

        try
        {
            return await ReadAndValidateAsync(ActivePath, cancellationToken);
        }
        catch when (File.Exists(PreviousPath))
        {
            var previous = await ReadAndValidateAsync(PreviousPath, cancellationToken);

            if (File.Exists(ActivePath))
                File.Delete(ActivePath);
            File.Move(PreviousPath, ActivePath);

            return previous;
        }
    }

    public void DiscardCandidate()
    {
        if (File.Exists(CandidatePath))
            File.Delete(CandidatePath);
    }

    private async Task<StoredContentSnapshot> ReadAndValidateAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var snapshot = await _store.ReadAsync(path, cancellationToken);
        var validation = _validator.Validate(snapshot.Content);
        if (!validation.IsValid)
            throw new InvalidDataException($"Content at '{path}' failed canonical validation.");
        return snapshot;
    }

    private void RestorePreviousAfterFailedActivation()
    {
        if (!File.Exists(PreviousPath))
            return;

        try
        {
            if (File.Exists(ActivePath))
            {
                File.Replace(
                    PreviousPath,
                    ActivePath,
                    destinationBackupFileName: null,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(PreviousPath, ActivePath);
            }
        }
        catch
        {
            // The original activation exception is more useful to the caller.
            // Startup recovery will attempt to use any remaining valid file.
        }
    }
}
