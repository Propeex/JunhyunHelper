using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public sealed class QuestAvailabilityEvaluator
{
    private readonly IReadOnlyDictionary<string, QuestDefinition> _questsById;
    private readonly IReadOnlyDictionary<string, EditionDefinition> _editionsById;
    private readonly IReadOnlySet<string> _exclusiveQuestIds;
    private readonly IReadOnlySet<string> _editionSensitiveQuestIds;
    private readonly GameProfileSnapshot _profile;
    private readonly IReadOnlyDictionary<string, InferredQuestFailure> _inferredFailures;
    private readonly IReadOnlySet<string> _effectiveFailedQuestIds;
    private readonly Dictionary<string, QuestAvailabilityResult> _memo =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> _visiting = new(StringComparer.Ordinal);

    private QuestAvailabilityEvaluator(
        IReadOnlyDictionary<string, QuestDefinition> questsById,
        IReadOnlyDictionary<string, EditionDefinition> editionsById,
        IReadOnlySet<string> exclusiveQuestIds,
        IReadOnlySet<string> editionSensitiveQuestIds,
        GameProfileSnapshot profile,
        IReadOnlyDictionary<string, InferredQuestFailure> inferredFailures)
    {
        _questsById = questsById;
        _editionsById = editionsById;
        _exclusiveQuestIds = exclusiveQuestIds;
        _editionSensitiveQuestIds = editionSensitiveQuestIds;
        _profile = profile;
        _inferredFailures = inferredFailures;
        _effectiveFailedQuestIds = questsById.Values
            .Where(quest => quest.RequiresExplicitFailureInput && profile.FailedQuestIds.Contains(quest.Id))
            .Select(quest => quest.Id)
            .Concat(inferredFailures.Keys)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, QuestAvailabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile) =>
        Evaluate(quests, profile, Array.Empty<EditionDefinition>());

    public static IReadOnlyDictionary<string, QuestAvailabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile,
        IEnumerable<EditionDefinition> editions)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(editions);

        var questList = quests.ToArray();
        var byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
        foreach (var quest in questList)
        {
            if (!byId.TryAdd(quest.Id, quest))
                throw new InvalidDataException($"Duplicate quest id '{quest.Id}'.");
        }

        var editionsById = new Dictionary<string, EditionDefinition>(StringComparer.Ordinal);
        var exclusiveQuestIds = new HashSet<string>(StringComparer.Ordinal);
        var editionSensitiveQuestIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edition in editions)
        {
            if (!editionsById.TryAdd(edition.Id, edition))
                throw new InvalidDataException($"Duplicate edition id '{edition.Id}'.");

            exclusiveQuestIds.UnionWith(edition.ExclusiveQuestIds);
            editionSensitiveQuestIds.UnionWith(edition.ExclusiveQuestIds);
            editionSensitiveQuestIds.UnionWith(edition.ExcludedQuestIds);
        }

        var inferredFailures = QuestFailureEvaluator.InferCompletionTriggeredFailures(
            questList,
            profile.CompletedQuestIds);
        var evaluator = new QuestAvailabilityEvaluator(
            byId,
            editionsById,
            exclusiveQuestIds,
            editionSensitiveQuestIds,
            profile,
            inferredFailures);
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
                [new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingReferencedQuest,
                    questId)]);
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

        if (_profile.FailedQuestIds.Contains(questId) && quest.RequiresExplicitFailureInput)
        {
            var failed = new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Unavailable,
                [new QuestAvailabilityReason(QuestAvailabilityReasonKind.Failed)]);
            _memo[questId] = failed;
            return failed;
        }

        if (_inferredFailures.TryGetValue(questId, out var inferredFailure))
        {
            var failed = new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Unavailable,
                [new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.FailedByQuest,
                    inferredFailure.TriggerQuestId)]);
            _memo[questId] = failed;
            return failed;
        }

        if (!_visiting.Add(questId))
        {
            return new QuestAvailabilityResult(
                questId,
                QuestAvailabilityState.Indeterminate,
                [new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.DependencyCycle,
                    questId)]);
        }

        try
        {
            var unavailableReasons = new List<QuestAvailabilityReason>();
            var lockedReasons = new List<QuestAvailabilityReason>();
            var unknownReasons = new List<QuestAvailabilityReason>();

            EvaluateStaticRules(quest, unavailableReasons, lockedReasons, unknownReasons);
            EvaluatePrerequisites(quest, unavailableReasons, lockedReasons, unknownReasons);

            QuestAvailabilityResult result;
            if (unavailableReasons.Count > 0)
            {
                result = new QuestAvailabilityResult(
                    questId,
                    QuestAvailabilityState.Unavailable,
                    unavailableReasons);
            }
            else if (lockedReasons.Count > 0)
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
        ICollection<QuestAvailabilityReason> unavailableReasons,
        ICollection<QuestAvailabilityReason> lockedReasons,
        ICollection<QuestAvailabilityReason> unknownReasons)
    {
        if (quest.Disabled)
        {
            unavailableReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.Disabled));
        }

        foreach (var requirementType in quest.UnsupportedAvailabilityRequirements)
        {
            unknownReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement,
                requirementType));
        }

        if (_profile.Level < quest.MinimumPlayerLevel)
        {
            lockedReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.MinimumLevel));
        }

        if (quest.RequiredFaction is { } faction && _profile.Faction != faction)
        {
            unavailableReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.Faction));
        }

        EvaluateEditionRule(quest, unavailableReasons, unknownReasons);

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
            if (!_profile.Traders.TryGetValue(requirement.TraderId, out var traderProgress) ||
                traderProgress.Standing is null)
            {
                unknownReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingProfileValue,
                    requirement.TraderId));
                continue;
            }

            var standing = traderProgress.Standing.Value;
            var standingMet = requirement.Operator switch
            {
                StandingRequirementOperator.AtLeast =>
                    standing >= requirement.RequiredStanding,
                StandingRequirementOperator.AtMost =>
                    standing <= requirement.RequiredStanding,
                StandingRequirementOperator.LessThan =>
                    standing < requirement.RequiredStanding,
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
            if (!_profile.Traders.TryGetValue(requirement.TraderId, out var traderProgress) ||
                traderProgress.LoyaltyLevel is null)
            {
                unknownReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.MissingProfileValue,
                    requirement.TraderId));
                continue;
            }

            if (traderProgress.LoyaltyLevel.Value < requirement.RequiredLoyaltyLevel)
            {
                lockedReasons.Add(new QuestAvailabilityReason(
                    QuestAvailabilityReasonKind.TraderLoyalty,
                    requirement.TraderId));
            }
        }
    }

    private void EvaluateEditionRule(
        QuestDefinition quest,
        ICollection<QuestAvailabilityReason> unavailableReasons,
        ICollection<QuestAvailabilityReason> unknownReasons)
    {
        if (!_editionSensitiveQuestIds.Contains(quest.Id))
            return;

        if (string.IsNullOrWhiteSpace(_profile.EditionId))
        {
            unknownReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.MissingProfileValue,
                "edition"));
            return;
        }

        if (!_editionsById.TryGetValue(_profile.EditionId, out var edition))
        {
            unknownReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.MissingProfileValue,
                $"edition:{_profile.EditionId}"));
            return;
        }

        if (edition.ExcludedQuestIds.Contains(quest.Id) ||
            (_exclusiveQuestIds.Contains(quest.Id) && !edition.ExclusiveQuestIds.Contains(quest.Id)))
        {
            unavailableReasons.Add(new QuestAvailabilityReason(
                QuestAvailabilityReasonKind.Edition,
                edition.Id));
        }
    }

    private void EvaluatePrerequisites(
        QuestDefinition quest,
        ICollection<QuestAvailabilityReason> unavailableReasons,
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
                case PrerequisiteOutcome.Unavailable:
                    unavailableReasons.Add(new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.PrerequisiteUnavailable,
                        requirement.RequiredQuestId));
                    break;
                case PrerequisiteOutcome.Indeterminate:
                    unknownReasons.Add(new QuestAvailabilityReason(
                        QuestAvailabilityReasonKind.MissingReferencedQuest,
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
        if (completed)
        {
            return requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Complete) ||
                   requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Active)
                ? PrerequisiteOutcome.Met
                : PrerequisiteOutcome.Unavailable;
        }

        if (_effectiveFailedQuestIds.Contains(requirement.RequiredQuestId))
        {
            return requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Failed)
                ? PrerequisiteOutcome.Met
                : PrerequisiteOutcome.Unavailable;
        }

        if (requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Active))
        {
            var requiredQuest = EvaluateQuest(requirement.RequiredQuestId);
            if (requiredQuest.State is QuestAvailabilityState.Current or QuestAvailabilityState.Completed)
                return PrerequisiteOutcome.Met;
            if (requiredQuest.State == QuestAvailabilityState.Unavailable)
                return PrerequisiteOutcome.Unavailable;
            if (requiredQuest.State == QuestAvailabilityState.Indeterminate)
                return PrerequisiteOutcome.Indeterminate;
        }

        if (requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Complete) ||
            requirement.AcceptedStatuses.Contains(QuestRequiredStatus.Failed))
        {
            var requiredQuest = EvaluateQuest(requirement.RequiredQuestId);
            return requiredQuest.State == QuestAvailabilityState.Unavailable
                ? PrerequisiteOutcome.Unavailable
                : PrerequisiteOutcome.NotMet;
        }

        return PrerequisiteOutcome.NotMet;
    }

    private enum PrerequisiteOutcome
    {
        Met,
        NotMet,
        Unavailable,
        Indeterminate,
    }
}
