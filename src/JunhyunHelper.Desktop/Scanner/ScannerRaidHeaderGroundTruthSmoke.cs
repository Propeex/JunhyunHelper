using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

internal static class ScannerRaidHeaderGroundTruthSmoke
{
    public static void Verify()
    {
        const int width = 1000;
        const int height = 600;
        const int stride = width * 4;
        var pixels = new byte[stride * height];
        FillGray(pixels, width, height, stride, 12);

        var panel = new ScannerDetectedRegion(250, 200, 500, 280, 0.98);
        var title = ScannerDetailGeometryDetector.GetTitleRegion(panel);
        var candidate = new ScannerDetectedCandidate(panel, title, default, "RED_X_CANDIDATE");

        // Dark title field.
        FillRect(pixels, width, height, stride, 250, 200, 500, 26, 24, 24, 24);

        // Raid inventory geometry can visually extend the same neutral line well to the
        // left of the inspect panel. The recovery must still own the header at panel.X.
        for (var x = 160; x <= 752; x++)
            SetPixel(pixels, width, height, stride, x, 199, 58, 58, 58);

        const int closeX = 730;
        const int closeY = 203;
        const int closeWidth = 16;
        const int closeHeight = 10;
        FillRect(
            pixels,
            width,
            height,
            stride,
            closeX,
            closeY,
            closeWidth,
            closeHeight,
            45,
            45,
            145);
        for (var py = 2; py <= 7; py++)
        {
            var px = (int)Math.Round(py * (closeWidth - 1) / (double)(closeHeight - 1));
            for (var offset = -1; offset <= 1; offset++)
            {
                SetPixel(
                    pixels,
                    width,
                    height,
                    stride,
                    closeX + Math.Clamp(px + offset, 1, closeWidth - 2),
                    closeY + py,
                    35,
                    35,
                    35);
                SetPixel(
                    pixels,
                    width,
                    height,
                    stride,
                    closeX + Math.Clamp(closeWidth - 1 - px + offset, 1, closeWidth - 2),
                    closeY + py,
                    35,
                    35,
                    35);
            }
        }

        // Procedural live magnifier: high/right lens with a down/left handle.
        const int magnifierX = 255;
        const int magnifierY = 204;
        const int magnifierSize = 8;
        for (var py = 0; py < magnifierSize; py++)
        for (var px = 0; px < magnifierSize; px++)
        {
            var u = (px + 0.5) / magnifierSize;
            var v = (py + 0.5) / magnifierSize;
            var dx = u - 0.58;
            var dy = v - 0.35;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            var ring = radius is >= 0.27 and <= 0.45 && !(u < 0.38 && v > 0.55);
            var handle = u <= 0.52 && v >= 0.48 && Math.Abs((u + v) - 0.92) <= 0.13;
            if (ring || handle)
            {
                SetPixel(
                    pixels,
                    width,
                    height,
                    stride,
                    magnifierX + px,
                    magnifierY + py,
                    210,
                    210,
                    210);
            }
        }

        // Sparse neutral title glyph evidence.
        for (var x = 268; x <= 340; x += 9)
            FillRect(pixels, width, height, stride, x, 204, 4, 10, 180, 180, 180);

        var recovered = ScannerRaidHeaderGroundTruthRefiner.TryRefine(
            pixels,
            width,
            height,
            stride,
            candidate);
        if (recovered is not { } locked ||
            !string.Equals(locked.Reason, "HEADER_FRAME_LOCKED", StringComparison.Ordinal) ||
            locked.Score < 0.68 ||
            locked.CloseButton.Width <= 0 ||
            locked.Magnifier.Width <= 0 ||
            locked.Title.Width <= 0)
        {
            throw new InvalidOperationException(
                "Raid Scanner reviewed-Ground-Truth header ownership recovery failed.");
        }

        var noClose = new byte[stride * height];
        FillGray(noClose, width, height, stride, 12);
        FillRect(noClose, width, height, stride, 250, 200, 500, 26, 24, 24, 24);
        for (var x = 160; x <= 752; x++)
            SetPixel(noClose, width, height, stride, x, 199, 58, 58, 58);
        if (ScannerRaidHeaderGroundTruthRefiner.TryRefine(
                noClose,
                width,
                height,
                stride,
                candidate) is not null)
        {
            throw new InvalidOperationException(
                "Raid Scanner header recovery accepted geometry without a red close control.");
        }
    }

    private static void FillGray(
        byte[] pixels,
        int width,
        int height,
        int stride,
        byte gray)
    {
        FillRect(pixels, width, height, stride, 0, 0, width, height, gray, gray, gray);
    }

    private static void FillRect(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int rectWidth,
        int rectHeight,
        byte b,
        byte g,
        byte r)
    {
        var left = Math.Clamp(x, 0, width);
        var top = Math.Clamp(y, 0, height);
        var right = Math.Clamp(x + rectWidth, left, width);
        var bottom = Math.Clamp(y + rectHeight, top, height);
        for (var py = top; py < bottom; py++)
        for (var px = left; px < right; px++)
            SetPixel(pixels, width, height, stride, px, py, b, g, r);
    }

    private static void SetPixel(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int x,
        int y,
        byte b,
        byte g,
        byte r)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;
        var offset = y * stride + x * 4;
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = 255;
    }
}
