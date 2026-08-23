namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Procedural, scale-normalized templates for the two stable inspect-header controls.
/// We intentionally do not bundle copyrighted Tarkov sprite bytes and do not require
/// byte-for-byte equality: capture method, DPI and anti-aliasing can change individual
/// pixels. The template encodes only the icon geometry that remains stable across those
/// renderings.
/// </summary>
internal static class ScannerHeaderIconTemplateMatcher
{
    public static double MagnifierScore(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y,
        int size)
    {
        if (size < 7)
            return 0;

        var ringHit = 0.0;
        var ringWeight = 0.0;
        var centerDark = 0.0;
        var centerWeight = 0.0;
        var handleHit = 0.0;
        var handleWeight = 0.0;
        var outsideDark = 0.0;
        var outsideWeight = 0.0;

        // Normalize every candidate to [0,1]. The live 13x13 bright core is a circular
        // lens plus a lower-right diagonal handle. Use soft bands so anti-aliasing and
        // DPI resampling do not require exact pixels.
        for (var py = 0; py < size; py++)
        for (var px = 0; px < size; px++)
        {
            var u = (px + 0.5) / size;
            var v = (py + 0.5) / size;
            var dx = u - 0.46;
            var dy = v - 0.46;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            var bright = BrightNeutralScore(bgra, stride, x + px, y + py);
            var dark = 1.0 - bright;

            var isRing = radius is >= 0.29 and <= 0.46 && !(u > 0.67 && v > 0.67);
            var isCenter = radius <= 0.22;
            var diagonalDistance = Math.Abs(u - v);
            var isHandle = u >= 0.58 && v >= 0.58 && diagonalDistance <= 0.13;
            var isOutside = radius >= 0.52 && !isHandle;

            if (isRing)
            {
                ringHit += bright;
                ringWeight++;
            }
            if (isCenter)
            {
                centerDark += dark;
                centerWeight++;
            }
            if (isHandle)
            {
                handleHit += bright;
                handleWeight++;
            }
            if (isOutside)
            {
                outsideDark += dark;
                outsideWeight++;
            }
        }

        if (ringWeight == 0 || centerWeight == 0 || handleWeight == 0)
            return 0;

        var ring = ringHit / ringWeight;
        var center = centerDark / centerWeight;
        var handle = handleHit / handleWeight;
        var outside = outsideWeight <= 0 ? 1 : outsideDark / outsideWeight;

        // A letter can have a hollow center or diagonal, but it is very unlikely to
        // satisfy all four regions simultaneously at the fixed search-icon lane.
        return Math.Clamp(
            ring * 0.43 +
            center * 0.27 +
            handle * 0.20 +
            outside * 0.10,
            0,
            1);
    }

    public static double CloseScore(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y,
        int width,
        int height)
    {
        if (width < 7 || height < 5)
            return 0;

        var red = 0.0;
        var redWeight = 0.0;
        var diagonal = 0.0;
        var diagonalWeight = 0.0;
        var edgeRed = 0.0;
        var edgeWeight = 0.0;

        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
        {
            var u = (px + 0.5) / width;
            var v = (py + 0.5) / height;
            var redScore = RedDominanceScore(bgra, stride, x + px, y + py);
            red += redScore;
            redWeight++;

            var onDiagonal = Math.Abs(u - v) <= 0.10 || Math.Abs((1.0 - u) - v) <= 0.10;
            if (onDiagonal && u is > 0.16 and < 0.84 && v is > 0.12 and < 0.88)
            {
                // The X stroke can be neutral/bright or simply less red than the box.
                // Either contrast direction is useful and capture-method invariant.
                var contrast = Math.Max(
                    BrightNeutralScore(bgra, stride, x + px, y + py),
                    1.0 - redScore);
                diagonal += contrast;
                diagonalWeight++;
            }

            var onEdge = px <= 1 || py <= 1 || px >= width - 2 || py >= height - 2;
            if (onEdge)
            {
                edgeRed += redScore;
                edgeWeight++;
            }
        }

        if (redWeight == 0 || diagonalWeight == 0)
            return 0;

        var body = red / redWeight;
        var xStroke = diagonal / diagonalWeight;
        var edge = edgeWeight <= 0 ? body : edgeRed / edgeWeight;
        return Math.Clamp(body * 0.54 + xStroke * 0.30 + edge * 0.16, 0, 1);
    }

    private static double BrightNeutralScore(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var luminance = (77 * r + 150 * g + 29 * b) / 256.0;
        var brightness = Math.Clamp((luminance - 72) / 105.0, 0, 1);
        var neutrality = 1.0 - Math.Clamp((max - min) / 70.0, 0, 1);
        return brightness * neutrality;
    }

    private static double RedDominanceScore(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var dominance = r - Math.Max(g, b);
        var chroma = r - Math.Min(g, b);
        return Math.Clamp(
            Math.Clamp((r - 55) / 105.0, 0, 1) * 0.45 +
            Math.Clamp((dominance - 14) / 70.0, 0, 1) * 0.40 +
            Math.Clamp((chroma - 18) / 90.0, 0, 1) * 0.15,
            0,
            1);
    }
}
