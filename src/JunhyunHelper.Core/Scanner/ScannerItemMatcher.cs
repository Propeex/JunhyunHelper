using System.Globalization;
using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Conservative matcher from noisy OCR text to the current official Korean-client
/// catalog. False positives are treated as worse than misses.
/// </summary>
public sealed class ScannerItemMatcher
{
    private ScannerCatalogItem[] _items = [];
    private string[] _normalizedNames = [];
    private Dictionary<string, int> _nameCounts = new(StringComparer.Ordinal);

    public int Count => _items.Length;

    public void ReplaceCatalog(IEnumerable<ScannerCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var materialized = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .ToArray();
        var normalized = new string[materialized.Length];
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < materialized.Length; index++)
        {
            var name = Normalize(materialized[index].OfficialName);
            normalized[index] = name;
            if (name.Length == 0)
                continue;
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        _items = materialized;
        _normalizedNames = normalized;
        _nameCounts = counts;
    }

    public ScannerRecognition Resolve(
        string noisyText,
        double minimumConfidence = 0.90,
        double minimumMargin = 0.05)
    {
        if (_items.Length == 0)
            return ScannerRecognition.Failed("NO_CATALOG");

        var variants = BuildTextVariants(noisyText);
        if (variants.Count == 0)
            return ScannerRecognition.Failed("EMPTY_OCR");

        var match = FindBest(variants);
        if (match.BestIndex < 0)
            return ScannerRecognition.Failed("NO_CANDIDATE");

        var item = _items[match.BestIndex];
        var official = _normalizedNames[match.BestIndex];

        if (_nameCounts.TryGetValue(official, out var duplicates) && duplicates > 1)
        {
            return new ScannerRecognition(
                false,
                "AMBIGUOUS_OFFICIAL_NAME",
                item.Id,
                item.OfficialName,
                match.BestScore,
                match.SecondScore);
        }

        if (match.Exact)
        {
            return new ScannerRecognition(
                true,
                "EXACT",
                item.Id,
                item.OfficialName,
                1.0,
                match.SecondScore);
        }

        var threshold = official.Length switch
        {
            <= 6 => Math.Max(0.98, minimumConfidence),
            <= 12 => Math.Max(0.94, minimumConfidence),
            _ => minimumConfidence,
        };
        var margin = official.Length <= 8
            ? Math.Max(0.08, minimumMargin)
            : minimumMargin;

        if (match.BestScore >= threshold && match.BestScore - match.SecondScore >= margin)
        {
            return new ScannerRecognition(
                true,
                "FUZZY",
                item.Id,
                item.OfficialName,
                match.BestScore,
                match.SecondScore);
        }

        return new ScannerRecognition(
            false,
            "LOW_CONFIDENCE",
            item.Id,
            item.OfficialName,
            match.BestScore,
            match.SecondScore);
    }

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormKC)
            .ToLower(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private MatchResult FindBest(IReadOnlyList<string> variants)
    {
        for (var itemIndex = 0; itemIndex < _normalizedNames.Length; itemIndex++)
        {
            var official = _normalizedNames[itemIndex];
            if (official.Length == 0)
                continue;
            if (!variants.Contains(official, StringComparer.Ordinal))
                continue;

            var exactSecondScore = FindSecondBestScore(itemIndex, variants);
            return new MatchResult(itemIndex, 1.0, exactSecondScore, true);
        }

        var variantBigrams = variants
            .Select(Bigrams)
            .ToArray();
        var prefiltered = new List<(int Index, double Overlap)>();

        for (var index = 0; index < _normalizedNames.Length; index++)
        {
            var official = _normalizedNames[index];
            if (official.Length < 3)
                continue;

            var grams = Bigrams(official);
            if (grams.Count == 0)
                continue;

            var overlap = 0.0;
            foreach (var textGrams in variantBigrams)
            {
                var hits = grams.Count(textGrams.Contains);
                overlap = Math.Max(overlap, (double)hits / grams.Count);
            }

            var requiredOverlap = official.Length <= 6 ? 0.70 : official.Length <= 12 ? 0.50 : 0.35;
            if (overlap >= requiredOverlap)
                prefiltered.Add((index, overlap));
        }

        var bestIndex = -1;
        var best = 0.0;
        var second = 0.0;

        foreach (var candidate in prefiltered
                     .OrderByDescending(candidate => candidate.Overlap)
                     .Take(320))
        {
            var score = variants.Max(variant => GlobalSimilarity(
                _normalizedNames[candidate.Index],
                variant));

            if (score > best)
            {
                second = best;
                best = score;
                bestIndex = candidate.Index;
            }
            else if (score > second)
            {
                second = score;
            }
        }

        return new MatchResult(bestIndex, best, second, false);
    }

    private double FindSecondBestScore(int exactIndex, IReadOnlyList<string> variants)
    {
        var second = 0.0;
        for (var index = 0; index < _normalizedNames.Length; index++)
        {
            if (index == exactIndex || _normalizedNames[index].Length == 0)
                continue;
            var score = variants.Max(variant => GlobalSimilarity(_normalizedNames[index], variant));
            if (score > second)
                second = score;
        }
        return second;
    }

    private static IReadOnlyList<string> BuildTextVariants(string noisyText)
    {
        if (string.IsNullOrWhiteSpace(noisyText))
            return [];

        var variants = new HashSet<string>(StringComparer.Ordinal);
        AddVariant(noisyText);

        foreach (var line in noisyText.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            AddVariant(line);
        }

        return variants.ToArray();

        void AddVariant(string value)
        {
            var normalized = Normalize(value);
            if (normalized.Length >= 2)
                variants.Add(normalized);
        }
    }

    private static HashSet<string> Bigrams(string value)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (value.Length == 1)
        {
            result.Add(value);
            return result;
        }

        for (var index = 0; index < value.Length - 1; index++)
            result.Add(value.Substring(index, 2));
        return result;
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

        var distance = previous[right.Length];
        return Math.Clamp(
            1.0 - (double)distance / Math.Max(left.Length, right.Length),
            0,
            1);
    }

    private sealed record MatchResult(
        int BestIndex,
        double BestScore,
        double SecondScore,
        bool Exact);
}
