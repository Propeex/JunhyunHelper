using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed partial class ScannerCoordinator
{
    public IReadOnlyList<ScannerOcrSubstitutionRule> OcrSubstitutions =>
        Settings.OcrSubstitutions.Select(rule => rule.Clone()).ToArray();

    public void ReplaceOcrSubstitutions(IEnumerable<ScannerOcrSubstitutionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var normalized = ScannerOcrSubstitutionEngine.NormalizeRules(rules)
            .Select(rule => rule.Clone())
            .ToList();
        UpdateDisplaySettings(settings => settings.OcrSubstitutions = normalized);
    }

    public void ResetOcrSubstitutions() =>
        UpdateDisplaySettings(settings => settings.OcrSubstitutions = []);
}
