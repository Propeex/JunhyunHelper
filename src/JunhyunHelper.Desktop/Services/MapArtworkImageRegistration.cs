using SkiaSharp;

namespace JunhyunHelper.Desktop.Services;

public readonly record struct MapArtworkImageTransform(
    double Scale,
    double TranslateX,
    double TranslateY)
{
    public (double U, double V) Apply(double u, double v) =>
        (
            0.5 + Scale * (u - 0.5) + TranslateX,
            0.5 + Scale * (v - 0.5) + TranslateY);
}

public readonly record struct MapArtworkRegistrationRegion(
    double MinU,
    double MinV,
    double MaxU,
    double MaxV)
{
    public MapArtworkRegistrationRegion Expanded(double amount) =>
        new(
            Math.Clamp(MinU - amount, 0, 1),
            Math.Clamp(MinV - amount, 0, 1),
            Math.Clamp(MaxU + amount, 0, 1),
            Math.Clamp(MaxV + amount, 0, 1));
}

/// <summary>
/// Registers a newly downloaded presentation image against the previous validated revision.
/// The normal RE3MR update case is a mostly unchanged top-down canvas with local artwork edits;
/// therefore a global normalized scale + translation is sufficient and deliberately safer than
/// accepting an unconstrained projective transform. Large redesigns are rejected and fall back to
/// the previous validated artwork instead of silently moving gameplay markers.
/// </summary>
public static class MapArtworkImageRegistration
{
    private const int SamplesPerAxis = 34;
    private const double MinimumUsefulContrast = 0.025;

    public static bool TryRegister(
        byte[] previousImage,
        byte[] currentImage,
        MapArtworkRegistrationRegion region,
        out MapArtworkImageTransform transform,
        out double score)
    {
        transform = new MapArtworkImageTransform(1, 0, 0);
        score = double.NegativeInfinity;
        if (previousImage.Length == 0 || currentImage.Length == 0)
            return false;

        using var previous = SKBitmap.Decode(previousImage);
        using var current = SKBitmap.Decode(currentImage);
        if (previous is null || current is null ||
            previous.Width < 64 || previous.Height < 64 ||
            current.Width < 64 || current.Height < 64)
            return false;

        var bounded = region.Expanded(0.04);
        if (bounded.MaxU - bounded.MinU < 0.1 || bounded.MaxV - bounded.MinV < 0.1)
            return false;

        // Coarse search handles common canvas resize/crop changes while remaining bounded enough
        // that a materially redesigned Map cannot be forced into an apparently valid alignment.
        Search(
            previous,
            current,
            bounded,
            scales: Range(0.90, 1.10, 0.025),
            translations: Range(-0.08, 0.08, 0.02),
            ref transform,
            ref score);

        if (!double.IsFinite(score))
            return false;

        var coarse = transform;
        Search(
            previous,
            current,
            bounded,
            scales: Range(coarse.Scale - 0.035, coarse.Scale + 0.035, 0.007),
            translationsX: Range(coarse.TranslateX - 0.025, coarse.TranslateX + 0.025, 0.005),
            translationsY: Range(coarse.TranslateY - 0.025, coarse.TranslateY + 0.025, 0.005),
            ref transform,
            ref score);

        return score >= 0.72 &&
               transform.Scale is >= 0.86 and <= 1.14 &&
               Math.Abs(transform.TranslateX) <= 0.11 &&
               Math.Abs(transform.TranslateY) <= 0.11;
    }

    private static void Search(
        SKBitmap previous,
        SKBitmap current,
        MapArtworkRegistrationRegion region,
        IEnumerable<double> scales,
        IEnumerable<double> translations,
        ref MapArtworkImageTransform best,
        ref double bestScore) =>
        Search(
            previous,
            current,
            region,
            scales,
            translations,
            translations,
            ref best,
            ref bestScore);

    private static void Search(
        SKBitmap previous,
        SKBitmap current,
        MapArtworkRegistrationRegion region,
        IEnumerable<double> scales,
        IEnumerable<double> translationsX,
        IEnumerable<double> translationsY,
        ref MapArtworkImageTransform best,
        ref double bestScore)
    {
        foreach (var scale in scales)
        foreach (var tx in translationsX)
        foreach (var ty in translationsY)
        {
            var candidate = new MapArtworkImageTransform(scale, tx, ty);
            var candidateScore = Correlation(previous, current, region, candidate);
            if (candidateScore > bestScore)
            {
                bestScore = candidateScore;
                best = candidate;
            }
        }
    }

    private static double Correlation(
        SKBitmap previous,
        SKBitmap current,
        MapArtworkRegistrationRegion region,
        MapArtworkImageTransform transform)
    {
        Span<double> oldValues = stackalloc double[SamplesPerAxis * SamplesPerAxis];
        Span<double> newValues = stackalloc double[SamplesPerAxis * SamplesPerAxis];
        var count = 0;

        for (var row = 0; row < SamplesPerAxis; row++)
        {
            var v = Lerp(region.MinV, region.MaxV, (row + 0.5) / SamplesPerAxis);
            for (var column = 0; column < SamplesPerAxis; column++)
            {
                var u = Lerp(region.MinU, region.MaxU, (column + 0.5) / SamplesPerAxis);
                var mapped = transform.Apply(u, v);
                if (mapped.U is < 0 or > 1 || mapped.V is < 0 or > 1)
                    return double.NegativeInfinity;

                oldValues[count] = Luminance(previous, u, v);
                newValues[count] = Luminance(current, mapped.U, mapped.V);
                count++;
            }
        }

        var oldMean = Mean(oldValues[..count]);
        var newMean = Mean(newValues[..count]);
        double covariance = 0;
        double oldVariance = 0;
        double newVariance = 0;
        for (var index = 0; index < count; index++)
        {
            var oldDelta = oldValues[index] - oldMean;
            var newDelta = newValues[index] - newMean;
            covariance += oldDelta * newDelta;
            oldVariance += oldDelta * oldDelta;
            newVariance += newDelta * newDelta;
        }

        oldVariance /= count;
        newVariance /= count;
        if (oldVariance < MinimumUsefulContrast * MinimumUsefulContrast ||
            newVariance < MinimumUsefulContrast * MinimumUsefulContrast)
            return double.NegativeInfinity;

        var denominator = Math.Sqrt(oldVariance * count * newVariance * count);
        return denominator <= 0 ? double.NegativeInfinity : covariance / denominator;
    }

    private static double Luminance(SKBitmap bitmap, double u, double v)
    {
        var x = Math.Clamp((int)Math.Round(u * (bitmap.Width - 1)), 0, bitmap.Width - 1);
        var y = Math.Clamp((int)Math.Round(v * (bitmap.Height - 1)), 0, bitmap.Height - 1);
        var color = bitmap.GetPixel(x, y);
        return (0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue) / 255.0;
    }

    private static double Mean(ReadOnlySpan<double> values)
    {
        double sum = 0;
        foreach (var value in values)
            sum += value;
        return values.Length == 0 ? 0 : sum / values.Length;
    }

    private static double Lerp(double first, double second, double amount) =>
        first + (second - first) * amount;

    private static IEnumerable<double> Range(double start, double end, double step)
    {
        if (step <= 0)
            throw new ArgumentOutOfRangeException(nameof(step));
        for (var value = start; value <= end + step * 0.25; value += step)
            yield return value;
    }
}
