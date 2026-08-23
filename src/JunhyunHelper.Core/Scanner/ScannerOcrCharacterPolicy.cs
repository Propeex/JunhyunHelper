using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Sanitizes OCR evidence against the character shape of the current official item-name
/// catalog. Letters/digits remain available for fuzzy correction because OCR can confuse
/// one glyph for another. Punctuation/symbols are stricter: only symbols that actually
/// occur in the current official catalog survive into matcher evidence. CJK Han
/// ideographs remain a hard rejection for the Korean Tarkov item-title contract.
/// </summary>
public sealed class ScannerOcrCharacterPolicy
{
    private readonly object _gate = new();
    private HashSet<char> _allowedSymbols = [];

    public void ReplaceCatalog(IEnumerable<ScannerCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var allowedSymbols = new HashSet<char>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OfficialName))
                continue;

            foreach (var character in item.OfficialName)
            {
                if (!char.IsWhiteSpace(character) && !char.IsLetterOrDigit(character))
                    allowedSymbols.Add(character);
            }
        }

        lock (_gate)
            _allowedSymbols = allowedSymbols;
    }

    public ScannerOcrTextAssessment Assess(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScannerOcrTextAssessment.Empty;

        HashSet<char> allowedSymbols;
        lock (_gate)
            allowedSymbols = new HashSet<char>(_allowedSymbols);

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
            var sanitized = new StringBuilder(variant.Length);
            var variantHan = 0;
            var removedUnsupportedSymbols = 0;

            foreach (var character in variant)
            {
                if (char.IsWhiteSpace(character))
                {
                    // Keep one logical separator for diagnostics/render-guided recovery.
                    if (sanitized.Length > 0 && !char.IsWhiteSpace(sanitized[^1]))
                        sanitized.Append(' ');
                    continue;
                }

                totalCharacters++;
                if (IsHanIdeograph(character))
                {
                    variantHan++;
                    hanCharacters++;
                    invalidCharacters++;
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    // A letter/digit can itself be the OCR mistake (I/l, r omission, etc.).
                    // Preserve it so the constrained catalog matcher can correct it.
                    validCharacters++;
                    sanitized.Append(character);
                    continue;
                }

                if (allowedSymbols.Contains(character))
                {
                    validCharacters++;
                    sanitized.Append(character);
                    continue;
                }

                // Unlike letters/digits, an impossible punctuation mark contributes no
                // identity information. Drop it before matching instead of letting it
                // consume the old generic 18% error allowance.
                removedUnsupportedSymbols++;
                invalidCharacters++;
            }

            if (variantHan > 0)
                continue;

            var clean = CollapseWhitespace(sanitized.ToString()).Trim();
            var identityLength = ScannerItemMatcher.Normalize(clean).Length;

            // Do not let symbol stripping turn a tiny noisy token (e.g. C※U) into a
            // deceptively trustworthy two-character candidate. Real item-title evidence
            // must retain at least three alphanumeric characters after sanitation.
            if (identityLength < 3)
                continue;

            if (clean.Length > 0)
                accepted.Add(clean);
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

    private static string CollapseWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        var previousWhitespace = false;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWhitespace && builder.Length > 0)
                    builder.Append(' ');
                previousWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWhitespace = false;
        }
        return builder.ToString();
    }
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
