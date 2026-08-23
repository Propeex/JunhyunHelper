using System.Globalization;
using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Conservative matcher from noisy OCR text to the current official Korean-client
/// catalog. False positives are treated as worse than misses.
/// </summary>
public sealed class ScannerItemMatcher
{
    private const int DiagnosticCandidateLimit = 5;

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
                match.SecondScore,
                match.TopCandidates);
        }

        if (match.Exact)
        {
            return new ScannerRecognition(
                true,
                "EXACT",
                item.Id,
                item.OfficialName,
                1.0,
                match.SecondScore,
                match.TopCandidates);
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
        var scoreMargin = match.BestScore - match.SecondScore;

        if (match.BestScore >= threshold && scoreMargin >= margin)
        {
            return new ScannerRecognition(
                true,
                "FUZZY",
                item.Id,
                item.OfficialName,
                match.BestScore,
                match.SecondScore,
                match.TopCandidates);
        }

        // A medium-length title can fall below the percentage floor from exactly one
        // missing/substituted OCR glyph. Recover only when the one-edit candidate is
        // unique across the complete current catalog and clearly separated globally.
        if (official.Length >= 7 &&
            TryFindUniqueSingleEditCandidate(variants, out var singleEditIndex) &&
            singleEditIndex == match.BestIndex)
        {
            var globalRanking = RankAllCandidates(variants, DiagnosticCandidateLimit);
            var globalSecondScore = globalRanking
                .Where(candidate => candidate.Index != match.BestIndex)
                .Select(candidate => candidate.Score)
                .DefaultIfEmpty(0)
                .Max();
            var boundedEditMargin = Math.Max(0.10, minimumMargin);
            if (match.BestScore - globalSecondScore >= boundedEditMargin)
            {
                return new ScannerRecognition(
                    true,
                    "BOUNDED_EDIT_1",
                    item.Id,
                    item.OfficialName,
                    match.BestScore,
                    globalSecondScore,
                    ToPublicRanking(globalRanking));
            }
        }

        return new ScannerRecognition(
            false,
            "LOW_CONFIDENCE",
            item.Id,
            item.OfficialName,
            match.BestScore,
            match.SecondScore,
            match.TopCandidates);
    }

    /// <summary>
    /// Resolves one unknown OCR glyph without guessing its character. The pattern uses
    /// '?' for exactly one glyph that WinRT OCR rendered as impossible punctuation.
    /// A result is accepted only when one current official name matches that exact slot
    /// and no global alternative is within the conservative 10 percentage-point margin.
    /// </summary>
    public ScannerRecognition ResolveSingleUnknownGlyph(
        string patternText,
        double minimumMargin = 0.10)
    {
        if (_items.Length == 0)
            return ScannerRecognition.Failed("NO_CATALOG");

        var patterns = BuildPatternVariants(patternText);
        if (patterns.Count == 0)
            return ScannerRecognition.Failed("NO_UNKNOWN_GLYPH_PATTERN");

        var exactMatches = new HashSet<int>();
        foreach (var pattern in patterns)
        {
            if (pattern.Length < 7 || pattern.Count(character => character == ScannerOcrCharacterPolicy.UnknownGlyph) != 1)
                continue;

            for (var index = 0; index < _normalizedNames.Length; index++)
            {
                var official = _normalizedNames[index];
                if (official.Length != pattern.Length || official.Length < 7)
                    continue;
                if (PatternMatchesExactly(official, pattern))
                    exactMatches.Add(index);
            }
        }

        if (exactMatches.Count != 1)
            return ScannerRecognition.Failed(exactMatches.Count == 0
                ? "UNKNOWN_GLYPH_NO_CANDIDATE"
                : "UNKNOWN_GLYPH_AMBIGUOUS");

        var bestIndex = exactMatches.Single();
        var normalizedOfficial = _normalizedNames[bestIndex];
        var wildcardRanking = RankWildcardCandidates(patterns, DiagnosticCandidateLimit);
        var publicRanking = ToPublicRanking(wildcardRanking);
        if (_nameCounts.TryGetValue(normalizedOfficial, out var duplicates) && duplicates > 1)
        {
            return new ScannerRecognition(
                false,
                "AMBIGUOUS_OFFICIAL_NAME",
                _items[bestIndex].Id,
                _items[bestIndex].OfficialName,
                0,
                0,
                publicRanking);
        }

        var second = wildcardRanking
            .Where(candidate => candidate.Index != bestIndex)
            .Select(candidate => candidate.Score)
            .DefaultIfEmpty(0)
            .Max();

        var requiredMargin = Math.Max(0.10, minimumMargin);
        if (1.0 - second < requiredMargin)
        {
            return new ScannerRecognition(
                false,
                "UNKNOWN_GLYPH_LOW_MARGIN",
                _items[bestIndex].Id,
                _items[bestIndex].OfficialName,
                1.0 - 1.0 / Math.Max(1, normalizedOfficial.Length),
                second,
                publicRanking);
        }

        // Report one unknown glyph as one unit of uncertainty even though the wildcard
        // pattern structurally matched. This keeps diagnostics comparable to edit-based
        // OCR confidence instead of pretending the OCR itself was exact.
        var confidence = 1.0 - 1.0 / Math.Max(1, normalizedOfficial.Length);
        return new ScannerRecognition(
            true,
            "UNKNOWN_GLYPH_1",
            _items[bestIndex].Id,
            _items[bestIndex].OfficialName,
            confidence,
            second,
            publicRanking);
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

    public static string NormalizePattern(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormKC)
            .ToLower(CultureInfo.InvariantCulture);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character == ScannerOcrCharacterPolicy.UnknownGlyph)
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

            var globalRanking = RankAllCandidates(variants, DiagnosticCandidateLimit);
            var exactSecondScore = globalRanking
                .Where(candidate => candidate.Index != itemIndex)
                .Select(candidate => candidate.Score)
                .DefaultIfEmpty(0)
                .Max();
            return new MatchResult(
                itemIndex,
                1.0,
                exactSecondScore,
                true,
                ToPublicRanking(globalRanking));
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
        var scored = new List<ScoredCandidate>();

        foreach (var candidate in prefiltered
                     .OrderByDescending(candidate => candidate.Overlap)
                     .Take(320))
        {
            var score = variants.Max(variant => GlobalSimilarity(
                _normalizedNames[candidate.Index],
                variant));
            scored.Add(new ScoredCandidate(candidate.Index, score));

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

        var ranking = scored
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => _items[candidate.Index].OfficialName, StringComparer.Ordinal)
            .Take(DiagnosticCandidateLimit)
            .ToArray();
        return new MatchResult(
            bestIndex,
            best,
            second,
            false,
            ToPublicRanking(ranking));
    }

    private ScoredCandidate[] RankAllCandidates(
        IReadOnlyList<string> variants,
        int limit)
    {
        var ranking = new List<ScoredCandidate>(_normalizedNames.Length);
        for (var index = 0; index < _normalizedNames.Length; index++)
        {
            if (_normalizedNames[index].Length == 0)
                continue;
            var score = variants.Max(variant => GlobalSimilarity(_normalizedNames[index], variant));
            ranking.Add(new ScoredCandidate(index, score));
        }

        return ranking
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => _items[candidate.Index].OfficialName, StringComparer.Ordinal)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    private ScoredCandidate[] RankWildcardCandidates(
        IReadOnlyList<string> patterns,
        int limit)
    {
        var ranking = new List<ScoredCandidate>(_normalizedNames.Length);
        for (var index = 0; index < _normalizedNames.Length; index++)
        {
            var official = _normalizedNames[index];
            if (official.Length == 0)
                continue;
            var score = patterns.Max(pattern => WildcardSimilarity(official, pattern));
            ranking.Add(new ScoredCandidate(index, score));
        }

        return ranking
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => _items[candidate.Index].OfficialName, StringComparer.Ordinal)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    private IReadOnlyList<ScannerMatchCandidate> ToPublicRanking(
        IEnumerable<ScoredCandidate> ranking) =>
        ranking
            .Select(candidate => new ScannerMatchCandidate(
                _items[candidate.Index].Id,
                _items[candidate.Index].OfficialName,
                candidate.Score))
            .ToArray();

    private bool TryFindUniqueSingleEditCandidate(
        IReadOnlyList<string> variants,
        out int candidateIndex)
    {
        candidateIndex = -1;

        for (var index = 0; index < _normalizedNames.Length; index++)
        {
            var official = _normalizedNames[index];
            if (official.Length < 7)
                continue;

            var oneEdit = false;
            foreach (var variant in variants)
            {
                if (Math.Abs(official.Length - variant.Length) > 1)
                    continue;
                if (EditDistance(official, variant) == 1)
                {
                    oneEdit = true;
                    break;
                }
            }

            if (!oneEdit)
                continue;

            if (candidateIndex >= 0)
                return false;

            candidateIndex = index;
        }

        return candidateIndex >= 0;
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

    private static IReadOnlyList<string> BuildPatternVariants(string patternText)
    {
        if (string.IsNullOrWhiteSpace(patternText))
            return [];

        var variants = new HashSet<string>(StringComparer.Ordinal);
        AddVariant(patternText);
        foreach (var line in patternText.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            AddVariant(line);
        }
        return variants.ToArray();

        void AddVariant(string value)
        {
            var normalized = NormalizePattern(value);
            if (normalized.Length >= 7 && normalized.Count(character => character == ScannerOcrCharacterPolicy.UnknownGlyph) == 1)
                variants.Add(normalized);
        }
    }

    private static bool PatternMatchesExactly(string official, string pattern)
    {
        if (official.Length != pattern.Length)
            return false;
        for (var index = 0; index < official.Length; index++)
        {
            if (pattern[index] == ScannerOcrCharacterPolicy.UnknownGlyph)
                continue;
            if (official[index] != pattern[index])
                return false;
        }
        return true;
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

        var distance = EditDistance(left, right);
        return Math.Clamp(
            1.0 - (double)distance / Math.Max(left.Length, right.Length),
            0,
            1);
    }

    private static double WildcardSimilarity(string official, string pattern)
    {
        if (official.Length == 0 || pattern.Length == 0)
            return 0;
        var distance = WildcardEditDistance(official, pattern);
        return Math.Clamp(
            1.0 - (double)distance / Math.Max(official.Length, pattern.Length),
            0,
            1);
    }

    private static int EditDistance(string left, string right) =>
        EditDistanceCore(left, right, wildcard: false);

    private static int WildcardEditDistance(string left, string right) =>
        EditDistanceCore(left, right, wildcard: true);

    private static int EditDistanceCore(string left, string right, bool wildcard)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var index = 0; index <= right.Length; index++)
            previous[index] = index;

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var patternCharacter = right[column - 1];
                var substitution = left[row - 1] == patternCharacter ||
                                   (wildcard && patternCharacter == ScannerOcrCharacterPolicy.UnknownGlyph)
                    ? 0
                    : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitution);
            }
            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private sealed record MatchResult(
        int BestIndex,
        double BestScore,
        double SecondScore,
        bool Exact,
        IReadOnlyList<ScannerMatchCandidate> TopCandidates);

    private readonly record struct ScoredCandidate(int Index, double Score);
}
