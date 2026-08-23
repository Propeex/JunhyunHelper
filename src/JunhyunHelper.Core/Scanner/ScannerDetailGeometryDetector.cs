namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Scanner structural proposal generator for Tarkov item inspection windows.
/// Geometry deliberately proposes plausible rectangles; it is not allowed to prove
/// that a rectangle is an inspect window. Production acceptance is owned by the Desktop
/// semantic header lock (close X + magnifier + header/title evidence) and then OCR/catalog
/// identity. Overlapping but materially different rectangles therefore survive until the
/// semantic stage instead of being removed by IoU alone.
/// </summary>
public static class ScannerDetailGeometryDetector
{
    private const double PrimarySemanticFloor = 0.34;
    private const int DefaultCandidateLimit = 12;
    private const double MinimumPlausibleAspect = 0.62;
    private const double MaximumPlausibleAspect = 2.60;

    public static IReadOnlyList<ScannerDetectedCandidate> FindCandidates(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        int maximum = DefaultCandidateLimit)
    {
        if (width < 150 || height < 110 || stride < width * 4)
            return [];
        if (bgraPixels.Length < stride * height)
            return [];

        maximum = Math.Clamp(maximum, 1, 24);
        var pixels = ReadPixels(bgraPixels, width, height, stride);
        var redComponents = FindRedComponents(pixels);
        var all = new List<ScannerDetectedCandidate>();

        // A red close control is a useful proposal seed, but still not semantic proof.
        // The Desktop header lock independently verifies the actual Tarkov close glyph.
        foreach (var component in redComponents)
        {
            var candidate = ScoreRedCloseCandidate(pixels, component);
            if (candidate is { Score: >= PrimarySemanticFloor })
                all.Add(candidate.Value with { Reason = "RED_X_CANDIDATE" });
        }

        all.AddRange(FindRectangleCandidates(pixels, redComponents));
        return DeduplicateCandidates(all, maximum);
    }

    /// <summary>
    /// Compatibility helper for geometry-only tests/diagnostics. Runtime recognition should use
    /// <see cref="FindCandidates"/> and semantically verify candidates with the inspect-header lock.
    /// </summary>
    public static ScannerDetectedRegion? Detect(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        bool extendedScaleSearch)
    {
        _ = extendedScaleSearch;
        var candidate = FindCandidates(bgraPixels, width, height, stride, 1).FirstOrDefault();
        return candidate.Window.Width <= 0
            ? null
            : candidate.Window;
    }

    public static ScannerDetectedRegion GetTitleRegion(ScannerDetectedRegion panel)
    {
        var titleX = panel.X + (int)Math.Round(panel.Width * 0.032);
        var titleY = Math.Max(0, panel.Y - 1);
        var titleWidth = Math.Max(1, (int)Math.Round(panel.Width * 0.64));
        var titleHeight = Math.Max(12, (int)Math.Round(panel.Height * 0.052));

        titleWidth = Math.Min(titleWidth, Math.Max(1, panel.X + panel.Width - titleX));
        titleHeight = Math.Min(titleHeight, Math.Max(1, panel.Y + panel.Height - titleY));

        return new ScannerDetectedRegion(titleX, titleY, titleWidth, titleHeight, panel.Score);
    }

    private static PixelBuffer ReadPixels(ReadOnlySpan<byte> source, int width, int height, int stride)
    {
        var gray = new byte[width * height];
        var red = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            var sourceRow = y * stride;
            var targetRow = y * width;
            for (var x = 0; x < width; x++)
            {
                var offset = sourceRow + x * 4;
                var b = source[offset];
                var g = source[offset + 1];
                var r = source[offset + 2];
                gray[targetRow + x] = (byte)((77 * r + 150 * g + 29 * b) >> 8);

                if (r >= 45 && r - g >= 20 && r - b >= 20)
                    red[targetRow + x] = 1;
            }
        }

        return new PixelBuffer(width, height, gray, red);
    }

    private static List<RedComponent> FindRedComponents(PixelBuffer pixels)
    {
        var components = new List<RedComponent>();
        var visited = new byte[pixels.Red.Length];
        var queue = new Queue<int>();

        for (var y = 0; y < pixels.Height; y++)
        {
            var row = y * pixels.Width;
            for (var x = 0; x < pixels.Width; x++)
            {
                var start = row + x;
                if (pixels.Red[start] == 0 || visited[start] != 0)
                    continue;

                visited[start] = 1;
                queue.Enqueue(start);
                var minX = x;
                var maxX = x;
                var minY = y;
                var maxY = y;
                var area = 0;

                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    var cy = index / pixels.Width;
                    var cx = index - cy * pixels.Width;
                    area++;
                    minX = Math.Min(minX, cx);
                    maxX = Math.Max(maxX, cx);
                    minY = Math.Min(minY, cy);
                    maxY = Math.Max(maxY, cy);

                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var ny = cy + dy;
                        if (ny < 0 || ny >= pixels.Height)
                            continue;

                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            var nx = cx + dx;
                            if (nx < 0 || nx >= pixels.Width)
                                continue;

                            var neighbor = ny * pixels.Width + nx;
                            if (pixels.Red[neighbor] == 0 || visited[neighbor] != 0)
                                continue;
                            visited[neighbor] = 1;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                var componentWidth = maxX - minX + 1;
                var componentHeight = maxY - minY + 1;
                var aspect = componentHeight <= 0 ? 0 : (double)componentWidth / componentHeight;
                if (componentWidth is >= 8 and <= 70 &&
                    componentHeight is >= 5 and <= 42 &&
                    area >= 18 &&
                    aspect is >= 0.7 and <= 4.5)
                {
                    components.Add(new RedComponent(minX, minY, componentWidth, componentHeight, area));
                }
            }
        }

        return components;
    }

    private static ScannerDetectedCandidate? ScoreRedCloseCandidate(PixelBuffer pixels, RedComponent close)
    {
        var edgeThreshold = Math.Max(9, Math.Min(18, (int)Math.Round(Math.Min(pixels.Width, pixels.Height) / 45.0)));
        var y0 = Math.Max(0, close.Y - 2);

        var rightXStart = Math.Max(1, close.X + close.Width - 3);
        var rightXEnd = Math.Min(pixels.Width - 2, close.X + close.Width + 8);
        var rightX = -1;
        var rightScore = -1;
        for (var x = rightXStart; x <= rightXEnd; x++)
        {
            var score = CountVerticalEdges(pixels, x, y0, pixels.Height - 1, edgeThreshold);
            if (score <= rightScore)
                continue;
            rightScore = score;
            rightX = x;
        }
        if (rightX < 0)
            return null;

        var firstEdge = -1;
        for (var offset = 0; offset <= 15 && y0 + offset < pixels.Height; offset++)
        {
            if (EdgeX(pixels, rightX, y0 + offset) <= edgeThreshold)
                continue;
            firstEdge = y0 + offset;
            break;
        }
        if (firstEdge < 0)
            return null;

        var lastEdge = firstEdge;
        var lastSeenEdge = firstEdge;
        for (var y = firstEdge + 1; y < pixels.Height; y++)
        {
            if (EdgeX(pixels, rightX, y) > edgeThreshold)
            {
                if (y - lastSeenEdge > 7)
                    break;
                lastSeenEdge = y;
                lastEdge = y;
            }
            else if (y - lastSeenEdge > 7)
            {
                break;
            }
        }

        var topGuess = Math.Max(0, close.Y - 4);
        var bottomGuess = lastEdge;
        var guessedHeight = bottomGuess - topGuess + 1;
        if (guessedHeight < 110)
            return null;

        // Detail-window height varies substantially with item/stat panels. Use only a
        // broad impossible-shape guard here; aspect is no longer an identity condition.
        var minWidth = (int)Math.Round(guessedHeight * MinimumPlausibleAspect);
        var maxWidth = (int)Math.Round(guessedHeight * MaximumPlausibleAspect);
        var leftXStart = Math.Max(1, rightX - maxWidth);
        var leftXEnd = Math.Max(1, rightX - minWidth);
        if (leftXStart > leftXEnd)
            return null;

        var leftX = -1;
        var leftScore = double.MinValue;
        for (var x = leftXStart; x <= leftXEnd; x++)
        {
            var count = CountVerticalEdges(pixels, x, topGuess, bottomGuess, edgeThreshold);
            var continuity = (double)count / Math.Max(1, guessedHeight);
            var aspect = (double)(rightX - x + 1) / Math.Max(1, guessedHeight);
            var plausibility = AspectPlausibility(aspect);
            var score = continuity * 0.90 + plausibility * 0.10;
            if (score <= leftScore)
                continue;
            leftScore = score;
            leftX = x;
        }
        if (leftX < 0)
            return null;

        var provisionalWidth = rightX - leftX + 1;
        var topY = topGuess;
        var topScore = -1.0;
        var topStart = Math.Max(1, close.Y - 7);
        var topEnd = Math.Min(pixels.Height - 2, close.Y + 4);
        for (var y = topStart; y <= topEnd; y++)
        {
            var continuity = (double)CountHorizontalEdges(pixels, y, leftX, rightX, edgeThreshold) /
                             Math.Max(1, provisionalWidth);
            if (continuity <= topScore)
                continue;
            topScore = continuity;
            topY = y;
        }

        var bottomY = bottomGuess;
        var bottomScore = -1.0;
        var bottomStart = Math.Max(topY + 100, bottomGuess - 10);
        var bottomEnd = Math.Min(pixels.Height - 2, bottomGuess + 10);
        for (var y = bottomStart; y <= bottomEnd; y++)
        {
            var continuity = (double)CountHorizontalEdges(pixels, y, leftX, rightX, edgeThreshold) /
                             Math.Max(1, provisionalWidth);
            if (continuity <= bottomScore)
                continue;
            bottomScore = continuity;
            bottomY = y;
        }

        var windowWidth = rightX - leftX + 1;
        var windowHeight = bottomY - topY + 1;
        if (windowWidth < 150 || windowHeight < 110)
            return null;

        var aspectRatio = (double)windowWidth / windowHeight;
        if (aspectRatio is < MinimumPlausibleAspect or > MaximumPlausibleAspect)
            return null;

        var leftContinuity = (double)CountVerticalEdges(pixels, leftX, topY, bottomY, edgeThreshold) / windowHeight;
        var rightContinuity = (double)CountVerticalEdges(pixels, rightX, topY, bottomY, edgeThreshold) / windowHeight;
        var topContinuity = (double)CountHorizontalEdges(pixels, topY, leftX, rightX, edgeThreshold) / windowWidth;
        var bottomContinuity = (double)CountHorizontalEdges(pixels, bottomY, leftX, rightX, edgeThreshold) / windowWidth;
        var borderScore = (leftContinuity + rightContinuity + topContinuity + bottomContinuity) / 4.0;
        var aspectScore = AspectPlausibility(aspectRatio);
        var provisionalWindow = new ScannerDetectedRegion(leftX, topY, windowWidth, windowHeight, 0);
        var darkness = SampleInteriorDarkness(pixels, provisionalWindow);
        var redFill = Math.Clamp(close.Area / (double)Math.Max(1, close.Width * close.Height), 0, 1);
        var finalScore =
            borderScore * 0.70 +
            darkness * 0.12 +
            redFill * 0.12 +
            aspectScore * 0.06;

        var window = provisionalWindow with { Score = finalScore };
        return new ScannerDetectedCandidate(
            window,
            GetTitleRegion(window),
            new ScannerDetectedRegion(close.X, close.Y, close.Width, close.Height, finalScore),
            finalScore >= 0.70 ? "STRUCTURE_MATCH" : "LOW_STRUCTURE_CONFIDENCE");
    }

    private static IReadOnlyList<ScannerDetectedCandidate> FindRectangleCandidates(
        PixelBuffer pixels,
        IReadOnlyList<RedComponent> redComponents)
    {
        var threshold = Math.Max(8, Math.Min(16, (int)Math.Round(Math.Min(pixels.Width, pixels.Height) / 55.0)));
        var verticalProjection = new int[pixels.Width];
        var horizontalProjection = new int[pixels.Height];

        for (var x = 1; x < pixels.Width - 1; x++)
        {
            var count = 0;
            for (var y = 1; y < pixels.Height - 1; y++)
                if (EdgeX(pixels, x, y) > threshold)
                    count++;
            verticalProjection[x] = count;
        }

        for (var y = 1; y < pixels.Height - 1; y++)
        {
            var count = 0;
            for (var x = 1; x < pixels.Width - 1; x++)
                if (EdgeY(pixels, x, y) > threshold)
                    count++;
            horizontalProjection[y] = count;
        }

        var verticalTake = Math.Min(38, Math.Max(12, pixels.Width / 35));
        var horizontalTake = Math.Min(38, Math.Max(12, pixels.Height / 25));
        var xs = Enumerable.Range(1, Math.Max(1, pixels.Width - 2))
            .OrderByDescending(x => verticalProjection[x])
            .Take(verticalTake)
            .OrderBy(x => x)
            .ToArray();
        var ys = Enumerable.Range(1, Math.Max(1, pixels.Height - 2))
            .OrderByDescending(y => horizontalProjection[y])
            .Take(horizontalTake)
            .OrderBy(y => y)
            .ToArray();

        var approximated = new List<StructureCandidate>();
        var minWidth = Math.Max(120, (int)Math.Round(pixels.Width * 0.10));
        var minHeight = Math.Max(90, (int)Math.Round(pixels.Height * 0.08));
        var maxWidth = Math.Max(minWidth, (int)Math.Round(pixels.Width * 0.94));
        var maxHeight = Math.Max(minHeight, (int)Math.Round(pixels.Height * 0.94));

        for (var xi = 0; xi < xs.Length; xi++)
        {
            for (var xj = xi + 1; xj < xs.Length; xj++)
            {
                var left = xs[xi];
                var right = xs[xj];
                var rectWidth = right - left + 1;
                if (rectWidth < minWidth || rectWidth > maxWidth)
                    continue;

                var leftProjection = (double)verticalProjection[left] / Math.Max(1, pixels.Height);
                var rightProjection = (double)verticalProjection[right] / Math.Max(1, pixels.Height);

                for (var yi = 0; yi < ys.Length; yi++)
                {
                    for (var yj = yi + 1; yj < ys.Length; yj++)
                    {
                        var top = ys[yi];
                        var bottom = ys[yj];
                        var rectHeight = bottom - top + 1;
                        if (rectHeight < minHeight || rectHeight > maxHeight)
                            continue;

                        var aspect = (double)rectWidth / Math.Max(1, rectHeight);
                        if (aspect is < MinimumPlausibleAspect or > MaximumPlausibleAspect)
                            continue;

                        var topProjection = (double)horizontalProjection[top] / Math.Max(1, pixels.Width);
                        var bottomProjection = (double)horizontalProjection[bottom] / Math.Max(1, pixels.Width);
                        var aspectScore = AspectPlausibility(aspect);
                        var projectionScore = (leftProjection + rightProjection + topProjection + bottomProjection) / 4.0;

                        // Aspect is now a weak ranking hint only. Strong border evidence
                        // can therefore propose a tall/large inspect window for semantic
                        // close/magnifier validation instead of being rejected up front.
                        var approximate = projectionScore * 0.90 + aspectScore * 0.10;
                        if (approximate < 0.15)
                            continue;

                        var window = new ScannerDetectedRegion(left, top, rectWidth, rectHeight, approximate);
                        var darkness = SampleInteriorDarkness(pixels, window);
                        approximate *= 0.72 + darkness * 0.28;
                        approximated.Add(new StructureCandidate(window with { Score = approximate }, approximate));
                    }
                }
            }
        }

        if (approximated.Count == 0)
            return [];

        var results = new List<ScannerDetectedCandidate>();
        foreach (var candidate in approximated.OrderByDescending(c => c.ApproximateScore).Take(120))
        {
            var region = candidate.Region;
            var right = region.X + region.Width - 1;
            var bottom = region.Y + region.Height - 1;
            var leftContinuity = (double)CountVerticalEdges(pixels, region.X, region.Y, bottom, threshold) / Math.Max(1, region.Height);
            var rightContinuity = (double)CountVerticalEdges(pixels, right, region.Y, bottom, threshold) / Math.Max(1, region.Height);
            var topContinuity = (double)CountHorizontalEdges(pixels, region.Y, region.X, right, threshold) / Math.Max(1, region.Width);
            var bottomContinuity = (double)CountHorizontalEdges(pixels, bottom, region.X, right, threshold) / Math.Max(1, region.Width);
            var aspect = (double)region.Width / Math.Max(1, region.Height);
            var aspectScore = AspectPlausibility(aspect);
            var darkness = SampleInteriorDarkness(pixels, region);

            var bestRedProximity = 0.0;
            RedComponent? roughClose = null;
            foreach (var red in redComponents)
            {
                var centerX = red.X + red.Width / 2;
                var centerY = red.Y + red.Height / 2;
                var expectedX = region.X + region.Width;
                var expectedY = region.Y;
                var xTolerance = Math.Max(20.0, region.Width * 0.08);
                var yTolerance = Math.Max(20.0, Math.Min(region.Height * 0.08, 70.0));
                var dx = Math.Abs(centerX - expectedX);
                var dy = Math.Abs(centerY - expectedY);
                if (dx > xTolerance || dy > yTolerance)
                    continue;

                var proximity =
                    Math.Max(0, 1.0 - dx / xTolerance) * 0.60 +
                    Math.Max(0, 1.0 - dy / yTolerance) * 0.40;
                if (proximity <= bestRedProximity)
                    continue;
                bestRedProximity = proximity;
                roughClose = red;
            }

            var redBonus = roughClose.HasValue ? 0.08 * bestRedProximity : 0.0;
            var borderScore = (leftContinuity + rightContinuity + topContinuity + bottomContinuity) / 4.0;
            var finalScore =
                borderScore * 0.72 +
                darkness * 0.15 +
                aspectScore * 0.05 +
                redBonus;
            if (finalScore < PrimarySemanticFloor)
                continue;

            var window = region with { Score = finalScore };
            var closeHint = roughClose is { } closeComponent
                ? new ScannerDetectedRegion(
                    closeComponent.X,
                    closeComponent.Y,
                    closeComponent.Width,
                    closeComponent.Height,
                    bestRedProximity)
                : default;
            results.Add(new ScannerDetectedCandidate(
                window,
                GetTitleRegion(window),
                closeHint,
                roughClose.HasValue ? "RECTANGLE_RED_X_HINT" : "RECTANGLE_CANDIDATE"));
        }

        // Preserve a wider internal pool so outer red-X and rectangle proposals can be
        // merged without one geometry family erasing the other. The public caller's
        // requested maximum is still enforced by the final DeduplicateCandidates call.
        return DeduplicateCandidates(results, 24);
    }

    private static double SampleInteriorDarkness(PixelBuffer pixels, ScannerDetectedRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0)
            return 0;

        var dark = 0;
        var total = 0;
        for (var gy = 1; gy <= 5; gy++)
        {
            var y = Math.Clamp(region.Y + region.Height * gy / 6, 0, pixels.Height - 1);
            for (var gx = 1; gx <= 7; gx++)
            {
                var x = Math.Clamp(region.X + region.Width * gx / 8, 0, pixels.Width - 1);
                if (pixels.Gray[y * pixels.Width + x] <= 90)
                    dark++;
                total++;
            }
        }
        return total <= 0 ? 0 : (double)dark / total;
    }

    private static double AspectPlausibility(double aspect)
    {
        if (aspect is < MinimumPlausibleAspect or > MaximumPlausibleAspect)
            return 0;

        // A broad, deliberately weak prior. 1.3 remains a useful ordering hint from
        // historical captures but no longer acts as a hidden acceptance condition.
        return Math.Max(
            0.25,
            Math.Exp(-Math.Pow((aspect - 1.30) / 1.10, 2.0)));
    }

    private static IReadOnlyList<ScannerDetectedCandidate> DeduplicateCandidates(
        IEnumerable<ScannerDetectedCandidate> candidates,
        int maximum)
    {
        var result = new List<ScannerDetectedCandidate>();
        foreach (var candidate in candidates
                     .Where(c => c.Window.Width > 0 && c.Window.Height > 0)
                     .OrderByDescending(CandidateOrderingScore)
                     .ThenByDescending(c => c.Score))
        {
            // IoU alone is intentionally insufficient. A correct inspect rectangle and
            // a wrong stash/inventory rectangle can overlap heavily while having
            // materially different top/bottom/side ownership. Remove only edge-jitter
            // duplicates that represent effectively the same proposal.
            if (result.Any(existing => AreNearDuplicate(existing.Window, candidate.Window)))
                continue;

            result.Add(candidate);
            if (result.Count >= maximum)
                break;
        }
        return result;
    }

    private static double CandidateOrderingScore(ScannerDetectedCandidate candidate)
    {
        var roughCloseHint = candidate.CloseButton.Width > 0 && candidate.CloseButton.Height > 0
            ? 0.05
            : 0.0;
        return candidate.Score + roughCloseHint;
    }

    private static bool AreNearDuplicate(ScannerDetectedRegion left, ScannerDetectedRegion right)
    {
        var scale = Math.Max(
            1,
            Math.Min(
                Math.Min(left.Width, right.Width),
                Math.Min(left.Height, right.Height)));
        var tolerance = Math.Clamp((int)Math.Round(scale * 0.018), 5, 16);

        var leftRight = left.X + left.Width;
        var rightRight = right.X + right.Width;
        var leftBottom = left.Y + left.Height;
        var rightBottom = right.Y + right.Height;

        return Math.Abs(left.X - right.X) <= tolerance &&
               Math.Abs(left.Y - right.Y) <= tolerance &&
               Math.Abs(leftRight - rightRight) <= tolerance &&
               Math.Abs(leftBottom - rightBottom) <= tolerance;
    }

    private static int EdgeX(PixelBuffer pixels, int x, int y)
    {
        if (x <= 0 || x >= pixels.Width - 1 || y < 0 || y >= pixels.Height)
            return 0;
        var row = y * pixels.Width;
        return Math.Abs(pixels.Gray[row + x + 1] - pixels.Gray[row + x - 1]);
    }

    private static int EdgeY(PixelBuffer pixels, int x, int y)
    {
        if (y <= 0 || y >= pixels.Height - 1 || x < 0 || x >= pixels.Width)
            return 0;
        return Math.Abs(pixels.Gray[(y + 1) * pixels.Width + x] - pixels.Gray[(y - 1) * pixels.Width + x]);
    }

    private static int CountVerticalEdges(PixelBuffer pixels, int x, int y0, int y1, int threshold)
    {
        y0 = Math.Max(0, y0);
        y1 = Math.Min(pixels.Height - 1, y1);
        var count = 0;
        for (var y = y0; y <= y1; y++)
            if (EdgeX(pixels, x, y) > threshold)
                count++;
        return count;
    }

    private static int CountHorizontalEdges(PixelBuffer pixels, int y, int x0, int x1, int threshold)
    {
        x0 = Math.Max(0, x0);
        x1 = Math.Min(pixels.Width - 1, x1);
        var count = 0;
        for (var x = x0; x <= x1; x++)
            if (EdgeY(pixels, x, y) > threshold)
                count++;
        return count;
    }

    private sealed record PixelBuffer(int Width, int Height, byte[] Gray, byte[] Red);
    private readonly record struct RedComponent(int X, int Y, int Width, int Height, int Area);
    private readonly record struct StructureCandidate(ScannerDetectedRegion Region, double ApproximateScore);
}

public readonly record struct ScannerDetectedRegion(
    int X,
    int Y,
    int Width,
    int Height,
    double Score);

public readonly record struct ScannerDetectedCandidate(
    ScannerDetectedRegion Window,
    ScannerDetectedRegion Title,
    ScannerDetectedRegion CloseButton,
    string Reason)
{
    public double Score => Window.Score;
}
