using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public enum QuestFutureReachabilityState
{
    Potential,
    Completed,
    Unavailable,
    IndeterminatePotential,
}

public enum QuestFutureReachabilityReasonKind
{
    Disabled,
    Failed,
    FailedByQuest,
    Faction,
    Edition,
    PrerequisiteUnavailable,
    MissingReferencedQuest,
    UnsupportedAvailabilityRequirement,
    MissingProfileValue,
    DependencyCycle,
}

public sealed record QuestFutureReachabilityReason(
    QuestFutureReachabilityReasonKind Kind,
    string? ReferenceId = null);

public sealed record QuestFutureReachabilityResult(
    string QuestId,
    QuestFutureReachabilityState State,
    IReadOnlyList<QuestFutureReachabilityReason> Reasons)
{
    public bool IncludeFutureRequirements =>
        State is QuestFutureReachabilityState.Potential or QuestFutureReachabilityState.IndeterminatePotential;
}

/// <summary>
/// Determines whether a quest can still matter to future item planning.
/// This is intentionally different from current availability: level, trader and prestige
/// gates can be satisfied later, while faction/edition/disabled exclusions are permanent.
/// Unknown rules remain potential so Junhyun Helper never tells the user to discard an item
/// merely because it could not prove a future path.
/// </summary>
public static class QuestFutureReachabilityEvaluator
{
    public static IReadOnlyDictionary<string, QuestFutureReachabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile,
        IEnumerable<EditionDefinition>? editions = null)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);

        var questList = quests.ToArray();
        var byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        foreach (var quest in questList)
        {
            if (!byId.TryAdd(quest.Id, quest))
                throw new InvalidDataException($"Duplicate quest id '{quest.Id}'.");
        }

        var currentAvailability = QuestAvailabilityEvaluator.Evaluate(
            questList,
            profile,
            editions ?? Array.Empty<EditionDefinition>());

        var effectiveFailedQuestIds = QuestFailureEvaluator.EffectiveFailedQuestIds(questList, profile);
        var evaluator = new Evaluator(byId, currentAvailability, profile, effectiveFailedQuestIds);
        foreach (var questId in byId.Keys)
            evaluator.EvaluateQuest(questId);

        return evaluator.Results;
    }

    private sealed class Evaluator
    {
        private readonly IReadOnlyDictionary<string, QuestDefinition> _questsById;
        private readonly IReadOnlyDictionary<string, QuestAvailabilityResult> _availability;
        private readonly GameProfileSnapshot _profile;
        private readonly IReadOnlySet<string> _effectiveFailedQuestIds;
        private readonly Dictionary<string, QuestFutureReachabilityResult> _results =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> _visiting = new(StringComparer.Ordinal);

        public Evaluator(
            IReadOnlyDictionary<string, QuestDefinition> questsById,
            IReadOnlyDictionary<string, QuestAvailabilityResult> availability,
            GameProfileSnapshot profile,
            IReadOnlySet<string> effectiveFailedQuestIds)
        {
            _questsById = questsById;
            _availability = availability;
            _profile = profile;
            _effectiveFailedQuestIds = effectiveFailedQuestIds;
        }

        public IReadOnlyDictionary<string, QuestFutureReachabilityResult> Results => _results;

        public QuestFutureReachabilityResult EvaluateQuest(string questId)
        {
            if (_results.TryGetValue(questId, out var cached))
                return cached;

            if (!_questsById.TryGetValue(questId, out var quest))
            {
                return new QuestFutureReachabilityResult(
                    questId,
                    QuestFutureReachabilityState.IndeterminatePotential,
                    [new QuestFutureReachabilityReason(
                        QuestFutureReachabilityReasonKind.MissingReferencedQuest,
                        questId)]);
            }

            if (_profile.CompletedQuestIds.Contains(questId))
            {
                var completed = new QuestFutureReachabilityResult(
                    questId,
                    QuestFutureReachabilityState.Completed,
                    Array.Empty<QuestFutureReachabilityReason>());
                _results[questId] = completed;
                return completed;
            }

            if (!_visiting.Add(questId))
            {
                return new QuestFutureReachabilityResult(
                    questId,
                    QuestFutureReachabilityState.IndeterminatePotential,
                    [new QuestFutureReachabilityReason(
                        QuestFutureReachabilityReasonKind.DependencyCycle,
                        questId)]);
            }

            try
            {
                var reasons = new List<QuestFutureReachabilityReason>();
                var indeterminate = false;

                if (_availability.TryGetValue(questId, out var availability))
                {
                    foreach (var reason in availability.Reasons)
                    {
                        switch (reason.Kind)
                        {
                            case QuestAvailabilityReasonKind.Disabled:
                                return StoreUnavailable(
                                    questId,
                                    QuestFutureReachabilityReasonKind.Disabled,
                                    reason.ReferenceId);
                            case QuestAvailabilityReasonKind.Failed:
                                return StoreUnavailable(
                                    questId,
                                    QuestFutureReachabilityReasonKind.Failed,
                                    reason.ReferenceId);
                            case QuestAvailabilityReasonKind.FailedByQuest:
                                return StoreUnavailable(
                                    questId,
                                    QuestFutureReachabilityReasonKind.FailedByQuest,
                                    reason.ReferenceId);
                            case QuestAvailabilityReasonKind.Faction:
                                return StoreUnavailable(
                                    questId,
                                    QuestFutureReachabilityReasonKind.Faction,
                                    reason.ReferenceId);
                            case QuestAvailabilityReasonKind.Edition:
                                return StoreUnavailable(
                                    questId,
                                    QuestFutureReachabilityReasonKind.Edition,
                                    reason.ReferenceId);
                            case QuestAvailabilityReasonKind.MissingProfileValue:
                                indeterminate = true;
                                reasons.Add(new QuestFutureReachabilityReason(
                                    QuestFutureReachabilityReasonKind.MissingProfileValue,
                                    reason.ReferenceId));
                                break;
                            case QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement:
                                indeterminate = true;
                                reasons.Add(new QuestFutureReachabilityReason(
                                    QuestFutureReachabilityReasonKind.UnsupportedAvailabilityRequirement,
                                    reason.ReferenceId));
                                break;
                            case QuestAvailabilityReasonKind.MissingReferencedQuest:
                                indeterminate = true;
                                reasons.Add(new QuestFutureReachabilityReason(
                                    QuestFutureReachabilityReasonKind.MissingReferencedQuest,
                                    reason.ReferenceId));
                                break;
                            case QuestAvailabilityReasonKind.DependencyCycle:
                                indeterminate = true;
                                reasons.Add(new QuestFutureReachabilityReason(
                                    QuestFutureReachabilityReasonKind.DependencyCycle,
                                    reason.ReferenceId));
                                break;
                        }
                    }
                }

                foreach (var requirement in quest.TaskRequirements)
                {
                    var prerequisite = EvaluatePrerequisite(requirement);
                    if (prerequisite.State == PrerequisiteFutureState.Unavailable)
                    {
                        return StoreUnavailable(
                            questId,
                            QuestFutureReachabilityReasonKind.PrerequisiteUnavailable,
                            requirement.RequiredQuestId);
                    }

                    if (prerequisite.State == PrerequisiteFutureState.Indeterminate)
                    {
                        indeterminate = true;
                        reasons.Add(new QuestFutureReachabilityReason(
                            prerequisite.ReasonKind ?? QuestFutureReachabilityReasonKind.MissingReferencedQuest,
                            requirement.RequiredQuestId));
                    }
                }

                var result = new QuestFutureReachabilityResult(
                    questId,
                    indeterminate
                        ? QuestFutureReachabilityState.IndeterminatePotential
                        : QuestFutureReachabilityState.Potential,
                    reasons.Distinct().ToArray());
                _results[questId] = result;
                return result;
            }
            finally
            {
                _visiting.Remove(questId);
            }
        }

        private QuestFutureReachabilityResult StoreUnavailable(
            string questId,
            QuestFutureReachabilityReasonKind kind,
            string? referenceId)
        {
            var result = new QuestFutureReachabilityResult(
                questId,
                QuestFutureReachabilityState.Unavailable,
                [new QuestFutureReachabilityReason(kind, referenceId)]);
            _results[questId] = result;
            return result;
        }

        private PrerequisiteFutureResult EvaluatePrerequisite(QuestTaskRequirement requirement)
        {
            if (!_questsById.ContainsKey(requirement.RequiredQuestId))
            {
                return new PrerequisiteFutureResult(
                    PrerequisiteFutureState.Indeterminate,
                    QuestFutureReachabilityReasonKind.MissingReferencedQuest);
            }

            var prerequisiteCompleted = _profile.CompletedQuestIds.Contains(requirement.RequiredQuestId);
            if (prerequisiteCompleted)
            {
                if (requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Complete) ||
                    requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Active))
                {
                    return new PrerequisiteFutureResult(PrerequisiteFutureState.Possible);
                }

                return new PrerequisiteFutureResult(PrerequisiteFutureState.Unavailable);
            }

            if (_effectiveFailedQuestIds.Contains(requirement.RequiredQuestId))
            {
                return requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Failed)
                    ? new PrerequisiteFutureResult(PrerequisiteFutureState.Possible)
                    : new PrerequisiteFutureResult(PrerequisiteFutureState.Unavailable);
            }

            var acceptsSuccess =
                requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Complete) ||
                requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Active);
            var acceptsFailure = requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Failed);

            if (!acceptsSuccess && !acceptsFailure)
                return new PrerequisiteFutureResult(PrerequisiteFutureState.Unavailable);

            // Before the prerequisite reaches a terminal state, both success and failure
            // are legitimate future possibilities. A failed-only branch is therefore not
            // an Indeterminate problem by itself; it remains part of the future plan until
            // the prerequisite outcome closes it.
            var prerequisite = EvaluateQuest(requirement.RequiredQuestId);
            return prerequisite.State switch
            {
                QuestFutureReachabilityState.Unavailable =>
                    new PrerequisiteFutureResult(PrerequisiteFutureState.Unavailable),
                QuestFutureReachabilityState.IndeterminatePotential =>
                    new PrerequisiteFutureResult(
                        PrerequisiteFutureState.Indeterminate,
                        prerequisite.Reasons.FirstOrDefault()?.Kind),
                _ => new PrerequisiteFutureResult(PrerequisiteFutureState.Possible),
            };
        }

        private enum PrerequisiteFutureState
        {
            Possible,
            Unavailable,
            Indeterminate,
        }

        private sealed record PrerequisiteFutureResult(
            PrerequisiteFutureState State,
            QuestFutureReachabilityReasonKind? ReasonKind = null);
    }
}
