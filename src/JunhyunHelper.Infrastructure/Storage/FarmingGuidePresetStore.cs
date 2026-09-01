using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.FarmingGuide;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed record FarmingGuideProfileState(
    FarmingGuideLoadoutSnapshot WorkingSnapshot,
    string? SelectedPresetName,
    IReadOnlyList<FarmingGuidePreset> Presets,
    FarmingGuideLockState? Locks = null,
    FarmingGuideWeightSettings? WeightSettings = null);

public sealed record FarmingGuideFixedEquipmentState(
    FarmingGuideItemState? Melee,
    FarmingGuideItemState? Dogtag)
{
    public static FarmingGuideFixedEquipmentState Empty { get; } = new(null, null);

    public FarmingGuideFixedEquipmentState WithoutLegacyDogtag() => this with { Dogtag = null };
}

public sealed class FarmingGuidePresetStore
{
    private const int CurrentSchemaVersion = 3;
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
                ? NormalizeProfile(profile)
                : EmptyProfile();
        }
    }

    public FarmingGuideFixedEquipmentState LoadFixedEquipment()
    {
        lock (_gate)
            return LoadDocument().FixedEquipment.WithoutLegacyDogtag();
    }

    public void SaveWorking(
        string profileId,
        FarmingGuideLoadoutSnapshot snapshot,
        string? selectedPresetName,
        FarmingGuideLockState? locks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? NormalizeProfile(profile)
                : EmptyProfile();
            var profiles = CopyProfiles(document.Profiles);
            profiles[profileId] = previous with
            {
                WorkingSnapshot = NormalizeSnapshot(snapshot),
                SelectedPresetName = selectedPresetName,
                Locks = (locks ?? previous.Locks ?? FarmingGuideLockState.Empty).CopyNormalized(),
            };
            SaveDocument(document with { Profiles = profiles });
        }
    }

    public void SaveWeightSettings(string profileId, FarmingGuideWeightSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(settings);
        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? NormalizeProfile(profile)
                : EmptyProfile();
            var profiles = CopyProfiles(document.Profiles);
            profiles[profileId] = previous with { WeightSettings = settings.Normalized() };
            SaveDocument(document with { Profiles = profiles });
        }
    }

    public FarmingGuideProfileState SavePreset(
        string profileId,
        string name,
        FarmingGuideLoadoutSnapshot snapshot,
        FarmingGuideLockState? locks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(snapshot);

        var normalizedName = name.Trim();
        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? NormalizeProfile(profile)
                : EmptyProfile();
            var effectiveLocks = (locks ?? previous.Locks ?? FarmingGuideLockState.Empty).CopyNormalized();
            var normalizedSnapshot = NormalizeSnapshot(snapshot);
            var presets = previous.Presets
                .Where(preset => !string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
                .Append(new FarmingGuidePreset(normalizedName, normalizedSnapshot, DateTimeOffset.UtcNow, effectiveLocks))
                .OrderBy(preset => preset.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var updated = previous with
            {
                WorkingSnapshot = normalizedSnapshot,
                SelectedPresetName = normalizedName,
                Presets = presets,
                Locks = effectiveLocks,
            };
            var profiles = CopyProfiles(document.Profiles);
            profiles[profileId] = updated;
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
                ? NormalizeProfile(profile)
                : EmptyProfile();
            var preset = previous.Presets.FirstOrDefault(value =>
                string.Equals(value.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (preset is null)
                return previous;

            var updated = previous with
            {
                WorkingSnapshot = NormalizeSnapshot(preset.Snapshot),
                SelectedPresetName = preset.Name,
                Locks = (preset.Locks ?? FarmingGuideLockState.Empty).CopyNormalized(),
            };
            var profiles = CopyProfiles(document.Profiles);
            profiles[profileId] = updated;
            SaveDocument(document with { Profiles = profiles });
            return updated;
        }
    }

    public FarmingGuideProfileState DeletePreset(string profileId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        lock (_gate)
        {
            var document = LoadDocument();
            var previous = document.Profiles.TryGetValue(profileId, out var profile)
                ? NormalizeProfile(profile)
                : EmptyProfile();
            var presets = previous.Presets
                .Where(preset => !string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (presets.Length == previous.Presets.Count)
                return previous;

            var selectedPresetName = string.Equals(
                previous.SelectedPresetName,
                normalizedName,
                StringComparison.OrdinalIgnoreCase)
                ? null
                : previous.SelectedPresetName;
            var updated = previous with
            {
                SelectedPresetName = selectedPresetName,
                Presets = presets,
            };
            var profiles = CopyProfiles(document.Profiles);
            profiles[profileId] = updated;
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
            SaveDocument(document with { FixedEquipment = fixedEquipment.WithoutLegacyDogtag() });
        }
    }

    private FarmingGuideDocument LoadDocument()
    {
        var loaded = _store.LoadOrDefault(
            static () => FarmingGuideDocument.Empty,
            JsonOptions);
        if (loaded.SchemaVersion is not (1 or 2 or CurrentSchemaVersion))
            return FarmingGuideDocument.Empty;

        var profiles = loaded.Profiles.ToDictionary(
            entry => entry.Key,
            entry => NormalizeProfile(entry.Value),
            StringComparer.Ordinal);
        return loaded with
        {
            SchemaVersion = CurrentSchemaVersion,
            Profiles = profiles,
            FixedEquipment = loaded.FixedEquipment.WithoutLegacyDogtag(),
        };
    }

    private void SaveDocument(FarmingGuideDocument document) =>
        _store.Save(
            document with
            {
                SchemaVersion = CurrentSchemaVersion,
                FixedEquipment = document.FixedEquipment.WithoutLegacyDogtag(),
            },
            JsonOptions);

    private static FarmingGuideProfileState NormalizeProfile(FarmingGuideProfileState profile) =>
        profile with
        {
            WorkingSnapshot = NormalizeSnapshot(profile.WorkingSnapshot),
            Locks = (profile.Locks ?? FarmingGuideLockState.Empty).CopyNormalized(),
            WeightSettings = (profile.WeightSettings ?? FarmingGuideWeightSettings.Default).Normalized(),
            Presets = profile.Presets
                .Select(preset => preset with
                {
                    Snapshot = NormalizeSnapshot(preset.Snapshot),
                    Locks = (preset.Locks ?? FarmingGuideLockState.Empty).CopyNormalized(),
                })
                .ToArray(),
        };

    private static FarmingGuideLoadoutSnapshot NormalizeSnapshot(FarmingGuideLoadoutSnapshot snapshot) =>
        snapshot with
        {
            StoredItems = snapshot.StoredItems
                .Select(item => item with { Quantity = Math.Max(1, item.Quantity) })
                .ToArray(),
        };

    private static Dictionary<string, FarmingGuideProfileState> CopyProfiles(
        IReadOnlyDictionary<string, FarmingGuideProfileState> source) =>
        source.ToDictionary(
            entry => entry.Key,
            entry => entry.Value,
            StringComparer.Ordinal);

    private static FarmingGuideProfileState EmptyProfile() =>
        new(
            FarmingGuideLoadoutSnapshot.Empty,
            null,
            [],
            FarmingGuideLockState.Empty,
            FarmingGuideWeightSettings.Default);

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
