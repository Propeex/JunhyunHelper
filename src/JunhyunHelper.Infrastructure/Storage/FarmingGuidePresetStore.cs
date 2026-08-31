using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed record FarmingGuideProfileState(
    FarmingGuideLoadoutSnapshot WorkingSnapshot,
    string? SelectedPresetName,
    IReadOnlyList<FarmingGuidePreset> Presets);

public sealed record FarmingGuideFixedEquipmentState(
    FarmingGuideItemState? Melee,
    FarmingGuideItemState? Dogtag)
{
    public static FarmingGuideFixedEquipmentState Empty { get; } = new(null, null);
}

public sealed class FarmingGuidePresetStore
{
    private const int CurrentSchemaVersion = 1;
    private readonly object _gate = new();
    private readonly AtomicJsonFileStore _store;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public FarmingGuidePresetStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _store = new AtomicJsonFileStore(Path.Combine(rootDirectory, "farming-guide.json"));
    }

    public FarmingGuideProfileState LoadProfile(string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        lock (_gate)
        {
            var document = LoadDocument();
            return document.Profiles.TryGetValue(profileId, out var profile)
                ? profile
                : EmptyProfile();
        }
    }

    public FarmingGuideFixedEquipmentState LoadFixedEquipment()
    {
        lock (_gate)
            return LoadDocument().FixedEquipment;
    }

    public void SaveWorking(
        string profileId,
        FarmingGuideLoadoutSnapshot snapshot,
        string? selectedPresetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? profile
                : EmptyProfile();
            var profiles = new Dictionary<string, FarmingGuideProfileState>(document.Profiles, StringComparer.Ordinal)
            {
                [profileId] = previous with
                {
                    WorkingSnapshot = snapshot,
                    SelectedPresetName = selectedPresetName,
                },
            };
            SaveDocument(document with { Profiles = profiles });
        }
    }

    public FarmingGuideProfileState SavePreset(
        string profileId,
        string name,
        FarmingGuideLoadoutSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedName = name.Trim();
        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? profile
                : EmptyProfile();
            var presets = previous.Presets
                .Where(preset => !string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
                .Append(new FarmingGuidePreset(normalizedName, snapshot, DateTimeOffset.UtcNow))
                .OrderBy(preset => preset.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var updated = new FarmingGuideProfileState(snapshot, normalizedName, presets);
            var profiles = new Dictionary<string, FarmingGuideProfileState>(document.Profiles, StringComparer.Ordinal)
            {
                [profileId] = updated,
            };
            SaveDocument(document with { Profiles = profiles });
            return updated;
        }
    }

    public FarmingGuideProfileState SelectPreset(string profileId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? profile
                : EmptyProfile();
            var preset = previous.Presets.FirstOrDefault(value =>
                string.Equals(value.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (preset is null)
                return previous;

            var updated = previous with
            {
                WorkingSnapshot = preset.Snapshot,
                SelectedPresetName = preset.Name,
            };
            var profiles = new Dictionary<string, FarmingGuideProfileState>(document.Profiles, StringComparer.Ordinal)
            {
                [profileId] = updated,
            };
            SaveDocument(document with { Profiles = profiles });
            return updated;
        }
    }

    public void SaveFixedEquipment(FarmingGuideFixedEquipmentState fixedEquipment)
    {
        ArgumentNullException.ThrowIfNull(fixedEquipment);
        lock (_gate)
        {
            var document = LoadDocument();
            SaveDocument(document with { FixedEquipment = fixedEquipment });
        }
    }

    private FarmingGuideDocument LoadDocument()
    {
        var loaded = _store.LoadOrDefault(
            static () => FarmingGuideDocument.Empty,
            JsonOptions);
        return loaded.SchemaVersion == CurrentSchemaVersion
            ? loaded
            : FarmingGuideDocument.Empty;
    }

    private void SaveDocument(FarmingGuideDocument document) =>
        _store.Save(document with { SchemaVersion = CurrentSchemaVersion }, JsonOptions);

    private static FarmingGuideProfileState EmptyProfile() =>
        new(FarmingGuideLoadoutSnapshot.Empty, null, []);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record FarmingGuideDocument(
        int SchemaVersion,
        IReadOnlyDictionary<string, FarmingGuideProfileState> Profiles,
        FarmingGuideFixedEquipmentState FixedEquipment)
    {
        public static FarmingGuideDocument Empty { get; } = new(
            CurrentSchemaVersion,
            new Dictionary<string, FarmingGuideProfileState>(StringComparer.Ordinal),
            FarmingGuideFixedEquipmentState.Empty);
    }
}
