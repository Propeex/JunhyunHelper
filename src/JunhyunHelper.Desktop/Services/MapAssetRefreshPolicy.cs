using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Desktop.Services;

/// <summary>
/// Decides when Map presentation/layout assets must be rebuilt from their configured sources.
/// The policy deliberately lives outside the active/candidate directories so an asset swap
/// cannot accidentally erase update state.
/// </summary>
public static class MapAssetRefreshPolicy
{
    private const int StateSchemaVersion = 1;

    // Bump this whenever the artwork/layout ingestion formula changes. Existing installs
    // will then rebuild Map assets once without asking the user to delete their cache.
    public const string PipelineVersion = "legacy-tarkov-helper-map-minimap-v2-atomic-upstream";

    private const string StateFileName = "update-state.json";
    private const string RefreshRequestFileName = "refresh.requested";
    private static readonly TimeSpan MaximumRefreshAge = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static async Task<bool> NeedsRefreshAsync(
        MapAssetCacheService assets,
        GameContentCatalog content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(content);

        if (!await assets.HasUsableActiveAssetsAsync(cancellationToken))
            return true;

        var root = GetMapCacheRoot(assets);
        if (File.Exists(Path.Combine(root, RefreshRequestFileName)))
            return true;

        var state = await TryReadStateAsync(root, cancellationToken);
        if (state is null ||
            state.SchemaVersion != StateSchemaVersion ||
            !string.Equals(state.PipelineVersion, PipelineVersion, StringComparison.Ordinal))
            return true;

        if (!string.Equals(
                state.ContentFingerprint,
                ComputeContentFingerprint(content),
                StringComparison.Ordinal))
            return true;

        var age = DateTimeOffset.UtcNow - state.LastSuccessfulRefreshUtc;
        return age < TimeSpan.Zero || age >= MaximumRefreshAge;
    }

    /// <summary>
    /// Marks Map sources stale without touching the currently working active assets.
    /// Used whenever Game Content is activated so current Map IDs/floor metadata remain
    /// coupled to the user's Data Update action while legacy artwork/calibration is
    /// re-resolved as one upstream GitHub revision.
    /// </summary>
    public static void RequestRefresh(MapAssetCacheService assets, string reason)
    {
        ArgumentNullException.ThrowIfNull(assets);
        var root = GetMapCacheRoot(assets);
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, RefreshRequestFileName),
            $"{DateTimeOffset.UtcNow:O}\n{reason}");
    }

    public static async Task RecordSuccessfulRefreshAsync(
        MapAssetCacheService assets,
        GameContentCatalog content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(content);

        var root = GetMapCacheRoot(assets);
        Directory.CreateDirectory(root);
        var state = new MapAssetUpdateState(
            StateSchemaVersion,
            PipelineVersion,
            ComputeContentFingerprint(content),
            DateTimeOffset.UtcNow);

        var destination = Path.Combine(root, StateFileName);
        var temporary = destination + ".tmp";
        await File.WriteAllTextAsync(
            temporary,
            JsonSerializer.Serialize(state, JsonOptions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
        File.Move(temporary, destination, overwrite: true);

        var request = Path.Combine(root, RefreshRequestFileName);
        if (File.Exists(request))
            File.Delete(request);
    }

    public static string ComputeContentFingerprint(GameContentCatalog content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // MapReference is kept in source order because that order is part of the downloaded
        // canonical catalog. Dynamic markers are explicitly sorted so harmless source-order
        // changes do not force a rebuild.
        var payload = JsonSerializer.Serialize(
            new
            {
                maps = content.Maps,
                markers = content.MapMarkers
                    .OrderBy(marker => marker.MapId, StringComparer.Ordinal)
                    .ThenBy(marker => marker.Id, StringComparer.Ordinal)
                    .ToArray(),
            },
            JsonOptions);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static async Task<MapAssetUpdateState?> TryReadStateAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, StateFileName);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<MapAssetUpdateState>(json, JsonOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static string GetMapCacheRoot(MapAssetCacheService assets) =>
        Directory.GetParent(assets.ActiveDirectory)?.FullName
        ?? throw new InvalidOperationException("Map cache root could not be resolved.");

    private sealed record MapAssetUpdateState(
        int SchemaVersion,
        string PipelineVersion,
        string ContentFingerprint,
        DateTimeOffset LastSuccessfulRefreshUtc);
}
