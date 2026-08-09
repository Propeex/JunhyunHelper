using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace JunhyunHelper.Desktop.Map;

public sealed class RaidMapWatcher : IDisposable
{
    private static readonly Regex NetworkCreateRegex = new(
        @"TRACE-NetworkGameCreate.*?Location:\s*([^,']+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));
    private static readonly Regex ScenePresetRegex = new(
        @"scene preset path:maps/([^.\s]+)\.bundle",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    private readonly ConcurrentDictionary<string, long> _positions = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public event EventHandler<string>? MapAliasDetected;

    public bool StartDefault()
    {
        var path = DefaultLogFolder();
        return path is not null && Start(path);
    }

    public bool Start(string logFolderPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(logFolderPath) || !Directory.Exists(logFolderPath))
            return false;

        Stop();
        foreach (var file in Directory.EnumerateFiles(logFolderPath, "*application*.log", SearchOption.AllDirectories))
        {
            try { _positions[file] = new FileInfo(file).Length; }
            catch (IOException) { }
        }

        _watcher = new FileSystemWatcher(logFolderPath, "*application*.log")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        return true;
    }

    public void Stop()
    {
        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnChanged;
            _watcher.Created -= OnChanged;
            _watcher.Dispose();
            _watcher = null;
        }
        _positions.Clear();
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => _ = ProcessFileAsync(e.FullPath);

    private async Task ProcessFileAsync(string path)
    {
        await _readGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(path))
                return;
            long length;
            try { length = new FileInfo(path).Length; }
            catch (IOException) { return; }

            var start = _positions.GetOrAdd(path, length);
            if (length < start)
                start = 0;
            if (length == start)
                return;

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            stream.Seek(start, SeekOrigin.Begin);
            using var reader = new StreamReader(stream);
            var appended = await reader.ReadToEndAsync().ConfigureAwait(false);
            _positions[path] = stream.Position;

            foreach (var line in appended.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var alias = MatchAlias(line);
                if (!string.IsNullOrWhiteSpace(alias))
                    MapAliasDetected?.Invoke(this, alias.Trim());
            }
        }
        catch (IOException)
        {
        }
        finally
        {
            _readGate.Release();
        }
    }

    private static string? MatchAlias(string line)
    {
        var network = NetworkCreateRegex.Match(line);
        if (network.Success)
            return network.Groups[1].Value;
        var scene = ScenePresetRegex.Match(line);
        return scene.Success ? scene.Groups[1].Value : null;
    }

    private static string? DefaultLogFolder()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Battlestate Games",
            "EFT",
            "Logs");
        return Directory.Exists(path) ? path : null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
        _readGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
