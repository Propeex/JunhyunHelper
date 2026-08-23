using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Refines the coarse Scanner Lab title ROI from the complete Tarkov inspect-header
/// structure. The dark title field is established first, then the red close control,
/// magnifier morphology, and the first visible title glyph corroborate each other.
/// This deliberately avoids using a panel-relative bright square as the sole left
/// anchor because a Korean title glyph can otherwise be mistaken for the magnifier.
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

        var detectedClose = FindRedClose(bgra, width, height, stride, panel);
        var close = detectedClose.Width > 0
            ? detectedClose
            : IsValid(candidate.CloseButton, width, height)
                ? candidate.CloseButton
                : default;

        var broadLeft = Math.Clamp(
            panel.X - Math.Max(8, (int)Math.Round(panel.Width * 0.035)),
            0,
            width - 1);
        var broadRight = close.Width > 0
            ? Math.Max(broadLeft + 1, close.X - Math.Max(3, (int)Math.Round(panel.Width * 0.006)))
            : Math.Clamp(panel.X + panel.Width, broadLeft + 1, width - 1);

        // Find the title field independently of the magnifier. The old implementation
        // started the field search after its guessed magnifier, which made a wrong icon
        // guess self-reinforcing and could clip the first Korean glyph from OCR.
        var field = FindTitleStrip(
            bgra,
            width,
            height,
            stride,
            panel,
            broadLeft,
            broadRight);

        var magnifier = FindMagnifierLikeComponent(
            bgra,
            width,
            height,
            stride,
            panel,
            fallback,
            field.Region);

        var glyphs = FindTitleGlyphComponents(
            bgra,
            width,
            height,
            stride,
            panel,
            fallback,
            field.Region,
            magnifier,
            broadRight);

        var textStart = glyphs.Count > 0 ? glyphs[0].X : -1;
        var leftPadding = Math.Max(1, (int)Math.Round(panel.Width * 0.0015));
        var safeLeft = textStart >= 0
            ? Math.Max(field.Region.Width > 0 ? field.Region.X : broadLeft, textStart - leftPadding)
            : magnifier.Width > 0
                ? magnifier.X + magnifier.Width + Math.Max(2, (int)Math.Round(panel.Width * 0.003))
                : fallback.X;

        var maximumRight = close.Width > 0
            ? Math.Max(safeLeft + 1, close.X - Math.Max(3, (int)Math.Round(panel.Width * 0.006)))
            : Math.Clamp(panel.X + panel.Width, safeLeft + 1, width - 1);

        var fallbackRight = Math.Min(maximumRight, fallback.X + fallback.Width);
        var fieldRight = field.Region.Width > 0
            ? Math.Min(maximumRight, field.Region.X + field.Region.Width)
            : fallbackRight;
        var lastGlyphRight = glyphs.Count > 0
            ? glyphs[^1].X + glyphs[^1].Width + Math.Max(4, (int)Math.Round(panel.Width * 0.006))
            : fallbackRight;
        var right = Math.Min(maximumRight, Math.Max(fallbackRight, Math.Max(fieldRight, lastGlyphRight)));
        if (right <= safeLeft)
            right = Math.Min(maximumRight, Math.Max(safeLeft + 1, fallbackRight));

        var titleY = field.Region.Height > 0 ? field.Region.Y : fallback.Y;
        var titleHeight = field.Region.Height > 0 ? field.Region.Height : fallback.Height;
        if (glyphs.Count > 0)
        {
            // Preserve a small background margin around the actual glyph envelope. It
            // improves WinRT OCR without letting the icon back into the title crop.
            var glyphTop = glyphs.Min(component => component.Y);
            var glyphBottom = glyphs.Max(component => component.Y + component.Height);
            var verticalPadding = Math.Max(1, (int)Math.Round(titleHeight * 0.08));
            var fieldTop = field.Region.Height > 0 ? field.Region.Y : fallback.Y;
            var fieldBottom = field.Region.Height > 0
                ? field.Region.Y + field.Region.Height
                : fallback.Y + fallback.Height;
            titleY = Math.Max(fieldTop, glyphTop - verticalPadding);
            var bottom = Math.Min(fieldBottom, glyphBottom + verticalPadding);
            titleHeight = Math.Max(1, bottom - titleY);
        }

        var refined = Clamp(
            new ScannerDetectedRegion(
                safeLeft,
                titleY,
                Math.Max(1, right - safeLeft),
                titleHeight,
                fallback.Score),
            width,
            height);

        var closeScore = close.Width > 0 ? Math.Clamp(close.Score, 0, 1) : 0.0;
        var magnifierScore = magnifier.Width > 0 ? Math.Clamp(magnifier.Score, 0, 1) : 0.0;
        var fieldScore = field.Region.Width > 0 ? Math.Clamp(field.Score, 0, 1) : 0.0;
        var textScore = glyphs.Count == 0 ? 0.0 : Math.Min(1.0, 0.55 + glyphs.Count * 0.12);
        var anchorScore = closeScore * 0.27 + magnifierScore * 0.28 + fieldScore * 0.25 + textScore * 0.20;
        var reason = field.Region.Width > 0 && glyphs.Count > 0 && magnifier.Width > 0 && close.Width > 0
            ? "TITLE_HEADER_TEXT_REFINED"
            : anchorScore >= 0.72
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
        var left = Math.Clamp(panel.X + (int)Math.Round(panel.Width * 0.75), 0, width - 1);
        var top = Math.Clamp(panel.Y - Math.Max(6, panel.Height / 70), 0, height - 1);
        var right = Math.Clamp(panel.X + panel.Width - 1, left, width - 1);
        var bottom = Math.Clamp(panel.Y + Math.Max(14, (int)Math.Round(panel.Height * 0.10)), top, height - 1);
        var visited = new HashSet<int>();
        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsRed(bgra, stride, x, y))
                continue;

            var component = FloodComponent(
                bgra,
                width,
                height,
                stride,
                x,
                y,
                left,
                top,
                right,
                bottom,
                visited,
                IsRed);
            if (component.Area < 10 || component.Width is < 5 or > 80 || component.Height is < 4 or > 55)
                continue;

            var expectedRight = panel.X + panel.Width;
            var proximity = 1.0 - Math.Min(
                1.0,
                Math.Abs(expectedRight - (component.X + component.Width)) / Math.Max(1.0, panel.Width * 0.13));
            var compact = Math.Min(component.Width, component.Height) /
                          (double)Math.Max(component.Width, component.Height);
            var upperBand = 1.0 - Math.Min(
                1.0,
                Math.Abs(component.Y - panel.Y) / Math.Max(5.0, panel.Height * 0.07));
            var score = proximity * 0.58 + compact * 0.22 + upperBand * 0.20;
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = new ScannerDetectedRegion(
                component.X,
                component.Y,
                component.Width,
                component.Height,
                Math.Clamp(score, 0, 1));
        }

        return bestScore >= 0.42 ? best : default;
    }

    private static ScannerDetectedRegion FindMagnifierLikeComponent(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        ScannerDetectedRegion fallbackTitle,
        ScannerDetectedRegion titleField)
    {
        var fieldHeight = titleField.Height > 0 ? titleField.Height : fallbackTitle.Height;
        var left = Math.Clamp(
            panel.X - Math.Max(8, (int)Math.Round(panel.Width * 0.04)),
            0,
            width - 1);
        var topBase = titleField.Height > 0 ? titleField.Y : fallbackTitle.Y;
        var bottomBase = titleField.Height > 0
            ? titleField.Y + titleField.Height
            : fallbackTitle.Y + fallbackTitle.Height;
        var top = Math.Clamp(topBase - Math.Max(4, (int)Math.Round(fieldHeight * 0.55)), 0, height - 1);
        var right = Math.Clamp(
            fallbackTitle.X + Math.Max(36, (int)Math.Round(panel.Width * 0.085)),
            left,
            width - 1);
        var bottom = Math.Clamp(bottomBase + Math.Max(4, (int)Math.Round(fieldHeight * 0.45)), top, height - 1);

        var raw = FindBrightComponents(bgra, width, height, stride, left, top, right, bottom);
        var components = MergeNearbyComponents(raw, Math.Max(2, fieldHeight / 10));
        if (components.Count == 0)
            return default;

        var maximumSize = Math.Max(34, (int)Math.Round(fieldHeight * 1.8));
        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;

        for (var index = 0; index < components.Count; index++)
        {
            var component = components[index];
            if (component.Area < 5 ||
                component.Width is < 4 || component.Height is < 4 ||
                component.Width > maximumSize || component.Height > maximumSize)
            {
                continue;
            }

            var aspect = component.Width / (double)Math.Max(1, component.Height);
            if (aspect is < 0.52 or > 1.65)
                continue;

            var textFollowers = components
                .Skip(index + 1)
                .Where(value =>
                    value.X > component.X + component.Width &&
                    value.X - (component.X + component.Width) <= Math.Max(20, (int)Math.Round(fieldHeight * 1.4)) &&
                    IsTextLike(value, fieldHeight))
                .Take(4)
                .ToArray();
            if (textFollowers.Length == 0)
                continue;

            var followerHeight = Median(textFollowers.Select(value => value.Height));
            var followerWidth = Median(textFollowers.Select(value => value.Width));
            var dominance = Math.Max(
                component.Height / Math.Max(1.0, followerHeight),
                component.Width / Math.Max(1.0, followerWidth));

            var scaleTarget = Math.Max(8.0, fieldHeight * 1.05);
            var scale = Math.Max(
                0,
                1.0 - Math.Abs(Math.Max(component.Width, component.Height) - scaleTarget) / scaleTarget);
            var square = 1.0 - Math.Min(1.0, Math.Abs(1.0 - aspect));
            var centerY = component.Y + component.Height / 2.0;
            var fieldCenterY = topBase + fieldHeight / 2.0;
            var vertical = Math.Max(
                0,
                1.0 - Math.Abs(centerY - fieldCenterY) / Math.Max(4.0, fieldHeight * 0.85));
            var leftOrder = components.Count <= 1
                ? 1.0
                : 1.0 - Math.Min(1.0, index / (double)Math.Min(5, components.Count - 1));
            var morphology = MagnifierMorphologyScore(bgra, stride, component);
            var followerScore = Math.Clamp((dominance - 0.92) / 0.35, 0, 1);

            // A real Tarkov magnifier is visibly larger than the following glyphs at
            // the same UI scale and has ring/handle morphology. A first Korean glyph
            // can be square and bright, but normally fails both of these safeguards.
            if (dominance < 1.08 && morphology < 0.68)
                continue;

            var score =
                leftOrder * 0.20 +
                scale * 0.18 +
                square * 0.10 +
                vertical * 0.12 +
                morphology * 0.28 +
                followerScore * 0.12;
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = new ScannerDetectedRegion(
                component.X,
                component.Y,
                component.Width,
                component.Height,
                Math.Clamp(score, 0, 1));
        }

        return bestScore >= 0.56 ? best : default;
    }

    private static IReadOnlyList<Component> FindTitleGlyphComponents(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        ScannerDetectedRegion fallbackTitle,
        ScannerDetectedRegion titleField,
        ScannerDetectedRegion magnifier,
        int maximumRight)
    {
        var fieldTop = titleField.Height > 0 ? titleField.Y : fallbackTitle.Y;
        var fieldHeight = titleField.Height > 0 ? titleField.Height : fallbackTitle.Height;
        var top = Math.Clamp(fieldTop - Math.Max(1, fieldHeight / 8), 0, height - 1);
        var bottom = Math.Clamp(
            fieldTop + fieldHeight + Math.Max(1, fieldHeight / 8),
            top,
            height - 1);
        var start = magnifier.Width > 0
            ? magnifier.X + magnifier.Width + 1
            : Math.Max(
                titleField.Width > 0 ? titleField.X : 0,
                fallbackTitle.X - Math.Max(4, (int)Math.Round(panel.Width * 0.012)));
        var right = Math.Clamp(
            Math.Min(maximumRight, titleField.Width > 0
                ? titleField.X + titleField.Width
                : fallbackTitle.X + Math.Max(fallbackTitle.Width, (int)Math.Round(panel.Width * 0.72))),
            Math.Min(start, width - 1),
            width - 1);
        start = Math.Clamp(start, 0, right);
        if (right - start < 4)
            return [];

        var components = MergeNearbyComponents(
            FindBrightComponents(bgra, width, height, stride, start, top, right, bottom),
            Math.Max(1, fieldHeight / 12));
        return components
            .Where(component => IsTextLike(component, fieldHeight))
            .Where(component => component.X + component.Width <= maximumRight)
            .OrderBy(component => component.X)
            .ThenBy(component => component.Y)
            .ToArray();
    }

    private static (ScannerDetectedRegion Region, double Score) FindTitleStrip(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        int searchLeft,
        int maximumRight)
    {
        var searchTop = Math.Clamp(panel.Y - Math.Max(3, panel.Height / 120), 0, height - 1);
        var searchBottom = Math.Clamp(
            panel.Y + Math.Max(16, (int)Math.Round(panel.Height * 0.08)),
            searchTop,
            height - 1);
        var searchRight = Math.Clamp(
            Math.Min(maximumRight, panel.X + (int)Math.Round(panel.Width * 0.84)),
            searchLeft + 1,
            width - 1);
        if (searchRight - searchLeft < Math.Max(40, panel.Width / 5))
            return default;

        var rowScores = new double[searchBottom - searchTop + 1];
        for (var y = searchTop; y <= searchBottom; y++)
        {
            var field = 0;
            var samples = 0;
            var step = Math.Max(1, (searchRight - searchLeft) / 260);
            for (var x = searchLeft; x <= searchRight; x += step)
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
            if (rowScores[index] < 0.43)
            {
                index++;
                continue;
            }

            var start = index;
            var sum = 0.0;
            while (index < rowScores.Length && rowScores[index] >= 0.43)
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
        var bandHeight = bandBottom - bandTop + 1;
        var firstGood = -1;
        var lastGood = -1;
        var gap = 0;
        var maximumGap = Math.Max(14, (int)Math.Round(bandHeight * 0.9));
        for (var x = searchLeft; x <= searchRight; x++)
        {
            var dark = 0;
            for (var y = bandTop; y <= bandBottom; y++)
            {
                if (IsTitleFieldPixel(bgra, stride, x, y))
                    dark++;
            }

            // Text and anti-aliased icon pixels interrupt the dark background, so a
            // column only needs a minority of field-colored pixels to remain inside the
            // same strip. This prevents the first glyph from terminating the field.
            var good = dark / (double)Math.Max(1, bandHeight) >= 0.22;
            if (good)
            {
                if (firstGood < 0)
                    firstGood = x;
                lastGood = x;
                gap = 0;
            }
            else if (firstGood >= 0)
            {
                gap++;
                if (gap > maximumGap && lastGood - firstGood >= panel.Width * 0.28)
                    break;
            }
        }

        if (firstGood < 0 || lastGood <= firstGood)
            return default;

        var region = new ScannerDetectedRegion(
            firstGood,
            bandTop,
            lastGood - firstGood + 1,
            bandHeight,
            Math.Clamp(bestAverage, 0, 1));
        return (region, Math.Clamp(bestAverage, 0, 1));
    }

    private static List<Component> FindBrightComponents(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        int left,
        int top,
        int right,
        int bottom)
    {
        var visited = new HashSet<int>();
        var components = new List<Component>();
        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsBrightNeutral(bgra, stride, x, y))
                continue;

            var component = FloodComponent(
                bgra,
                width,
                height,
                stride,
                x,
                y,
                left,
                top,
                right,
                bottom,
                visited,
                IsBrightNeutral);
            if (component.Area >= 2)
                components.Add(component);
        }

        return components
            .OrderBy(component => component.X)
            .ThenBy(component => component.Y)
            .ToList();
    }

    private static List<Component> MergeNearbyComponents(IReadOnlyList<Component> source, int maximumGap)
    {
        if (source.Count == 0)
            return [];

        var result = new List<Component>();
        foreach (var component in source.OrderBy(value => value.X).ThenBy(value => value.Y))
        {
            if (result.Count == 0)
            {
                result.Add(component);
                continue;
            }

            var previous = result[^1];
            var gap = component.X - (previous.X + previous.Width);
            var overlap = VerticalOverlapRatio(previous, component);
            if (gap is >= 0 && gap <= maximumGap && overlap >= 0.42)
            {
                var left = Math.Min(previous.X, component.X);
                var top = Math.Min(previous.Y, component.Y);
                var right = Math.Max(previous.X + previous.Width, component.X + component.Width);
                var bottom = Math.Max(previous.Y + previous.Height, component.Y + component.Height);
                result[^1] = new Component(
                    left,
                    top,
                    right - left,
                    bottom - top,
                    previous.Area + component.Area);
            }
            else
            {
                result.Add(component);
            }
        }

        return result;
    }

    private static double VerticalOverlapRatio(Component left, Component right)
    {
        var top = Math.Max(left.Y, right.Y);
        var bottom = Math.Min(left.Y + left.Height, right.Y + right.Height);
        var overlap = Math.Max(0, bottom - top);
        return overlap / (double)Math.Max(1, Math.Min(left.Height, right.Height));
    }

    private static bool IsTextLike(Component component, int fieldHeight)
    {
        if (component.Area < 3)
            return false;
        var minimumHeight = Math.Max(3, (int)Math.Round(fieldHeight * 0.28));
        var maximumHeight = Math.Max(minimumHeight, (int)Math.Round(fieldHeight * 1.45));
        var maximumWidth = Math.Max(8, (int)Math.Round(fieldHeight * 1.85));
        return component.Height >= minimumHeight &&
               component.Height <= maximumHeight &&
               component.Width <= maximumWidth;
    }

    private static double MagnifierMorphologyScore(
        ReadOnlySpan<byte> bgra,
        int stride,
        Component component)
    {
        var innerLeft = component.X + Math.Max(1, (int)Math.Round(component.Width * 0.30));
        var innerRight = component.X + Math.Max(1, (int)Math.Round(component.Width * 0.70));
        var innerTop = component.Y + Math.Max(1, (int)Math.Round(component.Height * 0.28));
        var innerBottom = component.Y + Math.Max(1, (int)Math.Round(component.Height * 0.68));
        var centerBright = BrightRatio(bgra, stride, innerLeft, innerTop, innerRight, innerBottom);
        var hollowCenter = 1.0 - centerBright;

        var edge = Math.Max(1, Math.Min(component.Width, component.Height) / 5);
        var topEdge = BrightRatio(
            bgra,
            stride,
            component.X + edge,
            component.Y,
            component.X + component.Width - edge - 1,
            component.Y + edge);
        var leftEdge = BrightRatio(
            bgra,
            stride,
            component.X,
            component.Y + edge,
            component.X + edge,
            component.Y + component.Height - edge - 1);
        var rightEdge = BrightRatio(
            bgra,
            stride,
            component.X + component.Width - edge - 1,
            component.Y + edge,
            component.X + component.Width - 1,
            component.Y + component.Height - edge - 1);
        var perimeter = Math.Clamp((topEdge + leftEdge + rightEdge) / 1.35, 0, 1);

        var handle = BrightRatio(
            bgra,
            stride,
            component.X + (int)Math.Round(component.Width * 0.58),
            component.Y + (int)Math.Round(component.Height * 0.58),
            component.X + component.Width - 1,
            component.Y + component.Height - 1);
        var handleScore = Math.Clamp(handle * 2.2, 0, 1);

        return Math.Clamp(hollowCenter * 0.45 + perimeter * 0.35 + handleScore * 0.20, 0, 1);
    }

    private static double BrightRatio(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int top,
        int right,
        int bottom)
    {
        if (right < left || bottom < top)
            return 0;

        var bright = 0;
        var total = 0;
        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            total++;
            if (IsBrightNeutral(bgra, stride, x, y))
                bright++;
        }
        return total == 0 ? 0 : bright / (double)total;
    }

    private static double Median(IEnumerable<int> values)
    {
        var ordered = values.Order().ToArray();
        if (ordered.Length == 0)
            return 0;
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2.0;
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
        return gray >= 82 && max - min <= 52;
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
        return gray is >= 12 and <= 68 && max - min <= 28;
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
