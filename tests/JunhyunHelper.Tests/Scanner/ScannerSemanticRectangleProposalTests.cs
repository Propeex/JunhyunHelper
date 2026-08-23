using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerSemanticRectangleProposalTests
{
    [Fact]
    public void FindCandidates_TallDetailWindow_IsNotRejectedByLegacyAspectPrior()
    {
        const int width = 1280;
        const int height = 900;
        const int x = 380;
        const int y = 90;
        const int panelWidth = 520;
        const int panelHeight = 650;
        var pixels = CreateFrame(width, height, 28);

        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight, includeRedClose: true);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(
            pixels,
            width,
            height,
            width * 4,
            12);

        Assert.Contains(candidates, candidate =>
            Math.Abs(candidate.Window.X - x) <= 10 &&
            Math.Abs(candidate.Window.Y - y) <= 10 &&
            Math.Abs(candidate.Window.Width - panelWidth) <= 18 &&
            Math.Abs(candidate.Window.Height - panelHeight) <= 20);
    }

    [Fact]
    public void FindCandidates_HeavilyOverlappingDifferentBottoms_AreKeptForSemanticValidation()
    {
        const int width = 1280;
        const int height = 820;
        const int x = 330;
        const int y = 90;
        const int panelWidth = 620;
        const int shortHeight = 500;
        const int tallHeight = 560;
        var pixels = CreateFrame(width, height, 26);

        // Same top/left/right but two strong bottom edges. Their IoU is ~0.89, so the
        // legacy IoU>=0.72 dedupe removed one before X/magnifier semantic validation.
        DrawPanel(pixels, width, height, x, y, panelWidth, tallHeight, includeRedClose: true);
        Fill(pixels, width, height, x, y + shortHeight - 2, panelWidth, 2, 180);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(
            pixels,
            width,
            height,
            width * 4,
            12);

        Assert.Contains(candidates, candidate =>
            Math.Abs(candidate.Window.X - x) <= 12 &&
            Math.Abs(candidate.Window.Y - y) <= 12 &&
            Math.Abs(candidate.Window.Width - panelWidth) <= 20 &&
            Math.Abs(candidate.Window.Height - shortHeight) <= 20);
        Assert.Contains(candidates, candidate =>
            Math.Abs(candidate.Window.X - x) <= 12 &&
            Math.Abs(candidate.Window.Y - y) <= 12 &&
            Math.Abs(candidate.Window.Width - panelWidth) <= 20 &&
            Math.Abs(candidate.Window.Height - tallHeight) <= 20);
    }

    private static byte[] CreateFrame(int width, int height, byte value)
    {
        var pixels = new byte[width * height * 4];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = value;
            pixels[offset + 1] = value;
            pixels[offset + 2] = value;
            pixels[offset + 3] = 255;
        }
        return pixels;
    }

    private static void DrawPanel(
        byte[] pixels,
        int width,
        int height,
        int x,
        int y,
        int panelWidth,
        int panelHeight,
        bool includeRedClose)
    {
        Fill(pixels, width, height, x, y, panelWidth, panelHeight, 44);
        Fill(pixels, width, height, x, y, panelWidth, 2, 180);
        Fill(pixels, width, height, x, y + panelHeight - 2, panelWidth, 2, 180);
        Fill(pixels, width, height, x, y, 2, panelHeight, 180);
        Fill(pixels, width, height, x + panelWidth - 2, y, 2, panelHeight, 180);

        if (!includeRedClose)
            return;

        const int closeWidth = 25;
        const int closeHeight = 16;
        var closeX = x + panelWidth - closeWidth - 4;
        var closeY = y + 1;
        FillRgb(pixels, width, height, closeX, closeY, closeWidth, closeHeight, 65, 12, 12);
        for (var step = 4; step < 14; step++)
        {
            SetPixel(pixels, width, closeX + step, closeY + step - 2, 180, 180, 180);
            SetPixel(pixels, width, closeX + closeWidth - 1 - step, closeY + step - 2, 180, 180, 180);
        }
    }

    private static void Fill(
        byte[] pixels,
        int width,
        int height,
        int x,
        int y,
        int rectWidth,
        int rectHeight,
        byte value) =>
        FillRgb(pixels, width, height, x, y, rectWidth, rectHeight, value, value, value);

    private static void FillRgb(
        byte[] pixels,
        int width,
        int height,
        int x,
        int y,
        int rectWidth,
        int rectHeight,
        byte r,
        byte g,
        byte b)
    {
        for (var py = Math.Max(0, y); py < Math.Min(height, y + rectHeight); py++)
        for (var px = Math.Max(0, x); px < Math.Min(width, x + rectWidth); px++)
            SetPixel(pixels, width, px, py, r, g, b);
    }

    private static void SetPixel(
        byte[] pixels,
        int width,
        int x,
        int y,
        byte r,
        byte g,
        byte b)
    {
        if (x < 0 || y < 0 || x >= width || y >= pixels.Length / (width * 4))
            return;

        var offset = (y * width + x) * 4;
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = 255;
    }
}
