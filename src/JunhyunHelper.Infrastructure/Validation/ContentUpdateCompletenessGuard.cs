using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Infrastructure.Validation;

/// <summary>
/// Compares a newly built canonical catalog with the last-known-good snapshot. This is
/// intentionally conservative: ordinary Tarkov data churn is accepted, while a source
/// that suddenly returns less than half of a previously healthy critical domain is
/// treated as a suspicious partial payload and never activated automatically.
/// </summary>
public sealed class ContentUpdateCompletenessGuard
{
    public const double MinimumRetainedFraction = 0.50;

    public ContentValidationResult Validate(
        GameContentCatalog candidate,
        GameContentCatalog? baseline)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (baseline is null)
            return new ContentValidationResult(Array.Empty<ContentValidationIssue>());

        var issues = new List<ContentValidationIssue>();
        Check("items", candidate.Items.Count, baseline.Items.Count, issues);
        Check("traders", candidate.Traders.Count, baseline.Traders.Count, issues);
        Check("maps", candidate.Maps.Count, baseline.Maps.Count, issues);
        Check("quests", candidate.Quests.Count, baseline.Quests.Count, issues);
        Check("quest-objectives", candidate.QuestObjectives.Count, baseline.QuestObjectives.Count, issues);
        Check("quest-items", candidate.QuestItemRequirements.Count, baseline.QuestItemRequirements.Count, issues);
        Check("hideout", candidate.HideoutStations.Count, baseline.HideoutStations.Count, issues);
        Check("ammo", candidate.Ammunition.Count, baseline.Ammunition.Count, issues);
        Check("editions", candidate.Editions.Count, baseline.Editions.Count, issues);
        return new ContentValidationResult(issues);
    }

    private static void Check(
        string domain,
        int candidateCount,
        int baselineCount,
        ICollection<ContentValidationIssue> issues)
    {
        if (baselineCount <= 0)
            return;

        var minimum = Math.Max(1, (int)Math.Floor(baselineCount * MinimumRetainedFraction));
        if (candidateCount >= minimum)
            return;

        issues.Add(new ContentValidationIssue(
            ContentValidationSeverity.Fatal,
            $"update.{domain}.suspicious-shrink",
            $"Candidate {domain} count '{candidateCount}' is below the safe retained floor '{minimum}' from baseline '{baselineCount}'."));
    }
}
