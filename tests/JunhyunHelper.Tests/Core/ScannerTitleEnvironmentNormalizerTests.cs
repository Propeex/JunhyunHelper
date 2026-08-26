using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class ScannerTitleEnvironmentNormalizerTests
{
    [Fact]
    public void ReferenceSdrProfileKeepsHistoricalDeepOcrTransforms()
    {
        var image = CreateSyntheticTitle(80, 20, background: 32, foreground: 210);
        var profile = Analyze(image);

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
        var image = CreateSyntheticTitle(80, 20, background, foreground);
        var profile = Analyze(image);

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

    [Theory]
    [InlineData(80, 20)]   // 1080p-class title raster
    [InlineData(107, 27)]  // 1440p-class proportional raster
    [InlineData(160, 40)]  // 4K-class proportional raster
    public void CommonResolutionScaleClassesProduceEquivalentAdaptiveProfile(int width, int height)
    {
        var image = CreateSyntheticTitle(width, height, background: 110, foreground: 180);
        var profile = Analyze(image);

        Assert.True(profile.UseAdaptiveNormalization);
        Assert.True(profile.HasUsableContrast);
        Assert.InRange(profile.BackgroundLuminance, 108, 112);
        Assert.InRange(profile.ForegroundLuminance, 178, 182);
        Assert.InRange(profile.AdaptiveThreshold, 132, 142);
        Assert.Equal(0, ScannerTitleEnvironmentNormalizer.TransformGray(110, 2, profile));
        Assert.Equal(255, ScannerTitleEnvironmentNormalizer.TransformGray(180, 2, profile));
    }

    [Fact]
    public void WashedEnvironmentBinaryMaskMatchesReferenceStructure()
    {
        var reference = CreateSyntheticTitle(80, 20, background: 30, foreground: 215);
        var washed = CreateSyntheticTitle(80, 20, background: 112, foreground: 178);
        var referenceProfile = Analyze(reference);
        var washedProfile = Analyze(washed);

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
        var image = CreateSyntheticTitle(80, 20, background: 118, foreground: 128);
        var profile = Analyze(image);

        Assert.False(profile.HasUsableContrast);
        Assert.False(profile.UseAdaptiveNormalization);
        Assert.Equal(105, profile.AdaptiveThreshold);
    }

    private static ScannerTitleLuminanceProfile Analyze(SyntheticImage image) =>
        ScannerTitleEnvironmentNormalizer.AnalyzeBgra(
            image.Pixels,
            image.Width,
            image.Height,
            image.Stride);

    private static SyntheticImage CreateSyntheticTitle(
        int width,
        int height,
        byte background,
        byte foreground)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var glyph = IsGlyphPixel(x, y, width, height);
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

    private static bool IsGlyphPixel(int x, int y, int width, int height)
    {
        // Normalize coordinates to the 80x20 procedural reference so the same title
        // structure can be deterministically replayed across common resolution classes.
        var referenceX = Math.Clamp((int)Math.Floor(x * 80.0 / width), 0, 79);
        var referenceY = Math.Clamp((int)Math.Floor(y * 20.0 / height), 0, 19);
        if (referenceY < 4 || referenceY > 15)
            return false;

        return (referenceX >= 6 && referenceX <= 9) ||
               (referenceX >= 13 && referenceX <= 16 && (referenceY <= 6 || referenceY >= 13)) ||
               (referenceX >= 20 && referenceX <= 23) ||
               (referenceX >= 27 && referenceX <= 35 && (referenceY == 4 || referenceY == 9 || referenceY == 15)) ||
               (referenceX >= 40 && referenceX <= 43) ||
               (referenceX >= 47 && referenceX <= 55 && (referenceY == 4 || referenceY == 15));
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
