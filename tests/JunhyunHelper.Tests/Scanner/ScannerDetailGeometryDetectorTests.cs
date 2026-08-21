using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerDetailGeometryDetectorTests
{
    [Fact]
    public void Detect_FramedCenteredDetailPanel_ReturnsCandidateAndTitleRegion()
    {
        const int width = 960;
        const int height = 540;
        var pixels = CreateFrame(width, height, 38);

        var panelWidth = (int)Math.Round(width * (674.0 / 1920.0));
        var panelHeight = (int)Math.Round(height * (672.0 / 1080.0));
        var x = (width - panelWidth) / 2;
        var y = (int)Math.Round(height * (500.0 / 1080.0) - panelHeight / 2.0);
        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: false);

        Assert.NotNull(result);
        Assert.InRange(result.Value.X, x - 18, x + 18);
        Assert.InRange(result.Value.Y, y - 18, y + 18);
        Assert.InRange(result.Value.Width, panelWidth - 20, panelWidth + 20);
        Assert.InRange(result.Value.Height, panelHeight - 22, panelHeight + 22);
        Assert.True(result.Value.Score >= 14);

        var title = ScannerDetailGeometryDetector.GetTitleRegion(result.Value);
        Assert.True(title.X >= result.Value.X);
        Assert.True(title.Y >= result.Value.Y);
        Assert.True(title.X + title.Width <= result.Value.X + result.Value.Width);
        Assert.True(title.Y + title.Height <= result.Value.Y + result.Value.Height);
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
    public void Detect_DisplayTestScaledScreenshot_FindsReducedPanel()
    {
        const int width = 1280;
        const int height = 720;
        var pixels = CreateFrame(width, height, 32);

        const double imageScale = 0.65;
        var panelWidth = (int)Math.Round(width * (674.0 / 1920.0) * imageScale);
        var panelHeight = (int)Math.Round(height * (672.0 / 1080.0) * imageScale);
        var x = (width - panelWidth) / 2;
        var y = (int)Math.Round(height * (500.0 / 1080.0) - panelHeight / 2.0);
        DrawPanel(pixels, width, height, x, y, panelWidth, panelHeight);

        var result = ScannerDetailGeometryDetector.Detect(pixels, width, height, width * 4, extendedScaleSearch: true);

        Assert.NotNull(result);
        Assert.InRange(result.Value.Width, panelWidth - 28, panelWidth + 28);
        Assert.InRange(result.Value.Height, panelHeight - 30, panelHeight + 30);
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

    private static void DrawPanel(byte[] pixels, int width, int height, int x, int y, int panelWidth, int panelHeight)
    {
        Fill(pixels, width, height, x, y, panelWidth, panelHeight, 68);
        DrawRect(pixels, width, height, x, y, panelWidth, panelHeight, 170, 3);

        var titleHeight = Math.Max(28, (int)Math.Round(panelHeight * 0.075));
        Fill(pixels, width, height, x + 8, y + 7, panelWidth - 48, titleHeight, 82);
        DrawLine(pixels, width, height, x + 20, y + titleHeight / 2, x + panelWidth / 2, y + titleHeight / 2, 205, 2);

        var glyphSize = Math.Max(18, Math.Min(panelWidth, panelHeight) / 22);
        var glyphX = x + panelWidth - glyphSize - 7;
        var glyphY = y + 7;
        DrawLine(pixels, width, height, glyphX, glyphY, glyphX + glyphSize - 1, glyphY + glyphSize - 1, 235, 2);
        DrawLine(pixels, width, height, glyphX + glyphSize - 1, glyphY, glyphX, glyphY + glyphSize - 1, 235, 2);
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
