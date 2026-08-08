using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using Microsoft.Data.Sqlite;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed class UserProfileStore
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databasePath;

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

        var payload = JsonSerializer.Serialize(ProfileDocument.From(profile), JsonOptions);

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
        command.Parameters.AddWithValue("$profileId", profile.ProfileId);
        command.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
        command.Parameters.AddWithValue("$payload", payload);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GameProfileSnapshot?> LoadAsync(
        string profileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT schema_version, payload_json
            FROM profiles
            WHERE profile_id = $profileId;
            """;
        command.Parameters.AddWithValue("$profileId", profileId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return Deserialize(
            reader.GetInt32(0),
            reader.GetString(1));
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
            result.Add(Deserialize(
                reader.GetInt32(0),
                reader.GetString(1)));
        }

        return result;
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

    private SqliteConnection OpenConnection() =>
        new($"Data Source={_databasePath};Mode=ReadWriteCreate;Cache=Private");

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

        foreach (var (itemId, quantity) in profile.Inventory)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity.Fir < 0 || quantity.NonFir < 0)
                throw new InvalidDataException("Inventory contains an invalid item quantity.");
        }

        if (profile.CompletedQuestIds.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Completed quest ids cannot contain empty values.");
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
        public Dictionary<string, int> HideoutLevels { get; init; } =
            new(StringComparer.Ordinal);
        public Dictionary<string, InventoryQuantity> Inventory { get; init; } =
            new(StringComparer.Ordinal);

        public static ProfileDocument From(GameProfileSnapshot snapshot) =>
            new()
            {
                ProfileId = snapshot.ProfileId,
                GameMode = snapshot.GameMode,
                Level = snapshot.Level,
                Faction = snapshot.Faction,
                EditionId = snapshot.EditionId,
                PrestigeLevel = snapshot.PrestigeLevel,
                Traders = new Dictionary<string, TraderProgress>(snapshot.Traders, StringComparer.Ordinal),
                CompletedQuestIds = snapshot.CompletedQuestIds.Order(StringComparer.Ordinal).ToArray(),
                HideoutLevels = new Dictionary<string, int>(snapshot.HideoutLevels, StringComparer.Ordinal),
                Inventory = new Dictionary<string, InventoryQuantity>(snapshot.Inventory, StringComparer.Ordinal),
            };

        public GameProfileSnapshot ToSnapshot() =>
            new()
            {
                ProfileId = ProfileId,
                GameMode = GameMode,
                Level = Level,
                Faction = Faction,
                EditionId = EditionId,
                PrestigeLevel = PrestigeLevel,
                Traders = new Dictionary<string, TraderProgress>(Traders, StringComparer.Ordinal),
                CompletedQuestIds = new HashSet<string>(CompletedQuestIds, StringComparer.Ordinal),
                HideoutLevels = new Dictionary<string, int>(HideoutLevels, StringComparer.Ordinal),
                Inventory = new Dictionary<string, InventoryQuantity>(Inventory, StringComparer.Ordinal),
            };
    }
}
