using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public sealed record QuestCatalogEntry(
    QuestDefinition Quest,
    QuestAvailabilityResult Availability);

public static class QuestCatalogQuery
{
    public static IReadOnlyList<QuestCatalogEntry> Evaluate(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);

        // Exact imported profile variable values remain authoritative. When they are
        // absent, current quest presentation may use the fail-closed audited EFT 1.1
        // LL2–LL4 task-pool reconstruction. The enriched values are runtime-only.
        var availabilityProfile = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(
            content.Quests,
            profile);
        var availability = QuestAvailabilityEvaluator.Evaluate(
            content.Quests,
            availabilityProfile,
            content.Editions);
        return content.Quests
            .Select(quest => new QuestCatalogEntry(quest, availability[quest.Id]))
            .ToArray();
    }

    public static IReadOnlyList<QuestCatalogEntry> Current(
        GameContentCatalog content,
        GameProfileSnapshot profile) =>
        Evaluate(content, profile)
            .Where(entry => entry.Availability.State == QuestAvailabilityState.Current)
            .ToArray();
}
