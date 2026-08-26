using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Tests.Core;

public sealed class ScannerTitleEnvironmentNormalizerTests
{
    [Fact]
    public void ReferenceSdrProfileKeepsHistoricalDeepOcrTransforms()
    {
        var image = CreateSyntheticTitle(background: 32, foreground: 210);
        var profile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride);

        Assert.False(profile.UseAdaptiveNormalization);
        Assert.True(profile.HasUsableContrast);
        Assert.Equal(Math.Clamp((int)((120 - 55) * 1.8), 0, 255),
            ScannerTitleEnvironmentNormalizer.TransformGray(120, 1, profile));
        Assert.Equal(255, ScannerTitleEnvironmentNormalizer.TransformGray(120, 2, profile));
        Assert.Equal(0, ScannerTitleEnvironmentNormalizer.TransformGray(120, 3, profile));
        Assert.Equal(0, ScannerTitleEnvironmentNormalizer.TransformGray(90, 2, profile));
        Assert.Equal(255, ScannerTitleEnvironmentNormalizer.TransformGray(90, 3, profile));
    }

    [Theory]
    [InlineData(108, 188)] // lifted / washed HDR-to-SDR-like capture
    [InlineData(92, 150)]  // lifted and compressed contrast
    [InlineData(74, 142)]  // low-contrast gamma/rendering variation
    public void EnvironmentShiftUsesAdaptiveThresholdAndRetainsGlyphSeparation(
        byte background,
        byte foreground)
    {
        var image = CreateSyntheticTitle(background, foreground);
        var profile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride);

        Assert.True(profile.UseAdaptiveNormalization);
        Assert.True(profile.HasUsableContrast);
        Assert.InRange(profile.AdaptiveThreshold, background + 5, foreground - 5);

        Assert.Equal(0,
            ScannerTitleEnvironmentNormalizer.TransformGray(background, 2, profile));
        Assert.Equal(255,
            ScannerTitleEnvironmentNormalizer.TransformGray(foreground, 2, profile));
        Assert.Equal(255,
            ScannerTitleEnvironmentNormalizer.TransformGray(background, 3, profile));
        Assert.Equal(0,
            ScannerTitleEnvironmentNormalizer.TransformGray(foreground, 3, profile));

        var normalizedBackground = ScannerTitleEnvironmentNormalizer.TransformGray(background, 1, profile);
        var normalizedForeground = ScannerTitleEnvironmentNormalizer.TransformGray(foreground, 1, profile);
        Assert.InRange(normalizedBackground, 0, 24);
        Assert.InRange(normalizedForeground, 230, 255);
        Assert.True(normalizedForeground - normalizedBackground >= 200);
    }

    [Fact]
    public void WashedEnvironmentBinaryMaskMatchesReferenceStructure()
    {
        var reference = CreateSyntheticTitle(background: 30, foreground: 215);
        var washed = CreateSyntheticTitle(background: 112, foreground: 178);
        var referenceProfile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            reference.Pixels,
            reference.Width,
            reference.Height,
            reference.Stride);
        var washedProfile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            washed.Pixels,
            washed.Width,
            washed.Height,
            washed.Stride);

        Assert.False(referenceProfile.UseAdaptiveNormalization);
        Assert.True(washedProfile.UseAdaptiveNormalization);

        var matches = 0;
        var samples = 0;
        for (var y = 0; y < reference.Height; y++)
        {
            for (var x = 0; x < reference.Width; x++)
            {
                var referenceGray = ReadGray(reference, x, y);
                var washedGray = ReadGray(washed, x, y);
                var referenceBit = ScannerTitleEnvironmentNormalizer.TransformGray(referenceGray, 2, referenceProfile);
                var washedBit = ScannerTitleEnvironmentNormalizer.TransformGray(washedGray, 2, washedProfile);
                if (referenceBit == washedBit)
                    matches++;
                samples++;
            }
        }

        Assert.True(matches / (double)samples >= 0.995);
    }

    [Fact]
    public void VeryFlatImageDoesNotInventAdaptiveContrast()
    {
        var image = CreateSyntheticTitle(background: 118, foreground: 128);
        var profile = ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride);

        Assert.False(profile.HasUsableContrast);
        Assert.False(profile.UseAdaptiveNormalization);
        Assert.Equal(105, profile.AdaptiveThreshold);
    }

    private static SyntheticImage CreateSyntheticTitle(byte background, byte foreground)
    {
        const int width = 80;
        const int height = 20;
        var stride = width * 4;
        var pixels = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var glyph = IsGlyphPixel(x, y);
                var value = glyph ? foreground : background;
                var offset = y * stride + x * 4;
                pixels[offset] = value;
                pixels[offset + 1] = value;
                pixels[offset + 2] = value;
                pixels[offset + 3] = 255;
            }
        }

        return new SyntheticImage(width, height, stride, pixels);
    }

    private static bool IsGlyphPixel(int x, int y)
    {
        if (y < 4 || y > 15)
            return false;

        // Procedural title-like strokes. They intentionally occupy less than half the
        // field so percentile analysis must distinguish sparse bright glyphs from the
        // long trailing dark title background.
        return (x >= 6 && x <= 9) ||
               (x >= 13 && x <= 16 && (y <= 6 || y >= 13)) ||
               (x >= 20 && x <= 23) ||
               (x >= 27 && x <= 35 && (y == 4 || y == 9 || y == 15)) ||
               (x >= 40 && x <= 43) ||
               (x >= 47 && x <= 55 && (y == 4 || y == 15));
    }

    private static int ReadGray(SyntheticImage image, int x, int y)
    {
        var offset = y * image.Stride + x * 4;
        return ScannerTitleEnvironmentNormalizer.ToGray(
            image.Pixels[offset + 2],
            image.Pixels[offset + 1],
            image.Pixels[offset]);
    }

    private readonly record struct SyntheticImage(
        int Width,
        int Height,
        int Stride,
        byte[] Pixels);
}
