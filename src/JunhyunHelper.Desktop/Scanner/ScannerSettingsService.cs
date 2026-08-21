using JunhyunHelper.Infrastructure.Storage;

namespace JunhyunHelper.Desktop.Scanner;

public sealed class ScannerSettingsService
{
    private readonly object _gate = new();
    private readonly AtomicJsonFileStore _store;
    private ScannerDisplaySettings _settings;

    public ScannerSettingsService(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _store = new AtomicJsonFileStore(Path.Combine(
            Path.GetFullPath(rootDirectory),
            "scanner-settings.json"));
        _settings = Load();
    }

    public event Action<ScannerDisplaySettings>? SettingsChanged;

    public ScannerDisplaySettings Current
    {
        get
        {
            lock (_gate)
                return _settings.Clone();
        }
    }

    public ScannerDisplaySettings Update(Action<ScannerDisplaySettings> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        ScannerDisplaySettings snapshot;
        lock (_gate)
        {
            var next = _settings.Clone();
            update(next);
            next.Normalize();
            try
            {
                _store.Save(next);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                App.WriteDiagnostic("Failed to save Scanner preferences", exception);
            }

            _settings = next;
            snapshot = next.Clone();
        }

        SettingsChanged?.Invoke(snapshot);
        return snapshot;
    }

    public ScannerDisplaySettings ResetPosition() => Update(settings =>
    {
        settings.PositionX = null;
        settings.PositionY = null;
    });

    private ScannerDisplaySettings Load()
    {
        try
        {
            var settings = _store.LoadOrDefault(() => new ScannerDisplaySettings());
            settings.Normalize();
            return settings;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            App.WriteDiagnostic("Failed to load Scanner preferences", exception);
            return new ScannerDisplaySettings();
        }
    }
}
