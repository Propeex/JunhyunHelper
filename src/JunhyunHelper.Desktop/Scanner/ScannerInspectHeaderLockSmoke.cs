using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerInspectHeaderLockSmoke
{
    public static void Verify()
    {
        var cases = new (int Width, int CloseW, int CloseH, int MagX, int Gap, int Glyphs)[]
        {
            (834, 25, 16, 12, 6, 12),
            (862, 27, 17, 13, 6, 18),
            (861, 27, 17, 12, 5, 3),
            (862, 27, 17, 13, 6, 15),
            (856, 27, 17, 13, 6, 14),
            (855, 27, 17, 12, 6, 17),
            (862, 27, 17, 13, 5, 17),
            (854, 27, 17, 12, 6, 14),
            (861, 27, 17, 12, 6, 8),
            (861, 27, 17, 12, 6, 20),
            (827, 26, 16, 11, 5, 9),
            (822, 26, 16, 11, 6, 15),
        };

        foreach (var sample in cases)
        {
            VerifyLockedHeader(
                headerLeft: 170,
                headerTop: 150,
                headerWidth: sample.Width,
                closeWidth: sample.CloseW,
                closeHeight: sample.CloseH,
                magnifierOffsetX: sample.MagX,
                titleGap: sample.Gap,
                glyphCount: sample.Glyphs,
                addDecoyRing: true);
        }

        // Live MP-153 evidence: the same header structure can be shifted far right and
        // must remain coordinate-relative rather than tied to screen-center heuristics.
        VerifyLockedHeader(
            headerLeft: 360,
            headerTop: 122,
            headerWidth: 855,
            closeWidth: 27,
            closeHeight: 17,
            magnifierOffsetX: 12,
            titleGap: 6,
            glyphCount: 17,
            addDecoyRing: true);

        VerifyMissingMagnifierFailsClosed();
    }

    private static void VerifyLockedHeader(
        int headerLeft,
        int headerTop,
        int headerWidth,
        int closeWidth,
        int closeHeight,
        int magnifierOffsetX,
        int titleGap,
        int glyphCount,
        bool addDecoyRing)
    {
        const int width = 1400;
        const int height = 820;
        const int stride = width * 4;
        var pixels = NewFrame(width, height, stride);

        var headerRight = headerLeft + headerWidth - 1;
        var closeX = headerRight - closeWidth - 4;
        var closeY = headerTop + 5;
        var magnifierX = headerLeft + magnifierOffsetX;
        var magnifierY = headerTop + 7;
        var expectedTitleStart = magnifierX + 13 + titleGap;

        DrawHeaderField(pixels, stride, headerLeft, headerTop, headerWidth, closeHeight);
        DrawClose(pixels, stride, closeX, closeY, closeWidth, closeHeight);
        DrawMagnifierCore(pixels, stride, magnifierX, magnifierY);
        DrawTitleGlyphs(
            pixels,
            stride,
            expectedTitleStart,
            headerTop + 5,
            glyphCount,
            fragmentedFirst: true);

        if (addDecoyRing)
        {
            // This intentionally looks more ring-like than some anti-aliased glyphs.
            // It sits in the title lane, so it must never outrank the real search icon.
            DrawMagnifierCore(
                pixels,
                stride,
                expectedTitleStart + 70,
                magnifierY);
        }

        var panel = new ScannerDetectedRegion(
            headerLeft,
            headerTop,
            headerWidth,
            500,
            0.98);
        var candidate = new ScannerDetectedCandidate(
            panel,
            ScannerDetailGeometryDetector.GetTitleRegion(panel),
            default,
            "STRUCTURE_MATCH");

        var result = ScannerTitleAnchorRefiner.Refine(
            pixels,
            width,
            height,
            stride,
            candidate);

        if (result.Reason != "HEADER_FRAME_LOCKED" ||
            result.Score < 0.68 ||
            result.CloseButton.Width < closeWidth - 2 ||
            Math.Abs(result.CloseButton.X - closeX) > 3 ||
            result.Magnifier.Width is < 9 or > 18 ||
            Math.Abs(result.Magnifier.X - magnifierX) > 3 ||
            result.Title.X <= result.Magnifier.X + result.Magnifier.Width ||
            result.Title.X > expectedTitleStart ||
            result.Title.X < expectedTitleStart - 4 ||
            result.Title.X + result.Title.Width >= result.CloseButton.X ||
            result.Title.Height is < 16 or > 28)
        {
            throw new InvalidOperationException(
                $"Scanner live header lock regression failed: header={headerLeft},{headerTop},{headerWidth}, " +
                $"magnifier={result.Magnifier}, title={result.Title}, close={result.CloseButton}, " +
                $"score={result.Score:F3}, reason={result.Reason}.");
        }
    }

    private static void VerifyMissingMagnifierFailsClosed()
    {
        const int width = 1200;
        const int height = 760;
        const int stride = width * 4;
        var pixels = NewFrame(width, height, stride);
        const int headerLeft = 150;
        const int headerTop = 140;
        const int headerWidth = 856;
        const int closeWidth = 27;
        const int closeHeight = 17;
        var headerRight = headerLeft + headerWidth - 1;
        var closeX = headerRight - closeWidth - 4;

        DrawHeaderField(pixels, stride, headerLeft, headerTop, headerWidth, closeHeight);
        DrawClose(pixels, stride, closeX, headerTop + 5, closeWidth, closeHeight);
        DrawTitleGlyphs(pixels, stride, headerLeft + 34, headerTop + 5, 12, fragmentedFirst: false);

        var panel = new ScannerDetectedRegion(headerLeft, headerTop, headerWidth, 500, 0.98);
        var candidate = new ScannerDetectedCandidate(
            panel,
            ScannerDetailGeometryDetector.GetTitleRegion(panel),
            default,
            "STRUCTURE_MATCH");
        var result = ScannerTitleAnchorRefiner.Refine(
            pixels,
            width,
            height,
            stride,
            candidate);

        if (result.Reason == "HEADER_FRAME_LOCKED" || result.Magnifier.Width > 0)
        {
            throw new InvalidOperationException(
                $"Scanner must fail closed without the search icon: magnifier={result.Magnifier}, " +
                $"score={result.Score:F3}, reason={result.Reason}.");
        }
    }

    private static byte[] NewFrame(int width, int height, int stride)
    {
        var pixels = new byte[stride * height];
        for (var offset = 3; offset < pixels.Length; offset += 4)
            pixels[offset] = 255;
        return pixels;
    }

    private static void DrawHeaderField(
        byte[] pixels,
        int stride,
        int x,
        int y,
        int width,
        int closeHeight)
    {
        Fill(pixels, stride, x, y, width, 1, 68, 68, 68);
        Fill(
            pixels,
            stride,
            x,
            y + 1,
            width,
            Math.Max(23, (int)Math.Round(closeHeight * 1.48)),
            30,
            30,
            30);
    }

    private static void DrawClose(
        byte[] pixels,
        int stride,
        int x,
        int y,
        int width,
        int height)
    {
        Fill(pixels, stride, x, y, width, height, 12, 12, 126);
        var bright = (byte)158;
        for (var step = 4; step < Math.Min(width - 4, height - 2); step++)
        {
            SetPixel(pixels, stride, x + step, y + step - 2, bright, bright, bright);
            SetPixel(pixels, stride, x + width - 1 - step, y + step - 2, bright, bright, bright);
        }
    }

    private static void DrawMagnifierCore(byte[] pixels, int stride, int x, int y)
    {
        const byte bright = 176;
        const int size = 13;
        var center = (size - 1) / 2.0;
        for (var yy = 0; yy < size; yy++)
        for (var xx = 0; xx < size; xx++)
        {
            var dx = xx - center;
            var dy = yy - center;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance is >= 4.2 and <= 5.8)
                SetPixel(pixels, stride, x + xx, y + yy, bright, bright, bright);
        }

        for (var step = 0; step < 5; step++)
        {
            var px = Math.Min(size - 1, 8 + step);
            var py = Math.Min(size - 1, 8 + step);
            SetPixel(pixels, stride, x + px, y + py, bright, bright, bright);
            if (px + 1 < size)
                SetPixel(pixels, stride, x + px + 1, y + py, bright, bright, bright);
        }
    }

    private static void DrawTitleGlyphs(
        byte[] pixels,
        int stride,
        int x,
        int y,
        int count,
        bool fragmentedFirst)
    {
        const byte bright = 172;
        var cursor = x;
        for (var index = 0; index < count; index++)
        {
            var glyphWidth = index % 3 == 0 ? 8 : 7;
            if (index == 0 && fragmentedFirst)
            {
                Fill(pixels, stride, cursor, y + 1, 2, 12, bright, bright, bright);
                Fill(pixels, stride, cursor + 5, y, 2, 14, bright, bright, bright);
                Fill(pixels, stride, cursor + 2, y + 6, 4, 2, bright, bright, bright);
            }
            else
            {
                Fill(pixels, stride, cursor, y, 2, 14, bright, bright, bright);
                Fill(pixels, stride, cursor, y, glyphWidth, 2, bright, bright, bright);
                Fill(pixels, stride, cursor, y + 12, glyphWidth, 2, bright, bright, bright);
                Fill(pixels, stride, cursor + glyphWidth - 2, y + 4, 2, 8, bright, bright, bright);
            }
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
        {
            var offset = yy * stride + xx * 4;
            pixels[offset] = b;
            pixels[offset + 1] = g;
            pixels[offset + 2] = r;
            pixels[offset + 3] = 255;
        }
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
