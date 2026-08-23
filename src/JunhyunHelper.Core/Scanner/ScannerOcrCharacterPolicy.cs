using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Sanitizes OCR evidence against the character shape of the current official item-name
/// catalog. Letters/digits remain available for fuzzy correction because OCR can confuse
/// one glyph for another. Punctuation/symbols are stricter: only symbols that actually
/// occur in the current official catalog survive into ordinary matcher evidence.
///
/// A single unsupported symbol that is physically between two letters/digits is also
/// preserved in a separate pattern as '?' (unknown one-glyph evidence). This does NOT
/// guess which character it was. The matcher may use that pattern only when the complete
/// current catalog has one unique, safely separated name at that exact character slot.
/// CJK Han ideographs remain a hard rejection for the Korean Tarkov item-title contract.
/// </summary>
public sealed class ScannerOcrCharacterPolicy
{
    public const char UnknownGlyph = '?';

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
        var wildcardPatterns = new List<string>();
        var totalVariants = 0;
        var totalCharacters = 0;
        var validCharacters = 0;
        var invalidCharacters = 0;
        var hanCharacters = 0;
        var unknownGlyphs = 0;

        foreach (var raw in text.Split(
                     ['\r', '\n', '|'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var variant = raw.Trim();
            if (variant.Length < 2)
                continue;

            totalVariants++;
            var sanitized = new StringBuilder(variant.Length);
            var pattern = new StringBuilder(variant.Length);
            var variantHan = 0;
            var variantUnknownGlyphs = 0;

            for (var index = 0; index < variant.Length; index++)
            {
                var character = variant[index];
                if (char.IsWhiteSpace(character))
                {
                    AppendLogicalSpace(sanitized);
                    AppendLogicalSpace(pattern);
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
                    pattern.Append(character);
                    continue;
                }

                if (allowedSymbols.Contains(character))
                {
                    validCharacters++;
                    sanitized.Append(character);
                    pattern.Append(character);
                    continue;
                }

                invalidCharacters++;

                // WinRT OCR repeatedly renders narrow Latin glyphs (notably lower-case r)
                // as punctuation such as the Japanese bracket '「'. Do not hard-code an
                // r replacement. Preserve only the fact that one unknown glyph existed,
                // and only when it is embedded between two alphanumeric glyphs. Leading
                // backticks and free punctuation therefore never become wildcards.
                if (variantUnknownGlyphs == 0 &&
                    index > 0 &&
                    index + 1 < variant.Length &&
                    char.IsLetterOrDigit(variant[index - 1]) &&
                    char.IsLetterOrDigit(variant[index + 1]))
                {
                    pattern.Append(UnknownGlyph);
                    variantUnknownGlyphs++;
                    unknownGlyphs++;
                }
            }

            if (variantHan > 0)
                continue;

            var clean = CollapseWhitespace(sanitized.ToString()).Trim();
            var identityLength = ScannerItemMatcher.Normalize(clean).Length;

            // Do not let symbol stripping turn a tiny noisy token (e.g. C※U) into a
            // deceptively trustworthy two-character candidate. Real item-title evidence
            // must retain at least three alphanumeric characters after sanitation.
            if (identityLength >= 3 && clean.Length > 0)
                accepted.Add(clean);

            if (variantUnknownGlyphs == 1)
            {
                var wildcard = CollapseWhitespace(pattern.ToString()).Trim();
                var wildcardIdentityLength = ScannerItemMatcher.NormalizePattern(wildcard).Length;
                // Unknown-glyph recovery is intentionally not available to short names.
                if (wildcardIdentityLength >= 7 && wildcard.Count(c => c == UnknownGlyph) == 1)
                    wildcardPatterns.Add(wildcard);
            }
        }

        var overallRatio = totalCharacters <= 0 ? 0 : validCharacters / (double)totalCharacters;
        return new ScannerOcrTextAssessment(
            string.Join(" | ", accepted.Distinct(StringComparer.Ordinal)),
            string.Join(" | ", wildcardPatterns.Distinct(StringComparer.Ordinal)),
            overallRatio,
            invalidCharacters,
            hanCharacters,
            unknownGlyphs,
            accepted.Count,
            totalVariants);
    }

    public static bool IsHanIdeograph(char character) =>
        character is >= '\u3400' and <= '\u4DBF' ||
        character is >= '\u4E00' and <= '\u9FFF' ||
        character is >= '\uF900' and <= '\uFAFF';

    private static void AppendLogicalSpace(StringBuilder builder)
    {
        if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
            builder.Append(' ');
    }

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
    string UnknownGlyphPatternText,
    double ValidCharacterRatio,
    int InvalidCharacterCount,
    int HanCharacterCount,
    int UnknownGlyphCount,
    int AcceptedVariantCount,
    int TotalVariantCount)
{
    public static ScannerOcrTextAssessment Empty { get; } =
        new(string.Empty, string.Empty, 0, 0, 0, 0, 0, 0);

    public bool HasPlausibleVariant => AcceptedVariantCount > 0 && !string.IsNullOrWhiteSpace(FilteredText);

    public bool HasSingleUnknownGlyphPattern =>
        UnknownGlyphCount > 0 && !string.IsNullOrWhiteSpace(UnknownGlyphPatternText);

    public bool IsCorrupted => !HasPlausibleVariant || HanCharacterCount > 0 || ValidCharacterRatio < 0.72;
}
