using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public sealed class QuestAvailabilityEvaluator
{
    private readonly IReadOnlyDictionary<string, QuestDefinition> _questsById;
    private readonly GameProfileSnapshot _profile;
    private readonly Dictionary<string, QuestAvailabilityResult> _memo =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _visiting = new(StringComparer.Ordinal);

    private QuestAvailabilityEvaluator(
        IReadOnlyDictionary<string, QuestDefinition> questsById,
        GameProfileSnapshot profile)
    {
        _questsById = questsById;
        _profile = profile;
    }

    public static IReadOnlyDictionary<string, QuestAvailabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);

        var byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        foreach (var quest in quests)
        {
            if (!byId.TryAdd(quest.Id, quest))
                throw new InvalidDataException($"Duplicate quest id '{quest.Id}'.");
        }

        var evaluator = new QuestAvailabilityEvaluator(byId, profile);
        foreach (var questId in byId.Keys)
            evaluator.EvaluateQuest(questId);

        return evaluator._memo;
    }

    private QuestAvailabilityResult EvaluateQuest(string questId)
    {
        if (_memo.TryGetValue(questId, out var cached))
            return cached;

        if (!_questsById.TryGetValue(questId, out var quest))
        {
            return new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Indeterminate,
                new[]
                {
                    new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.MissingReferencedQuest,
                        questId),
                });
        }

        if (_profile.CompletedQuestIds.Contains(questId))
        {
            var completed = new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Completed,
                Array.Empty<QuestAvailabilityReason>());
            _memo[questId] = completed;
            return completed;
        }

        if (!_visiting.Add(questId))
        {
            return new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Indeterminate,
                new[]
                {
                    new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.DependencyCycle,
                        questId),
                });
        }

        try
        {
            var lockedReasons = new List<QuestAvailabilityReason>();
            var unknownReasons = new List<QuestAvailabilityReason>();

            EvaluateStaticRules(quest, lockedReasons, unknownReasons);
            EvaluatePrerequisites(quest, lockedReasons, unknownReasons);

            QuestAvailabilityResult result;
            if (lockedReasons.Count > 0)
            {
                result = new QuestAvailabilityResult(
                    questId,
                    QuestAvailabilityState.Locked,
                    lockedReasons);
            }
            else if (unknownReasons.Count > 0)
            {
                result = new QuestAvailabilityResult(
                    questId,
                    QuestAvailabilityState.Indeterminate,
                    unknownReasons);
            }
            else
            {
                result = new QuestAvailabilityResult(
                    questId,
                    QuestAvailabilityState.Current,
                    Array.Empty<QuestAvailabilityReason>());
            }

            _memo[questId] = result;
            return result;
        }
        finally
        {
            _visiting.Remove(questId);
        }
    }

    private void EvaluateStaticRules(
        QuestDefinition quest,
        ICollection<QuestAvailabilityReason> lockedReasons,
        ICollection<QuestAvailabilityReason> unknownReasons)
    {
        if (quest.Disabled)
        {
            lockedReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.Disabled));
        }

        if (_profile.Level < quest.MinimumPlayerLevel)
        {
            lockedReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.MinimumLevel));
        }

        if (quest.RequiredFaction is { } faction && _profile.Faction != faction)
        {
            lockedReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.Faction));
        }

        if (quest.RequiredPrestigeLevel is { } requiredPrestige)
        {
            if (_profile.PrestigeLevel is null)
            {
                unknownReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingProfileValue,
                    "prestige"));
            }
            else if (_profile.PrestigeLevel.Value < requiredPrestige)
            {
                lockedReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.Prestige));
            }
        }

        foreach (var requirement in quest.TraderStandingRequirements)
        {
            if (!_profile.Traders.TryGetValue(requirement.TraderId, out var traderProgress))
            {
                unknownReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingProfileValue,
                    requirement.TraderId));
                continue;
            }

            var standingMet = requirement.Operator switch
            {
                StandingRequirementOperator.AtLeast =>
                    traderProgress.Standing >= requirement.RequiredStanding,
                StandingRequirementOperator.AtMost =>
                    traderProgress.Standing <= requirement.RequiredStanding,
                _ => throw new InvalidDataException(
                    $"Unsupported standing operator '{requirement.Operator}'."),
            };

            if (!standingMet)
            {
                lockedReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.TraderStanding,
                    requirement.TraderId));
            }
        }

        foreach (var requirement in quest.TraderLoyaltyRequirements)
        {
            if (!_profile.Traders.TryGetValue(requirement.TraderId, out var traderProgress))
            {
                unknownReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingProfileValue,
                    requirement.TraderId));
                continue;
            }

            if (traderProgress.LoyaltyLevel < requirement.RequiredLoyaltyLevel)
            {
                lockedReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.TraderLoyalty,
                    requirement.TraderId));
            }
        }
    }

    private void EvaluatePrerequisites(
        QuestDefinition quest,
        ICollection<QuestAvailabilityReason> lockedReasons,
        ICollection<QuestAvailabilityReason> unknownReasons)
    {
        foreach (var requirement in quest.TaskRequirements)
        {
            var outcome = EvaluatePrerequisite(requirement);
            switch (outcome)
            {
                case PrerequisiteOutcome.Met:
                    break;
                case PrerequisiteOutcome.NotMet:
                    lockedReasons.Add(new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.Prerequisite,
                        requirement.RequiredQuestId));
                    break;
                case PrerequisiteOutcome.Indeterminate:
                    unknownReasons.Add(new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.FailedPrerequisiteStateNotTracked,
                        requirement.RequiredQuestId));
                    break;
                default:
                    throw new InvalidDataException(
                        $"Unsupported prerequisite outcome '{outcome}'.");
            }
        }
    }

    private PrerequisiteOutcome EvaluatePrerequisite(QuestTaskRequirement requirement)
    {
        if (!_questsById.ContainsKey(requirement.RequiredQuestId))
            return PrerequisiteOutcome.Indeterminate;

        var completed = _profile.CompletedQuestIds.Contains(requirement.RequiredQuestId);
        if (completed && requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Complete))
            return PrerequisiteOutcome.Met;

        if (requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Active))
        {
            var requiredQuest = EvaluateQuest(requirement.RequiredQuestId);
            if (requiredQuest.State is QuestAvailabilityState.Current or QuestAvailabilityState.Completed)
                return PrerequisiteOutcome.Met;
            if (requiredQuest.State == QuestAvailabilityState.Indeterminate)
                return PrerequisiteOutcome.Indeterminate;
        }

        if (requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Failed))
        {
            // Failed quest state is intentionally not persisted until its product UX is defined.
            // Do not guess whether the prerequisite failed.
            return PrerequisiteOutcome.Indeterminate;
        }

        return PrerequisiteOutcome.NotMet;
    }

    private enum PrerequisiteOutcome
    {
        Met,
        NotMet,
        Indeterminate,
    }
}
