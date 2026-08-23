namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Closed-domain recovery for OCR glyphs that cannot exist in the current official item
/// catalog. Patterns use '?' only as unknown-position evidence; they never encode a
/// guessed r/0/etc. Recovery succeeds only when the complete current catalog has exactly
/// one exact wildcard-pattern match and that match is globally separated from alternatives.
/// </summary>
public static class ScannerUnknownGlyphCatalogRecovery
{
    private const int DiagnosticCandidateLimit = 5;

    public static ScannerRecognition TryRecover(
        string patternText,
        IReadOnlyCollection<ScannerCatalogItem> catalog,
        ScannerRecognition ordinary,
        double minimumMargin = 0.10)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(ordinary);

        if (ordinary.Success || catalog.Count == 0 || string.IsNullOrWhiteSpace(patternText))
            return ordinary;

        var patterns = BuildPatterns(patternText);
        if (patterns.Count == 0)
            return ordinary;

        var entries = catalog
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .Select(item => new Entry(item, ScannerItemMatcher.Normalize(item.OfficialName)))
            .Where(entry => entry.Normalized.Length > 0)
            .ToArray();
        if (entries.Length == 0)
            return ordinary;

        var exactMatches = new Dictionary<string, ExactMatch>(StringComparer.Ordinal);
        foreach (var pattern in patterns)
        {
            foreach (var entry in entries)
            {
                if (entry.Normalized.Length != pattern.Value.Length)
                    continue;
                if (!PatternMatchesExactly(entry.Normalized, pattern.Value))
                    continue;

                if (!exactMatches.TryGetValue(entry.Item.Id, out var existing) ||
                    pattern.UnknownCount < existing.UnknownCount)
                {
                    exactMatches[entry.Item.Id] = new ExactMatch(entry, pattern.UnknownCount);
                }
            }
        }

        // Duplicate official names with different Item IDs are intentionally ambiguous.
        if (exactMatches.Count != 1)
            return ordinary;

        var exact = exactMatches.Values.Single();
        var minimumLength = exact.UnknownCount == 1 ? 5 : 10;
        if (exact.Entry.Normalized.Length < minimumLength)
            return ordinary;

        var knownRatio = 1.0 - exact.UnknownCount / (double)exact.Entry.Normalized.Length;
        var minimumKnownRatio = exact.UnknownCount == 1 ? 0.80 : 0.80;
        if (knownRatio < minimumKnownRatio)
            return ordinary;

        var ranking = entries
            .Select(entry => new RankedEntry(
                entry,
                patterns.Max(pattern => PatternSimilarity(entry.Normalized, pattern.Value))))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Entry.Item.OfficialName, StringComparer.Ordinal)
            .ToArray();

        var secondScore = ranking
            .Where(candidate => !string.Equals(candidate.Entry.Item.Id, exact.Entry.Item.Id, StringComparison.Ordinal))
            .Select(candidate => candidate.Score)
            .DefaultIfEmpty(0)
            .Max();
        var requiredMargin = exact.UnknownCount == 1
            ? Math.Max(0.12, minimumMargin)
            : Math.Max(0.18, minimumMargin);
        if (1.0 - secondScore < requiredMargin)
            return ordinary;

        var publicRanking = ranking
            .Take(DiagnosticCandidateLimit)
            .Select(candidate => new ScannerMatchCandidate(
                candidate.Entry.Item.Id,
                candidate.Entry.Item.OfficialName,
                candidate.Score))
            .ToArray();

        return new ScannerRecognition(
            true,
            exact.UnknownCount == 1
                ? "UNKNOWN_GLYPH_1_CATALOG"
                : "UNKNOWN_GLYPH_2_CATALOG",
            exact.Entry.Item.Id,
            exact.Entry.Item.OfficialName,
            knownRatio,
            secondScore,
            publicRanking);
    }

    private static IReadOnlyList<Pattern> BuildPatterns(string patternText)
    {
        var result = new Dictionary<string, Pattern>(StringComparer.Ordinal);
        foreach (var raw in patternText.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = ScannerItemMatcher.NormalizePattern(raw);
            var unknownCount = normalized.Count(character => character == ScannerOcrCharacterPolicy.UnknownGlyph);
            if (unknownCount is < 1 or > 2)
                continue;

            var minimumLength = unknownCount == 1 ? 5 : 10;
            if (normalized.Length < minimumLength)
                continue;

            result[normalized] = new Pattern(normalized, unknownCount);
        }
        return result.Values.ToArray();
    }

    private static bool PatternMatchesExactly(string official, string pattern)
    {
        if (official.Length != pattern.Length)
            return false;

        for (var index = 0; index < official.Length; index++)
        {
            var expected = pattern[index];
            if (expected == ScannerOcrCharacterPolicy.UnknownGlyph)
                continue;
            if (official[index] != expected)
                return false;
        }
        return true;
    }

    private static double PatternSimilarity(string official, string pattern)
    {
        if (official.Length == 0 || pattern.Length == 0)
            return 0;
        if (PatternMatchesExactly(official, pattern))
            return 1;

        var distance = WildcardEditDistance(official, pattern);
        return Math.Clamp(
            1.0 - distance / (double)Math.Max(official.Length, pattern.Length),
            0,
            1);
    }

    private static int WildcardEditDistance(string official, string pattern)
    {
        var previous = new int[pattern.Length + 1];
        var current = new int[pattern.Length + 1];
        for (var column = 0; column <= pattern.Length; column++)
            previous[column] = column;

        for (var row = 1; row <= official.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= pattern.Length; column++)
            {
                var patternCharacter = pattern[column - 1];
                var substitution = patternCharacter == ScannerOcrCharacterPolicy.UnknownGlyph ||
                                   official[row - 1] == patternCharacter
                    ? 0
                    : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitution);
            }
            (previous, current) = (current, previous);
        }

        return previous[pattern.Length];
    }

    private sealed record Entry(ScannerCatalogItem Item, string Normalized);
    private readonly record struct Pattern(string Value, int UnknownCount);
    private sealed record ExactMatch(Entry Entry, int UnknownCount);
    private sealed record RankedEntry(Entry Entry, double Score);
}
