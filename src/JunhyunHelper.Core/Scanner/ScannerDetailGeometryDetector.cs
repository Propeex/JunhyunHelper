namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Pure pixel-level detector for the centered Tarkov item-inspection window.
/// The detector is intentionally conservative: geometry only creates an OCR candidate;
/// the item is never accepted until the independent OCR matcher also passes its high
/// confidence and margin gates.
/// </summary>
public static class ScannerDetailGeometryDetector
{
    private const double CanonicalPanelWidthRatio = 674.0 / 1920.0;
    private const double CanonicalPanelHeightRatio = 672.0 / 1080.0;
    private const double CanonicalCenterYRatio = 500.0 / 1080.0;
    private const double MinimumScore = 14.0;

    private static readonly double[] GameWindowScales = [0.90, 0.95, 1.00, 1.05, 1.10];
    private static readonly double[] DisplayTestScales = [0.55, 0.65, 0.75, 0.85, 0.95, 1.00, 1.05, 1.15];

    public static ScannerDetectedRegion? Detect(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        bool extendedScaleSearch)
    {
        if (width < 640 || height < 360 || stride < width * 4)
            return null;
        if (bgraPixels.Length < stride * height)
            return null;

        var scales = extendedScaleSearch ? DisplayTestScales : GameWindowScales;
        ScannerDetectedRegion? best = null;

        foreach (var scale in scales)
        {
            var panelWidth = (int)Math.Round(width * CanonicalPanelWidthRatio * scale);
            var panelHeight = (int)Math.Round(height * CanonicalPanelHeightRatio * scale);
            if (panelWidth < 260 || panelHeight < 250 || panelWidth >= width * 0.78 || panelHeight >= height * 0.86)
                continue;

            var xRadius = extendedScaleSearch ? 0.12 : 0.07;
            var yRadius = extendedScaleSearch ? 0.12 : 0.07;
            var xStep = extendedScaleSearch ? 0.020 : 0.014;
            var yStep = extendedScaleSearch ? 0.018 : 0.014;

            for (var centerXRatio = 0.50 - xRadius; centerXRatio <= 0.50 + xRadius + 0.0001; centerXRatio += xStep)
            {
                for (var centerYRatio = CanonicalCenterYRatio - yRadius; centerYRatio <= CanonicalCenterYRatio + yRadius + 0.0001; centerYRatio += yStep)
                {
                    var x = (int)Math.Round(width * centerXRatio - panelWidth / 2.0);
                    var y = (int)Math.Round(height * centerYRatio - panelHeight / 2.0);
                    if (x < 5 || y < 5 || x + panelWidth + 5 >= width || y + panelHeight + 5 >= height)
                        continue;

                    var score = ScoreCandidate(bgraPixels, width, height, stride, x, y, panelWidth, panelHeight);
                    if (score < MinimumScore)
                        continue;
                    if (best is null || score > best.Value.Score)
                        best = new ScannerDetectedRegion(x, y, panelWidth, panelHeight, score);
                }
            }
        }

        return best;
    }

    public static ScannerDetectedRegion GetTitleRegion(ScannerDetectedRegion panel)
    {
        var x = panel.X + (int)Math.Round(panel.Width * 0.025);
        var y = panel.Y + (int)Math.Round(panel.Height * 0.010);
        var width = (int)Math.Round(panel.Width * 0.82);
        var height = (int)Math.Round(panel.Height * 0.075);
        return new ScannerDetectedRegion(x, y, Math.Max(80, width), Math.Max(28, height), panel.Score);
    }

    private static double ScoreCandidate(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int panelWidth,
        int panelHeight)
    {
        var sampleStepX = Math.Max(5, panelWidth / 95);
        var sampleStepY = Math.Max(5, panelHeight / 95);

        var top = HorizontalEdge(pixels, width, height, stride, x + 8, x + panelWidth - 8, y, sampleStepX);
        var bottom = HorizontalEdge(pixels, width, height, stride, x + 8, x + panelWidth - 8, y + panelHeight - 1, sampleStepX);
        var left = VerticalEdge(pixels, width, height, stride, x, y + 8, y + panelHeight - 8, sampleStepY);
        var right = VerticalEdge(pixels, width, height, stride, x + panelWidth - 1, y + 8, y + panelHeight - 8, sampleStepY);

        // A real inspect panel has a coherent outer frame. Requiring three useful
        // sides rejects most inventory-grid rectangles before OCR is considered.
        var usefulEdges = 0;
        if (top >= 8) usefulEdges++;
        if (bottom >= 7) usefulEdges++;
        if (left >= 7) usefulEdges++;
        if (right >= 7) usefulEdges++;
        if (usefulEdges < 3 || top < 8)
            return 0;

        var edgeMean = (top + bottom + left + right) / 4.0;
        var toneContrast = PanelToneContrast(pixels, width, height, stride, x, y, panelWidth, panelHeight);
        var closeGlyph = CloseGlyphContrast(pixels, width, height, stride, x, y, panelWidth, panelHeight);

        return edgeMean * 0.72 + toneContrast * 0.18 + closeGlyph * 0.10;
    }

    private static double HorizontalEdge(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int startX,
        int endX,
        int y,
        int step)
    {
        double total = 0;
        var count = 0;
        for (var px = startX; px <= endX; px += step)
        {
            var a = Luma(pixels, width, height, stride, px, y - 3);
            var b = Luma(pixels, width, height, stride, px, y);
            var c = Luma(pixels, width, height, stride, px, y + 3);
            total += Math.Max(Math.Abs(a - c), Math.Abs(b - (a + c) * 0.5));
            count++;
        }
        return count == 0 ? 0 : total / count;
    }

    private static double VerticalEdge(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int x,
        int startY,
        int endY,
        int step)
    {
        double total = 0;
        var count = 0;
        for (var py = startY; py <= endY; py += step)
        {
            var a = Luma(pixels, width, height, stride, x - 3, py);
            var b = Luma(pixels, width, height, stride, x, py);
            var c = Luma(pixels, width, height, stride, x + 3, py);
            total += Math.Max(Math.Abs(a - c), Math.Abs(b - (a + c) * 0.5));
            count++;
        }
        return count == 0 ? 0 : total / count;
    }

    private static double PanelToneContrast(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int panelWidth,
        int panelHeight)
    {
        double inside = 0;
        double outside = 0;
        var count = 0;
        var stepX = Math.Max(18, panelWidth / 12);
        var stepY = Math.Max(18, panelHeight / 12);

        for (var dx = stepX; dx < panelWidth - stepX; dx += stepX)
        {
            for (var dy = stepY; dy < panelHeight - stepY; dy += stepY)
            {
                inside += Luma(pixels, width, height, stride, x + dx, y + dy);
                var outsideX = dx < panelWidth / 2 ? x - 4 : x + panelWidth + 4;
                outside += Luma(pixels, width, height, stride, outsideX, y + dy);
                count++;
            }
        }

        if (count == 0)
            return 0;
        return Math.Min(64, Math.Abs(inside / count - outside / count));
    }

    private static double CloseGlyphContrast(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int panelWidth,
        int panelHeight)
    {
        var box = Math.Max(18, (int)Math.Round(Math.Min(panelWidth, panelHeight) * 0.045));
        var startX = x + panelWidth - box - Math.Max(5, box / 3);
        var startY = y + Math.Max(5, box / 4);
        double mean = 0;
        double square = 0;
        var bright = 0;
        var count = 0;

        for (var py = startY; py < startY + box; py += 2)
        {
            for (var px = startX; px < startX + box; px += 2)
            {
                var value = Luma(pixels, width, height, stride, px, py);
                mean += value;
                square += value * value;
                if (value >= 165)
                    bright++;
                count++;
            }
        }

        if (count == 0)
            return 0;
        mean /= count;
        var variance = Math.Max(0, square / count - mean * mean);
        var standardDeviation = Math.Sqrt(variance);
        var brightRatio = (double)bright / count;
        if (brightRatio < 0.005 || brightRatio > 0.55)
            return standardDeviation * 0.35;
        return Math.Min(64, standardDeviation);
    }

    private static double Luma(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        int x,
        int y)
    {
        x = Math.Clamp(x, 0, width - 1);
        y = Math.Clamp(y, 0, height - 1);
        var offset = y * stride + x * 4;
        var b = pixels[offset];
        var g = pixels[offset + 1];
        var r = pixels[offset + 2];
        return r * 0.2126 + g * 0.7152 + b * 0.0722;
    }
}

public readonly record struct ScannerDetectedRegion(
    int X,
    int Y,
    int Width,
    int Height,
    double Score);
