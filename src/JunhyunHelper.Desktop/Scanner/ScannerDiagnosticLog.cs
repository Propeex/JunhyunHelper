using System.Globalization;
using System.Text;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Small, bounded diagnostic stream for live Scanner validation. It records decisions
/// and capture/OCR metadata only; screenshots and raw pixel buffers are never persisted.
/// Failures in diagnostics are always non-fatal.
/// </summary>
internal static class ScannerDiagnosticLog
{
    private const long MaximumBytes = 2 * 1024 * 1024;
    private static readonly object Gate = new();

    public static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JunhyunHelper",
        "logs",
        "scanner.log");

    public static void Write(string eventName, ScannerCaptureMode? mode = null, params (string Key, object? Value)[] fields)
    {
        try
        {
            lock (Gate)
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
        }
        catch
        {
            // Diagnostics must never change Scanner or application behavior.
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
