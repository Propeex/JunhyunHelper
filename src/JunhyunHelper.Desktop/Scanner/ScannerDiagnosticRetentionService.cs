using System.Text.Json;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Bounds only automatically captured, unreviewed Scanner diagnostic cases. Human-reviewed
/// Ground Truth is intentionally outside this retention policy and is never deleted here.
/// Unknown/corrupt metadata also fails closed and remains untouched.
/// </summary>
internal sealed class ScannerDiagnosticRetentionService : IDisposable
{
    private const int MaximumAutomaticCaseCount = 300;
    private const long MaximumAutomaticBytes = 512L * 1024 * 1024;
    private static readonly TimeSpan MaximumAutomaticAge = TimeSpan.FromDays(30);
    private static readonly TimeSpan RecentCaseSafetyWindow = TimeSpan.FromHours(2);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
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
                PruneAutomaticCases();
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                App.WriteDiagnostic("Scanner automatic diagnostic retention failed", exception);
            }
            finally
            {
                Volatile.Write(ref _maintenanceRunning, 0);
            }
        });
    }

    private static void PruneAutomaticCases()
    {
        var casesRoot = Path.Combine(ScannerDiagnosticDataset.RootPath, "cases");
        if (!Directory.Exists(casesRoot))
            return;

        var nowUtc = DateTimeOffset.UtcNow;
        var cases = new List<AutomaticCaseInfo>();
        foreach (var directory in Directory.EnumerateDirectories(casesRoot, "case_*", SearchOption.TopDirectoryOnly))
        {
            if (TryReadAutomaticUnreviewedCase(directory, out var info))
                cases.Add(info);
        }

        if (cases.Count == 0)
            return;

        long totalBytes = cases.Sum(item => item.Bytes);
        var remainingCount = cases.Count;
        var deletedCount = 0;
        long deletedBytes = 0;

        foreach (var item in cases
                     .Where(item => nowUtc - item.TimestampUtc > MaximumAutomaticAge)
                     .OrderBy(item => item.TimestampUtc))
        {
            if (!IsSafeToDelete(item, nowUtc) || !TryDeleteStillAutomaticUnreviewed(item.DirectoryPath, nowUtc))
                continue;
            remainingCount--;
            totalBytes -= item.Bytes;
            deletedCount++;
            deletedBytes += item.Bytes;
        }

        if (remainingCount > MaximumAutomaticCaseCount || totalBytes > MaximumAutomaticBytes)
        {
            foreach (var item in cases.OrderBy(item => item.TimestampUtc))
            {
                if (remainingCount <= MaximumAutomaticCaseCount && totalBytes <= MaximumAutomaticBytes)
                    break;
                if (!Directory.Exists(item.DirectoryPath) ||
                    !IsSafeToDelete(item, nowUtc) ||
                    !TryDeleteStillAutomaticUnreviewed(item.DirectoryPath, nowUtc))
                {
                    continue;
                }

                remainingCount--;
                totalBytes -= item.Bytes;
                deletedCount++;
                deletedBytes += item.Bytes;
            }
        }

        if (deletedCount > 0)
        {
            ScannerDiagnosticLog.Write(
                "diagnostic-retention",
                null,
                ("deletedAutomaticCases", deletedCount),
                ("deletedBytes", deletedBytes),
                ("remainingAutomaticCases", Math.Max(0, remainingCount)),
                ("remainingAutomaticBytes", Math.Max(0, totalBytes)));
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
            var timestamp = ReadTimestamp(root) ?? new DateTimeOffset(directory.LastWriteTimeUtc, TimeSpan.Zero);
            info = new AutomaticCaseInfo(
                directoryPath,
                timestamp.ToUniversalTime(),
                GetDirectoryBytes(directoryPath),
                new DateTimeOffset(directory.LastWriteTimeUtc, TimeSpan.Zero));
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            // Fail closed: a case whose ownership/review status cannot be proven is retained.
            return false;
        }
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement root)
    {
        if (!root.TryGetProperty("timestamp", out var timestampElement))
            return null;
        return DateTimeOffset.TryParse(timestampElement.GetString(), out var timestamp)
            ? timestamp
            : null;
    }

    private static bool IsSafeToDelete(AutomaticCaseInfo item, DateTimeOffset nowUtc) =>
        nowUtc - item.LastWriteUtc >= RecentCaseSafetyWindow;

    private static bool TryDeleteStillAutomaticUnreviewed(string directoryPath, DateTimeOffset nowUtc)
    {
        try
        {
            if (!TryReadAutomaticUnreviewedCase(directoryPath, out var current) ||
                !IsSafeToDelete(current, nowUtc))
            {
                return false;
            }

            Directory.Delete(directoryPath, recursive: true);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
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
        DateTimeOffset TimestampUtc,
        long Bytes,
        DateTimeOffset LastWriteUtc);
}
