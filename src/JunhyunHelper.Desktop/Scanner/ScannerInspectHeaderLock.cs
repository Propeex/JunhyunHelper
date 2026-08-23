using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Locks the Tarkov inspect-title ROI to the actual header frame observed in live captures.
/// Horizontal ownership is structural: long neutral top border -> red close control ->
/// fixed left search-icon lane -> title text. Title glyph segmentation never owns the
/// left crop boundary.
/// </summary>
internal static class ScannerInspectHeaderLock
{
    private const string LockedReason = "HEADER_FRAME_LOCKED";

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

        if (!IsValid(panel, width, height) ||
            stride < width * 4 ||
            bgra.Length < stride * height)
        {
            return Failure(fallback, default, "HEADER_GEOMETRY_INVALID");
        }

        var close = FindRedClose(bgra, width, height, stride, panel);
        if (close.Width <= 0)
            return Failure(fallback, default, "HEADER_CLOSE_NOT_LOCKED");

        var frame = FindHeaderFrame(bgra, width, height, stride, panel, close);
        if (!frame.IsValid)
            return Failure(fallback, close, "HEADER_FRAME_NOT_LOCKED", close.Score * 0.20);

        var magnifier = FindMagnifierCore(bgra, width, height, stride, frame, close);
        if (magnifier.Width <= 0)
        {
            return Failure(
                HeaderFallbackTitle(frame, close, fallback, width, height),
                close,
                "HEADER_MAGNIFIER_NOT_LOCKED",
                close.Score * 0.18 + frame.Score * 0.32);
        }

        var fieldScore = MeasureDarkTitleField(bgra, stride, frame, close);
        if (fieldScore < 0.58)
        {
            return new ScannerTitleAnchorRefinement(
                HeaderFallbackTitle(frame, close, fallback, width, height),
                magnifier,
                close,
                Math.Clamp(close.Score * 0.18 + frame.Score * 0.30 + magnifier.Score * 0.30 + fieldScore * 0.12, 0, 1),
                "HEADER_FIELD_NOT_LOCKED");
        }

        var iconGap = Math.Max(3, (int)Math.Round(close.Height * 0.26));
        var closeGap = Math.Max(4, (int)Math.Round(close.Height * 0.32));
        var titleLeft = magnifier.X + magnifier.Width + iconGap;
        var titleRight = close.X - closeGap;
        if (titleRight - titleLeft < Math.Max(24, close.Height * 3))
        {
            return new ScannerTitleAnchorRefinement(
                fallback,
                magnifier,
                close,
                0,
                "HEADER_TITLE_SPAN_INVALID");
        }

        // Live 2048x1280 captures consistently place title glyphs in the first ~24 px
        // below the neutral top frame. Keep background margin for WinRT OCR, but stop
        // before the category/weight row underneath the title.
        var titleTop = frame.Top + Math.Max(2, (int)Math.Round(close.Height * 0.12));
        var titleBottom = frame.Top + Math.Max(19, (int)Math.Round(close.Height * 1.42));
        titleBottom = Math.Min(titleBottom, height - 1);

        var title = Clamp(
            new ScannerDetectedRegion(
                titleLeft,
                titleTop,
                titleRight - titleLeft,
                Math.Max(1, titleBottom - titleTop + 1),
                fallback.Score),
            width,
            height);

        var textEvidence = MeasureTextEvidence(bgra, stride, title, close.Height);
        if (textEvidence < 0.22)
        {
            return new ScannerTitleAnchorRefinement(
                title,
                magnifier,
                close,
                Math.Clamp(close.Score * 0.16 + frame.Score * 0.30 + magnifier.Score * 0.30 + fieldScore * 0.12 + textEvidence * 0.12, 0, 1),
                "HEADER_TEXT_NOT_LOCKED");
        }

        var score = Math.Clamp(
            close.Score * 0.18 +
            frame.Score * 0.30 +
            magnifier.Score * 0.32 +
            fieldScore * 0.12 +
            textEvidence * 0.08,
            0,
            1);

        if (score < 0.68)
        {
            return new ScannerTitleAnchorRefinement(
                title,
                magnifier,
                close,
                score,
                "HEADER_FRAME_WEAK");
        }

        return new ScannerTitleAnchorRefinement(
            title,
            magnifier,
            close,
            score,
            LockedReason);
    }

    private static ScannerDetectedRegion FindRedClose(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel)
    {
        var left = Math.Clamp(panel.X + (int)Math.Round(panel.Width * 0.72), 0, width - 1);
        var top = Math.Clamp(panel.Y - Math.Max(10, panel.Height / 65), 0, height - 1);
        var right = Math.Clamp(panel.X + panel.Width + Math.Max(8, panel.Width / 80), left, width - 1);
        var bottom = Math.Clamp(panel.Y + Math.Max(28, (int)Math.Round(panel.Height * 0.10)), top, height - 1);
        var visited = new HashSet<int>();
        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;

        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsRed(bgra, stride, x, y))
                continue;

            var component = FloodRed(
                bgra, width, height, stride,
                x, y, left, top, right, bottom, visited);
            if (component.Area < 16 ||
                component.Width is < 7 or > 55 ||
                component.Height is < 5 or > 36)
            {
                continue;
            }

            var aspect = component.Width / (double)Math.Max(1, component.Height);
            if (aspect is < 0.70 or > 2.25)
                continue;

            var template = ScannerHeaderIconTemplateMatcher.CloseScore(
                bgra, stride, component.X, component.Y, component.Width, component.Height);
            if (template < 0.46)
                continue;

            var expectedRight = panel.X + panel.Width;
            var rightScore = Math.Max(
                0,
                1.0 - Math.Abs(expectedRight - (component.X + component.Width)) /
                Math.Max(16.0, panel.Width * 0.08));
            var topScore = Math.Max(
                0,
                1.0 - Math.Abs(component.Y - panel.Y) /
                Math.Max(10.0, panel.Height * 0.065));
            var compact = Math.Min(component.Width, component.Height) /
                          (double)Math.Max(component.Width, component.Height);
            var fill = Math.Clamp(
                component.Area / (double)Math.Max(1, component.Width * component.Height),
                0,
                1);
            var score =
                template * 0.44 +
                rightScore * 0.31 +
                topScore * 0.17 +
                compact * 0.04 +
                fill * 0.04;
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

        return bestScore >= 0.60 ? best : default;
    }

    private static HeaderFrame FindHeaderFrame(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        ScannerDetectedRegion panel,
        ScannerDetectedRegion close)
    {
        var searchLeft = Math.Clamp(
            panel.X - Math.Max(42, (int)Math.Round(panel.Width * 0.09)),
            0,
            width - 1);
        var searchRight = Math.Clamp(
            close.X + close.Width + Math.Max(10, (int)Math.Round(close.Height * 0.70)),
            searchLeft + 1,
            width - 1);
        var searchTop = Math.Clamp(
            close.Y - Math.Max(13, (int)Math.Round(close.Height * 0.95)),
            0,
            height - 1);
        var searchBottom = Math.Clamp(
            close.Y + Math.Max(2, (int)Math.Round(close.Height * 0.16)),
            searchTop,
            height - 1);
        var maximumGap = Math.Max(2, close.Height / 5);
        var minimumLength = Math.Max(130, (int)Math.Round(panel.Width * 0.56));

        var best = HeaderFrame.Empty;
        var bestScore = double.MinValue;
        for (var y = searchTop; y <= searchBottom; y++)
        {
            var runs = FindHeaderRuns(
                bgra,
                stride,
                searchLeft,
                searchRight,
                y,
                maximumGap);
            foreach (var run in runs)
            {
                if (run.Length < minimumLength || run.Density < 0.80)
                    continue;

                var runRight = run.Left + run.Length - 1;
                var expectedRight = close.X + close.Width + (int)Math.Round(close.Height * 0.30);
                var rightScore = Math.Max(
                    0,
                    1.0 - Math.Abs(runRight - expectedRight) /
                    Math.Max(20.0, panel.Width * 0.065));
                var leftScore = Math.Max(
                    0,
                    1.0 - Math.Abs(run.Left - panel.X) /
                    Math.Max(28.0, panel.Width * 0.12));
                var lengthScore = Math.Clamp(run.Length / (double)Math.Max(1, panel.Width), 0, 1);
                var yExpected = close.Y - Math.Max(4, (int)Math.Round(close.Height * 0.30));
                var yScore = Math.Max(
                    0,
                    1.0 - Math.Abs(y - yExpected) /
                    Math.Max(5.0, close.Height * 0.65));
                var score =
                    run.Density * 0.38 +
                    lengthScore * 0.20 +
                    rightScore * 0.18 +
                    leftScore * 0.14 +
                    yScore * 0.10;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = new HeaderFrame(
                    run.Left,
                    y,
                    runRight,
                    Math.Clamp(score, 0, 1));
            }
        }

        return bestScore >= 0.74 ? best : HeaderFrame.Empty;
    }

    private static IReadOnlyList<HeaderRun> FindHeaderRuns(
        ReadOnlySpan<byte> bgra,
        int stride,
        int left,
        int right,
        int y,
        int maximumGap)
    {
        var result = new List<HeaderRun>();
        var start = -1;
        var lastGood = -1;
        var goodCount = 0;
        var gap = 0;

        void Complete()
        {
            if (start < 0 || lastGood < start)
                return;
            var length = lastGood - start + 1;
            result.Add(new HeaderRun(
                start,
                length,
                goodCount / (double)Math.Max(1, length)));
        }

        for (var x = left; x <= right; x++)
        {
            if (IsHeaderTopBorderPixel(bgra, stride, x, y))
            {
                if (start < 0)
                {
                    start = x;
                    goodCount = 0;
                }
                lastGood = x;
                goodCount++;
                gap = 0;
                continue;
            }

            if (start < 0)
                continue;

            gap++;
            if (gap <= maximumGap)
                continue;

            Complete();
            start = -1;
            lastGood = -1;
            goodCount = 0;
            gap = 0;
        }

        Complete();
        return result;
    }

    private static ScannerDetectedRegion FindMagnifierCore(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride,
        HeaderFrame frame,
        ScannerDetectedRegion close)
    {
        var scale = Math.Clamp(close.Height / 17.0, 0.55, 1.85);
        var expectedX = frame.Left + (int)Math.Round(12.0 * scale);
        var expectedY = frame.Top + (int)Math.Round(7.0 * scale);
        var expectedSize = Math.Clamp((int)Math.Round(13.0 * scale), 7, 24);
        var offsetRadius = Math.Max(2, (int)Math.Ceiling(2.5 * scale));
        var sizeRadius = Math.Max(1, (int)Math.Ceiling(1.5 * scale));

        var best = default(ScannerDetectedRegion);
        var bestScore = double.MinValue;
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

                // Hard lane bound: no title glyph to the right is allowed to enter the
                // magnifier candidate pool even if it looks ring-like.
                var laneRight = frame.Left + (int)Math.Ceiling(29.0 * scale);
                if (x < frame.Left + Math.Max(3, (int)Math.Floor(5.0 * scale)) ||
                    x + size > laneRight)
                {
                    continue;
                }

                var template = ScannerHeaderIconTemplateMatcher.MagnifierScore(
                    bgra, stride, x, y, size);
                if (template < 0.54)
                    continue;

                var xScore = Math.Max(0, 1.0 - Math.Abs(x - expectedX) / Math.Max(3.0, 3.2 * scale));
                var yScore = Math.Max(0, 1.0 - Math.Abs(y - expectedY) / Math.Max(3.0, 3.0 * scale));
                var sizeScore = Math.Max(0, 1.0 - Math.Abs(size - expectedSize) / Math.Max(2.0, 2.5 * scale));
                var location = xScore * 0.48 + yScore * 0.34 + sizeScore * 0.18;
                var score = template * 0.72 + location * 0.28;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                best = new ScannerDetectedRegion(x, y, size, size, Math.Clamp(score, 0, 1));
            }
        }

        return bestScore >= 0.66 ? best : default;
    }

    private static double MeasureDarkTitleField(
        ReadOnlySpan<byte> bgra,
        int stride,
        HeaderFrame frame,
        ScannerDetectedRegion close)
    {
        var left = frame.Left + 2;
        var right = close.X - 3;
        var top = frame.Top + 2;
        var bottom = frame.Top + Math.Max(19, (int)Math.Round(close.Height * 1.48));
        if (right <= left || bottom <= top)
            return 0;

        var dark = 0;
        var total = 0;
        var stepX = Math.Max(1, (right - left) / 220);
        for (var y = top; y <= bottom; y += 2)
        for (var x = left; x <= right; x += stepX)
        {
            total++;
            if (IsTitleFieldPixel(bgra, stride, x, y))
                dark++;
        }

        return total == 0 ? 0 : dark / (double)total;
    }

    private static double MeasureTextEvidence(
        ReadOnlySpan<byte> bgra,
        int stride,
        ScannerDetectedRegion title,
        int closeHeight)
    {
        var scanRight = Math.Min(
            title.X + title.Width - 1,
            title.X + Math.Max(90, closeHeight * 22));
        var bright = 0;
        var total = 0;
        for (var y = title.Y; y < title.Y + title.Height; y++)
        for (var x = title.X; x <= scanRight; x++)
        {
            total++;
            if (IsBrightNeutral(bgra, stride, x, y))
                bright++;
        }

        if (bright < Math.Max(10, closeHeight))
            return 0;

        // Text is sparse by nature. Saturate once enough bright evidence exists rather
        // than treating large blank title-field area as a penalty.
        return Math.Clamp(bright / (double)Math.Max(24, closeHeight * 5), 0, 1);
    }

    private static ScannerDetectedRegion HeaderFallbackTitle(
        HeaderFrame frame,
        ScannerDetectedRegion close,
        ScannerDetectedRegion fallback,
        int width,
        int height)
    {
        var left = Math.Clamp(
            frame.Left + Math.Max(18, close.Height),
            0,
            width - 1);
        var right = Math.Clamp(
            close.X - Math.Max(4, close.Height / 3),
            left + 1,
            width - 1);
        var top = Math.Clamp(frame.Top + 2, 0, height - 1);
        var bottom = Math.Clamp(
            frame.Top + Math.Max(19, (int)Math.Round(close.Height * 1.42)),
            top,
            height - 1);
        return new ScannerDetectedRegion(
            left,
            top,
            right - left,
            Math.Max(1, bottom - top + 1),
            fallback.Score);
    }

    private static ScannerTitleAnchorRefinement Failure(
        ScannerDetectedRegion title,
        ScannerDetectedRegion close,
        string reason,
        double score = 0) =>
        new(title, default, close, Math.Clamp(score, 0, 1), reason);

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
        var result = new List<Component>();
        for (var y = top; y <= bottom; y++)
        for (var x = left; x <= right; x++)
        {
            var key = y * width + x;
            if (visited.Contains(key) || !IsBrightNeutral(bgra, stride, x, y))
                continue;

            var component = FloodBright(
                bgra, width, height, stride,
                x, y, left, top, right, bottom, visited);
            if (component.Area >= 2)
                result.Add(component);
        }
        return result;
    }

    private static Component FloodBright(
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
        HashSet<int> visited)
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
                if (x < left || x > right || y < top || y > bottom ||
                    x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                var key = y * width + x;
                if (visited.Contains(key) || !IsBrightNeutral(bgra, stride, x, y))
                    continue;
                visited.Add(key);
                queue.Enqueue((x, y));
            }
        }

        return new Component(minX, minY, maxX - minX + 1, maxY - minY + 1, area);
    }

    private static Component FloodRed(
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
        HashSet<int> visited)
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
                if (x < left || x > right || y < top || y > bottom ||
                    x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                var key = y * width + x;
                if (visited.Contains(key) || !IsRed(bgra, stride, x, y))
                    continue;
                visited.Add(key);
                queue.Enqueue((x, y));
            }
        }

        return new Component(minX, minY, maxX - minX + 1, maxY - minY + 1, area);
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
        var hollowCenter = 1.0 - BrightRatio(
            bgra, stride,
            innerLeft, innerTop, innerRight, innerBottom);

        var edge = Math.Max(1, Math.Min(component.Width, component.Height) / 5);
        var topEdge = BrightRatio(
            bgra, stride,
            component.X + edge,
            component.Y,
            component.X + component.Width - edge - 1,
            component.Y + edge);
        var leftEdge = BrightRatio(
            bgra, stride,
            component.X,
            component.Y + edge,
            component.X + edge,
            component.Y + component.Height - edge - 1);
        var rightEdge = BrightRatio(
            bgra, stride,
            component.X + component.Width - edge - 1,
            component.Y + edge,
            component.X + component.Width - 1,
            component.Y + component.Height - edge - 1);
        var perimeter = Math.Clamp((topEdge + leftEdge + rightEdge) / 1.35, 0, 1);
        var handle = BrightRatio(
            bgra, stride,
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

    private static bool IsHeaderTopBorderPixel(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray is >= 40 and <= 108 && max - min <= 36;
    }

    private static bool IsTitleFieldPixel(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var gray = (77 * r + 150 * g + 29 * b) >> 8;
        return gray is >= 10 and <= 74 && max - min <= 34;
    }

    private static bool IsBrightNeutral(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y)
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

    private static bool IsRed(
        ReadOnlySpan<byte> bgra,
        int stride,
        int x,
        int y)
    {
        var offset = y * stride + x * 4;
        var b = bgra[offset];
        var g = bgra[offset + 1];
        var r = bgra[offset + 2];
        return r >= 52 && r - g >= 20 && r - b >= 20;
    }

    private static bool IsValid(ScannerDetectedRegion region, int width, int height) =>
        region.Width > 0 && region.Height > 0 &&
        region.X >= 0 && region.Y >= 0 &&
        region.X < width && region.Y < height;

    private static ScannerDetectedRegion Clamp(
        ScannerDetectedRegion region,
        int width,
        int height)
    {
        var x = Math.Clamp(region.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(region.Y, 0, Math.Max(0, height - 1));
        var w = Math.Clamp(region.Width, 1, Math.Max(1, width - x));
        var h = Math.Clamp(region.Height, 1, Math.Max(1, height - y));
        return new ScannerDetectedRegion(x, y, w, h, region.Score);
    }

    private readonly record struct Component(int X, int Y, int Width, int Height, int Area);
    private readonly record struct HeaderRun(int Left, int Length, double Density);

    private readonly record struct HeaderFrame(int Left, int Top, int Right, double Score)
    {
        public static HeaderFrame Empty { get; } = new(0, 0, -1, 0);
        public bool IsValid => Right > Left;
    }
}
