using System.Text;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// One user-defined OCR substitution. Rules are exact and case-sensitive because their
/// primary purpose is to correct deterministic glyph errors such as `「` -> `r`.
/// </summary>
public sealed class ScannerOcrSubstitutionRule
{
    public bool Enabled { get; set; } = true;
    public string Source { get; set; } = string.Empty;
    public string Replacement { get; set; } = string.Empty;

    public ScannerOcrSubstitutionRule Clone() => new()
    {
        Enabled = Enabled,
        Source = Source,
        Replacement = Replacement,
    };
}

public sealed record ScannerOcrSubstitutionResult(
    string Text,
    int ReplacementCount,
    IReadOnlyList<string> AppliedSources)
{
    public bool Changed => ReplacementCount > 0;
}

/// <summary>
/// Applies enabled user substitutions against the original OCR stream exactly once.
/// Replacement output is never fed back through another rule, so chaining/cycles such
/// as A->B and B->A cannot recursively transform evidence. At each original input
/// position the longest matching source wins; ties preserve user rule order.
/// </summary>
public static class ScannerOcrSubstitutionEngine
{
    public const int MaximumRules = 64;
    public const int MaximumSourceLength = 32;
    public const int MaximumReplacementLength = 32;

    public static ScannerOcrSubstitutionResult Apply(
        string? rawText,
        IEnumerable<ScannerOcrSubstitutionRule>? rules)
    {
        var text = rawText ?? string.Empty;
        if (text.Length == 0 || rules is null)
            return new ScannerOcrSubstitutionResult(text, 0, []);

        var active = rules
            .Take(MaximumRules)
            .Select((rule, index) => (Rule: rule, Index: index))
            .Where(entry => entry.Rule is not null &&
                            entry.Rule.Enabled &&
                            !string.IsNullOrEmpty(entry.Rule.Source) &&
                            entry.Rule.Source.Length <= MaximumSourceLength &&
                            entry.Rule.Replacement.Length <= MaximumReplacementLength)
            .OrderByDescending(entry => entry.Rule.Source.Length)
            .ThenBy(entry => entry.Index)
            .ToArray();
        if (active.Length == 0)
            return new ScannerOcrSubstitutionResult(text, 0, []);

        var builder = new StringBuilder(text.Length);
        var applied = new List<string>();
        var replacementCount = 0;

        for (var index = 0; index < text.Length;)
        {
            ScannerOcrSubstitutionRule? matched = null;
            foreach (var entry in active)
            {
                var source = entry.Rule.Source;
                if (index + source.Length > text.Length)
                    continue;
                if (string.CompareOrdinal(text, index, source, 0, source.Length) != 0)
                    continue;
                matched = entry.Rule;
                break;
            }

            if (matched is null)
            {
                builder.Append(text[index]);
                index++;
                continue;
            }

            builder.Append(matched.Replacement);
            index += matched.Source.Length;
            replacementCount++;
            if (!applied.Contains(matched.Source, StringComparer.Ordinal))
                applied.Add(matched.Source);
        }

        return new ScannerOcrSubstitutionResult(builder.ToString(), replacementCount, applied);
    }

    public static IReadOnlyList<ScannerOcrSubstitutionRule> NormalizeRules(
        IEnumerable<ScannerOcrSubstitutionRule>? rules)
    {
        if (rules is null)
            return [];

        var normalized = new List<ScannerOcrSubstitutionRule>();
        foreach (var rule in rules.Take(MaximumRules))
        {
            if (rule is null)
                continue;

            var source = rule.Source ?? string.Empty;
            var replacement = rule.Replacement ?? string.Empty;
            if (source.Length == 0 ||
                source.Length > MaximumSourceLength ||
                replacement.Length > MaximumReplacementLength)
            {
                continue;
            }

            normalized.Add(new ScannerOcrSubstitutionRule
            {
                Enabled = rule.Enabled,
                Source = source,
                Replacement = replacement,
            });
        }

        return normalized;
    }
}
