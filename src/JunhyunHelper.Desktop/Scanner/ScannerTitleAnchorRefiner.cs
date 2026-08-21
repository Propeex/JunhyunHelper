using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Refines the coarse Scanner Lab title ROI with stable UI evidence from the Tarkov
/// inspect header. The red close control, the left magnifier-side bright component and
/// the dark title strip are treated as independent anchors. Failure to find an anchor
/// never invents a new ROI: the proven geometry ROI remains the fallback.
/// </summary>
internal static class ScannerTitleAnchorRefiner
{
    public static ScannerTitleAnchorRefinement Refine(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedCandidate candidate)
    {
        var panel = candidate.Window;
        var fallback = candidate.Title.Width > 0
            ? candidate.Title
            : ScannerDetailGeometryDetector.GetTitleRegion(panel);
        if (!IsValid(panel, width, height) || stride < width * 4 || bgra.Length < stride * height)
            return new ScannerTitleAnchorRefinement(fallback, default, candidate.CloseButton, 0, "GEOMETRY_FALLBACK");

        var close = IsValid(candidate.CloseButton, width, height)
            ? candidate.CloseButton
            : FindRedClose(bgra, width, height, stride, panel);
        var magnifier = FindMagnifierLikeComponent(bgra, width, height, stride, panel, fallback);

        var safeLeft = fallback.X;
        if (magnifier.Width > 0)
        {
            var margin = Math.Max(2, (int)Math.Round(panel.Width * 0.004));
            safeLeft = Math.Max(safeLeft, magnifier.X + magnifier.Width + margin);
        }

        var maximumRight = close.Width > 0
            ? Math.Max(safeLeft + 1, close.X - Math.Max(3, (int)Math.Round(panel.Width * 0.006)))
            : panel.X + panel.Width;
        var band = FindTitleStrip(bgra, width, height, stride, panel, safeLeft, maximumRight);

        var refined = fallback;
        var fieldScore = 0.0;
        if (band.Region.Width > 0)
        {
            fieldScore = band.Score;
            var fallbackRight = fallback.X + fallback.Width;
            var bandRight = band.Region.X + band.Region.Width;
            var right = bandRight >= safeLeft + panel.Width * 0.30
                ? Math.Min(maximumRight, bandRight)
                : Math.Min(maximumRight, fallbackRight);
            if (right <= safeLeft)
                right = Math.Min(maximumRight, fallbackRight);

            refined = new ScannerDetectedRegion(
                safeLeft,
                band.Region.Y,
                Math.Max(1, right - safeLeft),
                band.Region.Height,
                fallback.Score);
        }
        else if (safeLeft > fallback.X)
        {
            var fallbackRight = Math.Min(maximumRight, fallback.X + fallback.Width);
            refined = new ScannerDetectedRegion(
                safeLeft,
                fallback.Y,
                Math.Max(1, fallbackRight - safeLeft),
                fallback.Height,
                fallback.Score);
        }

        refined = Clamp(refined, width, height);
        var closeScore = close.Width > 0 ? 1.0 : 0.0;
        var magnifierScore = magnifier.Width > 0 ? 1.0 : 0.0;
        var anchorScore = closeScore * 0.38 + magnifierScore * 0.37 + fieldScore * 0.25;
        var reason = anchorScore >= 0.72
            ? "TITLE_ANCHORS_STRONG"
            : anchorScore >= 0.42
                ? "TITLE_ANCHORS_PARTIAL"
                : "GEOMETRY_FALLBACK";

        return new ScannerTitleAnchorRefinement(refined, magnifier, close, anchorScore, reason);
    }

    private static ScannerDetectedRegion FindRedClose(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel)
    {
        var left = Math.Clamp(panel.X + (int)Math.Round(panel.Width * 0.82), 0, width - 1);
        var top = Math.Clamp(panel.Y - Math.Max(4, panel.Height / 80), 0, height - 1);
        var right = Math.Clamp(panel.X + panel.Width - 1, left, width - 1);
        var bottom = Math.Clamp(panel.Y + Math.Max(12, (int)Math.Round(panel.Height * 0.09)), top, height - 1);
        var visited = new HashSet<int>();
        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsRed(bgra, stride, x, y))
                continue;

            var component = FloodComponent(bgra, width, height, stride, x, y, left, top, right, bottom, visited, IsRed);
            if (component.Area < 10 || component.Width is < 5 or > 70 || component.Height is < 4 or > 45)
                continue;
            var expectedX = panel.X + panel.Width;
            var proximity = 1.0 - Math.Min(1.0, Math.Abs(expectedX - (component.X + component.Width)) / Math.Max(1.0, panel.Width * 0.12));
            var compact = Math.Min(component.Width, component.Height) / (double)Math.Max(component.Width, component.Height);
            var score = proximity * 0.70 + compact * 0.30;
            if (score <= bestScore)
                continue;
            bestScore = score;
            best = new ScannerDetectedRegion(component.X, component.Y, component.Width, component.Height, score);
        }

        return best;
    }

    private static ScannerDetectedRegion FindMagnifierLikeComponent(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        ScannerDetectedRegion fallbackTitle)
    {
        var left = Math.Clamp(panel.X, 0, width - 1);
        var top = Math.Clamp(panel.Y - Math.Max(3, panel.Height / 100), 0, height - 1);
        var right = Math.Clamp(panel.X + Math.Max(24, (int)Math.Round(panel.Width * 0.075)), left, width - 1);
        var bottom = Math.Clamp(panel.Y + Math.Max(16, (int)Math.Round(panel.Height * 0.09)), top, height - 1);
        var visited = new HashSet<int>();
        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsBrightNeutral(bgra, stride, x, y))
                continue;

            var component = FloodComponent(bgra, width, height, stride, x, y, left, top, right, bottom, visited, IsBrightNeutral);
            if (component.Area < 5 || component.Width is < 4 or > 30 || component.Height is < 4 or > 30)
                continue;

            var aspect = component.Width / (double)Math.Max(1, component.Height);
            if (aspect is < 0.48 or > 1.90)
                continue;
            var centerX = component.X + component.Width / 2.0;
            var centerY = component.Y + component.Height / 2.0;
            var expectedX = panel.X + panel.Width * 0.020;
            var expectedY = panel.Y + panel.Height * 0.025;
            var dx = Math.Abs(centerX - expectedX) / Math.Max(5.0, panel.Width * 0.045);
            var dy = Math.Abs(centerY - expectedY) / Math.Max(5.0, panel.Height * 0.050);
            var proximity = Math.Max(0, 1.0 - (dx + dy) / 2.0);
            var square = 1.0 - Math.Min(1.0, Math.Abs(1.0 - aspect));
            var beforeTitleBonus = component.X < fallbackTitle.X + Math.Max(3, panel.Width * 0.01) ? 0.18 : 0;
            var score = proximity * 0.62 + square * 0.20 + beforeTitleBonus;
            if (score <= bestScore)
                continue;
            bestScore = score;
            best = new ScannerDetectedRegion(component.X, component.Y, component.Width, component.Height, score);
        }

        return bestScore >= 0.38 ? best : default;
    }

    private static (ScannerDetectedRegion Region, double Score) FindTitleStrip(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        int safeLeft,
        int maximumRight)
    {
        var searchTop = Math.Clamp(panel.Y - 2, 0, height - 1);
        var searchBottom = Math.Clamp(panel.Y + Math.Max(14, (int)Math.Round(panel.Height * 0.075)), searchTop, height - 1);
        var searchRight = Math.Clamp(Math.Min(maximumRight, panel.X + (int)Math.Round(panel.Width * 0.78)), safeLeft + 1, width - 1);
        if (searchRight - safeLeft < Math.Max(40, panel.Width / 5))
            return default;

        var rowScores = new double[searchBottom - searchTop + 1];
        for (var y = searchTop; y <= searchBottom; y++)
        {
            var field = 0;
            var samples = 0;
            var step = Math.Max(1, (searchRight - safeLeft) / 220);
            for (var x = safeLeft; x <= searchRight; x += step)
            {
                samples++;
                if (IsTitleFieldPixel(bgra, stride, x, y))
                    field++;
            }
            rowScores[y - searchTop] = samples == 0 ? 0 : field / (double)samples;
        }

        var bestStart = -1;
        var bestEnd = -1;
        var bestAverage = 0.0;
        for (var index = 0; index < rowScores.Length;)
        {
            if (rowScores[index] < 0.48)
            {
                index++;
                continue;
            }
            var start = index;
            var sum = 0.0;
            while (index < rowScores.Length && rowScores[index] >= 0.48)
            {
                sum += rowScores[index];
                index++;
            }
            var end = index - 1;
            var length = end - start + 1;
            var average = sum / Math.Max(1, length);
            if (length >= 5 && average > bestAverage)
            {
                bestStart = start;
                bestEnd = end;
                bestAverage = average;
            }
        }

        if (bestStart < 0)
            return default;

        var bandTop = searchTop + bestStart;
        var bandBottom = searchTop + bestEnd;
        var probeTop = Math.Min(bandBottom, bandTop + 1);
        var probeBottom = Math.Max(bandTop, bandBottom - 1);
        var lastGood = safeLeft;
        var gap = 0;
        var maxGap = Math.Max(6, panel.Width / 90);
        for (var x = safeLeft; x <= searchRight; x++)
        {
            var good = IsTitleFieldPixel(bgra, stride, x, probeTop) ||
                       IsTitleFieldPixel(bgra, stride, x, probeBottom);
            if (good)
            {
                lastGood = x;
                gap = 0;
            }
            else
            {
                gap++;
                if (gap > maxGap && lastGood - safeLeft >= panel.Width * 0.30)
                    break;
            }
        }

        var region = new ScannerDetectedRegion(
            safeLeft,
            bandTop,
            Math.Max(1, lastGood - safeLeft + 1),
            Math.Max(1, bandBottom - bandTop + 1),
            bestAverage);
        return (region, Math.Clamp(bestAverage, 0, 1));
    }

    private static Component FloodComponent(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        int startX,
        int startY,
        int left,
        int top,
        int right,
        int bottom,
        HashSet<int> visited,
        PixelPredicate predicate)
    {
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((startX, startY));
        visited.Add(startY * width + startX);
        var minX = startX;
        var maxX = startX;
        var minY = startY;
        var maxY = startY;
        var area = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            area++;
            minX = Math.Min(minX, current.X);
            maxX = Math.Max(maxX, current.X);
            minY = Math.Min(minY, current.Y);
            maxY = Math.Max(maxY, current.Y);
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                var x = current.X + dx;
                var y = current.Y + dy;
                if (x < left || x > right || y < top || y > bottom || x < 0 || x >= width || y < 0 || y >= height)
                    continue;
                var key = y * width + x;
                if (visited.Contains(key) || !predicate(bgra, stride, x, y))
                    continue;
                visited.Add(key);
                queue.Enqueue((x, y));
            }
        }

        return new Component(minX, minY, maxX - minX + 1, maxY - minY + 1, area);
    }

    private static bool IsRed(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        return r >= 52 && r - g >= 20 && r - b >= 20;
    }

    private static bool IsBrightNeutral(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray >= 88 && max - min <= 48;
    }

    private static bool IsTitleFieldPixel(ReadOnlySpan<byte> bgra, int stride, int x, int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray is >= 14 and <= 62 && max - min <= 24;
    }

    private static bool IsValid(ScannerDetectedRegion region, int width, int height) =>
        region.Width > 0 && region.Height > 0 &&
        region.X >= 0 && region.Y >= 0 &&
        region.X < width && region.Y < height;

    private static ScannerDetectedRegion Clamp(ScannerDetectedRegion region, int width, int height)
    {
        var x = Math.Clamp(region.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(region.Y, 0, Math.Max(0, height - 1));
        var w = Math.Clamp(region.Width, 1, Math.Max(1, width - x));
        var h = Math.Clamp(region.Height, 1, Math.Max(1, height - y));
        return new ScannerDetectedRegion(x, y, w, h, region.Score);
    }

    private delegate bool PixelPredicate(ReadOnlySpan<byte> bgra, int stride, int x, int y);
    private readonly record struct Component(int X, int Y, int Width, int Height, int Area);
}

internal readonly record struct ScannerTitleAnchorRefinement(
    ScannerDetectedRegion Title,
    ScannerDetectedRegion Magnifier,
    ScannerDetectedRegion CloseButton,
    double Score,
    string Reason);
