using JunhyunHelper.Core.Content;

namespace JunhyunHelper.Infrastructure.Validation;

/// <summary>
/// Compares a newly built canonical catalog with the last-known-good snapshot. This is
/// intentionally conservative: ordinary Tarkov data churn is accepted, while a source
/// that suddenly returns less than half of a previously healthy critical domain,
/// relationship set, localization set, or visible-resource set is treated as a
/// suspicious partial payload and never activated automatically.
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

        // Top-level counts alone cannot detect a source that keeps entity shells while
        // silently dropping important nested data. Protect the relationships that drive
        // quest routing, required-item totals, hideout progress and ammo acquisition.
        Check(
            "quest-prerequisites",
            candidate.Quests.Sum(static quest => quest.TaskRequirements.Count),
            baseline.Quests.Sum(static quest => quest.TaskRequirements.Count),
            issues);
        Check(
            "quest-map-locations",
            candidate.QuestObjectives.Sum(static objective => objective.MapLocations.Count),
            baseline.QuestObjectives.Sum(static objective => objective.MapLocations.Count),
            issues);
        Check(
            "hideout-levels",
            candidate.HideoutStations.Sum(static station => station.Levels.Count),
            baseline.HideoutStations.Sum(static station => station.Levels.Count),
            issues);
        Check(
            "hideout-items",
            candidate.HideoutStations.Sum(static station =>
                station.Levels.Sum(static level => level.ItemRequirements.Count)),
            baseline.HideoutStations.Sum(static station =>
                station.Levels.Sum(static level => level.ItemRequirements.Count)),
            issues);
        Check(
            "ammo-acquisitions",
            candidate.Ammunition.Sum(static ammo => ammo.Acquisitions.Count),
            baseline.Ammunition.Sum(static ammo => ammo.Acquisitions.Count),
            issues);
        Check(
            "ammo-acquisition-requirements",
            candidate.Ammunition.Sum(static ammo =>
                ammo.Acquisitions.Sum(static acquisition => acquisition.Requirements.Count)),
            baseline.Ammunition.Sum(static ammo =>
                ammo.Acquisitions.Sum(static acquisition => acquisition.Requirements.Count)),
            issues);

        ProtectItemRelationshipCoverage(candidate, baseline, issues);

        // Translation endpoints are deliberately fail-soft in the source loader. That is
        // useful on a first install, but an established installation must not replace a
        // healthy Korean catalog with raw translation keys after a partial localization
        // outage. NameKo/DescriptionKo are null when the Korean key cannot be resolved,
        // so baseline coverage gives us a precise, non-heuristic regression signal.
        CheckCoverage(
            "item-korean",
            candidate.Items.Count(static item => !string.IsNullOrWhiteSpace(item.NameKo)),
            baseline.Items.Count(static item => !string.IsNullOrWhiteSpace(item.NameKo)),
            issues);
        CheckCoverage(
            "trader-korean",
            candidate.Traders.Count(static trader => !string.IsNullOrWhiteSpace(trader.NameKo)),
            baseline.Traders.Count(static trader => !string.IsNullOrWhiteSpace(trader.NameKo)),
            issues);
        CheckCoverage(
            "map-korean",
            candidate.Maps.Count(static map => !string.IsNullOrWhiteSpace(map.NameKo)),
            baseline.Maps.Count(static map => !string.IsNullOrWhiteSpace(map.NameKo)),
            issues);
        CheckCoverage(
            "quest-korean",
            candidate.Quests.Count(static quest => !string.IsNullOrWhiteSpace(quest.NameKo)),
            baseline.Quests.Count(static quest => !string.IsNullOrWhiteSpace(quest.NameKo)),
            issues);
        CheckCoverage(
            "quest-objective-korean",
            candidate.QuestObjectives.Count(static objective => !string.IsNullOrWhiteSpace(objective.DescriptionKo)),
            baseline.QuestObjectives.Count(static objective => !string.IsNullOrWhiteSpace(objective.DescriptionKo)),
            issues);
        CheckCoverage(
            "hideout-korean",
            candidate.HideoutStations.Count(static station => !string.IsNullOrWhiteSpace(station.NameKo)),
            baseline.HideoutStations.Count(static station => !string.IsNullOrWhiteSpace(station.NameKo)),
            issues);

        // URLs/images are optional per individual record, but a sudden bulk loss of
        // previously populated coverage would break visible product behavior. Only compare
        // an established baseline so first-install/source sparsity remains supported.
        CheckCoverage(
            "item-icons",
            candidate.Items.Count(static item => !string.IsNullOrWhiteSpace(item.IconUrl)),
            baseline.Items.Count(static item => !string.IsNullOrWhiteSpace(item.IconUrl)),
            issues);
        CheckCoverage(
            "item-wiki",
            candidate.Items.Count(static item => !string.IsNullOrWhiteSpace(item.WikiUrl)),
            baseline.Items.Count(static item => !string.IsNullOrWhiteSpace(item.WikiUrl)),
            issues);
        CheckCoverage(
            "quest-wiki",
            candidate.Quests.Count(static quest => !string.IsNullOrWhiteSpace(quest.WikiUrl)),
            baseline.Quests.Count(static quest => !string.IsNullOrWhiteSpace(quest.WikiUrl)),
            issues);
        CheckCoverage(
            "hideout-images",
            candidate.HideoutStations.Count(static station => !string.IsNullOrWhiteSpace(station.ImageUrl)),
            baseline.HideoutStations.Count(static station => !string.IsNullOrWhiteSpace(station.ImageUrl)),
            issues);

        return new ContentValidationResult(issues);
    }

    private static void ProtectItemRelationshipCoverage(
        GameContentCatalog candidate,
        GameContentCatalog baseline,
        ICollection<ContentValidationIssue> issues)
    {
        // v3-v7 snapshots intentionally have no item relationship graph. They remain
        // readable and cannot provide a trustworthy relative baseline for the first v8+
        // refresh. Once a healthy relationship graph exists, protect every acquisition
        // domain and its nested material coverage with the same 50% LKG rule.
        if (baseline.ItemRelationshipData is null)
            return;

        var candidateRelationships = candidate.ItemRelationships;
        var baselineRelationships = baseline.ItemRelationshipData;

        Check(
            "item-trader-purchases",
            candidateRelationships.TraderPurchases.Count,
            baselineRelationships.TraderPurchases.Count,
            issues);
        Check(
            "item-barters",
            candidateRelationships.Barters.Count,
            baselineRelationships.Barters.Count,
            issues);
        Check(
            "item-crafts",
            candidateRelationships.Crafts.Count,
            baselineRelationships.Crafts.Count,
            issues);
        Check(
            "item-flea-acquisitions",
            candidateRelationships.FleaMarketItemIds.Count,
            baselineRelationships.FleaMarketItemIds.Count,
            issues);
        Check(
            "item-barter-requirements",
            candidateRelationships.Barters.Sum(static barter => barter.RequiredItems.Count),
            baselineRelationships.Barters.Sum(static barter => barter.RequiredItems.Count),
            issues);
        Check(
            "item-craft-requirements",
            candidateRelationships.Crafts.Sum(static craft => craft.RequiredItems.Count),
            baselineRelationships.Crafts.Sum(static craft => craft.RequiredItems.Count),
            issues);
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

    private static void CheckCoverage(
        string domain,
        int candidateCount,
        int baselineCount,
        ICollection<ContentValidationIssue> issues)
    {
        // Small optional/localized sets are too volatile to infer a source regression safely.
        if (baselineCount < 10)
            return;
        Check(domain, candidateCount, baselineCount, issues);
    }
}
