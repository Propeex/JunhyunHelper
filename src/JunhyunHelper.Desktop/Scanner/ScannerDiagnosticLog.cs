using System.Globalization;
using System.Text;
using JunhyunHelper.Infrastructure.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Small, bounded diagnostic stream for live Scanner validation. Durable Ground Truth
/// images live separately under ScannerDiagnosticDataset and are created only through
/// explicit user correction. This stream records runtime decisions and automatically
/// includes the latest in-memory Case ID when one exists.
///
/// Recognition attempts are also projected into a bounded user activity feed. Repeated
/// equivalent failures are collapsed so normal continuous monitoring does not bury useful
/// results, while the small rotated text log remains available for support diagnostics.
/// </summary>
internal static class ScannerDiagnosticLog
{
    private const long MaximumBytes = 2 * 1024 * 1024;
    private const int MaximumRecentActivities = 60;
    private const int MaximumFailureSignatures = 256;
    private static readonly TimeSpan MaximumAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan RepeatedFailureActivityWindow = TimeSpan.FromSeconds(30);
    private static readonly object Gate = new();
    private static readonly Dictionary<ScannerCaptureMode, string> LastOcrByMode = [];
    private static readonly Dictionary<string, DateTimeOffset> LastFailureActivityBySignature = new(StringComparer.Ordinal);
    private static readonly List<ScannerActivityEntry> RecentActivities = [];
    private static bool _historyHydrated;

    public static event Action<ScannerActivityEntry>? ActivityAdded;
    public static event Action? ActivitiesCleared;

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JunhyunHelper",
        "logs",
        "scanner.log");

    public static IReadOnlyList<ScannerActivityEntry> GetRecentActivities()
    {
        lock (Gate)
        {
            EnsureRecentActivitiesLoaded();
            return RecentActivities.ToArray();
        }
    }

    /// <summary>
    /// Removes Scanner runtime log lines older than the product retention window.
    /// Ground Truth/correction cases are stored elsewhere and are never touched here.
    /// </summary>
    public static bool PruneExpiredEntries()
    {
        lock (Gate)
            return PruneExpiredEntriesLocked();
    }

    /// <summary>
    /// Clears both the user-facing recognition history and the bounded scanner.log files.
    /// Ground Truth/correction cases are intentionally not deleted here.
    /// </summary>
    public static bool Clear()
    {
        var success = true;
        lock (Gate)
        {
            _historyHydrated = true;
            LastOcrByMode.Clear();
            LastFailureActivityBySignature.Clear();
            RecentActivities.Clear();

            success &= TryDelete(Path);
            success &= TryDelete(Path + ".1");
        }

        try
        {
            ActivitiesCleared?.Invoke();
        }
        catch
        {
        }

        return success;
    }

    public static void Write(string eventName, ScannerCaptureMode? mode = null, params (string Key, object? Value)[] fields)
    {
        ScannerActivityEntry? activity = null;
        try
        {
            lock (Gate)
            {
                EnsureRecentActivitiesLoaded();

                var explicitCaseId = FieldText(fields, "caseId");
                var caseId = string.IsNullOrWhiteSpace(explicitCaseId)
                    ? ScannerRecognitionDebugStore.GetSnapshot()?.CaseId
                    : explicitCaseId;
                activity = UpdateUserActivity(eventName, mode, fields, DateTimeOffset.Now, caseId);

                try
                {
                    var path = Path;
                    var directory = System.IO.Path.GetDirectoryName(path)!;
                    Directory.CreateDirectory(directory);
                    RotateIfNeeded(path);

                    var builder = new StringBuilder()
                        .Append(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture))
                        .Append(" | ")
                        .Append(eventName);

                    if (mode is not null)
                        builder.Append(" | mode=").Append(mode.Value);

                    var hasExplicitCaseId = !string.IsNullOrWhiteSpace(explicitCaseId);
                    if (!hasExplicitCaseId && !string.IsNullOrWhiteSpace(caseId))
                        builder.Append(" | caseId=").Append(Sanitize(caseId));

                    foreach (var (key, value) in fields)
                    {
                        builder.Append(" | ")
                            .Append(Sanitize(key))
                            .Append('=')
                            .Append(Sanitize(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty));
                    }

                    builder.AppendLine();
                    File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
                }
                catch
                {
                    // The UI activity feed must still work when diagnostic file I/O fails.
                }
            }
        }
        catch
        {
            // Diagnostics and activity projection must never change Scanner behavior.
        }

        if (activity is null)
            return;

        try
        {
            ActivityAdded?.Invoke(activity);
        }
        catch
        {
        }
    }

    private static void EnsureRecentActivitiesLoaded()
    {
        if (_historyHydrated)
            return;
        _historyHydrated = true;

        // Runtime logs are ephemeral. Prune them before hydration so the Scanner page
        // never restores activity older than the same seven-day on-disk policy.
        PruneExpiredEntriesLocked();

        var pendingOcr = new Dictionary<ScannerCaptureMode, string>();
        foreach (var path in new[] { Path + ".1", Path })
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                foreach (var line in File.ReadLines(path, Encoding.UTF8))
                    ReplayDiagnosticLine(line, pendingOcr);
            }
            catch
            {
            }
        }

        if (RecentActivities.Count > MaximumRecentActivities)
            RecentActivities.RemoveRange(MaximumRecentActivities, RecentActivities.Count - MaximumRecentActivities);
    }

    private static bool PruneExpiredEntriesLocked()
    {
        try
        {
            var result = ScannerLogRetention.PruneFiles(
                Path,
                Path + ".1",
                MaximumAge,
                MaximumBytes,
                DateTimeOffset.UtcNow);
            return result.Success;
        }
        catch
        {
            // Log maintenance is best-effort and must never affect Scanner recognition.
            return false;
        }
    }

    private static void ReplayDiagnosticLine(
        string line,
        Dictionary<ScannerCaptureMode, string> pendingOcr)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        var segments = line.Split(" | ", StringSplitOptions.None);
        if (segments.Length < 3 ||
            !DateTimeOffset.TryParse(
                segments[0],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            return;
        }

        var eventName = segments[1].Trim();
        var fields = ParseFields(segments.AsSpan(2));
        if (!fields.TryGetValue("mode", out var modeText) ||
            !Enum.TryParse<ScannerCaptureMode>(modeText, ignoreCase: true, out var mode))
        {
            return;
        }

        if (string.Equals(eventName, "ocr-result", StringComparison.Ordinal))
        {
            pendingOcr[mode] = fields.GetValueOrDefault(
                "rawText",
                fields.GetValueOrDefault("text", string.Empty));
            return;
        }

        if (!string.Equals(eventName, "match-result", StringComparison.Ordinal))
            return;

        pendingOcr.TryGetValue(mode, out var ocrText);
        pendingOcr.Remove(mode);

        var activity = new ScannerActivityEntry(
            timestamp,
            mode,
            ocrText ?? string.Empty,
            EmptyToNull(fields.GetValueOrDefault("officialName", string.Empty)),
            ParseDouble(fields.GetValueOrDefault("confidence", string.Empty)),
            ParseDouble(fields.GetValueOrDefault("secondScore", string.Empty)),
            ParseBoolean(fields.GetValueOrDefault("success", string.Empty)),
            fields.GetValueOrDefault("reason", string.Empty),
            EmptyToNull(fields.GetValueOrDefault("caseId", string.Empty)));

        AddRecentActivity(activity);
    }

    private static Dictionary<string, string> ParseFields(ReadOnlySpan<string> segments)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var segment in segments)
        {
            var equals = segment.IndexOf('=');
            if (equals <= 0)
                continue;
            var key = segment[..equals].Trim();
            if (key.Length == 0)
                continue;
            result[key] = segment[(equals + 1)..].Trim();
        }
        return result;
    }

    private static ScannerActivityEntry? UpdateUserActivity(
        string eventName,
        ScannerCaptureMode? mode,
        IReadOnlyList<(string Key, object? Value)> fields,
        DateTimeOffset timestamp,
        string? caseId)
    {
        if (mode is null)
            return null;

        if (string.Equals(eventName, "ocr-result", StringComparison.Ordinal))
        {
            var raw = FieldText(fields, "rawText");
            LastOcrByMode[mode.Value] = string.IsNullOrWhiteSpace(raw)
                ? FieldText(fields, "text")
                : raw;
            return null;
        }

        if (!string.Equals(eventName, "match-result", StringComparison.Ordinal))
            return null;

        LastOcrByMode.TryGetValue(mode.Value, out var ocrText);
        LastOcrByMode.Remove(mode.Value);

        var activity = new ScannerActivityEntry(
            timestamp,
            mode.Value,
            ocrText ?? string.Empty,
            EmptyToNull(FieldText(fields, "officialName")),
            FieldDouble(fields, "confidence"),
            FieldDouble(fields, "secondScore"),
            FieldBoolean(fields, "success"),
            FieldText(fields, "reason"),
            EmptyToNull(caseId ?? string.Empty));

        return AddRecentActivity(activity) ? activity : null;
    }

    private static bool AddRecentActivity(ScannerActivityEntry activity)
    {
        if (!activity.Success)
        {
            var signature = BuildFailureSignature(activity);
            if (LastFailureActivityBySignature.TryGetValue(signature, out var previous) &&
                activity.Timestamp >= previous &&
                activity.Timestamp - previous < RepeatedFailureActivityWindow)
            {
                return false;
            }

            LastFailureActivityBySignature[signature] = activity.Timestamp;
            TrimFailureSignatures();
        }

        RecentActivities.Insert(0, activity);
        if (RecentActivities.Count > MaximumRecentActivities)
            RecentActivities.RemoveRange(MaximumRecentActivities, RecentActivities.Count - MaximumRecentActivities);
        return true;
    }

    private static string BuildFailureSignature(ScannerActivityEntry activity) => string.Join(
        '\u001f',
        activity.Mode.ToString(),
        activity.OcrText.Trim(),
        activity.OfficialName?.Trim() ?? string.Empty,
        activity.Reason.Trim());

    private static void TrimFailureSignatures()
    {
        if (LastFailureActivityBySignature.Count <= MaximumFailureSignatures)
            return;

        foreach (var key in LastFailureActivityBySignature
                     .OrderBy(pair => pair.Value)
                     .Take(LastFailureActivityBySignature.Count - MaximumFailureSignatures)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            LastFailureActivityBySignature.Remove(key);
        }
    }

    private static string FieldText(IReadOnlyList<(string Key, object? Value)> fields, string key)
    {
        foreach (var field in fields)
        {
            if (string.Equals(field.Key, key, StringComparison.Ordinal))
                return Convert.ToString(field.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        return string.Empty;
    }

    private static double FieldDouble(IReadOnlyList<(string Key, object? Value)> fields, string key)
    {
        foreach (var field in fields)
        {
            if (!string.Equals(field.Key, key, StringComparison.Ordinal) || field.Value is null)
                continue;
            try
            {
                return Convert.ToDouble(field.Value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
                return 0;
            }
        }
        return 0;
    }

    private static bool FieldBoolean(IReadOnlyList<(string Key, object? Value)> fields, string key)
    {
        foreach (var field in fields)
        {
            if (!string.Equals(field.Key, key, StringComparison.Ordinal) || field.Value is null)
                continue;
            try
            {
                return Convert.ToBoolean(field.Value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException)
            {
                return false;
            }
        }
        return false;
    }

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static bool ParseBoolean(string value) =>
        bool.TryParse(value, out var parsed) && parsed;

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaximumBytes)
            return;

        var previous = path + ".1";
        try
        {
            if (File.Exists(previous))
                File.Delete(previous);
            File.Move(path, previous);
        }
        catch
        {
        }
    }

    private static string Sanitize(string value) => value
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Replace("|", "/", StringComparison.Ordinal)
        .Trim();
}
