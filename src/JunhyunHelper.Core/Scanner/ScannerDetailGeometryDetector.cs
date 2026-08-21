namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Pure pixel-level detector for the Tarkov item-inspection window.
/// Geometry only creates an OCR candidate; an item is never accepted until the
/// independent OCR matcher also passes its confidence and margin gates.
/// </summary>
public static class ScannerDetailGeometryDetector
{
    // Current Korean-client inspection windows observed during Scanner validation are
    // approximately 676x522 at 1920x1080 UI scale. The previous integrated detector
    // accidentally used a 672px canonical height, which made high-contrast rectangles
    // inside the inspection window score as if they were the outer window.
    private const double CanonicalPanelWidthRatio = 676.0 / 1920.0;
    private const double CanonicalPanelHeightRatio = 522.0 / 1080.0;
    private const double CanonicalCenterYRatio = 500.0 / 1080.0;
    private const double MinimumScore = 18.0;
    private const double ScoreTieWindow = 2.0;
    private const int SearchStepXPixels = 8;
    private const int SearchStepYPixels = 6;
    private const int BorderProbeRadiusPixels = 5;

    private static readonly double[] GameWindowScales = [0.85, 0.90, 0.95, 1.00, 1.05, 1.10, 1.15];
    private static readonly double[] DisplayTestScales = [0.50, 0.55, 0.65, 0.75, 0.85, 0.95, 1.00, 1.05, 1.15, 1.25];

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
        var xRadiusRatio = extendedScaleSearch ? 0.20 : 0.12;
        var yRadiusRatio = extendedScaleSearch ? 0.20 : 0.12;
        var centerXStart = (int)Math.Round(width * (0.50 - xRadiusRatio));
        var centerXEnd = (int)Math.Round(width * (0.50 + xRadiusRatio));
        var centerYStart = (int)Math.Round(height * (CanonicalCenterYRatio - yRadiusRatio));
        var centerYEnd = (int)Math.Round(height * (CanonicalCenterYRatio + yRadiusRatio));

        foreach (var scale in scales)
        {
            var panelWidth = (int)Math.Round(width * CanonicalPanelWidthRatio * scale);
            var panelHeight = (int)Math.Round(height * CanonicalPanelHeightRatio * scale);
            if (panelWidth < 260 || panelHeight < 200 || panelWidth >= width * 0.78 || panelHeight >= height * 0.86)
                continue;

            // Border validation is intentionally strict (all four outer sides + close
            // control), so candidate centers must be sampled in pixels rather than the
            // old 20-30px ratio grid. Keeping the final border within a few pixels is
            // also important because the title row is only about 25px high.
            for (var centerX = centerXStart; centerX <= centerXEnd; centerX += SearchStepXPixels)
            {
                for (var centerY = centerYStart; centerY <= centerYEnd; centerY += SearchStepYPixels)
                {
                    var x = centerX - panelWidth / 2;
                    var y = centerY - panelHeight / 2;
                    if (x < 5 || y < 5 || x + panelWidth + 5 >= width || y + panelHeight + 5 >= height)
                        continue;

                    var score = ScoreCandidate(bgraPixels, width, height, stride, x, y, panelWidth, panelHeight);
                    if (score < MinimumScore)
                        continue;

                    var candidate = new ScannerDetectedRegion(x, y, panelWidth, panelHeight, score);
                    if (IsBetterCandidate(candidate, best))
                        best = candidate;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Returns only the inspection-window title row. It deliberately ends before the
    /// category/breadcrumb row directly below the title (for example
    /// "교환용 물품 &gt; 의료용품").
    /// </summary>
    public static ScannerDetectedRegion GetTitleRegion(ScannerDetectedRegion panel)
    {
        var x = panel.X + (int)Math.Round(panel.Width * 0.035);
        var y = panel.Y + (int)Math.Round(panel.Height * 0.002);
        var width = (int)Math.Round(panel.Width * 0.89);
        var height = Math.Max(12, (int)Math.Round(panel.Height * 0.048));

        width = Math.Min(width, panel.X + panel.Width - x - 4);
        height = Math.Min(height, panel.Y + panel.Height - y - 2);

        return new ScannerDetectedRegion(
            x,
            y,
            Math.Max(80, width),
            Math.Max(10, height),
            panel.Score);
    }

    private static bool IsBetterCandidate(ScannerDetectedRegion candidate, ScannerDetectedRegion? best)
    {
        if (best is null)
            return true;

        if (candidate.Score > best.Value.Score + ScoreTieWindow)
            return true;
        if (best.Value.Score > candidate.Score + ScoreTieWindow)
            return false;

        // Inner inventory/content rectangles often have slightly sharper edges than the
        // actual inspect window. When both candidates are structurally credible, prefer
        // the larger outer frame instead of rewarding the smaller high-contrast box.
        var candidateArea = (long)candidate.Width * candidate.Height;
        var bestArea = (long)best.Value.Width * best.Value.Height;
        if (candidateArea != bestArea)
            return candidateArea > bestArea;

        return candidate.Score > best.Value.Score;
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
        var closeGlyph = CloseGlyphContrast(pixels, width, height, stride, x, y, panelWidth, panelHeight);

        // The real inspection window has a coherent outer frame and a close control in
        // its top-right title bar. Requiring all four sides prevents a strong internal
        // separator/button rectangle from becoming the OCR anchor.
        if (top < 8 || bottom < 7 || left < 7 || right < 7 || closeGlyph < 16)
            return 0;

        var edgeMean = (top + bottom + left + right) / 4.0;
        var minimumEdge = Math.Min(Math.Min(top, bottom), Math.Min(left, right));
        var toneContrast = PanelToneContrast(pixels, width, height, stride, x, y, panelWidth, panelHeight);

        return edgeMean * 0.35 +
               minimumEdge * 0.30 +
               closeGlyph * 0.30 +
               toneContrast * 0.05;
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
            var a = Luma(pixels, width, height, stride, px, y - BorderProbeRadiusPixels);
            var b = Luma(pixels, width, height, stride, px, y);
            var c = Luma(pixels, width, height, stride, px, y + BorderProbeRadiusPixels);
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
            var a = Luma(pixels, width, height, stride, x - BorderProbeRadiusPixels, py);
            var b = Luma(pixels, width, height, stride, x, py);
            var c = Luma(pixels, width, height, stride, x + BorderProbeRadiusPixels, py);
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
