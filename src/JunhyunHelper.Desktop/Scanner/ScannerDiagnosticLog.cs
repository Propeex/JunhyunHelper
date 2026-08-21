using System.Globalization;
using System.Text;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Small, bounded diagnostic stream for live Scanner validation. It records decisions
/// and capture/OCR metadata only; screenshots and raw pixel buffers are never persisted.
/// Failures in diagnostics are always non-fatal.
///
/// Recognition attempts are also projected into a bounded in-memory user activity feed.
/// The feed is independent of file I/O success and intentionally contains only readable
/// OCR/match decision data, not low-level capture metadata.
/// </summary>
internal static class ScannerDiagnosticLog
{
    private const long MaximumBytes = 2 * 1024 * 1024;
    private const int MaximumRecentActivities = 60;
    private static readonly object Gate = new();
    private static readonly Dictionary<ScannerCaptureMode, string> LastOcrByMode = [];
    private static readonly List<ScannerActivityEntry> RecentActivities = [];

    public static event Action<ScannerActivityEntry>? ActivityAdded;

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JunhyunHelper",
        "logs",
        "scanner.log");

    public static IReadOnlyList<ScannerActivityEntry> GetRecentActivities()
    {
        lock (Gate)
            return RecentActivities.ToArray();
    }

    public static void Write(string eventName, ScannerCaptureMode? mode = null, params (string Key, object? Value)[] fields)
    {
        ScannerActivityEntry? activity = null;
        try
        {
            lock (Gate)
            {
                activity = UpdateUserActivity(eventName, mode, fields);

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

    private static ScannerActivityEntry? UpdateUserActivity(
        string eventName,
        ScannerCaptureMode? mode,
        IReadOnlyList<(string Key, object? Value)> fields)
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
            DateTimeOffset.Now,
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

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
