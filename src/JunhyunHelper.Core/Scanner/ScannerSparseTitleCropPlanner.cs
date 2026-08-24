namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Plans an OCR-only trailing-background crop for a detail-title field. The semantic
/// title ROI remains authoritative and unchanged; this helper only reduces the amount
/// of empty dark canvas sent to OCR when visible title ink occupies a small fraction of
/// a very wide field. The left edge and full height are always preserved so the first
/// glyph cannot be trimmed by this planner.
/// </summary>
public static class ScannerSparseTitleCropPlanner
{
    public static bool TryPlan(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        out ScannerSparseTitleCropPlan plan)
    {
        plan = default;
        if (width < 80 || height < 8 || stride < width * 4 || bgraPixels.Length < stride * height)
            return false;

        Span<int> histogram = stackalloc int[256];
        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = row + x * 4;
                var gray = (77 * bgraPixels[offset + 2] +
                            150 * bgraPixels[offset + 1] +
                            29 * bgraPixels[offset]) >> 8;
                histogram[gray]++;
            }
        }

        var background = 0;
        var backgroundCount = -1;
        // Tarkov inspect-title fields are dark. Limiting the mode search prevents a
        // bright glyph color from becoming the estimated background on pathological
        // tiny images while still covering observed live title backgrounds.
        for (var value = 0; value <= 140; value++)
        {
            if (histogram[value] <= backgroundCount)
                continue;
            backgroundCount = histogram[value];
            background = value;
        }

        if (backgroundCount <= 0 || background > 110)
            return false;

        var threshold = Math.Clamp(Math.Max(55, background + 30), 55, 190);
        var minimumColumnPixels = Math.Max(2, height / 14);
        var strongColumnPixels = Math.Max(4, minimumColumnPixels * 2);
        var brightPerColumn = new int[width];
        var foregroundPixels = 0;

        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = row + x * 4;
                var gray = (77 * bgraPixels[offset + 2] +
                            150 * bgraPixels[offset + 1] +
                            29 * bgraPixels[offset]) >> 8;
                if (gray < threshold)
                    continue;

                brightPerColumn[x]++;
                foregroundPixels++;
            }
        }

        if (foregroundPixels < Math.Max(8, height / 2))
            return false;

        var activeColumns = 0;
        for (var x = 0; x < width; x++)
        {
            if (brightPerColumn[x] >= minimumColumnPixels)
                activeColumns++;
        }

        if (activeColumns < 2)
            return false;

        var rightmostInk = -1;
        for (var x = width - 1; x >= 0; x--)
        {
            if (brightPerColumn[x] < minimumColumnPixels)
                continue;

            var supportedNeighbors = 0;
            for (var neighbor = Math.Max(0, x - 2); neighbor <= Math.Min(width - 1, x + 2); neighbor++)
            {
                if (brightPerColumn[neighbor] >= minimumColumnPixels)
                    supportedNeighbors++;
            }

            // A thin final glyph such as lowercase l can occupy only one or two columns
            // but will contain many bright pixels vertically. Isolated low-energy specks
            // in the far-right blank region are deliberately ignored.
            if (supportedNeighbors >= 2 || brightPerColumn[x] >= strongColumnPixels)
            {
                rightmostInk = x;
                break;
            }
        }

        if (rightmostInk < 2)
            return false;

        var padding = Math.Max(8, (int)Math.Ceiling(height * 0.85));
        var minimumCropWidth = Math.Max(48, height * 2);
        var cropWidth = Math.Min(width, Math.Max(minimumCropWidth, rightmostInk + 1 + padding));
        var removedWidth = width - cropWidth;

        // Tight OCR is a fallback only when the title is genuinely sparse. Small trims
        // are not worth changing OCR geometry and could make long titles less stable.
        if (cropWidth > width * 0.70 || removedWidth < Math.Max(48, height * 2))
            return false;

        plan = new ScannerSparseTitleCropPlan(
            cropWidth,
            rightmostInk,
            foregroundPixels,
            activeColumns,
            cropWidth / (double)width,
            background,
            threshold);
        return true;
    }
}

public readonly record struct ScannerSparseTitleCropPlan(
    int CropWidth,
    int RightmostInkX,
    int ForegroundPixelCount,
    int ActiveColumnCount,
    double RetainedWidthRatio,
    int BackgroundLuminance,
    int ForegroundThreshold);
