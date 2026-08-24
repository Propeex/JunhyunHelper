using System.Globalization;
using System.Text;

namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Bounded retention for the small text-only Scanner runtime log. Reviewed Ground Truth
/// lives in a separate dataset and is intentionally outside this helper.
/// </summary>
public static class ScannerLogRetention
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static ScannerLogRetentionResult PruneFiles(
        string primaryPath,
        string rotatedPath,
        TimeSpan maximumAge,
        long maximumBytes,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(rotatedPath);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumAge, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumBytes, 0);

        var tempPath = primaryPath + ".retention.tmp";
        try
        {
            var cutoffUtc = nowUtc.ToUniversalTime() - maximumAge;
            var retained = new List<LogLine>();
            var totalRead = 0;

            foreach (var path in new[] { rotatedPath, primaryPath })
            {
                if (!File.Exists(path))
                    continue;

                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                {
                    totalRead++;
                    if (!TryReadTimestamp(line, out var timestampUtc) || timestampUtc < cutoffUtc)
                        continue;

                    retained.Add(new LogLine(line, GetEncodedBytes(line)));
                }
            }

            long retainedBytes = retained.Sum(line => line.Bytes);
            var firstKeptIndex = 0;
            while (firstKeptIndex < retained.Count && retainedBytes > maximumBytes)
            {
                retainedBytes -= retained[firstKeptIndex].Bytes;
                firstKeptIndex++;
            }

            var retainedCount = retained.Count - firstKeptIndex;
            if (retainedCount == 0)
            {
                TryDelete(primaryPath);
                TryDelete(rotatedPath);
                TryDelete(tempPath);
                return new ScannerLogRetentionResult(
                    Success: true,
                    RetainedLines: 0,
                    RemovedLines: totalRead,
                    RetainedBytes: 0);
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(primaryPath));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            using (var writer = new StreamWriter(tempPath, append: false, Utf8NoBom))
            {
                for (var index = firstKeptIndex; index < retained.Count; index++)
                    writer.WriteLine(retained[index].Text);
            }

            File.Move(tempPath, primaryPath, overwrite: true);
            TryDelete(rotatedPath);

            return new ScannerLogRetentionResult(
                Success: true,
                RetainedLines: retainedCount,
                RemovedLines: Math.Max(0, totalRead - retainedCount),
                RetainedBytes: retainedBytes);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            System.Security.SecurityException)
        {
            TryDelete(tempPath);
            return new ScannerLogRetentionResult(
                Success: false,
                RetainedLines: 0,
                RemovedLines: 0,
                RetainedBytes: 0);
        }
    }

    private static bool TryReadTimestamp(string line, out DateTimeOffset timestampUtc)
    {
        timestampUtc = default;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var separator = line.IndexOf(" | ", StringComparison.Ordinal);
        var timestampText = separator > 0 ? line[..separator] : line;
        if (!DateTimeOffset.TryParse(
                timestampText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var timestamp))
        {
            return false;
        }

        timestampUtc = timestamp.ToUniversalTime();
        return true;
    }

    private static long GetEncodedBytes(string line) =>
        Utf8NoBom.GetByteCount(line) + Utf8NoBom.GetByteCount(Environment.NewLine);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            System.Security.SecurityException)
        {
        }
    }

    private readonly record struct LogLine(string Text, long Bytes);
}

public sealed record ScannerLogRetentionResult(
    bool Success,
    int RetainedLines,
    int RemovedLines,
    long RetainedBytes);
