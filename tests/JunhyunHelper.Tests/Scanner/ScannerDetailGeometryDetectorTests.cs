using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerDetailGeometryDetectorTests
{
    private const double PanelWidthRatio = 676.0 / 1920.0;
    private const double PanelHeightRatio = 522.0 / 1080.0;

    [Fact]
    public void Detect_FramedCenteredDetailPanel_ReturnsOuterCandidateAndTitleOnly()
    {
        const int width = 960;
        const int height = 540;
        var pixels = CreateFrame(width, height, 38);

        var panelWidth = (int)Math.Round(width * PanelWidthRatio);
        var panelHeight = (int)Math.Round(height * PanelHeightRatio);
        var x = (width - panelWidth) / 2;
        var y = (int)Math.Round(height * (500.0 / 1080.0) - panelHeight / 2.0);
        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight, drawStrongInnerFrame: true);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: false);

        Assert.NotNull(result);
        Assert.InRange(result.Value.X, x - 14, x + 14);
        Assert.InRange(result.Value.Y, y - 14, y + 14);
        Assert.InRange(result.Value.Width, panelWidth - 18, panelWidth + 18);
        Assert.InRange(result.Value.Height, panelHeight - 18, panelHeight + 18);
        Assert.True(result.Value.Score >= 18);

        var title = ScannerDetailGeometryDetector.GetTitleRegion(result.Value);
        var categoryStartY = result.Value.Y + (int)Math.Round(result.Value.Height * 0.058);
        Assert.True(title.X > result.Value.X);
        Assert.True(title.Y >= result.Value.Y);
        Assert.True(title.X + title.Width < result.Value.X + result.Value.Width);
        Assert.True(title.Y + title.Height < categoryStartY);
    }

    [Fact]
    public void Detect_StrongInnerRectangle_DoesNotStealOuterInspectWindow()
    {
        const int width = 1920;
        const int height = 1080;
        var pixels = CreateFrame(width, height, 26);
        const int panelWidth = 676;
        const int panelHeight = 522;
        const int x = 622;
        const int y = 239;
        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight, drawStrongInnerFrame: true);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: true);

        Assert.NotNull(result);
        Assert.InRange(result.Value.X, x - 18, x + 18);
        Assert.InRange(result.Value.Y, y - 18, y + 18);
        Assert.InRange(result.Value.Width, panelWidth - 28, panelWidth + 28);
        Assert.InRange(result.Value.Height, panelHeight - 28, panelHeight + 28);
    }

    [Fact]
    public void Detect_UniformFrame_ReturnsNull()
    {
        const int width = 960;
        const int height = 540;
        var pixels = CreateFrame(width, height, 48);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: true);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_DisplayTestScaledScreenshot_FindsReducedOuterPanel()
    {
        const int width = 1280;
        const int height = 720;
        var pixels = CreateFrame(width, height, 32);

        const double imageScale = 0.65;
        var panelWidth = (int)Math.Round(width * PanelWidthRatio * imageScale);
        var panelHeight = (int)Math.Round(height * PanelHeightRatio * imageScale);
        var x = (width - panelWidth) / 2;
        var y = (int)Math.Round(height * (500.0 / 1080.0) - panelHeight / 2.0);
        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight, drawStrongInnerFrame: true);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: true);

        Assert.NotNull(result);
        Assert.InRange(result.Value.X, x - 22, x + 22);
        Assert.InRange(result.Value.Y, y - 22, y + 22);
        Assert.InRange(result.Value.Width, panelWidth - 24, panelWidth + 24);
        Assert.InRange(result.Value.Height, panelHeight - 24, panelHeight + 24);
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
        bool drawStrongInnerFrame)
    {
        Fill(pixels, width, height, x, y, panelWidth, panelHeight, 55);
        DrawRect(pixels, width, height, x, y, panelWidth, panelHeight, 170, 3);

        var titleHeight = Math.Max(12, (int)Math.Round(panelHeight * 0.048));
        Fill(pixels, width, height, x + 5, y + 3, panelWidth - 42, titleHeight, 70);
        DrawLine(
            pixels,
            width,
            height,
            x + (int)Math.Round(panelWidth * 0.04),
            y + Math.Max(4, titleHeight / 2),
            x + (int)Math.Round(panelWidth * 0.58),
            y + Math.Max(4, titleHeight / 2),
            215,
            2);

        // Category/breadcrumb row starts below the title. The title ROI must never
        // include this line even if it has cleaner OCR contrast than the item name.
        var categoryY = y + (int)Math.Round(panelHeight * 0.064);
        DrawLine(
            pixels,
            width,
            height,
            x + 7,
            categoryY,
            x + (int)Math.Round(panelWidth * 0.30),
            categoryY,
            145,
            2);

        var glyphSize = Math.Max(14, (int)Math.Round(panelWidth * 0.036));
        var glyphX = x + panelWidth - glyphSize - 5;
        var glyphY = y + 4;
        DrawLine(pixels, width, height, glyphX, glyphY, glyphX + glyphSize - 1, glyphY + glyphSize - 1, 235, 2);
        DrawLine(pixels, width, height, glyphX + glyphSize - 1, glyphY, glyphX, glyphY + glyphSize - 1, 235, 2);

        if (!drawStrongInnerFrame)
            return;

        // Regress the v1.1.1 failure: a smaller high-contrast rectangle inside the
        // inspection window must not beat the real outer frame merely because some of
        // its separators are sharper.
        var innerX = x + (int)Math.Round(panelWidth * 0.44);
        var innerY = y + (int)Math.Round(panelHeight * 0.10);
        var innerWidth = (int)Math.Round(panelWidth * 0.53);
        var innerHeight = (int)Math.Round(panelHeight * 0.68);
        DrawRect(pixels, width, height, innerX, innerY, innerWidth, innerHeight, 220, 2);
    }

    private static void Fill(byte[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, byte value)
    {
        for (var py = Math.Max(0, y); py < Math.Min(height, y + rectHeight); py++)
        {
            for (var px = Math.Max(0, x); px < Math.Min(width, x + rectWidth); px++)
                SetPixel(pixels, width, px, py, value);
        }
    }

    private static void DrawRect(byte[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, byte value, int thickness)
    {
        Fill(pixels, width, height, x, y, rectWidth, thickness, value);
        Fill(pixels, width, height, x, y + rectHeight - thickness, rectWidth, thickness, value);
        Fill(pixels, width, height, x, y, thickness, rectHeight, value);
        Fill(pixels, width, height, x + rectWidth - thickness, y, thickness, rectHeight, value);
    }

    private static void DrawLine(byte[] pixels, int width, int height, int x0, int y0, int x1, int y1, byte value, int thickness)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            Fill(pixels, width, height, x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, value);
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

    private static void SetPixel(byte[] pixels, int width, int x, int y, byte value)
    {
        var offset = (y * width + x) * 4;
        pixels[offset] = value;
        pixels[offset + 1] = value;
        pixels[offset + 2] = value;
        pixels[offset + 3] = 255;
    }
}
