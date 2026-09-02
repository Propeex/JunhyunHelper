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
            return NormalizeFixedEquipment(LoadDocument().FixedEquipment);
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
                Locks = NormalizeLocks(locks ?? previous.Locks),
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
            var effectiveLocks = NormalizeLocks(locks ?? previous.Locks);
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
                Locks = NormalizeLocks(preset.Locks),
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
            SaveDocument(document with { FixedEquipment = NormalizeFixedEquipment(fixedEquipment) });
        }
    }

    private FarmingGuideDocument LoadDocument()
    {
        var loaded = _store.LoadOrDefault(
            static () => FarmingGuideDocument.Empty,
            JsonOptions);
        if (loaded.SchemaVersion is not (1 or 2 or CurrentSchemaVersion))
            return FarmingGuideDocument.Empty;

        var profiles = new Dictionary<string, FarmingGuideProfileState>(StringComparer.Ordinal);
        foreach (var entry in loaded.Profiles ?? new Dictionary<string, FarmingGuideProfileState>(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
                continue;
            profiles[entry.Key] = NormalizeProfile(entry.Value);
        }

        return loaded with
        {
            SchemaVersion = CurrentSchemaVersion,
            Profiles = profiles,
            FixedEquipment = NormalizeFixedEquipment(loaded.FixedEquipment),
        };
    }

    private void SaveDocument(FarmingGuideDocument document) =>
        _store.Save(
            document with
            {
                SchemaVersion = CurrentSchemaVersion,
                FixedEquipment = NormalizeFixedEquipment(document.FixedEquipment),
            },
            JsonOptions);

    private static FarmingGuideProfileState NormalizeProfile(FarmingGuideProfileState? profile)
    {
        if (profile is null)
            return EmptyProfile();

        var presets = (profile.Presets ?? Array.Empty<FarmingGuidePreset>())
            .OfType<FarmingGuidePreset>()
            .Where(static preset => !string.IsNullOrWhiteSpace(preset.Name))
            .Select(preset => preset with
            {
                Snapshot = NormalizeSnapshot(preset.Snapshot),
                Locks = NormalizeLocks(preset.Locks),
            })
            .ToArray();
        var selectedPresetName = presets.Any(preset =>
            string.Equals(preset.Name, profile.SelectedPresetName, StringComparison.OrdinalIgnoreCase))
            ? profile.SelectedPresetName
            : null;

        return profile with
        {
            WorkingSnapshot = NormalizeSnapshot(profile.WorkingSnapshot),
            SelectedPresetName = selectedPresetName,
            Locks = NormalizeLocks(profile.Locks),
            WeightSettings = (profile.WeightSettings ?? FarmingGuideWeightSettings.Default).Normalized(),
            Presets = presets,
        };
    }

    private static FarmingGuideLoadoutSnapshot NormalizeSnapshot(FarmingGuideLoadoutSnapshot? snapshot)
    {
        if (snapshot is null)
            return FarmingGuideLoadoutSnapshot.Empty;

        var equipment = new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>();
        foreach (var entry in snapshot.Equipment ?? new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>())
        {
            if (NormalizeItemState(entry.Value) is { } normalized)
                equipment[entry.Key] = normalized;
        }

        var storedItems = new List<FarmingGuideStoredItemState>();
        foreach (var item in (snapshot.StoredItems ?? Array.Empty<FarmingGuideStoredItemState>()).OfType<FarmingGuideStoredItemState>())
        {
            var normalizedItem = NormalizeItemState(item.Item);
            if (normalizedItem is null || string.IsNullOrWhiteSpace(item.InstanceId))
                continue;

            storedItems.Add(item with
            {
                Item = normalizedItem,
                Quantity = Math.Max(1, item.Quantity),
            });
        }

        return snapshot with
        {
            Equipment = equipment,
            Rig = NormalizeItemState(snapshot.Rig),
            Backpack = NormalizeItemState(snapshot.Backpack),
            SecureContainer = NormalizeItemState(snapshot.SecureContainer),
            StoredItems = storedItems,
        };
    }

    private static FarmingGuideItemState? NormalizeItemState(FarmingGuideItemState? state)
    {
        if (state is null || string.IsNullOrWhiteSpace(state.ItemId))
            return null;

        var attachments = new Dictionary<string, FarmingGuideItemState?>(StringComparer.Ordinal);
        foreach (var entry in state.Attachments ?? new Dictionary<string, FarmingGuideItemState?>())
        {
            if (!string.IsNullOrWhiteSpace(entry.Key))
                attachments[entry.Key] = NormalizeItemState(entry.Value);
        }

        var armorPlates = new Dictionary<string, FarmingGuideItemState?>(StringComparer.Ordinal);
        foreach (var entry in state.ArmorPlates ?? new Dictionary<string, FarmingGuideItemState?>())
        {
            if (!string.IsNullOrWhiteSpace(entry.Key))
                armorPlates[entry.Key] = NormalizeItemState(entry.Value);
        }

        return state with
        {
            Attachments = attachments,
            ArmorPlates = armorPlates,
        };
    }

    private static FarmingGuideLockState NormalizeLocks(FarmingGuideLockState? locks)
    {
        if (locks is null)
            return FarmingGuideLockState.Empty;

        return new FarmingGuideLockState(
            (locks.EquipmentSlots ?? Array.Empty<FarmingGuideEquipmentSlot>()).Distinct().ToArray(),
            (locks.Carriers ?? Array.Empty<FarmingGuideStorageKind>()).Distinct().ToArray(),
            (locks.ItemInstanceIds ?? Array.Empty<string>())
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            (locks.ReservedCells ?? Array.Empty<FarmingGuideLockedCell>())
                .OfType<FarmingGuideLockedCell>()
                .Distinct()
                .ToArray());
    }

    private static FarmingGuideFixedEquipmentState NormalizeFixedEquipment(FarmingGuideFixedEquipmentState? fixedEquipment) =>
        new(NormalizeItemState(fixedEquipment?.Melee), null);

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
