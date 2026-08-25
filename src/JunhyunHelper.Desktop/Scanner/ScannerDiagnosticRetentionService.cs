using System.Text.Json;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Removes legacy automatically captured, unreviewed Scanner diagnostic cases and
/// performs lightweight runtime-log maintenance. New Scanner builds do not create
/// durable automatic cases during normal monitoring; only explicit user review saves
/// correction/Ground Truth data. Human-reviewed data and unknown/corrupt metadata are
/// outside every automatic deletion policy here and fail closed.
/// </summary>
internal sealed class ScannerDiagnosticRetentionService : IDisposable
{
    private static readonly TimeSpan RecentCaseSafetyWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromHours(6);

    private readonly Timer _timer;
    private int _maintenanceRunning;
    private int _disposed;

    public ScannerDiagnosticRetentionService()
    {
        _timer = new Timer(
            static state => ((ScannerDiagnosticRetentionService)state!).QueueMaintenance(),
            this,
            InitialDelay,
            MaintenanceInterval);
    }

    private void QueueMaintenance()
    {
        if (Volatile.Read(ref _disposed) != 0 ||
            Interlocked.Exchange(ref _maintenanceRunning, 1) != 0)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                // Text-only Scanner runtime logs are ephemeral and independently bounded.
                // Durable correction data is user-owned and handled separately below.
                ScannerDiagnosticLog.PruneExpiredEntries();
                RemoveLegacyAutomaticCases();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                App.WriteDiagnostic("Scanner diagnostic maintenance failed", exception);
            }
            finally
            {
                Volatile.Write(ref _maintenanceRunning, 0);
            }
        });
    }

    private static void RemoveLegacyAutomaticCases()
    {
        var casesRoot = Path.Combine(ScannerDiagnosticDataset.RootPath, "cases");
        if (!Directory.Exists(casesRoot))
            return;

        var nowUtc = DateTimeOffset.UtcNow;
        var deletedCount = 0;
        long deletedBytes = 0;

        foreach (var directory in Directory.EnumerateDirectories(casesRoot, "case_*", SearchOption.TopDirectoryOnly))
        {
            if (!TryReadAutomaticUnreviewedCase(directory, out var info) ||
                nowUtc - info.LastWriteUtc < RecentCaseSafetyWindow)
            {
                continue;
            }

            // Re-read immediately before deletion. A case that was reviewed, manually
            // saved, changed, or became unreadable after enumeration is preserved.
            if (!TryReadAutomaticUnreviewedCase(directory, out var current) ||
                nowUtc - current.LastWriteUtc < RecentCaseSafetyWindow ||
                current.LastWriteUtc != info.LastWriteUtc)
            {
                continue;
            }

            try
            {
                Directory.Delete(directory, recursive: true);
                deletedCount++;
                deletedBytes += info.Bytes;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // Best effort. A locked or otherwise unsafe case is preserved and can be
                // reconsidered by a later maintenance pass.
            }
        }

        if (deletedCount > 0)
        {
            ScannerDiagnosticLog.Write(
                "diagnostic-retention",
                null,
                ("deletedLegacyAutomaticCases", deletedCount),
                ("deletedBytes", deletedBytes));
        }
    }

    private static bool TryReadAutomaticUnreviewedCase(
        string directoryPath,
        out AutomaticCaseInfo info)
    {
        info = default;
        try
        {
            var metadataPath = Path.Combine(directoryPath, "case.json");
            if (!File.Exists(metadataPath))
                return false;

            using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = document.RootElement;
            if (!root.TryGetProperty("retention", out var retention) ||
                !string.Equals(retention.GetString(), "automatic_sample", StringComparison.Ordinal) ||
                !root.TryGetProperty("review_status", out var reviewStatus) ||
                !string.Equals(reviewStatus.GetString(), "unreviewed", StringComparison.Ordinal))
            {
                return false;
            }

            var directory = new DirectoryInfo(directoryPath);
            info = new AutomaticCaseInfo(
                directoryPath,
                GetDirectoryBytes(directoryPath),
                new DateTimeOffset(directory.LastWriteTimeUtc, TimeSpan.Zero));
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            // Fail closed: ownership/review status must be proven before deletion.
            return false;
        }
    }

    private static long GetDirectoryBytes(string directoryPath)
    {
        long bytes = 0;
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                bytes += new FileInfo(file).Length;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
            }
        }
        return bytes;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _timer.Dispose();
    }

    private readonly record struct AutomaticCaseInfo(
        string DirectoryPath,
        long Bytes,
        DateTimeOffset LastWriteUtc);
}
