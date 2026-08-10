using System.IO;
using System.Linq;
using TarkovHelper.Services.Settings;

namespace TarkovHelper.Services.Logging;

/// <summary>
/// JunhyunHelper-owned replacement for the transplanted logging root policy.
/// Legacy Map logging APIs remain source-compatible, but log files belong with the
/// rest of JunhyunHelper's user-local data rather than beside the portable executable.
/// </summary>
public sealed class LoggingService : IDisposable
{
    private static LoggingService? _instance;
    private static readonly object Gate = new();

    public static LoggingService Instance
    {
        get
        {
            if (_instance is not null)
                return _instance;

            lock (Gate)
                return _instance ??= new LoggingService();
        }
    }

    private readonly string _logDirectory;
    private readonly string _sessionFolder;
    private readonly FileLogWriter _fileWriter;
    private LogLevel _minimumLevel;
    private bool _enableConsoleOutput;
    private bool _disposed;

    public string SessionFolder => _sessionFolder;
    public string LogDirectory => _logDirectory;

    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => _minimumLevel = value;
    }

    public bool EnableConsoleOutput
    {
        get => _enableConsoleOutput;
        set => _enableConsoleOutput = value;
    }

    private LoggingService()
    {
        var localRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper");
        _logDirectory = Path.Combine(localRoot, "Logs");
        Directory.CreateDirectory(_logDirectory);

        _sessionFolder = CreateSessionFolder();
        _fileWriter = new FileLogWriter(_sessionFolder);

#if DEBUG
        _minimumLevel = LogLevel.Trace;
        _enableConsoleOutput = true;
#else
        _minimumLevel = LogLevel.Warning;
        _enableConsoleOutput = false;
#endif

        Log(
            LogLevel.Info,
            "LoggingService",
            $"Logging initialized. Session: {Path.GetFileName(_sessionFolder)}, Level: {_minimumLevel}");
    }

    private string CreateSessionFolder()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var existingFolders = Directory.GetDirectories(_logDirectory, $"{today}-*")
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name =>
            {
                var parts = name!.Split('-');
                return parts.Length >= 4 && int.TryParse(parts[3], out var value)
                    ? value
                    : 0;
            })
            .ToList();

        var instanceNumber = existingFolders.Count == 0
            ? 1
            : existingFolders.Max() + 1;
        var sessionFolder = Path.Combine(_logDirectory, $"{today}-{instanceNumber:D3}");
        Directory.CreateDirectory(sessionFolder);
        return sessionFolder;
    }

    public void LoadSettingsFromDb()
    {
#if !DEBUG
        try
        {
            var levelText = SettingsService.Instance.GetValue(
                "logging.level",
                ((int)LogLevel.Warning).ToString());
            if (int.TryParse(levelText, out var level) && level is >= 0 and <= 6)
            {
                _minimumLevel = (LogLevel)level;
                Log(LogLevel.Info, "LoggingService", $"Loaded log level from settings: {_minimumLevel}");
            }
        }
        catch (Exception exception)
        {
            Log(
                LogLevel.Warning,
                "LoggingService",
                $"Failed to load log level from settings: {exception.Message}");
        }
#endif
    }

    public void SaveLogLevel(LogLevel level)
    {
        _minimumLevel = level;
        try
        {
            SettingsService.Instance.SetValue("logging.level", ((int)level).ToString());
            Log(LogLevel.Info, "LoggingService", $"Log level saved: {level}");
        }
        catch (Exception exception)
        {
            Log(
                LogLevel.Warning,
                "LoggingService",
                $"Failed to save log level: {exception.Message}");
        }
    }

    public void Log(
        LogLevel level,
        string category,
        string message,
        Exception? exception = null)
    {
        if (level < _minimumLevel || _disposed)
            return;

        var entry = new LogEntry(DateTime.Now, level, category, message, exception);
        _fileWriter.Enqueue(entry);

        if (!_enableConsoleOutput)
            return;

        var consoleMessage =
            $"[{entry.Timestamp:HH:mm:ss.fff}] [{GetLevelString(level)}] [{category}] {message}";
        System.Diagnostics.Debug.WriteLine(consoleMessage);
        if (exception is not null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"    Exception: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static string GetLevelString(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Info => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRIT",
        _ => "UNKN",
    };

    public ILogger CreateLogger<T>() => new CategoryLogger(typeof(T).Name, this);

    public ILogger CreateLogger(string category) => new CategoryLogger(category, this);

    public void Dispose()
    {
        if (_disposed)
            return;

        // Flush the writer after setting disposed; no further log entries should be queued.
        _disposed = true;
        _fileWriter.Dispose();
    }

    private sealed class CategoryLogger(string category, LoggingService service) : ILogger
    {
        public void Trace(string message) => service.Log(LogLevel.Trace, category, message);
        public void Debug(string message) => service.Log(LogLevel.Debug, category, message);
        public void Info(string message) => service.Log(LogLevel.Info, category, message);
        public void Warning(string message) => service.Log(LogLevel.Warning, category, message);
        public void Error(string message, Exception? exception = null) =>
            service.Log(LogLevel.Error, category, message, exception);
        public void Critical(string message, Exception? exception = null) =>
            service.Log(LogLevel.Critical, category, message, exception);
        public void Log(LogLevel level, string message, Exception? exception = null) =>
            service.Log(level, category, message, exception);
        public bool IsEnabled(LogLevel level) => level >= service.MinimumLevel;
    }
}
