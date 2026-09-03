using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class ScannerFirMarkerDetectorTests
{
    private const int Width = 400;
    private const int Height = 300;
    private const int Stride = Width * 4;
    private const int WindowX = 40;
    private const int WindowY = 30;
    private const int WindowWidth = 300;
    private const int WindowHeight = 220;

    [Fact]
    public void NeutralCheckCircleInsideLowerLeftInspectAreaIsPositiveEvidence()
    {
        var pixels = BlankFrame();
        DrawMarker(pixels, 70, 220, 7, blue: 205, green: 205, red: 205);

        Assert.True(Detect(pixels));
    }

    [Fact]
    public void YellowQuestCheckCircleAlsoCountsAsFoundInRaidEvidence()
    {
        var pixels = BlankFrame();
        DrawMarker(pixels, 70, 220, 7, blue: 35, green: 185, red: 220);

        Assert.True(Detect(pixels));
    }

    [Fact]
    public void CircleWithoutCheckIsNotEnoughToProveFoundInRaid()
    {
        var pixels = BlankFrame();
        DrawRing(pixels, 70, 220, 7, blue: 205, green: 205, red: 205);

        Assert.False(Detect(pixels));
    }

    [Fact]
    public void CheckWithoutCircleIsNotEnoughToProveFoundInRaid()
    {
        var pixels = BlankFrame();
        DrawCheck(pixels, 70, 220, 7, blue: 205, green: 205, red: 205);

        Assert.False(Detect(pixels));
    }

    [Fact]
    public void MatchingGlyphOutsideLowerLeftInspectAreaIsIgnored()
    {
        var pixels = BlankFrame();
        DrawMarker(pixels, 290, 100, 7, blue: 205, green: 205, red: 205);

        Assert.False(Detect(pixels));
    }

    [Fact]
    public void BrightPanelNoiseDoesNotBecomeFoundInRaidProof()
    {
        var pixels = BlankFrame();
        for (var x = 55; x < 115; x += 8)
        {
            DrawLine(pixels, x, 205, x + 5, 205, blue: 190, green: 190, red: 190);
            DrawLine(pixels, x, 230, x + 5, 230, blue: 210, green: 170, red: 40);
        }

        Assert.False(Detect(pixels));
    }

    private static bool Detect(byte[] pixels) =>
        ScannerFirMarkerDetector.HasFoundInRaidMarker(
            pixels,
            Width,
            Height,
            Stride,
            WindowX,
            WindowY,
            WindowWidth,
            WindowHeight);

    private static byte[] BlankFrame()
    {
        var pixels = new byte[Stride * Height];
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
                SetPixel(pixels, x, y, blue: 28, green: 28, red: 28);
        }
        return pixels;
    }

    private static void DrawMarker(
        byte[] pixels,
        int centerX,
        int centerY,
        int radius,
        byte blue,
        byte green,
        byte red)
    {
        DrawRing(pixels, centerX, centerY, radius, blue, green, red);
        DrawCheck(pixels, centerX, centerY, radius, blue, green, red);
    }

    private static void DrawRing(
        byte[] pixels,
        int centerX,
        int centerY,
        int radius,
        byte blue,
        byte green,
        byte red)
    {
        for (var y = centerY - radius - 1; y <= centerY + radius + 1; y++)
        {
            for (var x = centerX - radius - 1; x <= centerX + radius + 1; x++)
            {
                var distance = Math.Sqrt(
                    (x - centerX) * (x - centerX) +
                    (y - centerY) * (y - centerY));
                if (Math.Abs(distance - radius) <= 0.9)
                    SetPixel(pixels, x, y, blue, green, red);
            }
        }
    }

    private static void DrawCheck(
        byte[] pixels,
        int centerX,
        int centerY,
        int radius,
        byte blue,
        byte green,
        byte red)
    {
        DrawLine(
            pixels,
            (int)Math.Round(centerX - radius * 0.48),
            (int)Math.Round(centerY + radius * 0.02),
            (int)Math.Round(centerX - radius * 0.12),
            (int)Math.Round(centerY + radius * 0.36),
            blue,
            green,
            red);
        DrawLine(
            pixels,
            (int)Math.Round(centerX - radius * 0.12),
            (int)Math.Round(centerY + radius * 0.36),
            (int)Math.Round(centerX + radius * 0.55),
            (int)Math.Round(centerY - radius * 0.42),
            blue,
            green,
            red);
    }

    private static void DrawLine(
        byte[] pixels,
        int x0,
        int y0,
        int x1,
        int y1,
        byte blue,
        byte green,
        byte red)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            SetPixel(pixels, x0, y0, blue, green, red);
            if (x0 == x1 && y0 == y1)
                break;
            var doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                x0 += sx;
            }
            if (doubled <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private static void SetPixel(
        byte[] pixels,
        int x,
        int y,
        byte blue,
        byte green,
        byte red)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;
        var offset = y * Stride + x * 4;
        pixels[offset] = blue;
        pixels[offset + 1] = green;
        pixels[offset + 2] = red;
        pixels[offset + 3] = 255;
    }
}
