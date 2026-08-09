using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Map;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Services;

/// <summary>
/// Ground Zero detailed-artwork provider that keeps multi-floor behavior intact.
/// RE3MR is used only for the default ground presentation; non-default floors are
/// embedded from the current online Tarkov.dev schematic SVG and remain selectable.
/// Gameplay/world coordinates continue to come from the canonical Map pipeline.
/// </summary>
public sealed partial class GroundZeroRe3mrArtworkProviderV2 : IMapArtworkProvider
{
    private const int MaxImageBytes = 40 * 1024 * 1024;
    private const int MaxSvgBytes = 64 * 1024 * 1024;
    private const int MinimumMatchedAnchors = 4;
    private const double MaxResidual = 0.06;
    private const double MaxPointError = 0.10;
    private const double CyanAnchorRadius = 0.03;
    private const string PageUrl = "https://reemr.se/ground-zero/";
    private const string FallbackImageUrl = "https://www.re3mr.com/maps/Groundzero/GroundZero.png";

    private static readonly ArtworkAnchor[] BaselineAnchors =
    [
        new("Emercom Checkpoint", 0.600, 0.137),
        new("Scav Checkpoint (Co-Op)", 0.758, 0.137,
            ["Scav Checkpoint Co-Op", "Scav Checkpoint Coop"]),
        new("Mira Ave", 0.506, 0.357),
        new("Police Cordon V-Ex", 0.824, 0.477,
            ["Police Cordon VEx", "Police Cordon Vehicle Extract"]),
        new("Nakatani Basement Stairs", 0.807, 0.849),
    ];

    private readonly HttpClient _httpClient;

    public GroundZeroRe3mrArtworkProviderV2(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string ProviderId => "re3mr-groundzero-floor-aware";

    public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(canonicalMarkers);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        if (!IsGroundZero(layout))
            return Rejected($"Floor-aware RE3MR provider does not handle '{layout.NormalizedName}'.");

        try
        {
            var pageHtml = await DownloadTextAsync(PageUrl, cancellationToken);
            var version = ExtractVersion(pageHtml);
            var imageUrl = FindPreferredImageUrl(pageHtml) ?? FallbackImageUrl;
            var image = await DownloadBytesAsync(imageUrl, MaxImageBytes, cancellationToken);

            using (var bitmap = SKBitmap.Decode(image.Bytes))
            {
                if (bitmap is null || bitmap.Width < 500 || bitmap.Height < 500)
                    return Rejected("RE3MR Ground Zero image could not be decoded or is unexpectedly small.");
                if (!VisualAnchorsLookValid(bitmap, BaselineAnchors))
                    return Rejected("RE3MR Ground Zero visual extraction anchors no longer match the known layout.");
            }

            var pairs = BuildCalibrationPairs(layout, canonicalMarkers, BaselineAnchors);
            if (pairs.Count < MinimumMatchedAnchors)
            {
                return Rejected(
                    $"Only {pairs.Count} RE3MR extraction anchors matched current canonical markers; " +
                    $"{MinimumMatchedAnchors} are required.");
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
                return Rejected(
                    $"RE3MR Ground Zero calibration rejected (matches {pairs.Count}, " +
                    $"residual {residual:F4}, max {maxError:F4}).");
            }

            byte[]? schematicSvg = null;
            if (layout.Floors.Count > 1)
            {
                schematicSvg = (await DownloadBytesAsync(
                    layout.SvgUrl,
                    MaxSvgBytes,
                    cancellationToken)).Bytes;
                ValidateSvgBytes(schematicSvg);
            }

            await WriteCompositeSvgAsync(
                destination,
                layout,
                image.Bytes,
                NormalizeImageMediaType(image.MediaType, image.Bytes),
                transform,
                schematicSvg,
                cancellationToken);

            var hash = Convert.ToHexString(SHA256.HashData(image.Bytes)).ToLowerInvariant();
            return new MapArtworkProviderResult(
                true,
                ProviderId,
                $"{version}:{hash[..12]}",
                "RE3MR · CC BY-NC-SA",
                PageUrl,
                layout.Floors.Count > 1
                    ? "기본층은 RE3MR 상세 지도, 다른 층은 최신 온라인 schematic 레이어를 사용합니다."
                    : null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (File.Exists(destination))
                File.Delete(destination);
            return Rejected($"Floor-aware RE3MR refresh failed: {exception.Message}");
        }
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
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(url);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long contentLength && contentLength > maximumBytes)
            throw new InvalidDataException($"Downloaded Map asset is too large: {url}");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > maximumBytes)
                throw new InvalidDataException($"Downloaded Map asset exceeded the maximum size: {url}");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return new DownloadedBytes(output.ToArray(), response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("JunhyunHelper/0.1 (+non-commercial EFT companion)");
        return request;
    }

    private static string ExtractVersion(string html)
    {
        var match = VersionRegex().Match(WebUtility.HtmlDecode(html));
        return match.Success ? match.Groups["version"].Value : "unknown";
    }

    private static string? FindPreferredImageUrl(string html)
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
        var dx = left.X - right.X;
        var dz = left.Z - right.Z;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    private static bool VisualAnchorsLookValid(
        SKBitmap bitmap,
        IReadOnlyList<ArtworkAnchor> anchors)
    {
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

    private static async Task WriteCompositeSvgAsync(
        string destination,
        MapLayoutDefinition layout,
        byte[] re3mrImage,
        string re3mrMediaType,
        MapArtworkAffineTransform transform,
        byte[]? schematicSvg,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        XNamespace svg = "http://www.w3.org/2000/svg";
        XNamespace xlink = "http://www.w3.org/1999/xlink";

        var defaultFloor = layout.Floors.FirstOrDefault(floor => floor.IsDefault)
                           ?? layout.Floors.FirstOrDefault();
        var defaultLayer = defaultFloor?.SvgLayer ?? "Ground_Level";
        var root = new XElement(
            svg + "svg",
            new XAttribute(XNamespace.Xmlns + "xlink", xlink),
            new XAttribute("viewBox", "0 0 1 1"),
            new XAttribute("preserveAspectRatio", "none"),
            new XAttribute("data-junhyun-helper-artwork", "re3mr-floor-aware-v2"));

        var imageDataUri = $"data:{re3mrMediaType};base64,{Convert.ToBase64String(re3mrImage)}";
        var matrix = string.Create(
            CultureInfo.InvariantCulture,
            $"matrix({transform.A:R} {transform.D:R} {transform.B:R} {transform.E:R} {transform.C:R} {transform.F:R})");
        root.Add(
            new XElement(
                svg + "g",
                new XAttribute("id", defaultLayer),
                new XElement(
                    svg + "image",
                    new XAttribute("x", "0"),
                    new XAttribute("y", "0"),
                    new XAttribute("width", "1"),
                    new XAttribute("height", "1"),
                    new XAttribute("preserveAspectRatio", "none"),
                    new XAttribute("transform", matrix),
                    new XAttribute("href", imageDataUri),
                    new XAttribute(xlink + "href", imageDataUri))));

        if (layout.Floors.Count > 1)
        {
            if (schematicSvg is null)
                throw new InvalidDataException("Multi-floor Ground Zero requires a schematic floor source.");

            foreach (var floor in layout.Floors.Where(floor => !ReferenceEquals(floor, defaultFloor)))
            {
                if (string.IsNullOrWhiteSpace(floor.SvgLayer))
                    continue;
                var rendered = RenderSchematicFloor(schematicSvg, layout, floor);
                var floorDataUri = $"data:image/svg+xml;base64,{Convert.ToBase64String(rendered)}";
                root.Add(
                    new XElement(
                        svg + "g",
                        new XAttribute("id", floor.SvgLayer),
                        new XAttribute("style", "display:none"),
                        new XElement(
                            svg + "image",
                            new XAttribute("x", "0"),
                            new XAttribute("y", "0"),
                            new XAttribute("width", "1"),
                            new XAttribute("height", "1"),
                            new XAttribute("preserveAspectRatio", "none"),
                            new XAttribute("href", floorDataUri),
                            new XAttribute(xlink + "href", floorDataUri))));
            }
        }

        var document = new XDocument(root);
        await File.WriteAllTextAsync(
            destination,
            document.ToString(SaveOptions.DisableFormatting),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);
    }

    private static byte[] RenderSchematicFloor(
        byte[] sourceBytes,
        MapLayoutDefinition layout,
        MapFloorDefinition selectedFloor)
    {
        using var input = new MemoryStream(sourceBytes, writable: false);
        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);
        var knownLayers = layout.Floors
            .Select(floor => floor.SvgLayer)
            .Where(layer => !string.IsNullOrWhiteSpace(layer))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "g"))
        {
            var id = element.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(id) || !knownLayers.Contains(id))
                continue;
            SetDisplay(element, string.Equals(id, selectedFloor.SvgLayer, StringComparison.Ordinal));
        }

        var text = document.ToString(SaveOptions.DisableFormatting);
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text);
    }

    private static void SetDisplay(XElement element, bool visible)
    {
        var style = element.Attribute("style")?.Value ?? string.Empty;
        var parts = style
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("display", StringComparison.OrdinalIgnoreCase))
            .ToList();
        parts.Add($"display:{(visible ? "block" : "none")}");
        element.SetAttributeValue("style", string.Join(';', parts));
    }

    private static void ValidateSvgBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var document = XDocument.Load(stream, LoadOptions.None);
        if (document.Root is null || !string.Equals(document.Root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Online floor source is not an SVG document.");
    }

    private static string NormalizeImageMediaType(string? mediaType, byte[] bytes)
    {
        if (mediaType is "image/png" or "image/jpeg")
            return mediaType;
        return IsPng(bytes) ? "image/png" : "image/jpeg";
    }

    private static bool IsPng(byte[] bytes) =>
        bytes.Length >= 8 &&
        bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

    private static bool IsGroundZero(MapLayoutDefinition layout) =>
        NormalizeName(layout.NormalizedName) == "groundzero" ||
        NormalizeName(layout.Key) == "groundzero";

    private static string NormalizeName(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static MapArtworkProviderResult Rejected(string warning) =>
        new(false, null, null, null, null, warning);

    [GeneratedRegex(@"\bVersion\s+(?<version>[0-9]+(?:\.[0-9]+)*(?:[A-Za-z]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    [GeneratedRegex("href=[\\\"'](?<url>https?://[^\\\"']+\\.png(?:\\?[^\\\"']*)?)[\\\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ImageUrlRegex();

    private sealed record ArtworkAnchor(
        string Name,
        double U,
        double V,
        IReadOnlyList<string>? Aliases = null)
    {
        public IEnumerable<string> AllNames => new[] { Name }.Concat(Aliases ?? []);
    }

    private sealed record DownloadedBytes(byte[] Bytes, string? MediaType);
}
