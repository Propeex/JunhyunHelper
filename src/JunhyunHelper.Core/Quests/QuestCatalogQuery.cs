using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public sealed record QuestCatalogEntry(
    QuestDefinition Quest,
    QuestAvailabilityResult Availability);

public static class QuestCatalogQuery
{
    public static IReadOnlyList<QuestCatalogEntry> Evaluate(
        IReadOnlyList<QuestDefinition> quests,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);

        var availability = QuestAvailabilityEvaluator.Evaluate(quests, profile);
        return quests
            .Select(quest => new QuestCatalogEntry(quest, availability[quest.Id]))
            .ToArray();
    }

    public static IReadOnlyList<QuestCatalogEntry> Current(
        IReadOnlyList<QuestDefinition> quests,
        GameProfileSnapshot profile) =>
        Evaluate(quests, profile)
            .Where(entry => entry.Availability.State == QuestAvailabilityState.Current)
            .ToArray();
}
