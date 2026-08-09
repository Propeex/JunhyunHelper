using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Map;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Services;

/// <summary>
/// Detailed RE3MR presentation provider. Gameplay/world coordinates remain owned by the
/// canonical Tarkov data pipeline. This provider only proves how the current artwork maps onto
/// that coordinate surface, and rejects an update when it cannot prove the relationship.
/// </summary>
public sealed partial class Re3mrMapArtworkProvider : IMapArtworkProvider
{
    private const int MaxImageBytes = 40 * 1024 * 1024;
    private const int MinimumMatchedAnchors = 4;
    private const double MaxResidual = 0.055;
    private const double MaxPointError = 0.09;
    private const double CyanAnchorRadius = 0.028;
    private const string ProviderStateVersion = "re3mr-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly IReadOnlyDictionary<string, MapConfig> Configs =
        new Dictionary<string, MapConfig>(StringComparer.Ordinal)
        {
            ["groundzero"] = new(
                "groundzero",
                "https://reemr.se/ground-zero/",
                "https://www.re3mr.com/maps/Groundzero/GroundZero.png",
                [
                    new ArtworkAnchor("Emercom Checkpoint", 0.600, 0.137),
                    new ArtworkAnchor(
                        "Scav Checkpoint (Co-Op)",
                        0.758,
                        0.137,
                        ["Scav Checkpoint Co-Op", "Scav Checkpoint Coop"]),
                    new ArtworkAnchor("Mira Ave", 0.506, 0.357),
                    new ArtworkAnchor(
                        "Police Cordon V-Ex",
                        0.824,
                        0.477,
                        ["Police Cordon VEx", "Police Cordon Vehicle Extract"]),
                    new ArtworkAnchor("Nakatani Basement Stairs", 0.807, 0.849),
                ]),
        };

    private readonly HttpClient _httpClient;

    public Re3mrMapArtworkProvider(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string ProviderId => "re3mr";

    public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(canonicalMarkers);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        if (!TryResolveConfig(layout, out var config))
            return Rejected($"No RE3MR artwork configuration exists for '{layout.NormalizedName}'.");
        if (layout.Floors.Count > 1)
            return Rejected("RE3MR single-plane artwork is not enabled for a multi-floor layout yet.");

        var storage = ResolveStorage(destination, config.Key);
        try
        {
            var source = await DownloadSourceAsync(config, cancellationToken);
            var previous = await TryLoadPreviousStateAsync(storage, cancellationToken);
            var anchors = await ResolveCurrentAnchorsAsync(
                config,
                source,
                previous,
                storage,
                destination,
                cancellationToken);
            if (anchors is null)
            {
                return await ReusePreviousOrRejectAsync(
                    config,
                    storage,
                    destination,
                    "RE3MR artwork revision could not be registered safely.",
                    cancellationToken);
            }

            var pairs = BuildCalibrationPairs(layout, canonicalMarkers, anchors.Anchors);
            if (pairs.Count < MinimumMatchedAnchors)
            {
                return await ReusePreviousOrRejectAsync(
                    config,
                    storage,
                    destination,
                    $"Only {pairs.Count} named RE3MR anchors matched current canonical markers; {MinimumMatchedAnchors} are required.",
                    cancellationToken);
            }

            if (!MapArtworkAffineCalibration.TryFit(
                    pairs,
                    out var transform,
                    out var residual,
                    out var maxError) ||
                residual > MaxResidual ||
                maxError > MaxPointError ||
                !MapArtworkAffineCalibration.LooksSane(transform))
            {
                return await ReusePreviousOrRejectAsync(
                    config,
                    storage,
                    destination,
                    $"RE3MR calibration rejected (matches {pairs.Count}, residual {residual:F4}, max {maxError:F4}).",
                    cancellationToken);
            }

            await WriteSvgWrapperAsync(
                destination,
                source.ImageBytes,
                source.MediaType,
                transform,
                cancellationToken);
            await WriteCandidateStateAsync(
                storage,
                new ArtworkState(
                    ProviderStateVersion,
                    source.PageVersion,
                    source.ImageUrl,
                    source.Sha256,
                    source.Width,
                    source.Height,
                    anchors.Anchors,
                    residual,
                    anchors.RegistrationScore,
                    DateTimeOffset.UtcNow),
                source.ImageBytes,
                cancellationToken);

            return new MapArtworkProviderResult(
                true,
                ProviderId,
                $"{source.PageVersion}:{source.Sha256[..12]}",
                "RE3MR · CC BY-NC-SA",
                config.PageUrl,
                anchors.RegistrationScore is null
                    ? null
                    : $"RE3MR revision registered automatically (score {anchors.RegistrationScore.Value:F3}).");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return await ReusePreviousOrRejectAsync(
                config,
                storage,
                destination,
                $"RE3MR refresh failed: {exception.Message}",
                cancellationToken);
        }
    }

    private async Task<ResolvedAnchors?> ResolveCurrentAnchorsAsync(
        MapConfig config,
        DownloadedSource source,
        PreviousState? previous,
        ProviderStorage storage,
        string destination,
        CancellationToken cancellationToken)
    {
        if (previous is not null &&
            string.Equals(previous.State.SourceSha256, source.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedAnchors(previous.State.Anchors, previous.State.RegistrationScore);
        }

        if (VisualAnchorsLookValid(source.ImageBytes, config.BaselineAnchors))
            return new ResolvedAnchors(config.BaselineAnchors, null);

        if (previous is null)
            return null;

        var region = RegistrationRegion(previous.State.Anchors);
        if (!MapArtworkImageRegistration.TryRegister(
                previous.ImageBytes,
                source.ImageBytes,
                region,
                out var revisionTransform,
                out var score))
            return null;

        var registered = previous.State.Anchors
            .Select(anchor => TransformAnchor(anchor, revisionTransform))
            .ToArray();
        return VisualAnchorsLookValid(source.ImageBytes, registered)
            ? new ResolvedAnchors(registered, score)
            : null;
    }

    private async Task<DownloadedSource> DownloadSourceAsync(
        MapConfig config,
        CancellationToken cancellationToken)
    {
        var html = await DownloadTextAsync(config.PageUrl, cancellationToken);
        var versionMatch = VersionRegex().Match(WebUtility.HtmlDecode(html));
        var version = versionMatch.Success
            ? versionMatch.Groups["version"].Value
            : "unknown";
        var imageUrl = FindPreferredImageUrl(html, config) ?? config.FallbackImageUrl;
        var response = await DownloadBytesAsync(imageUrl, cancellationToken);

        using var bitmap = SKBitmap.Decode(response.Bytes)
                           ?? throw new InvalidDataException("RE3MR artwork could not be decoded.");
        if (bitmap.Width < 500 || bitmap.Height < 500)
            throw new InvalidDataException("RE3MR artwork dimensions are unexpectedly small.");

        var mediaType = response.MediaType;
        if (mediaType is not ("image/png" or "image/jpeg"))
            mediaType = IsPng(response.Bytes) ? "image/png" : "image/jpeg";

        return new DownloadedSource(
            version,
            imageUrl,
            Convert.ToHexString(SHA256.HashData(response.Bytes)).ToLowerInvariant(),
            response.Bytes,
            mediaType,
            bitmap.Width,
            bitmap.Height);
    }

    private async Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task<DownloadedBytes> DownloadBytesAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxImageBytes)
            throw new InvalidDataException("RE3MR artwork is larger than the allowed size.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > MaxImageBytes)
                throw new InvalidDataException("RE3MR artwork exceeded the allowed size.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return new DownloadedBytes(
            output.ToArray(),
            response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("JunhyunHelper/0.1 (+non-commercial EFT companion)");
        return request;
    }

    private static string? FindPreferredImageUrl(string html, MapConfig config)
    {
        foreach (Match match in ImageUrlRegex().Matches(WebUtility.HtmlDecode(html)))
        {
            var value = match.Groups["url"].Value;
            if (value.Contains("GroundZero", StringComparison.OrdinalIgnoreCase) &&
                value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return value;
        }
        return null;
    }

    private static IReadOnlyList<MapArtworkCalibrationPair> BuildCalibrationPairs(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        IReadOnlyList<ArtworkAnchor> anchors)
    {
        var relevant = canonicalMarkers
            .Where(marker => string.Equals(marker.MapId, layout.MapId, StringComparison.Ordinal))
            .Where(marker => marker.Kind is MapMarkerKind.PmcExtract or MapMarkerKind.ScavExtract or
                MapMarkerKind.SharedExtract or MapMarkerKind.Transit)
            .ToArray();
        var pairs = new List<MapArtworkCalibrationPair>();

        foreach (var anchor in anchors)
        {
            var names = anchor.AllNames.Select(NormalizeName).ToHashSet(StringComparer.Ordinal);
            var matches = relevant
                .Where(marker => names.Contains(NormalizeName(marker.Name)))
                .ToArray();
            var marker = CollapseMarkers(matches);
            if (marker is null ||
                !MapCoordinateTransformer.TryWorldToSurface(layout, marker.Position, 1, 1, out var point))
                continue;

            pairs.Add(new MapArtworkCalibrationPair(anchor.U, anchor.V, point.X, point.Y));
        }

        return pairs;
    }

    private static MapMarkerDefinition? CollapseMarkers(IReadOnlyList<MapMarkerDefinition> markers)
    {
        if (markers.Count == 0)
            return null;
        if (markers.Count == 1)
            return markers[0];

        var first = markers[0].Position;
        if (markers.Any(marker => Distance(first, marker.Position) > 12))
            return null;

        return markers[0] with
        {
            Position = new MapWorldPosition(
                markers.Average(marker => marker.Position.X),
                markers.Average(marker => marker.Position.Y),
                markers.Average(marker => marker.Position.Z)),
        };
    }

    private static double Distance(MapWorldPosition left, MapWorldPosition right)
    {
        var x = left.X - right.X;
        var z = left.Z - right.Z;
        return Math.Sqrt(x * x + z * z);
    }

    private static bool VisualAnchorsLookValid(
        byte[] imageBytes,
        IReadOnlyList<ArtworkAnchor> anchors)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap is null)
            return false;

        var valid = anchors.Count(anchor => PatchContainsCyanMarker(bitmap, anchor.U, anchor.V));
        return valid >= Math.Min(MinimumMatchedAnchors, anchors.Count);
    }

    private static bool PatchContainsCyanMarker(SKBitmap bitmap, double u, double v)
    {
        var centerX = (int)Math.Round(u * (bitmap.Width - 1));
        var centerY = (int)Math.Round(v * (bitmap.Height - 1));
        var radiusX = Math.Max(5, (int)Math.Round(bitmap.Width * CyanAnchorRadius));
        var radiusY = Math.Max(5, (int)Math.Round(bitmap.Height * CyanAnchorRadius));
        var cyan = 0;
        var sampled = 0;

        for (var y = Math.Max(0, centerY - radiusY); y <= Math.Min(bitmap.Height - 1, centerY + radiusY); y += 2)
        for (var x = Math.Max(0, centerX - radiusX); x <= Math.Min(bitmap.Width - 1, centerX + radiusX); x += 2)
        {
            var color = bitmap.GetPixel(x, y);
            sampled++;
            if (color.Green >= 125 &&
                color.Blue >= 95 &&
                color.Green >= color.Red * 1.35 &&
                color.Blue >= color.Red * 1.15)
                cyan++;
        }

        return sampled > 0 && cyan >= Math.Max(4, sampled / 220);
    }

    private static async Task WriteSvgWrapperAsync(
        string destination,
        byte[] imageBytes,
        string mediaType,
        MapArtworkAffineTransform transform,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        XNamespace svg = "http://www.w3.org/2000/svg";
        XNamespace xlink = "http://www.w3.org/1999/xlink";
        var dataUri = $"data:{mediaType};base64,{Convert.ToBase64String(imageBytes)}";
        var matrix = string.Create(
            CultureInfo.InvariantCulture,
            $"matrix({transform.A:R} {transform.D:R} {transform.B:R} {transform.E:R} {transform.C:R} {transform.F:R})");
        var document = new XDocument(
            new XElement(svg + "svg",
                new XAttribute(XNamespace.Xmlns + "xlink", xlink),
                new XAttribute("viewBox", "0 0 1 1"),
                new XAttribute("preserveAspectRatio", "none"),
                new XElement(svg + "image",
                    new XAttribute("x", "0"),
                    new XAttribute("y", "0"),
                    new XAttribute("width", "1"),
                    new XAttribute("height", "1"),
                    new XAttribute("preserveAspectRatio", "none"),
                    new XAttribute("transform", matrix),
                    new XAttribute("href", dataUri),
                    new XAttribute(xlink + "href", dataUri))));

        await File.WriteAllTextAsync(
            destination,
            document.ToString(SaveOptions.DisableFormatting),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
    }

    private static async Task WriteCandidateStateAsync(
        ProviderStorage storage,
        ArtworkState state,
        byte[] sourceImage,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(storage.CandidateProviderDirectory);
        await File.WriteAllBytesAsync(storage.CandidateSourceImage, sourceImage, cancellationToken);
        await File.WriteAllTextAsync(
            storage.CandidateState,
            JsonSerializer.Serialize(state, JsonOptions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
    }

    private async Task<PreviousState?> TryLoadPreviousStateAsync(
        ProviderStorage storage,
        CancellationToken cancellationToken)
    {
        foreach (var directory in storage.PreviousProviderDirectories)
        {
            var statePath = Path.Combine(directory, "state.json");
            var imagePath = Path.Combine(directory, "source.img");
            if (!File.Exists(statePath) || !File.Exists(imagePath))
                continue;

            try
            {
                var json = await File.ReadAllTextAsync(statePath, cancellationToken);
                var state = JsonSerializer.Deserialize<ArtworkState>(json, JsonOptions);
                if (state is null ||
                    !string.Equals(state.StateVersion, ProviderStateVersion, StringComparison.Ordinal))
                    continue;
                return new PreviousState(
                    directory,
                    state,
                    await File.ReadAllBytesAsync(imagePath, cancellationToken));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Broken provider metadata is ignored; the next provider remains available.
            }
        }

        return null;
    }

    private async Task<MapArtworkProviderResult> ReusePreviousOrRejectAsync(
        MapConfig config,
        ProviderStorage storage,
        string destination,
        string reason,
        CancellationToken cancellationToken)
    {
        var previous = await TryLoadPreviousStateAsync(storage, cancellationToken);
        if (previous is null)
            return Rejected(reason);

        for (var index = 0; index < storage.PreviousSvgFiles.Count; index++)
        {
            var previousSvg = storage.PreviousSvgFiles[index];
            if (!File.Exists(previousSvg))
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(previousSvg, destination, overwrite: true);
            CopyProviderDirectory(previous.Directory, storage.CandidateProviderDirectory);
            return new MapArtworkProviderResult(
                true,
                ProviderId,
                $"{previous.State.PageVersion}:{ShortHash(previous.State.SourceSha256)}",
                "RE3MR · CC BY-NC-SA",
                config.PageUrl,
                $"{reason} Previous validated RE3MR artwork was kept.");
        }

        return Rejected(reason);
    }

    private static void CopyProviderDirectory(string source, string destination)
    {
        if (Directory.Exists(destination))
            Directory.Delete(destination, recursive: true);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }

    private static ProviderStorage ResolveStorage(string destination, string mapKey)
    {
        var svgDirectory = Directory.GetParent(destination)
                           ?? throw new InvalidOperationException("Map SVG destination has no parent.");
        var candidateDirectory = svgDirectory.Parent
                                 ?? throw new InvalidOperationException("Map candidate directory could not be resolved.");
        var root = candidateDirectory.Parent
                   ?? throw new InvalidOperationException("Map cache root could not be resolved.");
        var safeKey = Sanitize(mapKey);
        var relativeSvg = Path.GetRelativePath(candidateDirectory.FullName, destination);

        return new ProviderStorage(
            Path.Combine(candidateDirectory.FullName, "providers", "re3mr", safeKey),
            Path.Combine(candidateDirectory.FullName, "providers", "re3mr", safeKey, "source.img"),
            Path.Combine(candidateDirectory.FullName, "providers", "re3mr", safeKey, "state.json"),
            [
                Path.Combine(root.FullName, "active", "providers", "re3mr", safeKey),
                Path.Combine(root.FullName, "previous", "providers", "re3mr", safeKey),
            ],
            [
                Path.Combine(root.FullName, "active", relativeSvg),
                Path.Combine(root.FullName, "previous", relativeSvg),
            ]);
    }

    private static MapArtworkRegistrationRegion RegistrationRegion(
        IReadOnlyList<ArtworkAnchor> anchors) =>
        new(
            anchors.Min(anchor => anchor.U),
            anchors.Min(anchor => anchor.V),
            anchors.Max(anchor => anchor.U),
            anchors.Max(anchor => anchor.V));

    private static ArtworkAnchor TransformAnchor(
        ArtworkAnchor anchor,
        MapArtworkImageTransform transform)
    {
        var mapped = transform.Apply(anchor.U, anchor.V);
        return anchor with { U = mapped.U, V = mapped.V };
    }

    private static bool TryResolveConfig(MapLayoutDefinition layout, out MapConfig config)
    {
        foreach (var value in new[] { layout.NormalizedName, layout.Key })
        {
            if (Configs.TryGetValue(NormalizeName(value), out config!))
                return true;
        }
        config = null!;
        return false;
    }

    private static string NormalizeName(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static string ShortHash(string hash) =>
        hash[..Math.Min(12, hash.Length)];

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static MapArtworkProviderResult Rejected(string warning) =>
        new(false, null, null, null, null, warning);

    [GeneratedRegex(@"\bVersion\s+(?<version>[0-9]+(?:\.[0-9]+)*(?:[A-Za-z]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    [GeneratedRegex("href=[\\\"'](?<url>https?://[^\\\"']+\\.png(?:\\?[^\\\"']*)?)[\\\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ImageUrlRegex();

    private sealed record MapConfig(
        string Key,
        string PageUrl,
        string FallbackImageUrl,
        IReadOnlyList<ArtworkAnchor> BaselineAnchors);

    private sealed record ArtworkAnchor(
        string Name,
        double U,
        double V,
        IReadOnlyList<string>? Aliases = null)
    {
        public IEnumerable<string> AllNames =>
            new[] { Name }.Concat(Aliases ?? []);
    }

    private sealed record DownloadedBytes(byte[] Bytes, string? MediaType);

    private sealed record DownloadedSource(
        string PageVersion,
        string ImageUrl,
        string Sha256,
        byte[] ImageBytes,
        string MediaType,
        int Width,
        int Height);

    private sealed record ResolvedAnchors(
        IReadOnlyList<ArtworkAnchor> Anchors,
        double? RegistrationScore);

    private sealed record ArtworkState(
        string StateVersion,
        string PageVersion,
        string ImageUrl,
        string SourceSha256,
        int Width,
        int Height,
        IReadOnlyList<ArtworkAnchor> Anchors,
        double CalibrationResidual,
        double? RegistrationScore,
        DateTimeOffset ValidatedAtUtc);

    private sealed record PreviousState(
        string Directory,
        ArtworkState State,
        byte[] ImageBytes);

    private sealed record ProviderStorage(
        string CandidateProviderDirectory,
        string CandidateSourceImage,
        string CandidateState,
        IReadOnlyList<string> PreviousProviderDirectories,
        IReadOnlyList<string> PreviousSvgFiles);
}
