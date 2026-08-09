using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Map;

namespace JunhyunHelper.Desktop.Services;

public sealed record FandomMapArtworkResult(
    bool Applied,
    string? Attribution,
    string? AttributionUrl,
    string? Warning,
    int MatchedMarkers,
    int InlierMarkers,
    double Residual);

/// <summary>
/// Builds a local SVG presentation wrapper from the Escape from Tarkov Wiki
/// Interactive Map background. The Wiki map uses its own 2D coordinate space,
/// so its markers are matched against canonical Tarkov marker names and an
/// affine transform is solved automatically. A background is accepted only
/// when the solved transform passes strict residual checks; otherwise callers
/// keep the already calibrated Tarkov.dev SVG fallback.
/// </summary>
public sealed class FandomMapArtworkService
{
    private const int MaxMapImageBytes = 16 * 1024 * 1024;
    private const double InlierThreshold = 0.035;
    private const double MaxAcceptedResidual = 0.022;
    private const double MaxAcceptedPointError = 0.055;
    private const int MinimumInliers = 4;

    private static readonly IReadOnlyDictionary<string, string> PageSlugs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["customs"] = "Customs",
            ["factory"] = "Factory",
            ["groundzero"] = "Ground_Zero",
            ["interchange"] = "Interchange",
            ["icebreaker"] = "Icebreaker",
            ["lighthouse"] = "Lighthouse",
            ["reserve"] = "Reserve",
            ["shoreline"] = "Shoreline",
            ["streetsoftarkov"] = "Streets_of_Tarkov",
            ["terminal"] = "Terminal",
            ["lab"] = "The_Lab",
            ["thelab"] = "The_Lab",
            ["laboratory"] = "The_Lab",
            ["labyrinth"] = "The_Labyrinth",
            ["thelabyrinth"] = "The_Labyrinth",
            ["woods"] = "Woods",
        };

    private static readonly HashSet<string> NoiseTokens = new(StringComparer.Ordinal)
    {
        "pmc", "scav", "extract", "extraction", "exit", "all", "to",
    };

    private readonly HttpClient _httpClient;

    public FandomMapArtworkService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<FandomMapArtworkResult> TryBuildAlignedSvgAsync(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        string destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(canonicalMarkers);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        // Fandom's Interactive Map background is a single 2D plane. Exploded or
        // true multi-floor layouts cannot be represented by one global affine
        // transform without making floor-specific positions misleading.
        if (layout.Floors.Count > 1)
        {
            return new FandomMapArtworkResult(
                false, null, null,
                "Wiki background skipped because this layout has multiple floor layers.",
                0, 0, double.NaN);
        }

        if (!TryResolvePageSlug(layout, out var pageSlug))
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"No Escape from Tarkov Wiki Interactive Map page is mapped for '{layout.NormalizedName}'.",
                0, 0, double.NaN);
        }

        var rawUrl = $"https://escapefromtarkov.fandom.com/wiki/Map:{pageSlug}?action=raw";
        string rawJson;
        try
        {
            rawJson = await _httpClient.GetStringAsync(rawUrl, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"Wiki map metadata could not be downloaded: {exception.Message}",
                0, 0, double.NaN);
        }

        WikiMapDefinition wiki;
        try
        {
            wiki = ParseWikiMap(rawJson);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"Wiki map metadata was not usable: {exception.Message}",
                0, 0, double.NaN);
        }

        var pairs = BuildMarkerPairs(layout, canonicalMarkers, wiki);
        if (pairs.Count < MinimumInliers)
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"Wiki background calibration found only {pairs.Count} matching stable markers; at least {MinimumInliers} are required.",
                pairs.Count, 0, double.NaN);
        }

        if (!TryFitRobustAffine(pairs, out var transform, out var inliers, out var residual, out var maxError))
        {
            return new FandomMapArtworkResult(
                false, null, null,
                "Wiki background calibration could not solve a stable affine transform.",
                pairs.Count, 0, double.NaN);
        }

        if (inliers.Count < MinimumInliers ||
            residual > MaxAcceptedResidual ||
            maxError > MaxAcceptedPointError ||
            !TransformLooksSane(transform))
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"Wiki background calibration was rejected (matches {pairs.Count}, inliers {inliers.Count}, residual {residual:F4}, max {maxError:F4}).",
                pairs.Count, inliers.Count, residual);
        }

        byte[] imageBytes;
        string mediaType;
        try
        {
            (imageBytes, mediaType) = await DownloadMapImageAsync(wiki.MapImage, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new FandomMapArtworkResult(
                false, null, null,
                $"Wiki background image could not be downloaded: {exception.Message}",
                pairs.Count, inliers.Count, residual);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await WriteSvgWrapperAsync(destination, imageBytes, mediaType, transform, cancellationToken);

        var pageUrl = $"https://escapefromtarkov.fandom.com/wiki/Map:{pageSlug}";
        return new FandomMapArtworkResult(
            true,
            "Escape from Tarkov Wiki Interactive Map · CC BY-NC-SA",
            pageUrl,
            null,
            pairs.Count,
            inliers.Count,
            residual);
    }

    private async Task<(byte[] Bytes, string MediaType)> DownloadMapImageAsync(
        string mapImage,
        CancellationToken cancellationToken)
    {
        var fileName = mapImage.StartsWith("File:", StringComparison.OrdinalIgnoreCase)
            ? mapImage[5..]
            : mapImage;
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidDataException("Wiki mapImage is empty.");

        var fileUrl = "https://escapefromtarkov.fandom.com/wiki/Special:Redirect/file/" +
                      Uri.EscapeDataString(fileName.Replace(' ', '_'));
        using var response = await _httpClient.GetAsync(
            fileUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxMapImageBytes)
            throw new InvalidDataException($"Wiki map image is larger than {MaxMapImageBytes / 1024 / 1024} MiB.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > MaxMapImageBytes)
                throw new InvalidDataException($"Wiki map image exceeded {MaxMapImageBytes / 1024 / 1024} MiB.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        var bytes = output.ToArray();
        if (bytes.Length == 0)
            throw new InvalidDataException("Wiki map image was empty.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            mediaType = GuessMediaType(fileName, bytes);
        if (mediaType is not ("image/png" or "image/jpeg" or "image/svg+xml" or "image/webp"))
            throw new InvalidDataException($"Unsupported Wiki map image type '{mediaType}'.");

        return (bytes, mediaType);
    }

    private static string GuessMediaType(string fileName, byte[] bytes)
    {
        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return "image/png";
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            return "image/svg+xml";
        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            return "image/webp";

        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";
        throw new InvalidDataException("Wiki map image type could not be determined.");
    }

    private static async Task WriteSvgWrapperAsync(
        string destination,
        byte[] imageBytes,
        string mediaType,
        AffineTransform transform,
        CancellationToken cancellationToken)
    {
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

    private static WikiMapDefinition ParseWikiMap(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Interactive Map source root is not an object.");

        var mapImage = ReadString(root, "mapImage")
                       ?? throw new InvalidDataException("Interactive Map has no mapImage.");
        var bounds = ReadBounds(root);
        var origin = ReadString(root, "origin") ?? "bottom-left";
        var coordinateOrder = ReadString(root, "coordinateOrder") ?? "xy";
        var markers = new List<WikiMarker>();
        if (root.TryGetProperty("markers", out var markerArray) && markerArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var marker in markerArray.EnumerateArray())
            {
                if (!TryReadPosition(marker, out var first, out var second))
                    continue;
                if (!marker.TryGetProperty("popup", out var popup) || popup.ValueKind != JsonValueKind.Object)
                    continue;
                var title = ReadString(popup, "title");
                if (string.IsNullOrWhiteSpace(title))
                    continue;
                markers.Add(new WikiMarker(title, first, second));
            }
        }

        return new WikiMapDefinition(mapImage, bounds, origin, coordinateOrder, markers);
    }

    private static MapBounds ReadBounds(JsonElement root)
    {
        if (!root.TryGetProperty("mapBounds", out var bounds) || bounds.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Interactive Map has no mapBounds.");
        var rows = bounds.EnumerateArray().Take(2).ToArray();
        if (rows.Length != 2 ||
            !TryReadPair(rows[0], out var minA, out var minB) ||
            !TryReadPair(rows[1], out var maxA, out var maxB))
            throw new InvalidDataException("Interactive Map mapBounds is invalid.");
        if (Math.Abs(maxA - minA) < 0.000001 || Math.Abs(maxB - minB) < 0.000001)
            throw new InvalidDataException("Interactive Map mapBounds has zero size.");
        return new MapBounds(minA, minB, maxA, maxB);
    }

    private static IReadOnlyList<CalibrationPair> BuildMarkerPairs(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
        WikiMapDefinition wiki)
    {
        var canonical = canonicalMarkers
            .Where(marker => string.Equals(marker.MapId, layout.MapId, StringComparison.Ordinal))
            .Where(marker => marker.Kind is MapMarkerKind.PmcExtract or MapMarkerKind.ScavExtract or
                MapMarkerKind.SharedExtract or MapMarkerKind.Transit or MapMarkerKind.BtrStop)
            .Select(marker => new { Key = MarkerKey(marker.Name), Marker = marker })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => CollapseCanonicalGroup(group.Select(item => item.Marker).ToArray()),
                StringComparer.Ordinal);

        var wikiMarkers = wiki.Markers
            .Select(marker => new { Key = MarkerKey(marker.Title), Marker = marker })
            .Where(item => item.Key.Length > 0)
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Marker, StringComparer.Ordinal);

        var pairs = new List<CalibrationPair>();
        foreach (var (key, marker) in canonical)
        {
            if (marker is null || !wikiMarkers.TryGetValue(key, out var wikiMarker))
                continue;
            if (!MapCoordinateTransformer.TryWorldToSurface(layout, marker.Position, 1, 1, out var surface))
                continue;
            if (!TryWikiNormalized(wiki, wikiMarker, out var u, out var v))
                continue;
            pairs.Add(new CalibrationPair(u, v, surface.X, surface.Y, key));
        }

        return pairs;
    }

    private static MapMarkerDefinition? CollapseCanonicalGroup(IReadOnlyList<MapMarkerDefinition> markers)
    {
        if (markers.Count == 0)
            return null;
        if (markers.Count == 1)
            return markers[0];

        var first = markers[0].Position;
        if (markers.All(marker => DistanceWorld(first, marker.Position) <= 8.0))
        {
            var average = new MapWorldPosition(
                markers.Average(marker => marker.Position.X),
                markers.Average(marker => marker.Position.Y),
                markers.Average(marker => marker.Position.Z));
            return markers[0] with { Position = average };
        }

        return null;
    }

    private static double DistanceWorld(MapWorldPosition first, MapWorldPosition second)
    {
        var dx = first.X - second.X;
        var dz = first.Z - second.Z;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    private static string MarkerKey(string value)
    {
        var normalized = new string(value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray());
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !NoiseTokens.Contains(token))
            .OrderBy(token => token, StringComparer.Ordinal)
            .ToArray();
        return string.Join('|', tokens);
    }

    private static bool TryWikiNormalized(
        WikiMapDefinition wiki,
        WikiMarker marker,
        out double u,
        out double v)
    {
        var x = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? marker.Second
            : marker.First;
        var y = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? marker.First
            : marker.Second;
        var minX = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? wiki.Bounds.MinB
            : wiki.Bounds.MinA;
        var maxX = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? wiki.Bounds.MaxB
            : wiki.Bounds.MaxA;
        var minY = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? wiki.Bounds.MinA
            : wiki.Bounds.MinB;
        var maxY = wiki.CoordinateOrder.Equals("yx", StringComparison.OrdinalIgnoreCase)
            ? wiki.Bounds.MaxA
            : wiki.Bounds.MaxB;

        u = (x - minX) / (maxX - minX);
        var vertical = (y - minY) / (maxY - minY);
        v = wiki.Origin.Equals("top-left", StringComparison.OrdinalIgnoreCase)
            ? vertical
            : 1.0 - vertical;
        return double.IsFinite(u) && double.IsFinite(v) && u >= -0.05 && u <= 1.05 && v >= -0.05 && v <= 1.05;
    }

    private static bool TryFitRobustAffine(
        IReadOnlyList<CalibrationPair> pairs,
        out AffineTransform transform,
        out IReadOnlyList<CalibrationPair> inliers,
        out double residual,
        out double maxError)
    {
        transform = default;
        inliers = Array.Empty<CalibrationPair>();
        residual = double.PositiveInfinity;
        maxError = double.PositiveInfinity;

        List<CalibrationPair>? bestInliers = null;
        AffineTransform bestTransform = default;
        var bestResidual = double.PositiveInfinity;
        var checkedTriples = 0;

        for (var i = 0; i < pairs.Count - 2; i++)
        for (var j = i + 1; j < pairs.Count - 1; j++)
        for (var k = j + 1; k < pairs.Count; k++)
        {
            if (++checkedTriples > 2500)
                break;
            var seed = new[] { pairs[i], pairs[j], pairs[k] };
            if (!TryFitAffine(seed, out var candidate))
                continue;
            var candidateInliers = pairs
                .Where(pair => Error(candidate, pair) <= InlierThreshold)
                .ToList();
            if (candidateInliers.Count < 3)
                continue;
            var candidateResidual = RootMeanSquare(candidate, candidateInliers);
            if (bestInliers is null ||
                candidateInliers.Count > bestInliers.Count ||
                candidateInliers.Count == bestInliers.Count && candidateResidual < bestResidual)
            {
                bestInliers = candidateInliers;
                bestTransform = candidate;
                bestResidual = candidateResidual;
            }
        }

        if (bestInliers is null || bestInliers.Count < 3)
            return false;
        if (!TryFitAffine(bestInliers, out bestTransform))
            return false;

        var finalInliers = pairs.Where(pair => Error(bestTransform, pair) <= InlierThreshold).ToList();
        if (finalInliers.Count >= 3 && TryFitAffine(finalInliers, out var refined))
            bestTransform = refined;

        transform = bestTransform;
        inliers = finalInliers;
        residual = RootMeanSquare(transform, finalInliers);
        maxError = finalInliers.Count == 0 ? double.PositiveInfinity : finalInliers.Max(pair => Error(transform, pair));
        return double.IsFinite(residual) && double.IsFinite(maxError);
    }

    private static bool TryFitAffine(IReadOnlyList<CalibrationPair> pairs, out AffineTransform transform)
    {
        transform = default;
        if (pairs.Count < 3)
            return false;

        double sUU = 0, sUV = 0, sU = 0, sVV = 0, sV = 0;
        double xU = 0, xV = 0, x1 = 0, yU = 0, yV = 0, y1 = 0;
        foreach (var pair in pairs)
        {
            sUU += pair.U * pair.U;
            sUV += pair.U * pair.V;
            sU += pair.U;
            sVV += pair.V * pair.V;
            sV += pair.V;
            xU += pair.U * pair.SurfaceX;
            xV += pair.V * pair.SurfaceX;
            x1 += pair.SurfaceX;
            yU += pair.U * pair.SurfaceY;
            yV += pair.V * pair.SurfaceY;
            y1 += pair.SurfaceY;
        }

        var matrix = new[,]
        {
            { sUU, sUV, sU },
            { sUV, sVV, sV },
            { sU, sV, (double)pairs.Count },
        };
        if (!TrySolve3x3(matrix, new[] { xU, xV, x1 }, out var x) ||
            !TrySolve3x3(matrix, new[] { yU, yV, y1 }, out var y))
            return false;

        transform = new AffineTransform(x[0], x[1], x[2], y[0], y[1], y[2]);
        return new[] { transform.A, transform.B, transform.C, transform.D, transform.E, transform.F }.All(double.IsFinite);
    }

    private static bool TrySolve3x3(double[,] source, double[] right, out double[] solution)
    {
        solution = new double[3];
        var augmented = new double[3, 4];
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
                augmented[row, col] = source[row, col];
            augmented[row, 3] = right[row];
        }

        for (var pivot = 0; pivot < 3; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < 3; row++)
            {
                if (Math.Abs(augmented[row, pivot]) > Math.Abs(augmented[best, pivot]))
                    best = row;
            }
            if (Math.Abs(augmented[best, pivot]) < 1e-9)
                return false;
            if (best != pivot)
            {
                for (var col = pivot; col < 4; col++)
                    (augmented[pivot, col], augmented[best, col]) = (augmented[best, col], augmented[pivot, col]);
            }

            var divisor = augmented[pivot, pivot];
            for (var col = pivot; col < 4; col++)
                augmented[pivot, col] /= divisor;
            for (var row = 0; row < 3; row++)
            {
                if (row == pivot)
                    continue;
                var factor = augmented[row, pivot];
                for (var col = pivot; col < 4; col++)
                    augmented[row, col] -= factor * augmented[pivot, col];
            }
        }

        for (var row = 0; row < 3; row++)
            solution[row] = augmented[row, 3];
        return solution.All(double.IsFinite);
    }

    private static double Error(AffineTransform transform, CalibrationPair pair)
    {
        var x = transform.A * pair.U + transform.B * pair.V + transform.C;
        var y = transform.D * pair.U + transform.E * pair.V + transform.F;
        var dx = x - pair.SurfaceX;
        var dy = y - pair.SurfaceY;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double RootMeanSquare(AffineTransform transform, IReadOnlyList<CalibrationPair> pairs)
    {
        if (pairs.Count == 0)
            return double.PositiveInfinity;
        return Math.Sqrt(pairs.Average(pair =>
        {
            var error = Error(transform, pair);
            return error * error;
        }));
    }

    private static bool TransformLooksSane(AffineTransform transform)
    {
        var determinant = transform.A * transform.E - transform.B * transform.D;
        if (!double.IsFinite(determinant) || Math.Abs(determinant) < 0.05 || Math.Abs(determinant) > 20)
            return false;

        var corners = new[]
        {
            Apply(transform, 0, 0), Apply(transform, 1, 0),
            Apply(transform, 0, 1), Apply(transform, 1, 1),
        };
        return corners.All(point =>
            point.X >= -0.75 && point.X <= 1.75 &&
            point.Y >= -0.75 && point.Y <= 1.75);
    }

    private static (double X, double Y) Apply(AffineTransform transform, double u, double v) =>
        (transform.A * u + transform.B * v + transform.C,
         transform.D * u + transform.E * v + transform.F);

    private static bool TryResolvePageSlug(MapLayoutDefinition layout, out string slug)
    {
        var candidates = new[] { layout.NormalizedName, layout.Key }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeMapKey);
        foreach (var candidate in candidates)
        {
            if (PageSlugs.TryGetValue(candidate, out slug!))
                return true;
        }
        slug = string.Empty;
        return false;
    }

    private static string NormalizeMapKey(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryReadPosition(JsonElement marker, out double first, out double second)
    {
        first = second = 0;
        return marker.TryGetProperty("position", out var position) && TryReadPair(position, out first, out second);
    }

    private static bool TryReadPair(JsonElement element, out double first, out double second)
    {
        first = second = 0;
        if (element.ValueKind != JsonValueKind.Array)
            return false;
        var values = element.EnumerateArray().Take(2).ToArray();
        if (values.Length != 2 ||
            values[0].ValueKind != JsonValueKind.Number || !values[0].TryGetDouble(out first) ||
            values[1].ValueKind != JsonValueKind.Number || !values[1].TryGetDouble(out second))
            return false;
        return double.IsFinite(first) && double.IsFinite(second);
    }

    private sealed record WikiMapDefinition(
        string MapImage,
        MapBounds Bounds,
        string Origin,
        string CoordinateOrder,
        IReadOnlyList<WikiMarker> Markers);

    private sealed record WikiMarker(string Title, double First, double Second);
    private sealed record MapBounds(double MinA, double MinB, double MaxA, double MaxB);
    private sealed record CalibrationPair(double U, double V, double SurfaceX, double SurfaceY, string Key);
    private readonly record struct AffineTransform(double A, double B, double C, double D, double E, double F);
}
