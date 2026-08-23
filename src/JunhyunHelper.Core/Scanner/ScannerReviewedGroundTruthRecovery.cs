namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Conservative recovery for OCR failures demonstrated by reviewed live Ground Truth.
/// This does not change generic matcher thresholds or guess glyph substitutions. It can
/// only accept the ordinary matcher's existing top candidate when the complete current
/// catalog proves a uniquely bounded edit pattern with a strong global margin.
/// </summary>
public static class ScannerReviewedGroundTruthRecovery
{
    public static ScannerRecognition TryRecover(
        string noisyText,
        IReadOnlyCollection<ScannerCatalogItem> catalog,
        ScannerRecognition ordinary,
        double minimumMargin = 0.05)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(ordinary);

        if (ordinary.Success ||
            !string.Equals(ordinary.Reason, "LOW_CONFIDENCE", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(ordinary.ItemId) ||
            catalog.Count == 0)
        {
            return ordinary;
        }

        var variants = BuildVariants(noisyText);
        if (variants.Count == 0)
            return ordinary;

        var entries = catalog
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.OfficialName))
            .Select(item => new Entry(item, ScannerItemMatcher.Normalize(item.OfficialName)))
            .Where(entry => entry.Normalized.Length > 0)
            .ToArray();
        if (entries.Length == 0)
            return ordinary;

        var candidate = entries.FirstOrDefault(entry =>
            string.Equals(entry.Item.Id, ordinary.ItemId, StringComparison.Ordinal));
        if (candidate is null)
            return ordinary;

        var globalSecond = entries
            .Where(entry => !string.Equals(entry.Item.Id, candidate.Item.Id, StringComparison.Ordinal))
            .Select(entry => BestSimilarity(entry.Normalized, variants))
            .DefaultIfEmpty(0)
            .Max();
        var bestScore = BestSimilarity(candidate.Normalized, variants);

        if (TryUniqueExactEdit(entries, variants, distance: 2, minimumOfficialLength: 10, out var twoEdit) &&
            string.Equals(twoEdit.Item.Id, candidate.Item.Id, StringComparison.Ordinal) &&
            bestScore >= 0.84 &&
            bestScore - globalSecond >= Math.Max(0.15, minimumMargin))
        {
            return Recovered(
                ordinary,
                candidate.Item,
                "BOUNDED_EDIT_2_UNIQUE",
                bestScore,
                globalSecond);
        }

        if (TryUniqueSuffixAnchored(entries, variants, out var suffixAnchored) &&
            string.Equals(suffixAnchored.Item.Id, candidate.Item.Id, StringComparison.Ordinal) &&
            bestScore >= 0.74 &&
            bestScore - globalSecond >= Math.Max(0.12, minimumMargin))
        {
            return Recovered(
                ordinary,
                candidate.Item,
                "SUFFIX_ANCHORED_EDIT_2_3",
                bestScore,
                globalSecond);
        }

        return ordinary;
    }

    private static ScannerRecognition Recovered(
        ScannerRecognition ordinary,
        ScannerCatalogItem item,
        string reason,
        double confidence,
        double secondScore) =>
        new(
            true,
            reason,
            item.Id,
            item.OfficialName,
            confidence,
            secondScore,
            ordinary.TopCandidates);

    private static bool TryUniqueExactEdit(
        IReadOnlyList<Entry> entries,
        IReadOnlyList<string> variants,
        int distance,
        int minimumOfficialLength,
        out Entry candidate)
    {
        candidate = null!;
        var matches = 0;
        foreach (var entry in entries)
        {
            if (entry.Normalized.Length < minimumOfficialLength)
                continue;

            var matched = variants.Any(variant =>
                Math.Abs(entry.Normalized.Length - variant.Length) <= distance &&
                EditDistance(entry.Normalized, variant) == distance);
            if (!matched)
                continue;

            matches++;
            if (matches > 1)
                return false;
            candidate = entry;
        }
        return matches == 1;
    }

    private static bool TryUniqueSuffixAnchored(
        IReadOnlyList<Entry> entries,
        IReadOnlyList<string> variants,
        out Entry candidate)
    {
        candidate = null!;
        var matches = 0;
        foreach (var entry in entries)
        {
            if (entry.Normalized.Length < 12)
                continue;

            var matched = false;
            foreach (var variant in variants)
            {
                if (variant.Length < 9 || Math.Abs(entry.Normalized.Length - variant.Length) > 3)
                    continue;

                var distance = EditDistance(entry.Normalized, variant);
                if (distance is < 2 or > 3)
                    continue;

                var suffix = CommonSuffixLength(entry.Normalized, variant);
                var shorter = Math.Min(entry.Normalized.Length, variant.Length);
                if (suffix >= 6 && suffix >= (int)Math.Ceiling(shorter * 0.45))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
                continue;
            matches++;
            if (matches > 1)
                return false;
            candidate = entry;
        }
        return matches == 1;
    }

    private static IReadOnlyList<string> BuildVariants(string noisyText)
    {
        if (string.IsNullOrWhiteSpace(noisyText))
            return [];

        var variants = new HashSet<string>(StringComparer.Ordinal);
        Add(noisyText);
        foreach (var line in noisyText.Split(
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

    private static double BestSimilarity(string official, IReadOnlyList<string> variants) =>
        variants.Select(variant => Similarity(official, variant)).DefaultIfEmpty(0).Max();

    private static double Similarity(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
            return 0;
        if (string.Equals(left, right, StringComparison.Ordinal))
            return 1;
        var distance = EditDistance(left, right);
        return Math.Clamp(1.0 - distance / (double)Math.Max(left.Length, right.Length), 0, 1);
    }

    private static int CommonSuffixLength(string left, string right)
    {
        var length = 0;
        while (length < left.Length &&
               length < right.Length &&
               left[left.Length - 1 - length] == right[right.Length - 1 - length])
        {
            length++;
        }
        return length;
    }

    private static int EditDistance(string left, string right)
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
                var substitution = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitution);
            }
            (previous, current) = (current, previous);
        }
        return previous[right.Length];
    }

    private sealed record Entry(ScannerCatalogItem Item, string Normalized);
}
