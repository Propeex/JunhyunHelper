using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerSparseTitleCropPlannerTests
{
    [Fact]
    public void TryPlan_AwlLikeSparseTitle_RemovesOnlyTrailingBlankCanvas()
    {
        const int width = 985;
        const int height = 28;
        var stride = width * 4;
        var pixels = CreateDarkBgra(width, height, 26);

        // Synthetic non-font glyph strokes occupying the same order of magnitude as the
        // reviewed v1.4.3 Awl title. The planner must not need user screenshot bytes.
        DrawVertical(pixels, stride, 3, 7, 20, 235);
        DrawVertical(pixels, stride, 12, 7, 20, 235);
        DrawVertical(pixels, stride, 20, 9, 20, 235);
        DrawVertical(pixels, stride, 28, 7, 20, 235);
        DrawHorizontal(pixels, stride, 3, 12, 13, 235);
        DrawHorizontal(pixels, stride, 20, 28, 20, 235);

        var success = ScannerSparseTitleCropPlanner.TryPlan(
            pixels,
            width,
            height,
            stride,
            out var plan);

        Assert.True(success);
        Assert.InRange(plan.RightmostInkX, 27, 29);
        Assert.InRange(plan.CropWidth, 50, 70);
        Assert.True(plan.RetainedWidthRatio < 0.10);
        Assert.True(plan.ForegroundPixelCount > 30);
    }

    [Fact]
    public void TryPlan_TitleInkExtendingAcrossField_DoesNotCrop()
    {
        const int width = 300;
        const int height = 28;
        var stride = width * 4;
        var pixels = CreateDarkBgra(width, height, 26);

        for (var x = 4; x < 250; x += 10)
            DrawVertical(pixels, stride, x, 7, 20, 225);

        var success = ScannerSparseTitleCropPlanner.TryPlan(
            pixels,
            width,
            height,
            stride,
            out _);

        Assert.False(success);
    }

    [Fact]
    public void TryPlan_FarRightSinglePixelNoise_DoesNotDefeatSparseCrop()
    {
        const int width = 985;
        const int height = 28;
        var stride = width * 4;
        var pixels = CreateDarkBgra(width, height, 26);

        DrawVertical(pixels, stride, 4, 7, 20, 235);
        DrawVertical(pixels, stride, 14, 7, 20, 235);
        DrawVertical(pixels, stride, 28, 7, 20, 235);
        SetGray(pixels, stride, 900, 10, 245);

        var success = ScannerSparseTitleCropPlanner.TryPlan(
            pixels,
            width,
            height,
            stride,
            out var plan);

        Assert.True(success);
        Assert.True(plan.RightmostInkX < 40);
        Assert.True(plan.CropWidth < 80);
    }

    private static byte[] CreateDarkBgra(int width, int height, byte gray)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                SetGray(pixels, width * 4, x, y, gray);
        }
        return pixels;
    }

    private static void DrawVertical(
        byte[] pixels,
        int stride,
        int x,
        int top,
        int bottom,
        byte gray)
    {
        for (var y = top; y <= bottom; y++)
            SetGray(pixels, stride, x, y, gray);
    }

    private static void DrawHorizontal(
        byte[] pixels,
        int stride,
        int left,
        int right,
        int y,
        byte gray)
    {
        for (var x = left; x <= right; x++)
            SetGray(pixels, stride, x, y, gray);
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
