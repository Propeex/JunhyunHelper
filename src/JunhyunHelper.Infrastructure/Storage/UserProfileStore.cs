using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using Microsoft.Data.Sqlite;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed class UserProfileStore
{
    // The SQLite table schema remains v1. New JSON properties are optional and defaulted,
    // so existing user.db rows remain readable without a destructive migration.
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databasePath;
    private readonly ConcurrentDictionary<string, GameProfileSnapshot> _memoryCache =
        new(StringComparer.Ordinal);

    public UserProfileStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = Path.GetFullPath(databasePath);
    }

    public async Task SaveAsync(
        GameProfileSnapshot profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Validate(profile);
        await EnsureSchemaAsync(cancellationToken);

        // The document is also the normalization boundary used by persisted reads
        // (for example an unspecified prestige value becomes zero). Build it once and
        // cache its canonical snapshot only after the SQLite transaction succeeds so
        // cached and cold-start behavior are byte-for-byte semantically equivalent.
        var document = ProfileDocument.From(profile);
        var payload = JsonSerializer.Serialize(document, JsonOptions);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO profiles(profile_id, schema_version, payload_json, updated_at_utc)
            VALUES ($profileId, $schemaVersion, $payload, $updatedAt)
            ON CONFLICT(profile_id) DO UPDATE SET
                schema_version = excluded.schema_version,
                payload_json = excluded.payload_json,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$profileId", document.ProfileId);
        command.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
        command.Parameters.AddWithValue("$payload", payload);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);

        _memoryCache[document.ProfileId] = document.ToSnapshot();
    }

    public async Task<GameProfileSnapshot?> LoadAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        var normalizedProfileId = profileId.Trim();
        if (_memoryCache.TryGetValue(normalizedProfileId, out var cached))
            return cached;

        await EnsureSchemaAsync(cancellationToken);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT schema_version, payload_json
            FROM profiles
            WHERE profile_id = $profileId;
            """;
        command.Parameters.AddWithValue("$profileId", normalizedProfileId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var snapshot = Deserialize(
            reader.GetInt32(0),
            reader.GetString(1));
        _memoryCache[snapshot.ProfileId] = snapshot;
        return snapshot;
    }

    public async Task<IReadOnlyList<GameProfileSnapshot>> LoadAllAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        var result = new List<GameProfileSnapshot>();
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT schema_version, payload_json
            FROM profiles
            ORDER BY profile_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var snapshot = Deserialize(
                reader.GetInt32(0),
                reader.GetString(1));
            result.Add(snapshot);
            _memoryCache[snapshot.ProfileId] = snapshot;
        }

        // Profiles deleted outside this store are not a supported live-edit workflow,
        // but LoadAllAsync is the full authoritative read used at startup/profile reload.
        // Remove stale in-process entries so the cache exactly mirrors user.db afterwards.
        var activeIds = result.Select(profile => profile.ProfileId).ToHashSet(StringComparer.Ordinal);
        foreach (var cachedId in _memoryCache.Keys.Where(id => !activeIds.Contains(id)).ToArray())
            _memoryCache.TryRemove(cachedId, out _);

        return result;
    }

    public async Task<bool> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        var normalizedProfileId = profileId.Trim();
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM profiles
            WHERE profile_id = $profileId;
            """;
        command.Parameters.AddWithValue("$profileId", normalizedProfileId);
        var deleted = await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        if (deleted)
            _memoryCache.TryRemove(normalizedProfileId, out _);
        return deleted;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS profiles(
                profile_id TEXT PRIMARY KEY,
                schema_version INTEGER NOT NULL,
                payload_json TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection OpenConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString();

        return new SqliteConnection(connectionString);
    }

    private static GameProfileSnapshot Deserialize(int schemaVersion, string payload)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported user profile schema version '{schemaVersion}'.");
        }

        var document = JsonSerializer.Deserialize<ProfileDocument>(payload, JsonOptions)
            ?? throw new InvalidDataException("User profile payload is empty or invalid.");
        var snapshot = document.ToSnapshot();
        Validate(snapshot);
        return snapshot;
    }

    private static void Validate(GameProfileSnapshot profile)
    {
        if (string.IsNullOrWhiteSpace(profile.ProfileId))
            throw new InvalidDataException("Profile id is required.");
        if (profile.Level < 0)
            throw new InvalidDataException("Profile level cannot be negative.");
        if (profile.PrestigeLevel is < 0)
            throw new InvalidDataException("Prestige level cannot be negative.");

        foreach (var (traderId, progress) in profile.Traders)
        {
            if (string.IsNullOrWhiteSpace(traderId))
                throw new InvalidDataException("Trader id cannot be empty.");
            if (progress.LoyaltyLevel < 0)
                throw new InvalidDataException($"Trader '{traderId}' loyalty level cannot be negative.");
        }

        foreach (var (stationId, level) in profile.HideoutLevels)
        {
            if (string.IsNullOrWhiteSpace(stationId) || level < 0)
                throw new InvalidDataException("Hideout progress contains an invalid station level.");
        }

        ValidateInventory(profile.Inventory, "Inventory");
        ValidateConsumptions(profile.QuestConsumptions, "Quest consumption");
        ValidateConsumptions(profile.HideoutUpgradeConsumptions, "Hideout consumption");

        if (profile.CompletedQuestIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Completed quest ids cannot contain empty values.");
        if (profile.FailedQuestIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Failed quest ids cannot contain empty values.");
        if (profile.CompletedQuestIds.Overlaps(profile.FailedQuestIds))
            throw new InvalidDataException("A quest cannot be both completed and explicitly failed.");
    }

    private static void ValidateInventory(
        IReadOnlyDictionary<string, InventoryQuantity> inventory,
        string label)
    {
        foreach (var (itemId, quantity) in inventory)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity.Fir < 0 || quantity.NonFir < 0)
                throw new InvalidDataException($"{label} contains an invalid item quantity.");
        }
    }

    private static void ValidateConsumptions(
        IReadOnlyDictionary<string, InventoryConsumption> consumptions,
        string label)
    {
        foreach (var (key, consumption) in consumptions)
        {
            if (string.IsNullOrWhiteSpace(key) || consumption is null)
                throw new InvalidDataException($"{label} contains an invalid key or record.");
            ValidateInventory(consumption.Items, label);
        }
    }

    private sealed record ProfileDocument
    {
        public required string ProfileId { get; init; }
        public required GameMode GameMode { get; init; }
        public required int Level { get; init; }
        public required PmcFaction Faction { get; init; }
        public string? EditionId { get; init; }
        public int? PrestigeLevel { get; init; }
        public Dictionary<string, TraderProgress> Traders { get; init; } =
            new(StringComparer.Ordinal);
        public string[] CompletedQuestIds { get; init; } = [];
        public string[] FailedQuestIds { get; init; } = [];
        public Dictionary<string, int> HideoutLevels { get; init; } =
            new(StringComparer.Ordinal);
        public Dictionary<string, InventoryQuantity> Inventory { get; init; } =
            new(StringComparer.Ordinal);
        public Dictionary<string, InventoryConsumption> QuestConsumptions { get; init; } =
            new(StringComparer.Ordinal);
        public Dictionary<string, InventoryConsumption> HideoutUpgradeConsumptions { get; init; } =
            new(StringComparer.Ordinal);

        public static ProfileDocument From(GameProfileSnapshot snapshot) =>
            new()
            {
                ProfileId = snapshot.ProfileId,
                GameMode = snapshot.GameMode,
                Level = snapshot.Level,
                Faction = snapshot.Faction,
                EditionId = snapshot.EditionId,
                PrestigeLevel = snapshot.PrestigeLevel ?? 0,
                Traders = new Dictionary<string, TraderProgress>(snapshot.Traders, StringComparer.Ordinal),
                CompletedQuestIds = snapshot.CompletedQuestIds.Order(StringComparer.Ordinal).ToArray(),
                FailedQuestIds = snapshot.FailedQuestIds.Order(StringComparer.Ordinal).ToArray(),
                HideoutLevels = new Dictionary<string, int>(snapshot.HideoutLevels, StringComparer.Ordinal),
                Inventory = new Dictionary<string, InventoryQuantity>(snapshot.Inventory, StringComparer.Ordinal),
                QuestConsumptions = CopyConsumptions(snapshot.QuestConsumptions),
                HideoutUpgradeConsumptions = CopyConsumptions(snapshot.HideoutUpgradeConsumptions),
            };

        public GameProfileSnapshot ToSnapshot() =>
            new()
            {
                ProfileId = ProfileId,
                GameMode = GameMode,
                Level = Level,
                Faction = Faction,
                EditionId = EditionId,
                PrestigeLevel = PrestigeLevel ?? 0,
                Traders = new Dictionary<string, TraderProgress>(Traders, StringComparer.Ordinal),
                CompletedQuestIds = new HashSet<string>(CompletedQuestIds, StringComparer.Ordinal),
                FailedQuestIds = new HashSet<string>(FailedQuestIds, StringComparer.Ordinal),
                HideoutLevels = new Dictionary<string, int>(HideoutLevels, StringComparer.Ordinal),
                Inventory = new Dictionary<string, InventoryQuantity>(Inventory, StringComparer.Ordinal),
                QuestConsumptions = CopyConsumptions(QuestConsumptions),
                HideoutUpgradeConsumptions = CopyConsumptions(HideoutUpgradeConsumptions),
            };

        private static Dictionary<string, InventoryConsumption> CopyConsumptions(
            IReadOnlyDictionary<string, InventoryConsumption> source) =>
            source.ToDictionary(
                pair => pair.Key,
                pair => new InventoryConsumption(
                    new Dictionary<string, InventoryQuantity>(pair.Value.Items, StringComparer.Ordinal)),
                StringComparer.Ordinal);
    }
}