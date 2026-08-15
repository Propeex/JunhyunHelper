using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;
using Microsoft.Data.Sqlite;

namespace JunhyunHelper.Infrastructure.Storage;

public sealed record StoredContentSnapshot(
    int SchemaVersion,
    GameMode GameMode,
    DateTimeOffset BuiltAt,
    GameContentCatalog Content,
    IReadOnlyList<string> Warnings);

public sealed class ContentSnapshotStore
{
    // v3-v5 remain readable as last-known-good offline snapshots. v6 separates
    // recoverable special-trader access (Lightkeeper) from monotonic quest prerequisites
    // and preserves precise upstream BTR/Ref prerequisite statuses. A successful data
    // update writes v6; older readable snapshots are never destroyed merely because the
    // app was upgraded while offline.
    public const int MinimumReadableSchemaVersion = 3;
    public const int CurrentSchemaVersion = 6;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async Task WriteNewAsync(
        string databasePath,
        GameMode gameMode,
        GameContentCatalog content,
        IReadOnlyList<string>? warnings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(content);

        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(
            connection,
            transaction,
            """
            CREATE TABLE ContentSnapshot (
                Id INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                SchemaVersion INTEGER NOT NULL,
                GameMode TEXT NOT NULL,
                BuiltAt TEXT NOT NULL,
                PayloadJson TEXT NOT NULL,
                WarningsJson TEXT NOT NULL
            );
            """,
            cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO ContentSnapshot
                    (Id, SchemaVersion, GameMode, BuiltAt, PayloadJson, WarningsJson)
                VALUES
                    (1, $schemaVersion, $gameMode, $builtAt, $payloadJson, $warningsJson);
                """;
            command.Parameters.AddWithValue("$schemaVersion", CurrentSchemaVersion);
            command.Parameters.AddWithValue("$gameMode", gameMode.ToString());
            command.Parameters.AddWithValue(
                "$builtAt",
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue(
                "$payloadJson",
                JsonSerializer.Serialize(content, JsonOptions));
            command.Parameters.AddWithValue(
                "$warningsJson",
                JsonSerializer.Serialize(warnings ?? Array.Empty<string>(), JsonOptions));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        await EnsureIntegrityAsync(connection, cancellationToken);
    }

    public async Task<StoredContentSnapshot> ReadAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Content database does not exist.", fullPath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureIntegrityAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SchemaVersion, GameMode, BuiltAt, PayloadJson, WarningsJson
            FROM ContentSnapshot
            WHERE Id = 1;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidDataException("Content database has no active snapshot row.");

        var schemaVersion = reader.GetInt32(0);
        if (schemaVersion < MinimumReadableSchemaVersion || schemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Unsupported content schema version '{schemaVersion}'. " +
                $"Readable range is '{MinimumReadableSchemaVersion}' through '{CurrentSchemaVersion}'.");
        }

        var gameModeText = reader.GetString(1);
        if (!Enum.TryParse<GameMode>(gameModeText, ignoreCase: false, out var gameMode))
            throw new InvalidDataException($"Content database has invalid game mode '{gameModeText}'.");

        if (!DateTimeOffset.TryParse(
                reader.GetString(2),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var builtAt))
        {
            throw new InvalidDataException("Content database has invalid build timestamp.");
        }

        var content = JsonSerializer.Deserialize<GameContentCatalog>(reader.GetString(3), JsonOptions)
                      ?? throw new InvalidDataException("Content payload could not be deserialized.");
        var warnings = JsonSerializer.Deserialize<string[]>(reader.GetString(4), JsonOptions)
                       ?? Array.Empty<string>();

        return new StoredContentSnapshot(
            schemaVersion,
            gameMode,
            builtAt,
            content,
            warnings);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureIntegrityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken),
            CultureInfo.InvariantCulture);

        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"SQLite integrity check failed: {result}");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
