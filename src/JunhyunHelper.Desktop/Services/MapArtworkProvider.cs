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

/// <summary>
/// Compatibility entry point used by the existing Map asset service. Ground Zero first uses
/// the floor-aware RE3MR implementation. The older revision-aware provider and the machine-readable
/// Escape from Tarkov Wiki artwork remain fallbacks before the calibrated schematic is used.
/// </summary>
public sealed class WikiMapArtworkProvider : IMapArtworkProvider
{
    private readonly GroundZeroRe3mrArtworkProviderV2 _groundZeroFloorAware;
    private readonly Re3mrMapArtworkProvider _re3mr;
    private readonly FandomMapArtworkService _wiki;

    public WikiMapArtworkProvider(HttpClient httpClient)
    {
        _groundZeroFloorAware = new GroundZeroRe3mrArtworkProviderV2(httpClient);
        _re3mr = new Re3mrMapArtworkProvider(httpClient);
        _wiki = new FandomMapArtworkService(httpClient);
    }

    public string ProviderId => "detailed-map-chain";

    public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var floorAware = await _groundZeroFloorAware.TryBuildAlignedSvgAsync(
            layout,
            canonicalMarkers,
            destination,
            cancellationToken);
        if (floorAware.Applied)
            return floorAware;

        DeleteCandidate(destination);
        var re3mr = await _re3mr.TryBuildAlignedSvgAsync(
            layout,
            canonicalMarkers,
            destination,
            cancellationToken);
        if (re3mr.Applied)
            return re3mr;

        DeleteCandidate(destination);
        var wiki = await _wiki.TryBuildAlignedSvgAsync(
            layout,
            canonicalMarkers,
            destination,
            cancellationToken);

        return new MapArtworkProviderResult(
            wiki.Applied,
            wiki.Applied ? "eft-wiki" : null,
            null,
            wiki.Attribution,
            wiki.AttributionUrl,
            wiki.Applied
                ? null
                : JoinWarnings(floorAware.Warning, re3mr.Warning, wiki.Warning));
    }

    private static void DeleteCandidate(string destination)
    {
        if (File.Exists(destination))
            File.Delete(destination);
    }

    private static string? JoinWarnings(params string?[] values)
    {
        var warnings = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return warnings.Length == 0 ? null : string.Join(" | ", warnings);
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
