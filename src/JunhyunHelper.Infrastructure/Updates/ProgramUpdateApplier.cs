using System.Diagnostics;

namespace JunhyunHelper.Infrastructure.Updates;

public sealed record ProgramUpdateApplyRequest(
    int ParentProcessId,
    string StagingDirectory,
    string TargetDirectory,
    string RestartExecutable,
    string VersionText);

public static class ProgramUpdateApplier
{
    public static async Task ApplyAsync(
        ProgramUpdateApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateApplyRequest(request);

        await WaitForParentExitAsync(request.ParentProcessId, cancellationToken).ConfigureAwait(false);
        await ReplaceProductFilesAsync(request.StagingDirectory, request.TargetDirectory, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task ReplaceProductFilesAsync(
        string stagingDirectory,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        GitHubProgramUpdateClient.ValidateStagingDirectory(stagingDirectory);
        Directory.CreateDirectory(targetDirectory);

        var token = Guid.NewGuid().ToString("N");
        var targetExecutable = Path.Combine(targetDirectory, "준현 헬퍼.exe");
        var targetFirstRun = Path.Combine(targetDirectory, "FIRST_RUN_KO.txt");
        var targetAssets = Path.Combine(targetDirectory, "Assets");

        var nextExecutable = Path.Combine(targetDirectory, $".__junhyun_update_{token}.exe");
        var nextFirstRun = Path.Combine(targetDirectory, $".__junhyun_update_{token}.txt");
        var nextAssets = Path.Combine(targetDirectory, $".__junhyun_update_assets_{token}");

        var previousExecutable = Path.Combine(targetDirectory, $".__junhyun_previous_{token}.exe");
        var previousFirstRun = Path.Combine(targetDirectory, $".__junhyun_previous_{token}.txt");
        var previousAssets = Path.Combine(targetDirectory, $".__junhyun_previous_assets_{token}");

        var executableMoved = false;
        var firstRunMoved = false;
        var assetsMoved = false;
        var newExecutableCommitted = false;
        var newFirstRunCommitted = false;
        var newAssetsCommitted = false;

        try
        {
            await CopyFileDurablyAsync(
                Path.Combine(stagingDirectory, "준현 헬퍼.exe"),
                nextExecutable,
                cancellationToken).ConfigureAwait(false);
            await CopyFileDurablyAsync(
                Path.Combine(stagingDirectory, "FIRST_RUN_KO.txt"),
                nextFirstRun,
                cancellationToken).ConfigureAwait(false);
            await CopyDirectoryAsync(
                Path.Combine(stagingDirectory, "Assets"),
                nextAssets,
                cancellationToken).ConfigureAwait(false);

            if (Directory.Exists(targetAssets))
            {
                Directory.Move(targetAssets, previousAssets);
                assetsMoved = true;
            }
            Directory.Move(nextAssets, targetAssets);
            newAssetsCommitted = true;

            if (File.Exists(targetFirstRun))
            {
                File.Move(targetFirstRun, previousFirstRun);
                firstRunMoved = true;
            }
            File.Move(nextFirstRun, targetFirstRun);
            newFirstRunCommitted = true;

            if (File.Exists(targetExecutable))
            {
                File.Move(targetExecutable, previousExecutable);
                executableMoved = true;
            }
            File.Move(nextExecutable, targetExecutable);
            newExecutableCommitted = true;

            TryDeleteFile(previousExecutable);
            TryDeleteFile(previousFirstRun);
            TryDeleteDirectory(previousAssets);
        }
        catch
        {
            RollBackFile(targetExecutable, previousExecutable, executableMoved, newExecutableCommitted);
            RollBackFile(targetFirstRun, previousFirstRun, firstRunMoved, newFirstRunCommitted);
            RollBackDirectory(targetAssets, previousAssets, assetsMoved, newAssetsCommitted);
            throw;
        }
        finally
        {
            TryDeleteFile(nextExecutable);
            TryDeleteFile(nextFirstRun);
            TryDeleteDirectory(nextAssets);
        }
    }

    public static void TryCleanupPreparedUpdate(PreparedProgramUpdate preparedUpdate)
    {
        ArgumentNullException.ThrowIfNull(preparedUpdate);
        TryDeleteDirectory(preparedUpdate.WorkDirectory);
    }

    private static async Task WaitForParentExitAsync(int parentProcessId, CancellationToken cancellationToken)
    {
        if (parentProcessId <= 0)
            return;

        Process process;
        try
        {
            process = Process.GetProcessById(parentProcessId);
        }
        catch (ArgumentException)
        {
            return;
        }

        using (process)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
    }

    private static void ValidateApplyRequest(ProgramUpdateApplyRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.StagingDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RestartExecutable);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.VersionText);

        var expectedExecutable = Path.GetFullPath(Path.Combine(request.TargetDirectory, "준현 헬퍼.exe"));
        var restartExecutable = Path.GetFullPath(request.RestartExecutable);
        if (!string.Equals(expectedExecutable, restartExecutable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The update restart executable is outside the JunhyunHelper product target.");

        if (!Version.TryParse(request.VersionText, out _))
            throw new InvalidDataException("The update version argument is invalid.");
    }

    private static async Task CopyFileDurablyAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 128 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        destination.Flush(flushToDisk: true);
    }

    private static async Task CopyDirectoryAsync(string sourceDirectory, string destinationDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            await CopyFileDurablyAsync(sourceFile, destinationFile, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void RollBackFile(
        string currentPath,
        string previousPath,
        bool previousMoved,
        bool newCommitted)
    {
        if (newCommitted)
            TryDeleteFile(currentPath);

        if (previousMoved && File.Exists(previousPath))
        {
            try
            {
                File.Move(previousPath, currentPath, overwrite: true);
            }
            catch
            {
                // Keep the original exception. Remaining previous file is recoverable evidence.
            }
        }
    }

    private static void RollBackDirectory(
        string currentPath,
        string previousPath,
        bool previousMoved,
        bool newCommitted)
    {
        if (newCommitted)
            TryDeleteDirectory(currentPath);

        if (previousMoved && Directory.Exists(previousPath))
        {
            try
            {
                Directory.Move(previousPath, currentPath);
            }
            catch
            {
                // Keep the original exception. Remaining previous directory is recoverable evidence.
            }
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort transaction cleanup.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort transaction cleanup.
        }
    }
}
