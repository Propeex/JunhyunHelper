using System.Text.Json;

namespace JunhyunHelper.Desktop.Map;

internal enum LegacyMiniMapViewMode
{
    Fixed = 0,
    PlayerTracking = 1,
}

internal sealed class LegacyMiniMapSettings
{
    public double PositionX { get; set; } = -1;
    public double PositionY { get; set; } = -1;
    public double Width { get; set; } = 300;
    public double Height { get; set; } = 300;
    public double Opacity { get; set; } = 0.8;
    public double ZoomMultiplier { get; set; } = 2.35;
    public double PlayerMarkerSize { get; set; } = 1;
    public LegacyMiniMapViewMode ViewMode { get; set; } = LegacyMiniMapViewMode.PlayerTracking;
    public bool ClickThrough { get; set; }
    public double FixedOffsetX { get; set; }
    public double FixedOffsetY { get; set; }
    public bool HasFixedOffset { get; set; }

    // Keep the same default key choices as the corrected legacy Tarkov-Helper.
    public int ZoomInKey { get; set; } = 0x6B;   // NumPad +
    public int ZoomOutKey { get; set; } = 0x6D;  // NumPad -
    public int FloorUpKey { get; set; } = 0x21;  // PageUp
    public int FloorDownKey { get; set; } = 0x22; // PageDown

    public void Normalize()
    {
        Width = Math.Clamp(Width, 200, 800);
        Height = Math.Clamp(Height, 200, 800);
        Opacity = Math.Clamp(Opacity, 0.1, 1);
        ZoomMultiplier = Math.Clamp(ZoomMultiplier, 0.5, 8);
        PlayerMarkerSize = Math.Clamp(PlayerMarkerSize, 0.5, 3);
        if (!double.IsFinite(PositionX)) PositionX = -1;
        if (!double.IsFinite(PositionY)) PositionY = -1;
        if (!double.IsFinite(FixedOffsetX)) FixedOffsetX = 0;
        if (!double.IsFinite(FixedOffsetY)) FixedOffsetY = 0;
    }
}

internal sealed class LegacyMiniMapSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly object _gate = new();
    private Task _saveTail = Task.CompletedTask;

    public LegacyMiniMapSettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "minimap-settings.json");
    }

    public LegacyMiniMapSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new LegacyMiniMapSettings();
            var settings = JsonSerializer.Deserialize<LegacyMiniMapSettings>(File.ReadAllText(_path), JsonOptions)
                           ?? new LegacyMiniMapSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new LegacyMiniMapSettings();
        }
    }

    public void QueueSave(LegacyMiniMapSettings settings)
    {
        string snapshot;
        try
        {
            settings.Normalize();
            snapshot = JsonSerializer.Serialize(settings, JsonOptions);
        }
        catch
        {
            return;
        }

        lock (_gate)
        {
            _saveTail = _saveTail.ContinueWith(
                async _ =>
                {
                    try
                    {
                        var temp = _path + ".tmp";
                        await File.WriteAllTextAsync(temp, snapshot).ConfigureAwait(false);
                        File.Move(temp, _path, overwrite: true);
                    }
                    catch
                    {
                        // MiniMap preferences are recoverable UI state. A failed save
                        // must never destabilize the Map runtime.
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default).Unwrap();
        }
    }

    public void Flush(LegacyMiniMapSettings settings)
    {
        QueueSave(settings);
        Task pending;
        lock (_gate)
            pending = _saveTail;
        try { pending.GetAwaiter().GetResult(); }
        catch { }
    }
}