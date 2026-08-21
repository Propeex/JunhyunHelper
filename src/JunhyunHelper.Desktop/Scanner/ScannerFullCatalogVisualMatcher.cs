using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// OCR-independent recovery path for badly corrupted or empty title OCR. The current
/// official catalog is treated as a closed candidate set. Candidate names are first
/// pruned by rendered title aspect, then a small shortlist is rasterized with Tarkov's
/// Bender + Noto CJK KR font stack and compared directly with the observed glyph mask.
/// Acceptance is deliberately strict because this path must prefer a miss over a false
/// positive.
/// </summary>
public sealed class ScannerFullCatalogVisualMatcher : IDisposable
{
    private const int CoarseShortlistPerVariant = 96;
    private const int DetailedShortlist = 72;
    private const int NormalizedHeight = 38;

    private readonly TarkovTitleFontProvider _fontProvider;
    private readonly ConcurrentDictionary<string, BinaryMask> _maskCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, double> _aspectCache = new(StringComparer.Ordinal);
    private bool _disposed;

    public ScannerFullCatalogVisualMatcher(TarkovTitleFontProvider fontProvider)
    {
        _fontProvider = fontProvider ?? throw new ArgumentNullException(nameof(fontProvider));
    }

    public FontVerificationResult? TryRecover(
        BitmapSource observedTitle,
        string? filteredOcrText,
        IReadOnlyList<ScannerCatalogItem> catalog,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(observedTitle);
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.Count == 0 || !_fontProvider.TryGetFonts(out var fonts))
            return null;

        var observed = CreateObservedMask(observedTitle);
        if (observed is null)
            return null;

        var observedAspect = observed.Width / (double)Math.Max(1, observed.Height);
        var ocrVariants = BuildTextVariants(filteredOcrText);
        var stopwatch = Stopwatch.StartNew();
        var coarse = new Dictionary<string, CoarseCandidate>(StringComparer.Ordinal);

        foreach (var bender in fonts.BenderVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var variantName = VariantName(bender);
            foreach (var candidate in catalog
                         .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
                         .Select(item =>
                         {
                             var aspect = GetRenderedAspect(item.OfficialName, bender, fonts.NotoKorean, variantName);
                             var ratio = aspect <= 0 || observedAspect <= 0
                                 ? 0
                                 : Math.Min(aspect, observedAspect) / Math.Max(aspect, observedAspect);
                             var semantic = BestSemanticScore(item.OfficialName, ocrVariants);
                             return new CoarseCandidate(item, variantName, ratio, semantic);
                         })
                         .Where(candidate => candidate.AspectScore >= 0.52)
                         .OrderByDescending(candidate => candidate.AspectScore + candidate.SemanticScore * 0.10)
                         .Take(CoarseShortlistPerVariant))
            {
                if (!coarse.TryGetValue(candidate.Item.Id, out var existing) ||
                    candidate.AspectScore + candidate.SemanticScore * 0.10 >
                    existing.AspectScore + existing.SemanticScore * 0.10)
                {
                    coarse[candidate.Item.Id] = candidate;
                }
            }
        }

        var detailedCandidates = coarse.Values
            .OrderByDescending(candidate => candidate.AspectScore + candidate.SemanticScore * 0.12)
            .Take(DetailedShortlist)
            .ToArray();
        if (detailedCandidates.Length == 0)
            return null;

        var scored = new List<VisualCandidate>(detailedCandidates.Length);
        foreach (var candidate in detailedCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var typeface = candidate.FontVariant.Contains("BOLD", StringComparison.Ordinal)
                ? fonts.BenderBold ?? fonts.BenderRegular
                : fonts.BenderRegular ?? fonts.BenderBold;
            if (typeface is null)
                continue;

            var cacheKey = $"{candidate.FontVariant}\n{candidate.Item.OfficialName}";
            BinaryMask template;
            try
            {
                template = _maskCache.GetOrAdd(
                    cacheKey,
                    _ => RenderTemplate(candidate.Item.OfficialName, typeface, fonts.NotoKorean));
            }
            catch (InvalidDataException)
            {
                continue;
            }

            var visual = CompareMasks(observed, template);
            if (visual <= 0)
                continue;

            // OCR contributes only weak supporting evidence here. The visual path must
            // stand on its own because this method is specifically for corrupted OCR.
            var combined = visual * 0.88 + candidate.SemanticScore * 0.08 + candidate.AspectScore * 0.04;
            scored.Add(new VisualCandidate(
                candidate.Item,
                candidate.FontVariant,
                candidate.AspectScore,
                candidate.SemanticScore,
                visual,
                combined));
        }

        var ordered = scored
            .OrderByDescending(candidate => candidate.CombinedScore)
            .ThenByDescending(candidate => candidate.VisualScore)
            .Take(2)
            .ToArray();
        if (ordered.Length == 0)
            return null;

        var best = ordered[0];
        var second = ordered.Length > 1 ? ordered[1] : null;
        var secondScore = second?.CombinedScore ?? 0;
        var normalizedLength = ScannerItemMatcher.Normalize(best.Item.OfficialName).Length;
        var shortName = normalizedLength <= 8;
        var minimumAspect = shortName ? 0.68 : 0.62;
        var minimumVisual = shortName ? 0.86 : 0.82;
        var minimumCombined = shortName ? 0.86 : 0.82;
        var minimumMargin = shortName ? 0.075 : 0.060;

        ScannerDiagnosticLog.Write(
            "title-visual-search",
            null,
            ("catalog", catalog.Count),
            ("coarse", coarse.Count),
            ("detailed", detailedCandidates.Length),
            ("elapsedMs", stopwatch.ElapsedMilliseconds),
            ("candidate", best.Item.OfficialName),
            ("aspect", best.AspectScore),
            ("semantic", best.SemanticScore),
            ("visual", best.VisualScore),
            ("combined", best.CombinedScore),
            ("second", secondScore),
            ("fontVariant", best.FontVariant));

        if (best.AspectScore < minimumAspect ||
            best.VisualScore < minimumVisual ||
            best.CombinedScore < minimumCombined ||
            best.CombinedScore - secondScore < minimumMargin)
        {
            return null;
        }

        var recognition = new ScannerRecognition(
            true,
            "FONT_VISUAL_VERIFIED",
            best.Item.Id,
            best.Item.OfficialName,
            best.CombinedScore,
            secondScore);
        return new FontVerificationResult(
            recognition,
            best.VisualScore,
            second?.VisualScore ?? 0,
            best.FontVariant);
    }

    private double GetRenderedAspect(
        string text,
        SKTypeface bender,
        SKTypeface korean,
        string variantName)
    {
        var key = $"{variantName}\n{text}";
        return _aspectCache.GetOrAdd(key, _ =>
        {
            using var benderFont = new SKFont(bender, 52f);
            using var koreanFont = new SKFont(korean, 52f);
            using var paint = new SKPaint { IsAntialias = true };
            var width = 0f;
            foreach (var run in BuildFontRuns(text))
            {
                var font = run.Korean ? koreanFont : benderFont;
                width += Math.Max(0, font.MeasureText(run.Text, paint));
            }
            // Cropped Bender/Noto title glyphs usually occupy roughly this share of
            // the nominal 52px em height. This value is only a coarse pruning aid;
            // exact acceptance always uses rasterized mask comparison below.
            return width / 43.0;
        });
    }

    private static double BestSemanticScore(string officialName, IReadOnlyList<string> variants)
    {
        if (variants.Count == 0)
            return 0;
        var normalized = ScannerItemMatcher.Normalize(officialName);
        var best = 0.0;
        foreach (var variant in variants)
            best = Math.Max(best, GlobalSimilarity(normalized, variant));
        return best;
    }

    private static IReadOnlyList<string> BuildTextVariants(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];
        return text.Split(
                ['\r', '\n', '|'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ScannerItemMatcher.Normalize)
            .Where(value => value.Length >= 2)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string VariantName(SKTypeface bender) =>
        bender.IsBold ? "BENDER_BOLD+NOTO_KR" : "BENDER_REGULAR+NOTO_KR";

    private static BinaryMask RenderTemplate(string text, SKTypeface bender, SKTypeface korean)
    {
        const float fontSize = 52f;
        using var benderFont = new SKFont(bender, fontSize);
        using var koreanFont = new SKFont(korean, fontSize);
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
        };

        var runs = BuildFontRuns(text);
        var measured = new List<(string Text, bool Korean, float Width)>(runs.Count);
        var width = 16f;
        foreach (var run in runs)
        {
            var font = run.Korean ? koreanFont : benderFont;
            var runWidth = Math.Max(0, font.MeasureText(run.Text, paint));
            measured.Add((run.Text, run.Korean, runWidth));
            width += runWidth;
        }

        var bitmapWidth = Math.Clamp((int)Math.Ceiling(width) + 12, 64, 4096);
        const int bitmapHeight = 92;
        using var bitmap = new SKBitmap(new SKImageInfo(
            bitmapWidth,
            bitmapHeight,
            SKColorType.Bgra8888,
            SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var x = 8f;
        const float baseline = 66f;
        foreach (var run in measured)
        {
            var font = run.Korean ? koreanFont : benderFont;
            canvas.DrawText(run.Text, x, baseline, SKTextAlign.Left, font, paint);
            x += run.Width;
        }
        canvas.Flush();

        var foreground = new bool[bitmapWidth * bitmapHeight];
        for (var y = 0; y < bitmapHeight; y++)
        {
            for (var xIndex = 0; xIndex < bitmapWidth; xIndex++)
                foreground[y * bitmapWidth + xIndex] = bitmap.GetPixel(xIndex, y).Alpha >= 40;
        }

        return CropMask(new BinaryMask(bitmapWidth, bitmapHeight, foreground))
               ?? throw new InvalidDataException("Rendered Tarkov title template contains no glyph pixels.");
    }

    private static List<FontRun> BuildFontRuns(string text)
    {
        var result = new List<FontRun>();
        if (string.IsNullOrEmpty(text))
            return result;

        var start = 0;
        var currentKorean = ScannerTitleFontVerifier.UsesKoreanFallback(text[0]);
        for (var index = 1; index < text.Length; index++)
        {
            var korean = ScannerTitleFontVerifier.UsesKoreanFallback(text[index]);
            if (korean == currentKorean)
                continue;
            result.Add(new FontRun(text[start..index], currentKorean));
            start = index;
            currentKorean = korean;
        }
        result.Add(new FontRun(text[start..], currentKorean));
        return result;
    }

    private static BinaryMask? CreateObservedMask(BitmapSource source)
    {
        BitmapSource bgra = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        if (bgra.PixelWidth < 4 || bgra.PixelHeight < 4)
            return null;

        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);
        var gray = new byte[bgra.PixelWidth * bgra.PixelHeight];
        for (var y = 0; y < bgra.PixelHeight; y++)
        {
            for (var x = 0; x < bgra.PixelWidth; x++)
            {
                var offset = y * stride + x * 4;
                gray[y * bgra.PixelWidth + x] = (byte)((77 * pixels[offset + 2] + 150 * pixels[offset + 1] + 29 * pixels[offset]) >> 8);
            }
        }

        var threshold = Math.Max(105, OtsuThreshold(gray));
        var foreground = new bool[gray.Length];
        var count = 0;
        for (var index = 0; index < gray.Length; index++)
        {
            if (gray[index] < threshold)
                continue;
            foreground[index] = true;
            count++;
        }

        if (count < 8 || count > gray.Length * 0.42)
            return null;
        return CropMask(new BinaryMask(bgra.PixelWidth, bgra.PixelHeight, foreground));
    }

    private static int OtsuThreshold(ReadOnlySpan<byte> gray)
    {
        Span<int> histogram = stackalloc int[256];
        foreach (var value in gray)
            histogram[value]++;
        var total = gray.Length;
        long sum = 0;
        for (var index = 0; index < 256; index++)
            sum += (long)index * histogram[index];

        long backgroundSum = 0;
        var backgroundWeight = 0;
        var bestThreshold = 128;
        var maximumVariance = -1.0;
        for (var threshold = 0; threshold < 255; threshold++)
        {
            backgroundWeight += histogram[threshold];
            if (backgroundWeight == 0)
                continue;
            var foregroundWeight = total - backgroundWeight;
            if (foregroundWeight == 0)
                break;
            backgroundSum += (long)threshold * histogram[threshold];
            var backgroundMean = (double)backgroundSum / backgroundWeight;
            var foregroundMean = (double)(sum - backgroundSum) / foregroundWeight;
            var difference = backgroundMean - foregroundMean;
            var variance = (double)backgroundWeight * foregroundWeight * difference * difference;
            if (variance > maximumVariance)
            {
                maximumVariance = variance;
                bestThreshold = threshold;
            }
        }
        return bestThreshold;
    }

    private static BinaryMask? CropMask(BinaryMask source)
    {
        var minX = source.Width;
        var minY = source.Height;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                if (!source.Pixels[y * source.Width + x])
                    continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        if (maxX < minX || maxY < minY)
            return null;

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (width < 2 || height < 2)
            return null;
        var pixels = new bool[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = source.Pixels[(minY + y) * source.Width + minX + x];
        }
        return new BinaryMask(width, height, pixels);
    }

    private static double CompareMasks(BinaryMask observed, BinaryMask template)
    {
        var normalizedObserved = ResizeToHeight(observed, NormalizedHeight, 1.0);
        var best = 0.0;
        foreach (var horizontalScale in new[] { 0.86, 0.92, 0.97, 1.0, 1.03, 1.08, 1.14 })
        {
            var normalizedTemplate = ResizeToHeight(template, NormalizedHeight, horizontalScale);
            var widthRatio = Math.Min(normalizedObserved.Width, normalizedTemplate.Width) /
                             (double)Math.Max(normalizedObserved.Width, normalizedTemplate.Width);
            if (widthRatio < 0.66)
                continue;
            var shape = BestTolerantF1(normalizedObserved, normalizedTemplate);
            best = Math.Max(best, shape * 0.88 + widthRatio * 0.12);
        }
        return best;
    }

    private static BinaryMask ResizeToHeight(BinaryMask source, int targetHeight, double horizontalScale)
    {
        var width = Math.Max(1, (int)Math.Round(source.Width * (double)targetHeight / source.Height * horizontalScale));
        var output = new bool[width * targetHeight];
        for (var y = 0; y < targetHeight; y++)
        {
            var sourceY = Math.Min(source.Height - 1, y * source.Height / targetHeight);
            for (var x = 0; x < width; x++)
            {
                var normalizedX = x / Math.Max(0.0001, horizontalScale);
                var sourceX = Math.Min(source.Width - 1, (int)(normalizedX * source.Height / targetHeight));
                output[y * width + x] = source.Pixels[sourceY * source.Width + sourceX];
            }
        }
        return new BinaryMask(width, targetHeight, output);
    }

    private static double BestTolerantF1(BinaryMask left, BinaryMask right)
    {
        var best = 0.0;
        for (var y = -1; y <= 1; y++)
        for (var x = -2; x <= 2; x++)
            best = Math.Max(best, TolerantF1(left, right, x, y));
        return best;
    }

    private static double TolerantF1(BinaryMask left, BinaryMask right, int shiftX, int shiftY)
    {
        var leftCount = CountInk(left);
        var rightCount = CountInk(right);
        if (leftCount == 0 || rightCount == 0)
            return 0;

        var matchedLeft = 0;
        for (var y = 0; y < left.Height; y++)
        for (var x = 0; x < left.Width; x++)
            if (left.Pixels[y * left.Width + x] && HasInkNear(right, x + shiftX, y + shiftY))
                matchedLeft++;

        var matchedRight = 0;
        for (var y = 0; y < right.Height; y++)
        for (var x = 0; x < right.Width; x++)
            if (right.Pixels[y * right.Width + x] && HasInkNear(left, x - shiftX, y - shiftY))
                matchedRight++;

        var precision = matchedLeft / (double)leftCount;
        var recall = matchedRight / (double)rightCount;
        return precision + recall <= 0 ? 0 : 2 * precision * recall / (precision + recall);
    }

    private static int CountInk(BinaryMask mask)
    {
        var count = 0;
        foreach (var pixel in mask.Pixels)
            if (pixel)
                count++;
        return count;
    }

    private static bool HasInkNear(BinaryMask mask, int x, int y)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            var yy = y + dy;
            if (yy < 0 || yy >= mask.Height)
                continue;
            for (var dx = -1; dx <= 1; dx++)
            {
                var xx = x + dx;
                if (xx >= 0 && xx < mask.Width && mask.Pixels[yy * mask.Width + xx])
                    return true;
            }
        }
        return false;
    }

    private static double GlobalSimilarity(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
            return 0;
        if (string.Equals(left, right, StringComparison.Ordinal))
            return 1;
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var index = 0; index <= right.Length; index++)
            previous[index] = index;
        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var substitution = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitution);
            }
            (previous, current) = (current, previous);
        }
        return Math.Clamp(1.0 - previous[right.Length] / (double)Math.Max(left.Length, right.Length), 0, 1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _maskCache.Clear();
        _aspectCache.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed record CoarseCandidate(
        ScannerCatalogItem Item,
        string FontVariant,
        double AspectScore,
        double SemanticScore);

    private sealed record VisualCandidate(
        ScannerCatalogItem Item,
        string FontVariant,
        double AspectScore,
        double SemanticScore,
        double VisualScore,
        double CombinedScore);

    private sealed record FontRun(string Text, bool Korean);
    private sealed record BinaryMask(int Width, int Height, bool[] Pixels);
}
