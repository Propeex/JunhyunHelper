using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Synthetic product-smoke regression for reviewed v1.4.1 cases where an oversized
/// structural frame contains the real inspect header hundreds of pixels below its top.
/// No user screenshot bytes or item identity are embedded.
/// </summary>
internal static class ScannerContainedSubpanelGroundTruthSmoke
{
    public static void Verify()
    {
        const int width = 1920;
        const int height = 1080;
        const int stride = width * 4;
        var pixels = NewFrame(width, height, stride);

        const int coarseLeft = 561;
        const int coarseTop = 41;
        const int coarseWidth = 1208;
        const int coarseHeight = 908;
        const int headerLeft = 599;
        const int headerTop = 321;
        const int headerWidth = 802;
        const int closeWidth = 25;
        const int closeHeight = 16;
        var headerRight = headerLeft + headerWidth - 1;
        var closeX = headerRight - closeWidth - 4;
        var closeY = headerTop + 5;
        var magnifierX = headerLeft + 11;
        var magnifierY = headerTop + 7;
        var titleX = magnifierX + 19;

        // Decoy neutral separators near the coarse frame top. They intentionally lack
        // the close/magnifier/title structure and must not be accepted.
        Fill(pixels, stride, coarseLeft, 61, 900, 1, 44, 44, 44);
        Fill(pixels, stride, coarseLeft, 111, 1010, 1, 42, 42, 42);
        Fill(pixels, stride, coarseLeft, 136, 780, 1, 41, 41, 41);

        Fill(pixels, stride, headerLeft, headerTop, headerWidth, 1, 38, 38, 38);
        Fill(pixels, stride, headerLeft, headerTop + 1, headerWidth, 24, 30, 30, 30);

        Fill(pixels, stride, closeX, closeY, closeWidth, closeHeight, 12, 12, 65);
        const byte closeStroke = 158;
        for (var step = 4; step < Math.Min(closeWidth - 4, closeHeight - 2); step++)
        {
            SetPixel(pixels, stride, closeX + step, closeY + step - 2, closeStroke, closeStroke, closeStroke);
            SetPixel(pixels, stride, closeX + closeWidth - 1 - step, closeY + step - 2, closeStroke, closeStroke, closeStroke);
        }

        const byte icon = 176;
        const int iconSize = 13;
        const double centerX = 7.1;
        const double centerY = 4.1;
        for (var yy = 0; yy < iconSize; yy++)
        for (var xx = 0; xx < iconSize; xx++)
        {
            var dx = xx - centerX;
            var dy = yy - centerY;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance is >= 3.6 and <= 5.1)
                SetPixel(pixels, stride, magnifierX + xx, magnifierY + yy, icon, icon, icon);
        }
        for (var step = 0; step < 6; step++)
        {
            var px = 5 - step;
            var py = 7 + step;
            SetPixel(pixels, stride, magnifierX + px, magnifierY + py, icon, icon, icon);
            if (px + 1 < iconSize)
                SetPixel(pixels, stride, magnifierX + px + 1, magnifierY + py, icon, icon, icon);
        }
        DrawTitleGlyphs(pixels, stride, titleX, headerTop + 5, 12);

        var coarse = new ScannerDetectedRegion(coarseLeft, coarseTop, coarseWidth, coarseHeight, 0.93);
        var candidate = new ScannerDetectedCandidate(
            coarse,
            ScannerDetailGeometryDetector.GetTitleRegion(coarse),
            default,
            "RECTANGLE_CANDIDATE");

        var result = ScannerContainedSubpanelGroundTruthRecovery.TryRefine(
            pixels,
            width,
            height,
            stride,
            candidate);

        if (result is not { } locked ||
            locked.Reason != "HEADER_FRAME_LOCKED" ||
            locked.Score < 0.68 ||
            Math.Abs(locked.Title.Y - headerTop) > 8 ||
            Math.Abs(locked.CloseButton.X - closeX) > 6 ||
            Math.Abs(locked.CloseButton.Y - closeY) > 6 ||
            locked.Title.Y < 250)
        {
            throw new InvalidOperationException(
                $"Scanner contained-subpanel Ground Truth regression failed: coarse={coarse}, result={result}.");
        }
    }

    private static byte[] NewFrame(int width, int height, int stride)
    {
        var pixels = new byte[stride * height];
        for (var offset = 3; offset < pixels.Length; offset += 4)
            pixels[offset] = 255;
        return pixels;
    }

    private static void DrawTitleGlyphs(byte[] pixels, int stride, int x, int y, int count)
    {
        const byte bright = 172;
        var cursor = x;
        for (var index = 0; index < count; index++)
        {
            var glyphWidth = index % 3 == 0 ? 8 : 7;
            Fill(pixels, stride, cursor, y, 2, 14, bright, bright, bright);
            Fill(pixels, stride, cursor, y, glyphWidth, 2, bright, bright, bright);
            Fill(pixels, stride, cursor, y + 12, glyphWidth, 2, bright, bright, bright);
            Fill(pixels, stride, cursor + glyphWidth - 2, y + 4, 2, 8, bright, bright, bright);
            cursor += glyphWidth + 3;
        }
    }

    private static void Fill(
        byte[] pixels,
        int stride,
        int x,
        int y,
        int width,
        int height,
        byte b,
        byte g,
        byte r)
    {
        for (var yy = y; yy < y + height; yy++)
        for (var xx = x; xx < x + width; xx++)
            SetPixel(pixels, stride, xx, yy, b, g, r);
    }

    private static void SetPixel(
        byte[] pixels,
        int stride,
        int x,
        int y,
        byte b,
        byte g,
        byte r)
    {
        var offset = y * stride + x * 4;
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = 255;
    }
}
