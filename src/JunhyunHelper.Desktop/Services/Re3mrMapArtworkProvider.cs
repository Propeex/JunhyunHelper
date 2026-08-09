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
/// Detailed RE3MR presentation provider. The provider does not own gameplay coordinates.
/// It aligns named visual extraction anchors to the current canonical world markers and keeps
/// anchor positions updateable across artwork revisions by image registration.
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

    private static readonly IReadOnlyDictionary<string, Re3mrMapConfig> Configs =
        new Dictionary<string, Re3mrMapConfig>(StringComparer.Ordinal)
        {
            ["groundzero"] = new(
                "groundzero",
                "https://reemr.se/ground-zero/",
                "https://www.re3mr.com/maps/Groundzero/GroundZero.png",
                "0.3C",
                [
                    new Re3mrAnchor("Emercom Checkpoint", 0.600, 0.137),
                    new Re3mrAnchor("Scav Checkpoint (Co-Op)", 0.758, 0.137,
                        ["Scav Checkpoint Co-Op", "Scav Checkpoint Coop"]),
                    new Re3mrAnchor("Mira Ave", 0.506, 0.357),
                    new Re3mrAnchor("Police Cordon V-Ex", 0.824, 0.477,
                        ["Police Cordon VEx", "Police Cordon Vehicle Extract"]),
                    new Re3mrAnchor("Nakatani Basement Stairs", 0.807, 0.849),
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
            return Rejected($"No RE3MR detailed artwork configuration exists for '{layout.NormalizedName}'.");
        if (layout.Floors.Count > 1)
            return Rejected("RE3MR single-plane artwork is not used for a multi-floor layout yet.");

        var storage = ResolveStorage(destination, config.Key);
        try
        {
            var source = await DownloadSourceAsync(config, cancellationToken);
            var anchors = config.BaselineAnchors.ToArray();
            double? registrationScore = null;

            var previous = await TryLoadPreviousStateAsync(storage, cancellationToken);
            if (previous is not null &&
                string.Equals(previous.State.SourceSha256, source.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                anchors = previous.State.Anchors.ToArray();
            }
            else if (VisualAnchorsLookValid(source.ImageBytes, anchors))
            {
                // The named extraction icons are still at the baseline normalized positions.
                // Local text/loot/building edits can therefore be accepted without changing
                // the gameplay transform, even when the page version/hash changed.
            }
            else if (previous is not null &&
                     MapArtworkImageRegistration.TryRegister(
                         previous.ImageBytes,
                         source.ImageBytes,
                         RegistrationRegion(previous.State.Anchors),
                         out var revisionTransform,
                         out var score))
            {
                var registered = previous.State.Anchors
                    .Select(anchor => TransformAnchor(anchor, revisionTransform))
                    .ToArray();
                if (!VisualAnchorsLookValid(source.ImageBytes, registered))
                    return await ReusePreviousOrRejectAsync(
                        storage,
                        destination,
                        "RE3MR revision registration found a transform, but named visual anchors no longer validate.",
                        cancellationToken);

                anchors = registered;
                registrationScore = score;
            }
            else
            {
                return await ReusePreviousOrRejectAsync(
                    storage,
                    destination,
                    "RE3MR artwork changed and could not be safely registered against the previous validated revision.",
                    cancellationToken);
            }

            var pairs = BuildCalibrationPairs(layout, canonicalMarkers, anchors);
            if (pairs.Count < MinimumMatchedAnchors)
            {
                return await ReusePreviousOrRejectAsync(
                    storage,
                    destination,
                    $"Only {pairs.Count} named RE3MR anchors matched current canonical Map markers; {MinimumMatchedAnchors} are required.",
                    cancellationToken);
            }

            if (!TryFitAffine(pairs, out var transform, out var residual, out var maxError) ||
                residual > MaxResidual ||
                maxError > MaxPointError ||
                !TransformLooksSane(transform))
            {
                return await ReusePreviousOrRejectAsync(
                    storage,
                    destination,
                    $"RE3MR calibration was rejected (matches {pairs.Count}, residual {residual:F4}, max {maxError:F4}).",
                    cancellationToken);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await WriteSvgWrapperAsync(
                destination,
                source.ImageBytes,
                source.MediaType,
                transform,
                cancellationToken);

            Directory.CreateDirectory(storage.CandidateProviderDirectory);
            await File.WriteAllBytesAsync(
                storage.CandidateSourceImage,
                source.ImageBytes,
                cancellationToken);
            var state = new Re3mrArtworkState(
                ProviderStateVersion,
                source.PageVersion,
                source.ImageUrl,
                source.Sha256,
                source.Width,
                source.Height,
                anchors,
                residual,
                registrationScore,
                DateTimeOffset.UtcNow);
            await File.WriteAllTextAsync(
                storage.CandidateState,
                JsonSerializer.Serialize(state, JsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken);

            return new MapArtworkProviderResult(
                true,
                ProviderId,
                $"{source.PageVersion}:{source.Sha256[..12]}",
                "RE3MR · CC BY-NC-SA",
                config.PageUrl,
                registrationScore is null
                    ? null
                    : $"RE3MR artwork revision registered automatically (score {registrationScore.Value:F3}).");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return await ReusePreviousOrRejectAsync(
                storage,
                destination,
                $"RE3MR source refresh failed: {exception.Message}",
                cancellationToken);
        }
    }

    private async Task<DownloadedSource> DownloadSourceAsync(
        Re3mrMapConfig config,
        CancellationToken cancellationToken)
    {
        using var pageRequest = new HttpRequestMessage(HttpMethod.Get, config.PageUrl);
        pageRequest.Headers.UserAgent.ParseAdd("JunhyunHelper/0.1 (+non-commercial EFT companion)");
        using var pageResponse = await _httpClient.SendAsync(
            pageRequest,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken);
        pageResponse.EnsureSuccessStatusCode();
        var html = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
        var version = VersionRegex().Match(WebUtility.HtmlDecode(html)).Groups["version"].Value;
        if (string.IsNullOrWhiteSpace(version))
            version = "unknown";

        var imageUrl = FindPreferredImageUrl(html, config) ?? config.FallbackImageUrl;
        using var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        imageRequest.Headers.UserAgent.ParseAdd("JunhyunHelper/0.1 (+non-commercial EFT companion)");
        using var response = await _httpClient.SendAsync(
            imageRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxImageBytes)
            throw new InvalidDataException("RE3MR image is larger than the allowed Map artwork size.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > MaxImageBytes)
                throw new InvalidDataException("RE3MR image exceeded the allowed Map artwork size.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        var bytes = output.ToArray();
        using var bitmap = SKBitmap.Decode(bytes)
                           ?? throw new InvalidDataException("RE3MR artwork could not be decoded as an image.");
        if (bitmap.Width < 500 || bitmap.Height < 500)
            throw new InvalidDataException("RE3MR artwork dimensions are unexpectedly small.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not ("image/png" or "image/jpeg"))
            mediaType = bytes.Length >= 8 &&
                        bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
                ? "image/png"
                : "image/jpeg";

        return new DownloadedSource(
            version,
            imageUrl,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            bytes,
            mediaType,
            bitmap.Width,
            bitmap.Height);
    }

    private static string? FindPreferredImageUrl(string html, Re3mrMapConfig config)
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

    private static IReadOnlyList<CalibrationPair> BuildCalibrationPairs(
        MapLayoutDefinition layout,
        IReadOnlyList<MapMarkerDefinition> markers,
        IReadOnlyList<Re3mrAnchor> anchors)
    {
        var relevant = markers
            .Where(marker => string.Equals(marker.MapId, layout.MapId, StringComparison.Ordinal))
            .Where(marker => marker.Kind is MapMarkerKind.PmcExtract or MapMarkerKind.ScavExtract or
                MapMarkerKind.SharedExtract or MapMarkerKind.Transit)
            .ToArray();

        var result = new List<CalibrationPair>();
        foreach (var anchor in anchors)
        {
            var keys = anchor.AllNames.Select(NormalizeName).ToHashSet(StringComparer.Ordinal);
            var matches = relevant
                .Where(marker => keys.Contains(NormalizeName(marker.Name)))
                .ToArray();
            if (matches.Length == 0)
                continue;

            var marker = CollapseMarkers(matches);
            if (marker is null ||
                !MapCoordinateTransformer.TryWorldToSurface(layout, marker.Position, 1, 1, out var point))
                continue;
            result.Add(new CalibrationPair(anchor.U, anchor.V, point.X, point.Y));
        }
        return result;
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

    private static bool VisualAnchorsLookValid(byte[] imageBytes, IReadOnlyList<Re3mrAnchor> anchors)
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

    private static bool TryFitAffine(
        IReadOnlyList<CalibrationPair> pairs,
        out AffineTransform transform,
        out double residual,
        out double maxError)
    {
        transform = default;
        residual = maxError = double.PositiveInfinity;
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
        if (!TrySolve3x3(matrix, [xU, xV, x1], out var x) ||
            !TrySolve3x3(matrix, [yU, yV, y1], out var y))
            return false;

        transform = new AffineTransform(x[0], x[1], x[2], y[0], y[1], y[2]);
        var errors = pairs.Select(pair => Error(transform, pair)).ToArray();
        residual = Math.Sqrt(errors.Average(error => error * error));
        maxError = errors.Max();
        return double.IsFinite(residual) && double.IsFinite(maxError);
    }

    private static bool TrySolve3x3(double[,] source, double[] right, out double[] solution)
    {
        solution = new double[3];
        var augmented = new double[3, 4];
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
                augmented[row, column] = source[row, column];
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
                for (var column = pivot; column < 4; column++)
                    (augmented[pivot, column], augmented[best, column]) =
                        (augmented[best, column], augmented[pivot, column]);
            }

            var divisor = augmented[pivot, pivot];
            for (var column = pivot; column < 4; column++)
                augmented[pivot, column] /= divisor;
            for (var row = 0; row < 3; row++)
            {
                if (row == pivot)
                    continue;
                var factor = augmented[row, pivot];
                for (var column = pivot; column < 4; column++)
                    augmented[row, column] -= factor * augmented[pivot, column];
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

    private static bool TransformLooksSane(AffineTransform transform)
    {
        var determinant = transform.A * transform.E - transform.B * transform.D;
        if (!double.IsFinite(determinant) || Math.Abs(determinant) < 0.05 || Math.Abs(determinant) > 30)
            return false;

        var corners = new[]
        {
            Apply(transform, 0, 0), Apply(transform, 1, 0),
            Apply(transform, 0, 1), Apply(transform, 1, 1),
        };
        return corners.All(point =>
            point.X >= -2.0 && point.X <= 3.0 &&
            point.Y >= -2.0 && point.Y <= 3.0);
    }

    private static (double X, double Y) Apply(AffineTransform transform, double u, double v) =>
        (transform.A * u + transform.B * v + transform.C,
         transform.D * u + transform.E * v + transform.F);

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

    private static MapArtworkRegistrationRegion RegistrationRegion(IReadOnlyList<Re3mrAnchor> anchors) =>
        new(
            anchors.Min(anchor => anchor.U),
            anchors.Min(anchor => anchor.V),
            anchors.Max(anchor => anchor.U),
            anchors.Max(anchor => anchor.V));

    private static Re3mrAnchor TransformAnchor(
        Re3mrAnchor anchor,
        MapArtworkImageTransform transform)
    {
        var mapped = transform.Apply(anchor.U, anchor.V);
        return anchor with { U = mapped.U, V = mapped.V };
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
                var state = JsonSerializer.Deserialize<Re3mrArtworkState>(json, JsonOptions);
                if (state is null || !string.Equals(state.StateVersion, ProviderStateVersion, StringComparison.Ordinal))
                    continue;
                var image = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                return new PreviousState(directory, state, image);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Corrupt provider metadata must not block the next fallback provider.
            }
        }
        return null;
    }

    private async Task<MapArtworkProviderResult> ReusePreviousOrRejectAsync(
        ProviderStorage storage,
        string destination,
        string reason,
        CancellationToken cancellationToken)
    {
        var previous = await TryLoadPreviousStateAsync(storage, cancellationToken);
        if (previous is null)
            return Rejected(reason);

        foreach (var previousSvg in storage.PreviousSvgFiles)
        {
            if (!File.Exists(previousSvg))
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(previousSvg, destination, overwrite: true);
            CopyProviderDirectory(previous.Directory, storage.CandidateProviderDirectory);
            return new MapArtworkProviderResult(
                true,
                ProviderId,
                $"{previous.State.PageVersion}:{previous.State.SourceSha256[..Math.Min(12, previous.State.SourceSha256.Length)]}",
                "RE3MR · CC BY-NC-SA",
                Configs["groundzero"].PageUrl,
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
                           ?? throw new InvalidOperationException("Map SVG destination has no parent directory.");
        var candidateDirectory = svgDirectory.Parent
                                 ?? throw new InvalidOperationException("Map candidate directory could not be resolved.");
        var root = candidateDirectory.Parent
                   ?? throw new InvalidOperationException("Map cache root could not be resolved.");
        var safeKey = new string(mapKey.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character).ToArray());
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

    private static bool TryResolveConfig(MapLayoutDefinition layout, out Re3mrMapConfig config)
    {
        foreach (var value in new[] { layout.NormalizedName, layout.Key })
        {
            var key = NormalizeName(value);
            if (Configs.TryGetValue(key, out config!))
                return true;
        }
        config = null!;
        return false;
    }

    private static string NormalizeName(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static MapArtworkProviderResult Rejected(string warning) =>
        new(false, null, null, null, null, warning);

    [GeneratedRegex(@"\bVersion\s+(?<version>[0-9]+(?:\.[0-9]+)*(?:[A-Za-z]+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionRegex();

    [GeneratedRegex("href=[\\\"'](?<url>https?://[^\\\"']+\\.png(?:\\?[^\\\"']*)?)[\\\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ImageUrlRegex();

    private sealed record Re3mrMapConfig(
        string Key,
        string PageUrl,
        string FallbackImageUrl,
        string BaselineVersion,
        IReadOnlyList<Re3mrAnchor> BaselineAnchors);

    private sealed record Re3mrAnchor(
        string Name,
        double U,
        double V,
        IReadOnlyList<string>? Aliases = null)
    {
        public IEnumerable<string> AllNames =>
            new[] { Name }.Concat(Aliases ?? []);
    }

    private sealed record DownloadedSource(
        string PageVersion,
        string ImageUrl,
        string Sha256,
        byte[] ImageBytes,
        string MediaType,
        int Width,
        int Height);

    private sealed record Re3mrArtworkState(
        string StateVersion,
        string PageVersion,
        string ImageUrl,
        string SourceSha256,
        int Width,
        int Height,
        IReadOnlyList<Re3mrAnchor> Anchors,
        double CalibrationResidual,
        double? RegistrationScore,
        DateTimeOffset ValidatedAtUtc);

    private sealed record PreviousState(
        string Directory,
        Re3mrArtworkState State,
        byte[] ImageBytes);

    private sealed record ProviderStorage(
        string CandidateProviderDirectory,
        string CandidateSourceImage,
        string CandidateState,
        IReadOnlyList<string> PreviousProviderDirectories,
        IReadOnlyList<string> PreviousSvgFiles);

    private readonly record struct CalibrationPair(
        double U,
        double V,
        double SurfaceX,
        double SurfaceY);

    private readonly record struct AffineTransform(
        double A,
        double B,
        double C,
        double D,
        double E,
        double F);
}
