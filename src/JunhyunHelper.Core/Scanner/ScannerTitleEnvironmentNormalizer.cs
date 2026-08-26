namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Pure title-image luminance policy used by the desktop OCR adapter to reduce
/// cross-environment variance without changing Scanner semantic or catalog thresholds.
///
/// The normalizer is intentionally conditional. Reference SDR-like title images keep the
/// historical preprocessing byte-for-byte; only images whose background/foreground
/// distribution makes the historical fixed 105 threshold unreliable switch to an
/// adaptive luminance profile.
/// </summary>
public static class ScannerTitleEnvironmentNormalizer
{
    private const int HistogramBins = 256;
    private const int ReferenceThreshold = 105;
    private const int AdaptiveBackgroundFloor = 70;
    private const int AdaptiveForegroundCeiling = 155;
    private const int AdaptiveContrastFloor = 72;
    private const int MinimumUsableContrast = 18;

    public static ScannerTitleLuminanceProfile AnalyzeBgra(
        ReadOnlySpan<byte> bgra,
        int width,
        int height,
        int stride)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < checked(width * 4))
            throw new ArgumentOutOfRangeException(nameof(stride));
        if (bgra.Length < checked(stride * height))
            throw new ArgumentException("BGRA buffer is smaller than the declared image.", nameof(bgra));

        Span<int> histogram = stackalloc int[HistogramBins];
        histogram.Clear();
        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            for (var x = 0; x < width; x++)
            {
                var offset = row + x * 4;
                var gray = ToGray(bgra[offset + 2], bgra[offset + 1], bgra[offset]);
                histogram[gray]++;
            }
        }

        var count = checked(width * height);
        // Title fields are predominantly dark background. P60 tracks that background
        // even when the full capture has been lifted by HDR/SDR tone mapping.
        var background = Percentile(histogram, count, 0.60);
        // Use a very high percentile so short item names still contribute the foreground
        // estimate instead of letting the large trailing dark title field dominate it.
        var foreground = Percentile(histogram, count, 0.9975);
        var contrast = Math.Max(0, foreground - background);

        var usable = contrast >= MinimumUsableContrast;
        var useAdaptive = usable &&
            (background >= AdaptiveBackgroundFloor ||
             foreground <= AdaptiveForegroundCeiling ||
             contrast <= AdaptiveContrastFloor);

        var threshold = useAdaptive
            ? Math.Clamp(
                background + (int)Math.Round(contrast * 0.42, MidpointRounding.AwayFromZero),
                background + 6,
                Math.Max(background + 6, foreground - 6))
            : ReferenceThreshold;

        return new ScannerTitleLuminanceProfile(
            background,
            foreground,
            contrast,
            threshold,
            usable,
            useAdaptive);
    }

    /// <summary>
    /// Applies the historical deep-OCR preprocessing in normal environments and an
    /// adaptive equivalent when the luminance profile indicates a lifted/washed or
    /// low-contrast capture. Modes intentionally match the existing desktop OCR modes.
    /// </summary>
    public static int TransformGray(
        int gray,
        int mode,
        ScannerTitleLuminanceProfile profile)
    {
        gray = Math.Clamp(gray, 0, 255);
        if (!profile.UseAdaptiveNormalization)
        {
            return mode switch
            {
                1 => Math.Clamp((int)((gray - 55) * 1.8), 0, 255),
                2 => gray >= ReferenceThreshold ? 255 : 0,
                3 => gray >= ReferenceThreshold ? 0 : 255,
                _ => gray,
            };
        }

        return mode switch
        {
            1 => NormalizeGray(gray, profile),
            2 => gray >= profile.AdaptiveThreshold ? 255 : 0,
            3 => gray >= profile.AdaptiveThreshold ? 0 : 255,
            _ => gray,
        };
    }

    public static int ToGray(byte r, byte g, byte b) =>
        (77 * r + 150 * g + 29 * b) >> 8;

    private static int NormalizeGray(int gray, ScannerTitleLuminanceProfile profile)
    {
        if (!profile.HasUsableContrast || profile.ContrastSpan <= 0)
            return gray;

        const int darkTarget = 12;
        const int brightTarget = 245;
        if (gray <= profile.BackgroundLuminance)
            return darkTarget;
        if (gray >= profile.ForegroundLuminance)
            return brightTarget;

        var numerator = (gray - profile.BackgroundLuminance) * (brightTarget - darkTarget);
        return Math.Clamp(
            darkTarget + numerator / profile.ContrastSpan,
            darkTarget,
            brightTarget);
    }

    private static int Percentile(ReadOnlySpan<int> histogram, int count, double percentile)
    {
        if (count <= 0)
            return 0;

        var target = Math.Clamp(
            (int)Math.Ceiling(count * percentile),
            1,
            count);
        var cumulative = 0;
        for (var value = 0; value < histogram.Length; value++)
        {
            cumulative += histogram[value];
            if (cumulative >= target)
                return value;
        }
        return histogram.Length - 1;
    }
}

public readonly record struct ScannerTitleLuminanceProfile(
    int BackgroundLuminance,
    int ForegroundLuminance,
    int ContrastSpan,
    int AdaptiveThreshold,
    bool HasUsableContrast,
    bool UseAdaptiveNormalization);
