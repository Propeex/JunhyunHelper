using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public sealed record InferredQuestFailure(
    string QuestId,
    string TriggerQuestId);

public static class QuestFailureEvaluator
{
    public static IReadOnlyDictionary<string, InferredQuestFailure> InferCompletionTriggeredFailures(
        IEnumerable<QuestDefinition> quests,
        IReadOnlySet<string> completedQuestIds)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(completedQuestIds);

        var result = new Dictionary<string, InferredQuestFailure>(StringComparer.Ordinal);
        foreach (var quest in quests)
        {
            if (completedQuestIds.Contains(quest.Id))
                continue;

            var trigger = quest.CompletionFailureConditions
                .Select(static condition => condition.TriggerQuestId)
                .Where(completedQuestIds.Contains)
                .Order(StringComparer.Ordinal)
                .FirstOrDefault();
            if (trigger is null)
                continue;

            result[quest.Id] = new InferredQuestFailure(quest.Id, trigger);
        }

        return result;
    }

    public static IReadOnlySet<string> EffectiveFailedQuestIds(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);

        var questList = quests.ToArray();
        var failed = questList
            .Where(quest => quest.RequiresExplicitFailureInput && profile.FailedQuestIds.Contains(quest.Id))
            .Select(quest => quest.Id)
            .ToHashSet(StringComparer.Ordinal);
        failed.UnionWith(InferCompletionTriggeredFailures(questList, profile.CompletedQuestIds).Keys);
        failed.ExceptWith(profile.CompletedQuestIds);
        return failed;
    }
}
