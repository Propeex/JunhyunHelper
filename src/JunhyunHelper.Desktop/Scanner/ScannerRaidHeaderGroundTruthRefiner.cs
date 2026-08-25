using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Reviewed-Ground-Truth recovery for raid inventory captures where a neutral horizontal
/// inventory line visually joins the inspect header and makes the older live fallback
/// assign the header's left edge too far to the left.
///
/// The coarse rectangle/title geometry remains proposal evidence only. This recovery
/// merely uses that strong proposal to own the horizontal header lane, then independently
/// requires the same red close control, neutral header line, magnifier template, dark
/// title field, text evidence and final 0.68 semantic score as the trusted header path.
/// No OCR/catalog threshold is relaxed here.
/// </summary>
internal static class ScannerRaidHeaderGroundTruthRefiner
{
    private const double StrongStructuralFloor = 0.90;
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
        if (candidate.Score < StrongStructuralFloor ||
            !string.Equals(candidate.Reason, "RED_X_CANDIDATE", StringComparison.Ordinal))
        {
            return null;
        }

        var panel = candidate.Window;
        var proposedTitle = candidate.Title;
        if (panel.Width < 150 || panel.Height < 110 ||
            proposedTitle.Width < 24 || proposedTitle.Height < 10)
        {
            return null;
        }

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
            var closeTemplate = ScannerHeaderIconTemplateMatcher.CloseScore(
                bgra,
                stride,
                close.X,
                close.Y,
                close.Width,
                close.Height);
            if (closeTemplate < CloseTemplateFloor)
                continue;

            var frame = FindCandidateOwnedHeaderFrame(
                bgra,
                width,
                height,
                stride,
                panel,
                close);
            if (frame is null)
                continue;

            var closeEvidence = ScoreCloseAgainstFrame(close, frame.Value, closeTemplate);
            if (closeEvidence < CloseEvidenceFloor)
                continue;

            var magnifier = FindTitleOwnedMagnifier(
                bgra,
                width,
                height,
                stride,
                panel,
                proposedTitle,
                frame.Value,
                close);
            if (magnifier is null)
                continue;

            var fieldDarkness = MeasureTitleFieldDarkness(
                bgra,
                width,
                height,
                stride,
                frame.Value,
                close);
            if (fieldDarkness < FieldDarknessFloor)
                continue;

            var closeGap = Math.Max(4, (int)Math.Round(close.Height * 0.32));
            var titleLeft = Math.Clamp(proposedTitle.X, frame.Value.Left + 1, close.X - 1);
            var titleRight = close.X - closeGap;
            if (titleRight - titleLeft < Math.Max(24, close.Height * 3))
                continue;

            var titleTop = Math.Clamp(
                frame.Value.Top + Math.Max(2, (int)Math.Round(close.Height * 0.12)),
                0,
                height - 1);
            var titleBottom = Math.Clamp(
                frame.Value.Top + Math.Max(19, (int)Math.Round(close.Height * 1.42)),
                titleTop,
                height - 1);
            var title = new ScannerDetectedRegion(
                titleLeft,
                titleTop,
                titleRight - titleLeft,
                Math.Max(1, titleBottom - titleTop + 1),
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
                frame.Value.Score * 0.30 +
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

    private static IReadOnlyList<RaidRedComponent> FindRedComponents(
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
        var results = new List<RaidRedComponent>();

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
                results.Add(new RaidRedComponent(
                    minX,
                    minY,
                    componentWidth,
                    componentHeight,
                    area));
            }
        }

        return results;
    }

    private static RaidHeaderFrame? FindCandidateOwnedHeaderFrame(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        RaidRedComponent close)
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
        var minimumLength = Math.Max(130, (int)Math.Round(panel.Width * 0.56));
        var panelLeftTolerance = Math.Max(6, (int)Math.Round(panel.Width * 0.015));

        RaidHeaderFrame? best = null;
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
                if (run.Length < minimumLength || run.Density < 0.80)
                    continue;

                var runRight = run.Left + run.Length - 1;
                if (run.Left > panel.X + panelLeftTolerance ||
                    runRight < panel.X + minimumLength - 1)
                {
                    continue;
                }

                // Raid inventory rows can extend the same neutral run far left of the
                // inspect panel. The strong RED_X_CANDIDATE owns only the left boundary;
                // every semantic pixel check below remains independent.
                var ownedLeft = Math.Clamp(panel.X, 0, width - 1);
                var ownedRight = Math.Clamp(runRight, ownedLeft + 1, width - 1);
                var ownedWidth = ownedRight - ownedLeft + 1;
                var widthTolerance = Math.Max(48.0, panel.Width * 0.12);
                if (Math.Abs(ownedWidth - panel.Width) > widthTolerance)
                    continue;

                var ownedDensity = MeasureHeaderDensity(
                    bgra,
                    stride,
                    ownedLeft,
                    ownedRight,
                    y);
                if (ownedDensity < 0.80)
                    continue;

                var expectedRight = close.X + close.Width + (int)Math.Round(close.Height * 0.30);
                var rightScore = Math.Max(
                    0,
                    1.0 - Math.Abs(ownedRight - expectedRight) /
                    Math.Max(20.0, panel.Width * 0.065));
                var lengthScore = Math.Clamp(ownedWidth / (double)Math.Max(1, panel.Width), 0, 1);
                var expectedY = close.Y - Math.Max(4, (int)Math.Round(close.Height * 0.30));
                var yScore = Math.Max(
                    0,
                    1.0 - Math.Abs(y - expectedY) / Math.Max(5.0, close.Height * 0.65));
                var score =
                    ownedDensity * 0.38 +
                    lengthScore * 0.20 +
                    rightScore * 0.18 +
                    0.14 +
                    yScore * 0.10;
                if (score < HeaderFrameFloor)
                    continue;
                if (best is { } existing && existing.Score >= score)
                    continue;

                best = new RaidHeaderFrame(ownedLeft, y, ownedRight, score);
            }
        }

        return best;
    }

    private static IReadOnlyList<RaidHeaderRun> FindHeaderRuns(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int right,
        int y,
        int maxGap)
    {
        var result = new List<RaidHeaderRun>();
        var start = -1;
        var last = -1;
        var good = 0;
        var gap = 0;

        void Complete()
        {
            if (start < 0 || last < start)
                return;
            var length = last - start + 1;
            result.Add(new RaidHeaderRun(start, length, good / (double)Math.Max(1, length)));
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

    private static double MeasureHeaderDensity(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int right,
        int y)
    {
        if (right < left)
            return 0;
        var good = 0;
        var total = 0;
        for (var x = left; x <= right; x++)
        {
            total++;
            if (IsHeaderTopBorderPixel(bgra, stride, x, y))
                good++;
        }
        return total <= 0 ? 0 : good / (double)total;
    }

    private static double ScoreCloseAgainstFrame(
        RaidRedComponent close,
        RaidHeaderFrame frame,
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

    private static RaidMagnifier? FindTitleOwnedMagnifier(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        ScannerDetectedRegion proposedTitle,
        RaidHeaderFrame frame,
        RaidRedComponent close)
    {
        var scale = Math.Clamp(close.Height / 17.0, 0.55, 1.85);
        var expectedY = frame.Top + (int)Math.Round(7.0 * scale);
        var expectedSize = Math.Clamp((int)Math.Round(13.0 * scale), 7, 24);
        var yRadius = Math.Max(2, (int)Math.Ceiling(3.0 * scale));
        var xRadius = Math.Max(3, (int)Math.Ceiling(5.0 * scale));
        var sizeRadius = Math.Max(1, (int)Math.Ceiling(1.5 * scale));
        var iconGap = Math.Max(3, (int)Math.Round(close.Height * 0.26));

        RaidMagnifier? best = null;
        for (var size = Math.Max(7, expectedSize - sizeRadius);
             size <= Math.Min(24, expectedSize + sizeRadius);
             size++)
        {
            var expectedX = proposedTitle.X - iconGap - size;
            for (var dy = -yRadius; dy <= yRadius; dy++)
            for (var dx = -xRadius; dx <= xRadius; dx++)
            {
                var x = expectedX + dx;
                var y = expectedY + dy;
                if (x < 0 || y < 0 || x + size > width || y + size > height)
                    continue;
                if (x < panel.X + Math.Max(3, (int)Math.Floor(5.0 * scale)) ||
                    x + size > proposedTitle.X + 2)
                {
                    continue;
                }

                var template = LiveMagnifierScore(bgra, stride, x, y, size);
                if (template < MagnifierTemplateFloor)
                    continue;

                var relationError = Math.Abs((x + size + iconGap) - proposedTitle.X);
                var relationScore = Math.Max(
                    0,
                    1.0 - relationError / Math.Max(4.0, 5.0 * scale));
                var yScore = Math.Max(
                    0,
                    1.0 - Math.Abs(y - expectedY) / Math.Max(3.0, 3.5 * scale));
                var sizeScore = Math.Max(
                    0,
                    1.0 - Math.Abs(size - expectedSize) / Math.Max(2.0, 3.0 * scale));
                var location = relationScore * 0.50 + yScore * 0.34 + sizeScore * 0.16;
                var score = template * 0.72 + location * 0.28;
                if (score < MagnifierEvidenceFloor)
                    continue;
                if (best is { } existing && existing.Score >= score)
                    continue;

                best = new RaidMagnifier(
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
        RaidHeaderFrame frame,
        RaidRedComponent close)
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

    private static double LiveMagnifierScore(
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

    private readonly record struct RaidRedComponent(int X, int Y, int Width, int Height, int Area);
    private readonly record struct RaidHeaderRun(int Left, int Length, double Density);
    private readonly record struct RaidHeaderFrame(int Left, int Top, int Right, double Score);
    private readonly record struct RaidMagnifier(ScannerDetectedRegion Region, double Score);
}
