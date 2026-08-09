using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

public sealed record ScreenshotPositionDetected(
    MapWorldPosition Position,
    double? HeadingDegrees,
    string FileName,
    DateTimeOffset DetectedAt);

public sealed class MapScreenshotTracker : IDisposable
{
    private static readonly Regex ScreenshotPattern = new(
        @"\d{4}-\d{2}-\d{2}\[\d{2}-\d{2}\]_(?<x>-?\d+(?:\.\d+)?),\s*(?<y>-?\d+(?:\.\d+)?),\s*(?<z>-?\d+(?:\.\d+)?)_(?<qx>-?\d+(?:\.\d+)?),\s*(?<qy>-?\d+(?:\.\d+)?),\s*(?<qz>-?\d+(?:\.\d+)?),\s*(?<qw>-?\d+(?:\.\d+)?)_",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private readonly ConcurrentDictionary<string, DateTimeOffset> _recent = new(StringComparer.OrdinalIgnoreCase);
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public event EventHandler<ScreenshotPositionDetected>? PositionDetected;
    public event EventHandler<string>? StatusChanged;

    public bool IsWatching => _watcher?.EnableRaisingEvents == true;
    public string? FolderPath => _watcher?.Path;

    public bool Start(string folderPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return false;

        Stop();
        _watcher = new FileSystemWatcher(folderPath, "*.png")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
        };
        _watcher.Created += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Error += OnError;
        StatusChanged?.Invoke(this, $"스크린샷 감시 중 · {folderPath}");
        return true;
    }

    public void Stop()
    {
        if (_watcher is null)
            return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnChanged;
        _watcher.Changed -= OnChanged;
        _watcher.Error -= OnError;
        _watcher.Dispose();
        _watcher = null;
        _recent.Clear();
        StatusChanged?.Invoke(this, "스크린샷 감시 중지");
    }

    public static string? TryDetectScreenshotFolder()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov", "Screenshots"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Escape from Tarkov"),
        };
        return candidates.FirstOrDefault(Directory.Exists);
    }

    public static bool TryParseFileName(string fileName, out ScreenshotPositionDetected? detected)
    {
        detected = null;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
        var match = ScreenshotPattern.Match(Path.GetFileName(fileName));
        if (!match.Success ||
            !TryParse(match, "x", out var x) ||
            !TryParse(match, "y", out var y) ||
            !TryParse(match, "z", out var z) ||
            !TryParse(match, "qx", out var qx) ||
            !TryParse(match, "qy", out var qy) ||
            !TryParse(match, "qz", out var qz) ||
            !TryParse(match, "qw", out var qw))
            return false;

        var heading = QuaternionYaw(qx, qy, qz, qw);
        detected = new ScreenshotPositionDetected(
            new MapWorldPosition(x, y, z),
            heading,
            Path.GetFileName(fileName),
            DateTimeOffset.Now);
        return true;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        if (_recent.TryGetValue(e.Name ?? e.FullPath, out var last) && (now - last).TotalMilliseconds < 600)
            return;
        _recent[e.Name ?? e.FullPath] = now;

        _ = Task.Run(async () =>
        {
            await Task.Delay(350).ConfigureAwait(false);
            if (TryParseFileName(e.Name ?? e.FullPath, out var detected) && detected is not null)
            {
                PositionDetected?.Invoke(this, detected);
                StatusChanged?.Invoke(this, $"위치 갱신 · X {detected.Position.X:0.0}, Z {detected.Position.Z:0.0}");
            }
            else
            {
                StatusChanged?.Invoke(this, $"스크린샷 좌표를 읽지 못했습니다 · {e.Name}");
            }
            CleanupRecent();
        });
    }

    private void OnError(object sender, ErrorEventArgs e) =>
        StatusChanged?.Invoke(this, $"스크린샷 감시 오류 · {e.GetException().Message}");

    private static bool TryParse(Match match, string group, out double value) =>
        double.TryParse(
            match.Groups[group].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value) && double.IsFinite(value);

    private static double QuaternionYaw(double qx, double qy, double qz, double qw)
    {
        var sinyCosp = 2.0 * (qw * qy + qx * qz);
        var cosyCosp = 1.0 - 2.0 * (qy * qy + qz * qz);
        var degrees = Math.Atan2(sinyCosp, cosyCosp) * 180.0 / Math.PI + 180.0;
        return (degrees % 360.0 + 360.0) % 360.0;
    }

    private void CleanupRecent()
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-1);
        foreach (var entry in _recent.Where(entry => entry.Value < cutoff).ToArray())
            _recent.TryRemove(entry.Key, out _);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
