using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Sanitizes OCR evidence against the character inventory of the current official
/// item-name catalog. The Scanner reads a closed-domain item title, not arbitrary text,
/// so even Unicode letters/digits are ordinary evidence only when that normalized
/// character actually occurs somewhere in the current catalog.
///
/// Punctuation/symbols follow the same rule. A catalog-impossible glyph embedded between
/// two catalog-valid identity characters can be preserved as '?' unknown-glyph evidence.
/// This never guesses that the glyph was specifically r, 0, or any other character; the
/// catalog matcher may recover it only when the complete current catalog proves a unique,
/// safely separated pattern. CJK Han ideographs remain a hard rejection for the Korean
/// Tarkov item-title contract.
/// </summary>
public sealed class ScannerOcrCharacterPolicy
{
    public const char UnknownGlyph = '?';
    private const int MaximumUnknownGlyphsPerVariant = 2;

    private readonly object _gate = new();
    private HashSet<char> _allowedIdentityCharacters = [];
    private HashSet<char> _allowedSymbols = [];

    public void ReplaceCatalog(IEnumerable<ScannerCatalogItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var allowedIdentityCharacters = new HashSet<char>();
        var allowedSymbols = new HashSet<char>();
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OfficialName))
                continue;

            var normalizedName = item.OfficialName
                .Normalize(NormalizationForm.FormKC)
                .ToLowerInvariant();
            foreach (var character in normalizedName)
            {
                if (char.IsWhiteSpace(character))
                    continue;
                if (char.IsLetterOrDigit(character))
                    allowedIdentityCharacters.Add(character);
                else
                    allowedSymbols.Add(character);
            }
        }

        lock (_gate)
        {
            _allowedIdentityCharacters = allowedIdentityCharacters;
            _allowedSymbols = allowedSymbols;
        }
    }

    public ScannerOcrTextAssessment Assess(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScannerOcrTextAssessment.Empty;

        HashSet<char> allowedIdentityCharacters;
        HashSet<char> allowedSymbols;
        lock (_gate)
        {
            allowedIdentityCharacters = new HashSet<char>(_allowedIdentityCharacters);
            allowedSymbols = new HashSet<char>(_allowedSymbols);
        }

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
            // Compatibility normalization makes OCR full-width forms comparable to the
            // same normalization used to build the current catalog character inventory.
            // Preserve casing in FilteredText; matching itself remains case-insensitive.
            var variant = raw.Trim().Normalize(NormalizationForm.FormKC);
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

                if (IsCatalogIdentityCharacter(character, allowedIdentityCharacters))
                {
                    validCharacters++;
                    sanitized.Append(character);
                    pattern.Append(character);
                    continue;
                }

                var normalizedCharacter = char.ToLowerInvariant(character);
                if (!char.IsLetterOrDigit(character) && allowedSymbols.Contains(normalizedCharacter))
                {
                    validCharacters++;
                    sanitized.Append(character);
                    pattern.Append(character);
                    continue;
                }

                invalidCharacters++;

                // WinRT OCR can render narrow Latin glyphs as brackets/punctuation and
                // slash-zero-like glyphs as Unicode letters such as Ø. Do not maintain a
                // guessed substitution table. Preserve only the fact that an impossible
                // embedded glyph occupied one character position. Leading/trailing noise
                // therefore still cannot manufacture a wildcard candidate.
                if (variantUnknownGlyphs < MaximumUnknownGlyphsPerVariant &&
                    IsEmbeddedBetweenCatalogIdentityCharacters(
                        variant,
                        index,
                        allowedIdentityCharacters))
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

            // Do not let stripping impossible glyphs turn a tiny noisy token into a
            // deceptively trustworthy candidate. Ordinary evidence still needs at least
            // three identity characters after sanitation.
            if (identityLength >= 3 && clean.Length > 0)
                accepted.Add(clean);

            if (variantUnknownGlyphs is >= 1 and <= MaximumUnknownGlyphsPerVariant)
            {
                var wildcard = CollapseWhitespace(pattern.ToString()).Trim();
                var normalizedPattern = ScannerItemMatcher.NormalizePattern(wildcard);
                var wildcardIdentityLength = normalizedPattern.Length;
                var wildcardCount = normalizedPattern.Count(c => c == UnknownGlyph);

                // One impossible glyph can be safely useful even for a medium-short name
                // because the downstream recovery requires an exact unique catalog
                // pattern. Two unknown glyphs require substantially more known context.
                var minimumPatternLength = wildcardCount == 1 ? 5 : 9;
                if (wildcardCount == variantUnknownGlyphs &&
                    wildcardIdentityLength >= minimumPatternLength)
                {
                    wildcardPatterns.Add(wildcard);
                }
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

    private static bool IsCatalogIdentityCharacter(
        char character,
        IReadOnlySet<char> allowedIdentityCharacters) =>
        char.IsLetterOrDigit(character) &&
        allowedIdentityCharacters.Contains(char.ToLowerInvariant(character));

    private static bool IsEmbeddedBetweenCatalogIdentityCharacters(
        string variant,
        int index,
        IReadOnlySet<char> allowedIdentityCharacters) =>
        index > 0 &&
        index + 1 < variant.Length &&
        IsCatalogIdentityCharacter(variant[index - 1], allowedIdentityCharacters) &&
        IsCatalogIdentityCharacter(variant[index + 1], allowedIdentityCharacters);

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

    public bool HasUnknownGlyphPattern =>
        UnknownGlyphCount > 0 && !string.IsNullOrWhiteSpace(UnknownGlyphPatternText);

    // Compatibility name retained for the catalog service. The pattern stream can now
    // contain one or two bounded unknown glyphs; the matcher adapter decides which
    // recovery contract applies.
    public bool HasSingleUnknownGlyphPattern => HasUnknownGlyphPattern;

    public bool IsCorrupted => !HasPlausibleVariant || HanCharacterCount > 0 || ValidCharacterRatio < 0.72;
}
