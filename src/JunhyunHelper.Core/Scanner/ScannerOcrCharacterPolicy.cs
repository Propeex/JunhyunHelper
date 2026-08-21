namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Restricts OCR evidence to characters that can actually occur in the current
/// official item-name catalog. Variants containing CJK Han ideographs are rejected
/// outright for the Korean Tarkov item-title contract; other unexpected characters
/// may be tolerated only when they are a small minority of an otherwise plausible
/// variant. Characters are never silently rewritten into a different item name.
/// </summary>
public sealed class ScannerOcrCharacterPolicy
{
    private readonly object _gate = new();
    private HashSet<char> _allowed = [];

    public void ReplaceCatalog(IEnumerable<ScannerCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var allowed = new HashSet<char>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OfficialName))
                continue;
            foreach (var character in item.OfficialName)
            {
                if (!char.IsWhiteSpace(character))
                    allowed.Add(character);
            }
        }

        lock (_gate)
            _allowed = allowed;
    }

    public ScannerOcrTextAssessment Assess(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScannerOcrTextAssessment.Empty;

        HashSet<char> allowed;
        lock (_gate)
            allowed = new HashSet<char>(_allowed);

        var accepted = new List<string>();
        var totalVariants = 0;
        var totalCharacters = 0;
        var validCharacters = 0;
        var invalidCharacters = 0;
        var hanCharacters = 0;

        foreach (var raw in text.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var variant = raw.Trim();
            if (variant.Length < 2)
                continue;

            totalVariants++;
            var variantTotal = 0;
            var variantValid = 0;
            var variantInvalid = 0;
            var variantHan = 0;

            foreach (var character in variant)
            {
                if (char.IsWhiteSpace(character))
                    continue;

                variantTotal++;
                if (IsHanIdeograph(character))
                {
                    variantHan++;
                    variantInvalid++;
                    continue;
                }

                if (allowed.Contains(character))
                    variantValid++;
                else
                    variantInvalid++;
            }

            totalCharacters += variantTotal;
            validCharacters += variantValid;
            invalidCharacters += variantInvalid;
            hanCharacters += variantHan;

            if (variantTotal < 2 || variantHan > 0)
                continue;

            var validRatio = variantTotal <= 0 ? 0 : variantValid / (double)variantTotal;
            var invalidLimit = Math.Max(1, (int)Math.Floor(variantTotal * 0.18));
            if (validRatio >= 0.82 && variantInvalid <= invalidLimit)
                accepted.Add(variant);
        }

        var overallRatio = totalCharacters <= 0 ? 0 : validCharacters / (double)totalCharacters;
        return new ScannerOcrTextAssessment(
            string.Join(" | ", accepted.Distinct(StringComparer.Ordinal)),
            overallRatio,
            invalidCharacters,
            hanCharacters,
            accepted.Count,
            totalVariants);
    }

    public static bool IsHanIdeograph(char character) =>
        character is >= '\u3400' and <= '\u4DBF' ||
        character is >= '\u4E00' and <= '\u9FFF' ||
        character is >= '\uF900' and <= '\uFAFF';
}

public sealed record ScannerOcrTextAssessment(
    string FilteredText,
    double ValidCharacterRatio,
    int InvalidCharacterCount,
    int HanCharacterCount,
    int AcceptedVariantCount,
    int TotalVariantCount)
{
    public static ScannerOcrTextAssessment Empty { get; } =
        new(string.Empty, 0, 0, 0, 0, 0);

    public bool HasPlausibleVariant => AcceptedVariantCount > 0 && !string.IsNullOrWhiteSpace(FilteredText);

    public bool IsCorrupted => !HasPlausibleVariant || HanCharacterCount > 0 || ValidCharacterRatio < 0.72;
}
