using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Evidence-backed fallback for live Tarkov inspect headers that the older synthetic
/// header template rejects. The v1.4.0 primary lock remains authoritative and is tried
/// first; this path runs only after that lock fails.
///
/// Calibration source: four user-reviewed 1920x1080 live captures from 2026-08-23.
/// No user screenshot or item identity is embedded in the product. Only measured UI
/// geometry/color/template properties are represented here.
/// </summary>
internal static class ScannerLiveHeaderGroundTruthRefiner
{
    private const double CloseTemplateFloor = 0.40;
    private const double CloseEvidenceFloor = 0.60;
    private const double HeaderFrameFloor = 0.74;
    private const double MagnifierTemplateFloor = 0.54;
    private const double MagnifierEvidenceFloor = 0.66;
    private const double FieldDarknessFloor = 0.58;
    private const double TextEvidenceFloor = 0.22;
    private const double FinalLockFloor = 0.68;

    public static ScannerTitleAnchorRefinement? TryRefine(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedCandidate candidate)
    {
        if (width < 120 || height < 80 || stride < width * 4 || bgra.Length < stride * height)
            return null;

        var panel = candidate.Window;
        if (panel.Width < 150 || panel.Height < 110)
            return null;

        // Large stash/inventory rectangles are common structural decoys. Search only the
        // right side of the coarse candidate, but allow the live close control to sit well
        // above the candidate top because one reviewed tall inspect panel was de-duplicated
        // against a lower overlapping candidate before header validation.
        var searchLeft = Math.Clamp(panel.X + (int)Math.Round(panel.Width * 0.60), 0, width - 1);
        var searchRight = Math.Clamp(
            panel.X + panel.Width + Math.Max(20, (int)Math.Round(panel.Width * 0.04)),
            searchLeft,
            width - 1);
        var searchUp = Math.Max(30, Math.Min(190, (int)Math.Round(panel.Height * 0.26)));
        var searchDown = Math.Max(36, (int)Math.Round(panel.Height * 0.14));
        var searchTop = Math.Clamp(panel.Y - searchUp, 0, height - 1);
        var searchBottom = Math.Clamp(panel.Y + searchDown, searchTop, height - 1);

        ScannerTitleAnchorRefinement? best = null;
        foreach (var close in FindRedComponents(
                     bgra,
                     width,
                     height,
                     stride,
                     searchLeft,
                     searchTop,
                     searchRight,
                     searchBottom))
        {
            var closeTemplate = CloseScore(bgra, stride, close.X, close.Y, close.Width, close.Height);
            if (closeTemplate < CloseTemplateFloor)
                continue;

            var frame = FindHeaderFrame(
                bgra,
                width,
                height,
                stride,
                panel,
                close);
            if (frame is null)
                continue;

            // A recovered header must still agree with the coarse candidate horizontally.
            // This prevents a high-scoring large inventory/stash rectangle from borrowing
            // a nearby inspect header. The live tall case differs mainly in top/bottom, not
            // in left/right ownership.
            var frameValue = frame.Value;
            var widthTolerance = Math.Max(48.0, frameValue.Width * 0.12);
            var leftTolerance = Math.Max(40.0, frameValue.Width * 0.15);
            if (Math.Abs(panel.Width - frameValue.Width) > widthTolerance ||
                Math.Abs(panel.X - frameValue.Left) > leftTolerance)
            {
                continue;
            }

            var closeEvidence = ScoreCloseAgainstFrame(
                bgra,
                stride,
                close,
                frameValue,
                closeTemplate);
            if (closeEvidence < CloseEvidenceFloor)
                continue;

            var magnifier = FindMagnifier(
                bgra,
                width,
                height,
                stride,
                frameValue,
                close);
            if (magnifier is null)
                continue;

            var fieldDarkness = MeasureTitleFieldDarkness(
                bgra,
                width,
                height,
                stride,
                frameValue,
                close);
            if (fieldDarkness < FieldDarknessFloor)
                continue;

            var iconGap = Math.Max(3, (int)Math.Round(close.Height * 0.26));
            var closeGap = Math.Max(4, (int)Math.Round(close.Height * 0.32));
            var titleLeft = magnifier.Value.Region.X + magnifier.Value.Region.Width + iconGap;
            var titleRight = close.X - closeGap;
            if (titleRight - titleLeft < Math.Max(24, close.Height * 3))
                continue;

            var titleTop = frameValue.Top + Math.Max(2, (int)Math.Round(close.Height * 0.12));
            var titleBottom = Math.Min(
                height - 1,
                frameValue.Top + Math.Max(19, (int)Math.Round(close.Height * 1.42)));
            var titleHeight = titleBottom - titleTop + 1;
            if (titleHeight < 12)
                continue;

            var title = new ScannerDetectedRegion(
                titleLeft,
                titleTop,
                titleRight - titleLeft,
                titleHeight,
                0);
            var textEvidence = MeasureTitleTextEvidence(
                bgra,
                width,
                height,
                stride,
                title,
                close.Height);
            if (textEvidence < TextEvidenceFloor)
                continue;

            var score = Math.Clamp(
                closeEvidence * 0.18 +
                frameValue.Score * 0.30 +
                magnifier.Value.Score * 0.32 +
                fieldDarkness * 0.12 +
                textEvidence * 0.08,
                0,
                1);
            if (score < FinalLockFloor)
                continue;

            var result = new ScannerTitleAnchorRefinement(
                title with { Score = score },
                magnifier.Value.Region with { Score = magnifier.Value.Score },
                new ScannerDetectedRegion(
                    close.X,
                    close.Y,
                    close.Width,
                    close.Height,
                    closeEvidence),
                score,
                "HEADER_FRAME_LOCKED");

            if (best is null || result.Score > best.Value.Score)
                best = result;
        }

        return best;
    }

    private static IReadOnlyList<LiveRedComponent> FindRedComponents(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        int left,
        int top,
        int right,
        int bottom)
    {
        var regionWidth = right - left + 1;
        var regionHeight = bottom - top + 1;
        if (regionWidth <= 0 || regionHeight <= 0)
            return [];

        var visited = new byte[regionWidth * regionHeight];
        var queue = new Queue<int>();
        var results = new List<LiveRedComponent>();

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var local = (y - top) * regionWidth + (x - left);
            if (visited[local] != 0 || !IsRed(bgra, stride, x, y))
                continue;

            visited[local] = 1;
            queue.Enqueue(local);
            var minX = x;
            var maxX = x;
            var minY = y;
            var maxY = y;
            var area = 0;

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var localY = index / regionWidth;
                var localX = index - localY * regionWidth;
                var cx = left + localX;
                var cy = top + localY;
                area++;
                minX = Math.Min(minX, cx);
                maxX = Math.Max(maxX, cx);
                minY = Math.Min(minY, cy);
                maxY = Math.Max(maxY, cy);

                for (var dy = -1; dy <= 1; dy++)
                {
                    var ny = localY + dy;
                    if (ny < 0 || ny >= regionHeight)
                        continue;

                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        var nx = localX + dx;
                        if (nx < 0 || nx >= regionWidth)
                            continue;

                        var neighbor = ny * regionWidth + nx;
                        if (visited[neighbor] != 0)
                            continue;
                        var absoluteX = left + nx;
                        var absoluteY = top + ny;
                        if (!IsRed(bgra, stride, absoluteX, absoluteY))
                            continue;
                        visited[neighbor] = 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            var componentWidth = maxX - minX + 1;
            var componentHeight = maxY - minY + 1;
            var aspect = componentHeight <= 0 ? 0 : (double)componentWidth / componentHeight;
            if (area >= 16 &&
                componentWidth is >= 7 and <= 55 &&
                componentHeight is >= 5 and <= 36 &&
                aspect is >= 0.70 and <= 2.25)
            {
                results.Add(new LiveRedComponent(
                    minX,
                    minY,
                    componentWidth,
                    componentHeight,
                    area));
            }
        }

        return results;
    }

    private static LiveHeaderFrame? FindHeaderFrame(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        LiveRedComponent close)
    {
        var searchLeft = Math.Max(0, close.X - (int)Math.Round(panel.Width * 1.15));
        var searchRight = Math.Min(
            width - 1,
            close.X + close.Width + Math.Max(12, (int)Math.Round(close.Height * 0.80)));
        var searchTop = Math.Max(0, close.Y - Math.Max(13, (int)Math.Round(close.Height * 0.95)));
        var searchBottom = Math.Min(
            height - 1,
            close.Y + Math.Max(2, (int)Math.Round(close.Height * 0.16)));
        var maxGap = Math.Max(2, close.Height / 5);
        var minLength = Math.Max(130, (int)Math.Round(panel.Width * 0.56));

        LiveHeaderFrame? best = null;
        for (var y = searchTop; y <= searchBottom; y++)
        {
            foreach (var run in FindHeaderRuns(
                         bgra,
                         stride,
                         searchLeft,
                         searchRight,
                         y,
                         maxGap))
            {
                if (run.Length < minLength || run.Density < 0.80)
                    continue;

                var runRight = run.Left + run.Length - 1;
                var expectedRight = close.X + close.Width + (int)Math.Round(close.Height * 0.30);
                var rightScore = Math.Max(
                    0,
                    1.0 - Math.Abs(runRight - expectedRight) / Math.Max(20.0, panel.Width * 0.065));
                var leftScore = Math.Max(
                    0,
                    1.0 - Math.Abs(run.Left - panel.X) / Math.Max(28.0, panel.Width * 0.15));
                var lengthScore = Math.Clamp((double)run.Length / Math.Max(1, panel.Width), 0, 1);
                var expectedY = close.Y - Math.Max(4, (int)Math.Round(close.Height * 0.30));
                var yScore = Math.Max(
                    0,
                    1.0 - Math.Abs(y - expectedY) / Math.Max(5.0, close.Height * 0.65));
                var score =
                    run.Density * 0.38 +
                    lengthScore * 0.20 +
                    rightScore * 0.18 +
                    leftScore * 0.14 +
                    yScore * 0.10;

                if (score < HeaderFrameFloor || best is { Score: >= var existing } && existing >= score)
                    continue;

                best = new LiveHeaderFrame(run.Left, y, runRight, score);
            }
        }

        return best;
    }

    private static IReadOnlyList<LiveHeaderRun> FindHeaderRuns(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int right,
        int y,
        int maxGap)
    {
        var result = new List<LiveHeaderRun>();
        var start = -1;
        var last = -1;
        var good = 0;
        var gap = 0;

        void Complete()
        {
            if (start < 0 || last < start)
                return;
            var length = last - start + 1;
            result.Add(new LiveHeaderRun(start, length, good / (double)Math.Max(1, length)));
        }

        for (var x = left; x <= right; x++)
        {
            if (IsHeaderTopBorderPixel(bgra, stride, x, y))
            {
                if (start < 0)
                {
                    start = x;
                    good = 0;
                }
                last = x;
                good++;
                gap = 0;
                continue;
            }

            if (start < 0)
                continue;
            gap++;
            if (gap <= maxGap)
                continue;

            Complete();
            start = -1;
            last = -1;
            good = 0;
            gap = 0;
        }

        Complete();
        return result;
    }

    private static double ScoreCloseAgainstFrame(
        ReadOnlySpan<byte> bgra,
        int stride,
        LiveRedComponent close,
        LiveHeaderFrame frame,
        double template)
    {
        var expectedRight = close.X + close.Width + (int)Math.Round(close.Height * 0.30);
        var rightScore = Math.Max(
            0,
            1.0 - Math.Abs(frame.Right - expectedRight) / Math.Max(10.0, close.Height * 1.5));
        var expectedTop = frame.Top + Math.Max(4, (int)Math.Round(close.Height * 0.30));
        var topScore = Math.Max(
            0,
            1.0 - Math.Abs(close.Y - expectedTop) / Math.Max(5.0, close.Height * 0.55));
        var fill = Math.Clamp(close.Area / (double)Math.Max(1, close.Width * close.Height), 0, 1);
        return Math.Clamp(
            template * 0.45 + rightScore * 0.30 + topScore * 0.20 + fill * 0.05,
            0,
            1);
    }

    private static LiveMagnifier? FindMagnifier(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        LiveHeaderFrame frame,
        LiveRedComponent close)
    {
        var scale = Math.Clamp(close.Height / 17.0, 0.55, 1.85);
        var expectedX = frame.Left + (int)Math.Round(12 * scale);
        var expectedY = frame.Top + (int)Math.Round(7 * scale);
        var expectedSize = Math.Clamp((int)Math.Round(13 * scale), 7, 24);
        var offsetRadius = Math.Max(2, (int)Math.Ceiling(2.5 * scale));
        var sizeRadius = Math.Max(1, (int)Math.Ceiling(1.5 * scale));

        LiveMagnifier? best = null;
        for (var size = Math.Max(7, expectedSize - sizeRadius);
             size <= Math.Min(24, expectedSize + sizeRadius);
             size++)
        {
            for (var dy = -offsetRadius; dy <= offsetRadius; dy++)
            for (var dx = -offsetRadius; dx <= offsetRadius; dx++)
            {
                var x = expectedX + dx;
                var y = expectedY + dy;
                if (x < 0 || y < 0 || x + size > width || y + size > height)
                    continue;

                var laneRight = frame.Left + (int)Math.Ceiling(29 * scale);
                if (x < frame.Left + Math.Max(3, (int)Math.Floor(5 * scale)) || x + size > laneRight)
                    continue;

                var template = MagnifierScore(bgra, stride, x, y, size);
                if (template < MagnifierTemplateFloor)
                    continue;

                var xScore = Math.Max(0, 1.0 - Math.Abs(x - expectedX) / Math.Max(3.0, 3.2 * scale));
                var yScore = Math.Max(0, 1.0 - Math.Abs(y - expectedY) / Math.Max(3.0, 3.0 * scale));
                var sizeScore = Math.Max(0, 1.0 - Math.Abs(size - expectedSize) / Math.Max(2.0, 2.5 * scale));
                var location = xScore * 0.48 + yScore * 0.34 + sizeScore * 0.18;
                var score = template * 0.72 + location * 0.28;
                if (score < MagnifierEvidenceFloor || best is { Score: >= var existing } && existing >= score)
                    continue;

                best = new LiveMagnifier(
                    new ScannerDetectedRegion(x, y, size, size, score),
                    score);
            }
        }

        return best;
    }

    private static double MeasureTitleFieldDarkness(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        LiveHeaderFrame frame,
        LiveRedComponent close)
    {
        var left = Math.Clamp(frame.Left + 2, 0, width - 1);
        var right = Math.Clamp(close.X - 3, left, width - 1);
        var top = Math.Clamp(frame.Top + 2, 0, height - 1);
        var bottom = Math.Clamp(
            frame.Top + Math.Max(19, (int)Math.Round(close.Height * 1.48)),
            top,
            height - 1);
        if (right <= left || bottom <= top)
            return 0;

        var dark = 0;
        var total = 0;
        var step = Math.Max(1, (right - left) / 220);
        for (var y = top; y <= bottom; y += 2)
        for (var x = left; x <= right; x += step)
        {
            total++;
            if (IsTitleFieldPixel(bgra, stride, x, y))
                dark++;
        }
        return total <= 0 ? 0 : dark / (double)total;
    }

    private static double MeasureTitleTextEvidence(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion title,
        int closeHeight)
    {
        _ = width;
        _ = height;
        var scanRight = Math.Min(
            title.X + title.Width - 1,
            title.X + Math.Max(90, closeHeight * 22));
        var bright = 0;
        var total = 0;
        for (var y = title.Y; y < title.Y + title.Height; y++)
        for (var x = title.X; x <= scanRight; x++)
        {
            total++;
            if (IsBrightNeutralPixel(bgra, stride, x, y))
                bright++;
        }

        if (bright < Math.Max(10, closeHeight))
            return 0;
        return Math.Clamp(bright / (double)Math.Max(24, closeHeight * 5), 0, 1);
    }

    private static double CloseScore(
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
        var edge = 0.0;
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
                diagonal += Math.Max(
                    BrightNeutralScore(bgra, stride, x + px, y + py),
                    1.0 - redScore);
                diagonalWeight++;
            }

            if (px <= 1 || py <= 1 || px >= width - 2 || py >= height - 2)
            {
                edge += redScore;
                edgeWeight++;
            }
        }

        var body = red / Math.Max(1.0, redWeight);
        var xStroke = diagonal / Math.Max(1.0, diagonalWeight);
        var edgeScore = edgeWeight <= 0 ? body : edge / edgeWeight;
        return Math.Clamp(body * 0.54 + xStroke * 0.30 + edgeScore * 0.16, 0, 1);
    }

    private static double MagnifierScore(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y,
        int size)
    {
        if (size < 7)
            return 0;

        var ring = 0.0;
        var ringWeight = 0.0;
        var centerDark = 0.0;
        var centerWeight = 0.0;
        var handle = 0.0;
        var handleWeight = 0.0;
        var outsideDark = 0.0;
        var outsideWeight = 0.0;

        for (var py = 0; py < size; py++)
        for (var px = 0; px < size; px++)
        {
            var u = (px + 0.5) / size;
            var v = (py + 0.5) / size;
            // Live icon: lens sits high/right; handle runs down-left.
            var dx = u - 0.58;
            var dy = v - 0.35;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            var bright = BrightNeutralScore(bgra, stride, x + px, y + py);
            var dark = 1.0 - bright;
            var isRing = radius is >= 0.27 and <= 0.45 && !(u < 0.38 && v > 0.55);
            var isCenter = radius <= 0.20;
            var isHandle = u <= 0.52 && v >= 0.48 && Math.Abs((u + v) - 0.92) <= 0.13;
            var isOutside = radius >= 0.50 && !isHandle;

            if (isRing)
            {
                ring += bright;
                ringWeight++;
            }
            if (isCenter)
            {
                centerDark += dark;
                centerWeight++;
            }
            if (isHandle)
            {
                handle += bright;
                handleWeight++;
            }
            if (isOutside)
            {
                outsideDark += dark;
                outsideWeight++;
            }
        }

        if (ringWeight <= 0 || centerWeight <= 0 || handleWeight <= 0)
            return 0;
        var ringScore = ring / ringWeight;
        var centerScore = centerDark / centerWeight;
        var handleScore = handle / handleWeight;
        var outsideScore = outsideWeight <= 0 ? 1 : outsideDark / outsideWeight;
        return Math.Clamp(
            ringScore * 0.43 +
            centerScore * 0.27 +
            handleScore * 0.20 +
            outsideScore * 0.10,
            0,
            1);
    }

    private static double BrightNeutralScore(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var luminance = (77.0 * r + 150.0 * g + 29.0 * b) / 256.0;
        var brightness = Math.Clamp((luminance - 72.0) / 105.0, 0, 1);
        var neutrality = 1.0 - Math.Clamp((max - min) / 70.0, 0, 1);
        return brightness * neutrality;
    }

    private static double RedDominanceScore(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var dominance = r - Math.Max(g, b);
        var chroma = r - Math.Min(g, b);
        return Math.Clamp(
            Math.Clamp((r - 55) / 105.0, 0, 1) * 0.45 +
            Math.Clamp((dominance - 14) / 70.0, 0, 1) * 0.40 +
            Math.Clamp((chroma - 18) / 90.0, 0, 1) * 0.15,
            0,
            1);
    }

    private static bool IsRed(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        return r >= 52 && r - g >= 20 && r - b >= 20;
    }

    private static bool IsHeaderTopBorderPixel(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        // Reviewed live captures contain long neutral border pixels at gray 38-39.
        return gray is >= 38 and <= 108 && max - min <= 36;
    }

    private static bool IsTitleFieldPixel(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray is >= 10 and <= 74 && max - min <= 34;
    }

    private static bool IsBrightNeutralPixel(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        ReadBgr(bgra, stride, x, y, out var b, out var g, out var r);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray >= 82 && max - min <= 52;
    }

    private static void ReadBgr(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y,
        out int b,
        out int g,
        out int r)
    {
        var offset = y * stride + x * 4;
        b = bgra[offset];
        g = bgra[offset + 1];
        r = bgra[offset + 2];
    }

    private readonly record struct LiveRedComponent(int X, int Y, int Width, int Height, int Area);
    private readonly record struct LiveHeaderRun(int Left, int Length, double Density);
    private readonly record struct LiveHeaderFrame(int Left, int Top, int Right, double Score)
    {
        public int Width => Right - Left + 1;
    }
    private readonly record struct LiveMagnifier(ScannerDetectedRegion Region, double Score);
}
