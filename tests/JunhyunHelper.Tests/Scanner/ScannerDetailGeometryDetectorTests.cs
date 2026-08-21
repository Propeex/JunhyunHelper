using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerDetailGeometryDetectorTests
{
    [Fact]
    public void FindCandidates_CroppedOphthalmoscopeShape_ReproducesLab38OuterWindowAndTitleRoi()
    {
        const int width = 676;
        const int height = 522;
        var pixels = CreateFrame(width, height, 24);
        DrawLab38Panel(pixels, width, height, 3, 3, 672, 514, includeRedClose: true, includeInnerFrame: true);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(pixels, width, height, width * 4, 8);

        Assert.NotEmpty(candidates);
        var result = candidates[0];
        Assert.InRange(result.Window.X, 0, 8);
        Assert.InRange(result.Window.Y, 0, 8);
        Assert.InRange(result.Window.Width, 660, 676);
        Assert.InRange(result.Window.Height, 500, 521);
        Assert.True(result.Score >= 0.70);

        // Scanner Lab 3.8 formula: x + 3.2%, top - 1, width 64%, height 5.2%.
        Assert.InRange(result.Title.X, result.Window.X + 18, result.Window.X + 26);
        Assert.InRange(result.Title.Y, Math.Max(0, result.Window.Y - 1), result.Window.Y);
        Assert.InRange(result.Title.Width, 420, 440);
        Assert.InRange(result.Title.Height, 24, 30);
    }

    [Fact]
    public void FindCandidates_FullWaterScreenshotShape_ReproducesLab38CentralWindow()
    {
        const int width = 1911;
        const int height = 1072;
        var pixels = CreateFrame(width, height, 22);
        DrawLab38Panel(pixels, width, height, 622, 282, 674, 514, includeRedClose: true, includeInnerFrame: true);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(pixels, width, height, width * 4, 8);

        Assert.NotEmpty(candidates);
        var result = candidates[0];
        Assert.InRange(result.Window.X, 617, 627);
        Assert.InRange(result.Window.Y, 277, 287);
        Assert.InRange(result.Window.Width, 666, 680);
        Assert.InRange(result.Window.Height, 505, 521);
        Assert.True(result.Score >= 0.70);
        Assert.InRange(result.Title.X, 638, 650);
        Assert.InRange(result.Title.Y, 276, 286);
        Assert.InRange(result.Title.Width, 424, 438);
        Assert.InRange(result.Title.Height, 24, 30);
    }

    [Fact]
    public void FindCandidates_StrongInnerRectangle_DoesNotRemoveOuterRedXCandidate()
    {
        const int width = 1280;
        const int height = 720;
        var pixels = CreateFrame(width, height, 30);
        const int panelWidth = 620;
        const int panelHeight = 476;
        const int x = 330;
        const int y = 120;
        DrawLab38Panel(pixels, width, height, x, y, panelWidth, panelHeight, includeRedClose: true, includeInnerFrame: true);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(pixels, width, height, width * 4, 8);

        Assert.Contains(candidates, candidate =>
            Math.Abs(candidate.Window.X - x) <= 8 &&
            Math.Abs(candidate.Window.Y - y) <= 8 &&
            Math.Abs(candidate.Window.Width - panelWidth) <= 14 &&
            Math.Abs(candidate.Window.Height - panelHeight) <= 16);
    }

    [Fact]
    public void FindCandidates_NoRedClose_StillHasRectangleFallback()
    {
        const int width = 1280;
        const int height = 720;
        var pixels = CreateFrame(width, height, 28);
        DrawLab38Panel(pixels, width, height, 340, 130, 600, 462, includeRedClose: false, includeInnerFrame: false);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(pixels, width, height, width * 4, 8);

        Assert.NotEmpty(candidates);
        Assert.Contains(candidates, candidate => candidate.StructuralReason == "RECTANGLE_CANDIDATE");
    }

    [Fact]
    public void FindCandidates_UniformFrame_FailsClosed()
    {
        const int width = 960;
        const int height = 540;
        var pixels = CreateFrame(width, height, 48);

        var candidates = ScannerDetailGeometryDetector.FindCandidates(pixels, width, height, width * 4, 8);

        Assert.Empty(candidates);
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

    private static void DrawLab38Panel(
        byte[] pixels,
        int width,
        int height,
        int x,
        int y,
        int panelWidth,
        int panelHeight,
        bool includeRedClose,
        bool includeInnerFrame)
    {
        Fill(pixels, width, height, x, y, panelWidth, panelHeight, 44);
        DrawRect(pixels, width, height, x, y, panelWidth, panelHeight, 175, 2);

        // Title text-like bright strokes.
        var titleY = y + Math.Max(5, (int)Math.Round(panelHeight * 0.025));
        DrawLine(
            pixels,
            width,
            height,
            x + (int)Math.Round(panelWidth * 0.04),
            titleY,
            x + (int)Math.Round(panelWidth * 0.45),
            titleY,
            205,
            2);

        // Breadcrumb/category row below title. It must not be required for title OCR.
        var categoryY = y + (int)Math.Round(panelHeight * 0.072);
        DrawLine(pixels, width, height, x + 8, categoryY, x + (int)Math.Round(panelWidth * 0.32), categoryY, 125, 2);

        if (includeRedClose)
        {
            var closeWidth = Math.Clamp((int)Math.Round(panelWidth * 0.040), 12, 30);
            var closeHeight = Math.Clamp((int)Math.Round(panelHeight * 0.034), 8, 22);
            var closeX = x + panelWidth - closeWidth - 3;
            var closeY = Math.Max(0, y - 1);
            FillRgb(pixels, width, height, closeX, closeY, closeWidth, closeHeight, 105, 18, 18);
            DrawLineRgb(pixels, width, height, closeX + 4, closeY + 3, closeX + closeWidth - 5, closeY + closeHeight - 4, 225, 220, 220, 1);
            DrawLineRgb(pixels, width, height, closeX + closeWidth - 5, closeY + 3, closeX + 4, closeY + closeHeight - 4, 225, 220, 220, 1);
        }

        if (includeInnerFrame)
        {
            var innerX = x + (int)Math.Round(panelWidth * 0.34);
            var innerY = y + (int)Math.Round(panelHeight * 0.12);
            var innerWidth = (int)Math.Round(panelWidth * 0.55);
            var innerHeight = (int)Math.Round(panelHeight * 0.60);
            DrawRect(pixels, width, height, innerX, innerY, innerWidth, innerHeight, 225, 2);
        }
    }

    private static void Fill(byte[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, byte value) =>
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
        {
            for (var px = Math.Max(0, x); px < Math.Min(width, x + rectWidth); px++)
                SetPixel(pixels, width, px, py, r, g, b);
        }
    }

    private static void DrawRect(byte[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, byte value, int thickness)
    {
        Fill(pixels, width, height, x, y, rectWidth, thickness, value);
        Fill(pixels, width, height, x, y + rectHeight - thickness, rectWidth, thickness, value);
        Fill(pixels, width, height, x, y, thickness, rectHeight, value);
        Fill(pixels, width, height, x + rectWidth - thickness, y, thickness, rectHeight, value);
    }

    private static void DrawLine(byte[] pixels, int width, int height, int x0, int y0, int x1, int y1, byte value, int thickness) =>
        DrawLineRgb(pixels, width, height, x0, y0, x1, y1, value, value, value, thickness);

    private static void DrawLineRgb(
        byte[] pixels,
        int width,
        int height,
        int x0,
        int y0,
        int x1,
        int y1,
        byte r,
        byte g,
        byte b,
        int thickness)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            FillRgb(pixels, width, height, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, r, g, b);
            if (x0 == x1 && y0 == y1)
                break;
            var twice = 2 * error;
            if (twice >= dy)
            {
                error += dy;
                x0 += sx;
            }
            if (twice <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void SetPixel(byte[] pixels, int width, int x, int y, byte r, byte g, byte b)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = b;
        pixels[offset + 1] = g;
        pixels[offset + 2] = r;
        pixels[offset + 3] = 255;
    }
}
