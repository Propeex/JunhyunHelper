using System.IO;
using System.Text.Json;
using JunhyunHelper.Infrastructure.Storage;
using TarkovHelper.Models.Map;

namespace JunhyunHelper.Desktop.Map;

/// <summary>
/// JunhyunHelper-owned persistence for Map product settings. The transplanted
/// Tarkov Helper subsystem historically stored settings under its own legacy data
/// root, which made settings unreliable after packaging/migration. Product settings
/// are authoritative here and live under %LocalAppData%/JunhyunHelper.
/// </summary>
public sealed class JunhyunMapProductSettingsStore
{
    private static readonly Lazy<JunhyunMapProductSettingsStore> LazyInstance =
        new(() => new JunhyunMapProductSettingsStore());

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly object _gate = new();
    private readonly AtomicJsonFileStore _fileStore;
    private JunhyunMapProductSettings _settings;

    private JunhyunMapProductSettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper");
        Directory.CreateDirectory(root);
        _fileStore = new AtomicJsonFileStore(Path.Combine(root, "map-product-settings.json"));
        _settings = Load();
    }

    public static JunhyunMapProductSettingsStore Instance => LazyInstance.Value;

    public bool? GetToggle(string controlName)
    {
        lock (_gate)
            return _settings.Toggles.TryGetValue(controlName, out var value) ? value : null;
    }

    public void SetToggle(string controlName, bool value) => Update(settings =>
        settings.Toggles[controlName] = value);

    public bool? GetQuestMarkerEnabled(string questId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);
        lock (_gate)
            return _settings.QuestMarkerToggles.TryGetValue(questId, out var value) ? value : null;
    }

    public void SetQuestMarkerEnabled(string questId, bool value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(questId);
        Update(settings => settings.QuestMarkerToggles[questId.Trim()] = value);
    }

    public double? GetValue(string controlName)
    {
        lock (_gate)
            return _settings.Values.TryGetValue(controlName, out var value) ? value : null;
    }

    public void SetValue(string controlName, double value) => Update(settings =>
        settings.Values[controlName] = value);

    public void SetValues(IReadOnlyDictionary<string, double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
            return;

        Update(settings =>
        {
            foreach (var (controlName, value) in values)
            {
                if (!string.IsNullOrWhiteSpace(controlName) && double.IsFinite(value))
                    settings.Values[controlName] = value;
            }
        });
    }

    public int? GetSelection(string controlName)
    {
        lock (_gate)
            return _settings.Selections.TryGetValue(controlName, out var value) ? value : null;
    }

    public void SetSelection(string controlName, int value) => Update(settings =>
        settings.Selections[controlName] = value);

    public string? ScreenshotFolder
    {
        get { lock (_gate) return _settings.ScreenshotFolder; }
        set => Update(settings => settings.ScreenshotFolder = value);
    }

    public int GetHotkey(OverlayMiniMapHotkeyAction action, int fallback)
    {
        lock (_gate)
        {
            return _settings.Hotkeys.TryGetValue(action.ToString(), out var value)
                ? Math.Max(0, value)
                : Math.Max(0, fallback);
        }
    }

    public void SetHotkey(OverlayMiniMapHotkeyAction action, int virtualKey) => Update(settings =>
    {
        RemoveDuplicateHotkey(settings, virtualKey, action.ToString());
        settings.Hotkeys[action.ToString()] = Math.Max(0, virtualKey);
    });

    public int TemporaryHideKey
    {
        get { lock (_gate) return Math.Max(0, _settings.TemporaryHideKey); }
        set => Update(settings =>
        {
            var key = Math.Max(0, value);
            RemoveDuplicateHotkey(settings, key, TemporaryHideKeyName);
            settings.TemporaryHideKey = key;
        });
    }

    public double TemporaryHideSeconds
    {
        get { lock (_gate) return Math.Clamp(_settings.TemporaryHideSeconds, 1.0, 15.0); }
        set => Update(settings => settings.TemporaryHideSeconds = Math.Clamp(value, 1.0, 15.0));
    }

    /// <summary>
    /// Base MiniMap opacity while neither hover-hide nor timed-hide is active.
    /// Stored independently from the transplanted OverlayMiniMapSettings because the
    /// product intentionally fixed/removed the legacy opacity controls.
    /// </summary>
    public double MiniMapOpacity
    {
        get { lock (_gate) return Math.Clamp(_settings.MiniMapOpacity, 0.10, 1.0); }
        set => Update(settings => settings.MiniMapOpacity = Math.Clamp(value, 0.10, 1.0));
    }

    /// <summary>
    /// MiniMap-only scale multiplier for non-player marker visuals. This deliberately
    /// excludes the player position marker, which has its own existing size setting.
    /// </summary>
    public double MiniMapMarkerScale
    {
        get { lock (_gate) return Math.Clamp(_settings.MiniMapMarkerScale, 0.25, 1.50); }
        set => Update(settings => settings.MiniMapMarkerScale = Math.Clamp(value, 0.25, 1.50));
    }

    public bool RaiderVisible
    {
        get { lock (_gate) return _settings.RaiderVisible ?? true; }
        set => Update(settings => settings.RaiderVisible = value);
    }

    private const string TemporaryHideKeyName = "TemporaryHide";

    private static void RemoveDuplicateHotkey(
        JunhyunMapProductSettings settings,
        int virtualKey,
        string keepName)
    {
        if (virtualKey == 0)
            return;

        foreach (var key in settings.Hotkeys
                     .Where(pair => pair.Value == virtualKey &&
                                    !string.Equals(pair.Key, keepName, StringComparison.Ordinal))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            settings.Hotkeys[key] = 0;
        }

        if (!string.Equals(keepName, TemporaryHideKeyName, StringComparison.Ordinal) &&
            settings.TemporaryHideKey == virtualKey)
        {
            settings.TemporaryHideKey = 0;
        }
    }

    private void Update(Action<JunhyunMapProductSettings> mutation)
    {
        lock (_gate)
        {
            mutation(_settings);
            SaveLocked();
        }
    }

    private JunhyunMapProductSettings Load()
    {
        try
        {
            return Normalize(_fileStore.LoadOrDefault(
                () => new JunhyunMapProductSettings(),
                JsonOptions));
        }
        catch (Exception exception)
        {
            App.WriteDiagnostic("Failed to load map product settings", exception);
            return new JunhyunMapProductSettings();
        }
    }

    private void SaveLocked()
    {
        try
        {
            _fileStore.Save(_settings, JsonOptions);
        }
        catch (Exception exception)
        {
            // Map settings are presentation preferences. Keep the live setting and log
            // the persistence problem instead of terminating the application.
            App.WriteDiagnostic("Failed to save map product settings", exception);
        }
    }

    private static JunhyunMapProductSettings Normalize(JunhyunMapProductSettings settings)
    {
        settings.Toggles ??= new Dictionary<string, bool>(StringComparer.Ordinal);
        settings.QuestMarkerToggles ??= new Dictionary<string, bool>(StringComparer.Ordinal);
        settings.Values ??= new Dictionary<string, double>(StringComparer.Ordinal);
        settings.Selections ??= new Dictionary<string, int>(StringComparer.Ordinal);
        settings.Hotkeys ??= new Dictionary<string, int>(StringComparer.Ordinal);

        settings.TemporaryHideKey = Math.Max(0, settings.TemporaryHideKey);
        settings.TemporaryHideSeconds = Math.Clamp(settings.TemporaryHideSeconds, 1.0, 15.0);
        settings.MiniMapOpacity = Math.Clamp(settings.MiniMapOpacity, 0.10, 1.0);
        settings.MiniMapMarkerScale = Math.Clamp(settings.MiniMapMarkerScale, 0.25, 1.50);

        foreach (var key in settings.Hotkeys.Keys.ToArray())
            settings.Hotkeys[key] = Math.Max(0, settings.Hotkeys[key]);

        foreach (var key in settings.Values
                     .Where(pair => !double.IsFinite(pair.Value))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            settings.Values.Remove(key);
        }

        return settings;
    }
}

public sealed class JunhyunMapProductSettings
{
    public Dictionary<string, bool> Toggles { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, bool> QuestMarkerToggles { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, double> Values { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Selections { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Hotkeys { get; set; } = new(StringComparer.Ordinal);
    public string? ScreenshotFolder { get; set; }
    public bool? RaiderVisible { get; set; }
    public int TemporaryHideKey { get; set; }
    public double TemporaryHideSeconds { get; set; } = 3.0;
    public double MiniMapOpacity { get; set; } = 1.0;
    public double MiniMapMarkerScale { get; set; } = 1.0;
}
