using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JunhyunHelper.Core.Scanner;
using SkiaSharp;

namespace JunhyunHelper.Desktop.Scanner;

/// <summary>
/// Conservative second-stage verifier for OCR failures. It renders current official
/// item-name candidates with Tarkov's own title font stack (Bender primary + Korean
/// Noto fallback) and compares glyph shape against the already-cropped title pixels.
/// Existing OCR successes are never weakened by this verifier.
/// </summary>
public sealed class ScannerTitleFontVerifier : IDisposable
{
    private const int ShortlistLimit = 12;
    private const int NormalizedHeight = 36;
    private readonly TarkovTitleFontProvider _fonts;
    private readonly ConcurrentDictionary<string, BinaryMask> _templateCache = new(StringComparer.Ordinal);
    private bool _disposed;

    public ScannerTitleFontVerifier(TarkovTitleFontProvider fonts)
    {
        _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
    }

    public FontVerificationResult? TryRecover(
        BitmapSource observedTitle,
        string ocrText,
        IReadOnlyList<ScannerCatalogItem> catalog,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(observedTitle);
        ArgumentNullException.ThrowIfNull(catalog);

        var variants = BuildTextVariants(ocrText);
        if (variants.Count == 0 || catalog.Count == 0)
            return null;
        if (!_fonts.TryGetFonts(out var fonts))
            return null;

        var observed = CreateObservedMask(observedTitle);
        if (observed is null)
            return null;

        var shortlist = BuildShortlist(variants, catalog);
        if (shortlist.Count == 0)
            return null;

        var scored = new List<VisualCandidate>(shortlist.Count);
        foreach (var candidate in shortlist)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var visual = ScoreRenderedCandidate(observed, candidate.Item.OfficialName, fonts);
            if (visual.Score <= 0)
                continue;

            var combined = candidate.SemanticScore * 0.46 + visual.Score * 0.54;
            scored.Add(new VisualCandidate(
                candidate.Item,
                candidate.SemanticScore,
                visual.Score,
                combined,
                visual.Variant));
        }

        if (scored.Count == 0)
            return null;

        var ordered = scored
            .OrderByDescending(candidate => candidate.CombinedScore)
            .ThenByDescending(candidate => candidate.VisualScore)
            .Take(2)
            .ToArray();
        var best = ordered[0];
        var secondCombined = ordered.Length > 1 ? ordered[1].CombinedScore : 0;
        var secondVisual = ordered.Length > 1 ? ordered[1].VisualScore : 0;

        var normalizedLength = ScannerItemMatcher.Normalize(best.Item.OfficialName).Length;
        var shortName = normalizedLength <= 8;
        var minimumSemantic = shortName ? 0.70 : 0.64;
        var minimumVisual = shortName ? 0.75 : 0.66;
        var minimumCombined = shortName ? 0.80 : 0.75;
        var minimumMargin = shortName ? 0.08 : 0.055;

        if (best.SemanticScore < minimumSemantic ||
            best.VisualScore < minimumVisual ||
            best.CombinedScore < minimumCombined ||
            best.CombinedScore - secondCombined < minimumMargin)
        {
            ScannerDiagnosticLog.Write(
                "title-font-verify-rejected",
                null,
                ("ocr", ocrText),
                ("candidate", best.Item.OfficialName),
                ("semantic", best.SemanticScore),
                ("visual", best.VisualScore),
                ("combined", best.CombinedScore),
                ("secondCombined", secondCombined),
                ("fontVariant", best.FontVariant));
            return null;
        }

        var recognition = new ScannerRecognition(
            true,
            "FONT_VERIFIED",
            best.Item.Id,
            best.Item.OfficialName,
            best.CombinedScore,
            secondCombined);

        ScannerDiagnosticLog.Write(
            "title-font-verify-accepted",
            null,
            ("ocr", ocrText),
            ("candidate", best.Item.OfficialName),
            ("semantic", best.SemanticScore),
            ("visual", best.VisualScore),
            ("combined", best.CombinedScore),
            ("secondCombined", secondCombined),
            ("fontVariant", best.FontVariant));

        return new FontVerificationResult(recognition, best.VisualScore, secondVisual, best.FontVariant);
    }

    private VisualScore ScoreRenderedCandidate(
        BinaryMask observed,
        string officialName,
        TarkovTitleFonts fonts)
    {
        VisualScore best = default;

        var variants = fonts.BenderVariants.ToArray();
        if (variants.Length == 0)
            return best;

        foreach (var bender in variants)
        {
            var variantName = bender.IsBold ? "BENDER_BOLD+NOTO_KR" : "BENDER_REGULAR+NOTO_KR";
            var key = $"{variantName}\n{officialName}";
            BinaryMask template;
            try
            {
                template = _templateCache.GetOrAdd(
                    key,
                    _ => RenderTemplate(officialName, bender, fonts.NotoKorean));
            }
            catch (InvalidDataException)
            {
                continue;
            }

            var score = CompareMasks(observed, template);
            if (score > best.Score)
                best = new VisualScore(score, variantName);
        }

        return best;
    }

    private static IReadOnlyList<SemanticCandidate> BuildShortlist(
        IReadOnlyList<string> variants,
        IReadOnlyList<ScannerCatalogItem> catalog)
    {
        var candidates = new List<SemanticCandidate>(catalog.Count);
        foreach (var item in catalog)
        {
            var official = ScannerItemMatcher.Normalize(item.OfficialName);
            if (official.Length < 2)
                continue;

            var score = 0.0;
            foreach (var variant in variants)
                score = Math.Max(score, GlobalSimilarity(official, variant));
            if (score < 0.50)
                continue;

            candidates.Add(new SemanticCandidate(item, score));
        }

        return candidates
            .OrderByDescending(candidate => candidate.SemanticScore)
            .Take(ShortlistLimit)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildTextVariants(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var variants = new HashSet<string>(StringComparer.Ordinal);
        Add(text);
        foreach (var line in text.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Add(line);
        }

        return variants.ToArray();

        void Add(string value)
        {
            var normalized = ScannerItemMatcher.Normalize(value);
            if (normalized.Length >= 2)
                variants.Add(normalized);
        }
    }

    private static BinaryMask RenderTemplate(
        string text,
        SKTypeface bender,
        SKTypeface korean)
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
            {
                var color = bitmap.GetPixel(xIndex, y);
                foreground[y * bitmapWidth + xIndex] = color.Alpha >= 40;
            }
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
        var currentKorean = UsesKoreanFallback(text[0]);
        for (var index = 1; index < text.Length; index++)
        {
            var korean = UsesKoreanFallback(text[index]);
            if (korean == currentKorean)
                continue;

            result.Add(new FontRun(text[start..index], currentKorean));
            start = index;
            currentKorean = korean;
        }
        result.Add(new FontRun(text[start..], currentKorean));
        return result;
    }

    internal static bool UsesKoreanFallback(char character) =>
        character is >= '\u1100' and <= '\u11FF' ||
        character is >= '\u3130' and <= '\u318F' ||
        character is >= '\uA960' and <= '\uA97F' ||
        character is >= '\uAC00' and <= '\uD7A3' ||
        character is >= '\uD7B0' and <= '\uD7FF';

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
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                gray[y * bgra.PixelWidth + x] = (byte)((77 * r + 150 * g + 29 * b) >> 8);
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

        if (count < 8 || count > gray.Length * 0.45)
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
        for (var index = 0; index < histogram.Length; index++)
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
            if (variance <= maximumVariance)
                continue;
            maximumVariance = variance;
            bestThreshold = threshold;
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

        var cropped = new bool[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
                cropped[y * width + x] = source.Pixels[(minY + y) * source.Width + minX + x];
        }
        return new BinaryMask(width, height, cropped);
    }

    private static double CompareMasks(BinaryMask observed, BinaryMask template)
    {
        var normalizedObserved = ResizeToHeight(observed, NormalizedHeight, 1.0);
        var best = 0.0;
        foreach (var horizontalScale in new[] { 0.88, 0.94, 1.0, 1.06, 1.12 })
        {
            var normalizedTemplate = ResizeToHeight(template, NormalizedHeight, horizontalScale);
            var widthRatio = (double)Math.Min(normalizedObserved.Width, normalizedTemplate.Width) /
                             Math.Max(normalizedObserved.Width, normalizedTemplate.Width);
            if (widthRatio < 0.62)
                continue;

            var shape = BestTolerantF1(normalizedObserved, normalizedTemplate);
            var score = shape * 0.86 + widthRatio * 0.14;
            best = Math.Max(best, score);
        }
        return best;
    }

    private static BinaryMask ResizeToHeight(BinaryMask source, int targetHeight, double horizontalScale)
    {
        var baseWidth = Math.Max(1, (int)Math.Round(
            source.Width * (double)targetHeight / source.Height * horizontalScale));
        var output = new bool[baseWidth * targetHeight];

        for (var y = 0; y < targetHeight; y++)
        {
            var sourceY = Math.Min(source.Height - 1, (int)((long)y * source.Height / targetHeight));
            for (var x = 0; x < baseWidth; x++)
            {
                var normalizedX = x / Math.Max(0.0001, horizontalScale);
                var sourceX = Math.Min(
                    source.Width - 1,
                    (int)(normalizedX * source.Height / targetHeight));
                output[y * baseWidth + x] = source.Pixels[sourceY * source.Width + sourceX];
            }
        }

        return new BinaryMask(baseWidth, targetHeight, output);
    }

    private static double BestTolerantF1(BinaryMask left, BinaryMask right)
    {
        var best = 0.0;
        for (var shiftY = -1; shiftY <= 1; shiftY++)
        {
            for (var shiftX = -2; shiftX <= 2; shiftX++)
                best = Math.Max(best, TolerantF1(left, right, shiftX, shiftY));
        }
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
        {
            for (var x = 0; x < left.Width; x++)
            {
                if (!left.Pixels[y * left.Width + x])
                    continue;
                if (HasInkNear(right, x + shiftX, y + shiftY))
                    matchedLeft++;
            }
        }

        var matchedRight = 0;
        for (var y = 0; y < right.Height; y++)
        {
            for (var x = 0; x < right.Width; x++)
            {
                if (!right.Pixels[y * right.Width + x])
                    continue;
                if (HasInkNear(left, x - shiftX, y - shiftY))
                    matchedRight++;
            }
        }

        var precision = (double)matchedLeft / leftCount;
        var recall = (double)matchedRight / rightCount;
        return precision + recall <= 0
            ? 0
            : 2 * precision * recall / (precision + recall);
    }

    private static int CountInk(BinaryMask mask)
    {
        var count = 0;
        foreach (var pixel in mask.Pixels)
        {
            if (pixel)
                count++;
        }
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
                if (xx < 0 || xx >= mask.Width)
                    continue;
                if (mask.Pixels[yy * mask.Width + xx])
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

        return Math.Clamp(
            1.0 - (double)previous[right.Length] / Math.Max(left.Length, right.Length),
            0,
            1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _templateCache.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed record SemanticCandidate(ScannerCatalogItem Item, double SemanticScore);
    private sealed record VisualCandidate(
        ScannerCatalogItem Item,
        double SemanticScore,
        double VisualScore,
        double CombinedScore,
        string FontVariant);
    private sealed record FontRun(string Text, bool Korean);
    private sealed record BinaryMask(int Width, int Height, bool[] Pixels);
    private readonly record struct VisualScore(double Score, string Variant);
}

public sealed record FontVerificationResult(
    ScannerRecognition Recognition,
    double VisualScore,
    double SecondVisualScore,
    string FontVariant);
