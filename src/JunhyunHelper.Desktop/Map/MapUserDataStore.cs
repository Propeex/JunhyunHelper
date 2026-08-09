using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

public sealed record UserMapMarker(
    string Id,
    string MapId,
    string? FloorId,
    string Name,
    string Color,
    MapWorldPosition Position);

public sealed class MapUserSettings
{
    public string? ScreenshotFolderPath { get; set; }
    public string? LastMapId { get; set; }
    public Dictionary<string, string> LastFloorByMap { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<MapMarkerKind, bool> MarkerVisibility { get; set; } = DefaultMarkerVisibility();
    public bool ShowQuestMarkers { get; set; } = true;
    public bool ShowUserMarkers { get; set; } = true;
    public bool ShowPlayerPosition { get; set; } = true;
    public bool ShowTrail { get; set; }

    public static Dictionary<MapMarkerKind, bool> DefaultMarkerVisibility() =>
        Enum.GetValues<MapMarkerKind>().ToDictionary(
            kind => kind,
            kind => kind is not MapMarkerKind.LootContainer and not MapMarkerKind.LooseLoot);
}

public sealed class MapUserDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();
    private readonly string _settingsPath;
    private readonly string _markersPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MapUserDataStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var root = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(root);
        _settingsPath = Path.Combine(root, "map-settings.json");
        _markersPath = Path.Combine(root, "map-markers.json");
    }

    public async Task<MapUserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
            return new MapUserSettings();
        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath, cancellationToken);
            var settings = JsonSerializer.Deserialize<MapUserSettings>(json, JsonOptions) ?? new MapUserSettings();
            foreach (var kind in Enum.GetValues<MapMarkerKind>())
            {
                if (!settings.MarkerVisibility.ContainsKey(kind))
                    settings.MarkerVisibility[kind] = kind is not MapMarkerKind.LootContainer and not MapMarkerKind.LooseLoot;
            }
            return settings;
        }
        catch (JsonException)
        {
            return new MapUserSettings();
        }
    }

    public Task SaveSettingsAsync(MapUserSettings settings, CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(_settingsPath, settings, cancellationToken);

    public async Task<IReadOnlyList<UserMapMarker>> LoadMarkersAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_markersPath))
            return Array.Empty<UserMapMarker>();
        try
        {
            var json = await File.ReadAllTextAsync(_markersPath, cancellationToken);
            return JsonSerializer.Deserialize<UserMapMarker[]>(json, JsonOptions) ?? Array.Empty<UserMapMarker>();
        }
        catch (JsonException)
        {
            return Array.Empty<UserMapMarker>();
        }
    }

    public Task SaveMarkersAsync(IReadOnlyList<UserMapMarker> markers, CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(_markersPath, markers, cancellationToken);

    private async Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var temp = path + ".tmp";
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await File.WriteAllTextAsync(temp, json, cancellationToken);
            if (File.Exists(path))
                File.Move(temp, path, overwrite: true);
            else
                File.Move(temp, path);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
