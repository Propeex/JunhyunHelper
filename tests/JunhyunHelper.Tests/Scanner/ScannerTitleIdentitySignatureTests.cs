using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerTitleIdentitySignatureTests
{
    [Fact]
    public void SameTitleInkIgnoresDarkBackgroundNoiseAndTrailingWidth()
    {
        var first = CreateTitle(120, 20, 28);
        DrawReferenceInk(first.Pixels, first.Width, first.Height, first.Stride);

        var second = CreateTitle(164, 20, 31);
        DrawReferenceInk(second.Pixels, second.Width, second.Height, second.Stride);
        SetGray(second.Pixels, second.Stride, 150, 10, 52);
        SetGray(second.Pixels, second.Stride, 155, 12, 48);

        Assert.True(ScannerTitleIdentitySignature.TryCompute(
            first.Pixels, first.Width, first.Height, first.Stride, out var firstSignature));
        Assert.True(ScannerTitleIdentitySignature.TryCompute(
            second.Pixels, second.Width, second.Height, second.Stride, out var secondSignature));
        Assert.Equal(firstSignature, secondSignature);
    }

    [Fact]
    public void ChangedGlyphShapeProducesDifferentIdentity()
    {
        var first = CreateTitle(140, 20, 28);
        DrawReferenceInk(first.Pixels, first.Width, first.Height, first.Stride);

        var second = CreateTitle(140, 20, 28);
        DrawReferenceInk(second.Pixels, second.Width, second.Height, second.Stride);
        DrawRect(second.Pixels, second.Stride, 31, 7, 4, 8, 220);

        Assert.True(ScannerTitleIdentitySignature.TryCompute(
            first.Pixels, first.Width, first.Height, first.Stride, out var firstSignature));
        Assert.True(ScannerTitleIdentitySignature.TryCompute(
            second.Pixels, second.Width, second.Height, second.Stride, out var secondSignature));
        Assert.NotEqual(firstSignature, secondSignature);
    }

    [Fact]
    public void NoVisibleTitleInkFailsClosed()
    {
        var title = CreateTitle(120, 20, 30);

        Assert.False(ScannerTitleIdentitySignature.TryCompute(
            title.Pixels, title.Width, title.Height, title.Stride, out _));
    }

    private static (byte[] Pixels, int Width, int Height, int Stride) CreateTitle(
        int width,
        int height,
        byte background)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                SetGray(pixels, stride, x, y, background);
        }
        return (pixels, width, height, stride);
    }

    private static void DrawReferenceInk(byte[] pixels, int width, int height, int stride)
    {
        _ = width;
        _ = height;
        DrawRect(pixels, stride, 8, 4, 3, 12, 220);
        DrawRect(pixels, stride, 8, 4, 13, 3, 220);
        DrawRect(pixels, stride, 8, 10, 10, 3, 220);
        DrawRect(pixels, stride, 23, 4, 3, 12, 220);
        DrawRect(pixels, stride, 23, 13, 7, 3, 220);
    }

    private static void DrawRect(
        byte[] pixels,
        int stride,
        int x,
        int y,
        int width,
        int height,
        byte gray)
    {
        for (var row = y; row < y + height; row++)
        {
            for (var column = x; column < x + width; column++)
                SetGray(pixels, stride, column, row, gray);
        }
    }

    private static void SetGray(byte[] pixels, int stride, int x, int y, byte gray)
    {
        var offset = y * stride + x * 4;
        pixels[offset] = gray;
        pixels[offset + 1] = gray;
        pixels[offset + 2] = gray;
        pixels[offset + 3] = 255;
    }
}