using System.IO;
using System.Text.Json;
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
    private readonly string _path;
    private JunhyunMapProductSettings _settings;

    private JunhyunMapProductSettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JunhyunHelper");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "map-product-settings.json");
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
            return _settings.Hotkeys.TryGetValue(action.ToString(), out var value) ? value : fallback;
    }

    public void SetHotkey(OverlayMiniMapHotkeyAction action, int virtualKey) => Update(settings =>
    {
        RemoveDuplicateHotkey(settings, virtualKey, action.ToString());
        settings.Hotkeys[action.ToString()] = Math.Max(0, virtualKey);
    });

    public int TemporaryHideKey
    {
        get { lock (_gate) return _settings.TemporaryHideKey; }
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
            if (!File.Exists(_path))
                return new JunhyunMapProductSettings();

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<JunhyunMapProductSettings>(json, JsonOptions)
                   ?? new JunhyunMapProductSettings();
        }
        catch
        {
            return new JunhyunMapProductSettings();
        }
    }

    private void SaveLocked()
    {
        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(temporary, json);
            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch
            {
            }
        }
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
}
