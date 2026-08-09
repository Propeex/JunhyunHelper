using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Services;

public sealed record MapArtworkProviderResult(
    bool Applied,
    string? ProviderId,
    string? SourceRevision,
    string? Attribution,
    string? AttributionUrl,
    string? Warning);

/// <summary>
/// Presentation artwork is intentionally independent from the canonical world-coordinate
/// source. A provider must output an SVG already aligned to the normalized Map surface.
/// Download success alone is never sufficient: providers are responsible for proving that
/// their artwork can safely share the canonical overlay coordinate system.
/// </summary>
public interface IMapArtworkProvider
{
    string ProviderId { get; }

    Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default);
}

public sealed class WikiMapArtworkProvider : IMapArtworkProvider
{
    private readonly FandomMapArtworkService _service;

    public WikiMapArtworkProvider(HttpClient httpClient)
    {
        _service = new FandomMapArtworkService(httpClient);
    }

    public string ProviderId => "eft-wiki";

    public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.TryBuildAlignedSvgAsync(
            layout,
            canonicalMarkers,
            destination,
            cancellationToken);

        return new MapArtworkProviderResult(
            result.Applied,
            result.Applied ? ProviderId : null,
            null,
            result.Attribution,
            result.AttributionUrl,
            result.Warning);
    }
}

/// <summary>
/// Tries presentation providers in product-priority order. Every provider is isolated:
/// a rejected/partial candidate is deleted before the next provider runs. If every detailed
/// artwork provider rejects the source, MapAssetCacheService keeps the calibrated schematic
/// SVG fallback rather than displaying an unproven image.
/// </summary>
public sealed class MapArtworkProviderPipeline
{
    private readonly IReadOnlyList<IMapArtworkProvider> _providers;

    public MapArtworkProviderPipeline(IEnumerable<IMapArtworkProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToArray();
    }

    public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteCandidate(destination);

            try
            {
                var result = await provider.TryBuildAlignedSvgAsync(
                    layout,
                    canonicalMarkers,
                    destination,
                    cancellationToken);
                if (result.Applied)
                    return result;

                if (!string.IsNullOrWhiteSpace(result.Warning))
                    warnings.Add($"{provider.ProviderId}: {result.Warning}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                warnings.Add($"{provider.ProviderId}: {exception.Message}");
            }
        }

        DeleteCandidate(destination);
        return new MapArtworkProviderResult(
            false,
            null,
            null,
            null,
            null,
            warnings.Count == 0
                ? "No detailed artwork provider accepted this Map."
                : string.Join(" | ", warnings));
    }

    private static void DeleteCandidate(string destination)
    {
        if (File.Exists(destination))
            File.Delete(destination);
    }
}
