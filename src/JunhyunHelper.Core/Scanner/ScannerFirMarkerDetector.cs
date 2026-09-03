namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Positive-only visual detector for Tarkov's Found-in-Raid check marker inside an already
/// verified item-inspect window.
///
/// This detector deliberately does not classify a missing marker as NotFoundInRaid. A miss is
/// only absence of positive visual proof and therefore remains Unknown at the product layer.
/// The caller must already have proven the inspect-window geometry/header and exact item
/// identity; this class only looks for the small check-in-circle glyph in the lower-left
/// portion of that same window.
///
/// No Tarkov image/template asset is embedded. Detection uses scale-relative geometry,
/// brightness/chroma and local shape evidence so the Scanner remains resolution tolerant.
/// </summary>
public static class ScannerFirMarkerDetector
{
    private const int RingSampleCount = 16;
    private const int MinimumRingHits = 11;
    private const int MaximumOuterHits = 6;
    private const int MinimumCheckHits = 8;

    public static bool HasFoundInRaidMarker(
        ReadOnlySpan<byte> bgraPixels,
        int width,
        int height,
        int stride,
        int windowX,
        int windowY,
        int windowWidth,
        int windowHeight)
    {
        if (width <= 0 || height <= 0 || stride < width * 4 ||
            bgraPixels.Length < stride * height ||
            windowWidth < 120 || windowHeight < 80)
        {
            return false;
        }

        var left = Math.Clamp(windowX, 0, width - 1);
        var top = Math.Clamp(windowY, 0, height - 1);
        var right = Math.Clamp(windowX + windowWidth, left + 1, width);
        var bottom = Math.Clamp(windowY + windowHeight, top + 1, height);
        var actualWidth = right - left;
        var actualHeight = bottom - top;
        if (actualWidth < 120 || actualHeight < 80)
            return false;

        // Tarkov renders the FIR check near the inspect panel's lower-left edge. Keep the
        // search deliberately narrow so unrelated bright text/icons elsewhere in the panel
        // cannot become FIR proof.
        var roiLeft = left + Math.Max(2, (int)Math.Round(actualWidth * 0.010));
        var roiRight = Math.Min(right - 2, left + (int)Math.Round(actualWidth * 0.30));
        var roiTop = Math.Max(top + 2, bottom - (int)Math.Round(actualHeight * 0.30));
        var roiBottom = bottom - Math.Max(2, (int)Math.Round(actualHeight * 0.015));
        if (roiRight - roiLeft < 12 || roiBottom - roiTop < 12)
            return false;

        var scaleBase = Math.Min(actualWidth, actualHeight);
        var minimumRadius = Math.Clamp((int)Math.Round(scaleBase * 0.012), 4, 10);
        var maximumRadius = Math.Clamp((int)Math.Round(scaleBase * 0.032), minimumRadius, 18);

        for (var radius = minimumRadius; radius <= maximumRadius; radius += radius < 8 ? 1 : 2)
        {
            var margin = radius + 2;
            var startX = roiLeft + margin;
            var endX = roiRight - margin;
            var startY = roiTop + margin;
            var endY = roiBottom - margin;
            if (startX > endX || startY > endY)
                continue;

            for (var centerY = startY; centerY <= endY; centerY += 2)
            {
                for (var centerX = startX; centerX <= endX; centerX += 2)
                {
                    if (MatchesMarker(bgraPixels, stride, width, height, centerX, centerY, radius))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesMarker(
        ReadOnlySpan<byte> pixels,
        int stride,
        int width,
        int height,
        int centerX,
        int centerY,
        int radius)
    {
        var ringHits = 0;
        var outerHits = 0;
        for (var index = 0; index < RingSampleCount; index++)
        {
            var angle = index * (Math.PI * 2.0 / RingSampleCount);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var ringX = centerX + (int)Math.Round(cos * radius);
            var ringY = centerY + (int)Math.Round(sin * radius);
            if (HasMarkerPixelNear(pixels, stride, width, height, ringX, ringY, radius >= 9 ? 2 : 1))
                ringHits++;

            var outerRadius = radius * 1.55;
            var outerX = centerX + (int)Math.Round(cos * outerRadius);
            var outerY = centerY + (int)Math.Round(sin * outerRadius);
            if (HasMarkerPixelNear(pixels, stride, width, height, outerX, outerY, 1))
                outerHits++;
        }

        if (ringHits < MinimumRingHits || outerHits > MaximumOuterHits)
            return false;

        // Approximate the canonical check glyph with two line segments. Neighborhood sampling
        // tolerates anti-aliasing and small scale/position shifts without using a bitmap template.
        var checkHits = 0;
        var checkSamples = 0;
        checkHits += SampleSegment(
            pixels, stride, width, height,
            centerX - radius * 0.48, centerY + radius * 0.02,
            centerX - radius * 0.12, centerY + radius * 0.36,
            5, ref checkSamples);
        checkHits += SampleSegment(
            pixels, stride, width, height,
            centerX - radius * 0.12, centerY + radius * 0.36,
            centerX + radius * 0.55, centerY - radius * 0.42,
            7, ref checkSamples);

        return checkSamples >= 10 && checkHits >= MinimumCheckHits;
    }

    private static int SampleSegment(
        ReadOnlySpan<byte> pixels,
        int stride,
        int width,
        int height,
        double startX,
        double startY,
        double endX,
        double endY,
        int samples,
        ref int totalSamples)
    {
        var hits = 0;
        for (var index = 0; index < samples; index++)
        {
            var t = samples == 1 ? 0.0 : index / (double)(samples - 1);
            var x = (int)Math.Round(startX + (endX - startX) * t);
            var y = (int)Math.Round(startY + (endY - startY) * t);
            totalSamples++;
            if (HasMarkerPixelNear(pixels, stride, width, height, x, y, 1))
                hits++;
        }
        return hits;
    }

    private static bool HasMarkerPixelNear(
        ReadOnlySpan<byte> pixels,
        int stride,
        int width,
        int height,
        int x,
        int y,
        int radius)
    {
        var minX = Math.Max(0, x - radius);
        var maxX = Math.Min(width - 1, x + radius);
        var minY = Math.Max(0, y - radius);
        var maxY = Math.Min(height - 1, y + radius);

        for (var sampleY = minY; sampleY <= maxY; sampleY++)
        {
            var row = sampleY * stride;
            for (var sampleX = minX; sampleX <= maxX; sampleX++)
            {
                var offset = row + sampleX * 4;
                if (IsMarkerPixel(pixels[offset], pixels[offset + 1], pixels[offset + 2]))
                    return true;
            }
        }
        return false;
    }

    private static bool IsMarkerPixel(byte blue, byte green, byte red)
    {
        var luminance = (77 * red + 150 * green + 29 * blue) >> 8;

        // Ordinary FIR markers can be neutral gray/white. Active-task FIR markers can be
        // yellow/gold. Keep both families while rejecting dim panel chrome.
        var neutral = luminance >= 145 &&
                      Math.Abs(red - green) <= 58 &&
                      Math.Abs(green - blue) <= 58;
        var yellow = red >= 145 && green >= 115 &&
                     red >= blue + 45 && green >= blue + 30;
        return neutral || yellow;
    }
}
