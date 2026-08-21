using System.Globalization;
using System.Text;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Small, bounded diagnostic stream for live Scanner validation. It records decisions
/// and capture/OCR metadata only; screenshots and raw pixel buffers are never persisted.
/// Failures in diagnostics are always non-fatal.
///
/// Recognition attempts are also projected into a bounded user activity feed. The feed
/// is independent of file I/O success and intentionally contains only readable OCR/match
/// decision data, not low-level capture metadata. On startup it restores recent decisions
/// from the existing bounded scanner.log(.1) files so the Scanner tab is useful across
/// app restarts without adding a second persistence format.
/// </summary>
internal static class ScannerDiagnosticLog
{
    private const long MaximumBytes = 2 * 1024 * 1024;
    private const int MaximumRecentActivities = 60;
    private static readonly object Gate = new();
    private static readonly Dictionary<ScannerCaptureMode, string> LastOcrByMode = [];
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
    /// Clears both the user-facing recognition history and the bounded scanner.log files.
    /// New runtime activity may create a fresh scanner.log immediately after this call.
    /// File-system failures are reported through the return value and never affect Scanner.
    /// </summary>
    public static bool Clear()
    {
        var success = true;
        lock (Gate)
        {
            // Do not hydrate history just before deleting it. Mark the current process as
            // hydrated so a partially undeletable old file is not replayed into the UI.
            _historyHydrated = true;
            LastOcrByMode.Clear();
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
            // A presentation subscriber must never affect diagnostics or Scanner.
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
                activity = UpdateUserActivity(eventName, mode, fields, DateTimeOffset.Now);

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
            // A presentation subscriber must never affect recognition.
        }
    }

    private static void EnsureRecentActivitiesLoaded()
    {
        if (_historyHydrated)
            return;
        _historyHydrated = true;

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
                // Existing diagnostics are best-effort history only.
            }
        }

        if (RecentActivities.Count > MaximumRecentActivities)
            RecentActivities.RemoveRange(MaximumRecentActivities, RecentActivities.Count - MaximumRecentActivities);
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
            pendingOcr[mode] = fields.GetValueOrDefault("text", string.Empty);
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
            fields.GetValueOrDefault("reason", string.Empty));

        RecentActivities.Insert(0, activity);
        if (RecentActivities.Count > MaximumRecentActivities)
            RecentActivities.RemoveAt(RecentActivities.Count - 1);
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
        DateTimeOffset timestamp)
    {
        if (mode is null)
            return null;

        if (string.Equals(eventName, "ocr-result", StringComparison.Ordinal))
        {
            LastOcrByMode[mode.Value] = FieldText(fields, "text");
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
            FieldText(fields, "reason"));

        RecentActivities.Insert(0, activity);
        if (RecentActivities.Count > MaximumRecentActivities)
            RecentActivities.RemoveRange(MaximumRecentActivities, RecentActivities.Count - MaximumRecentActivities);
        return activity;
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
            // If rotation cannot be completed, keep appending rather than affecting Scanner.
        }
    }

    private static string Sanitize(string value) => value
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Replace("|", "/", StringComparison.Ordinal)
        .Trim();
}
