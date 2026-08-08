using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestAvailabilityEvaluatorTests
{
    [Fact]
    public void QuestWithSatisfiedKnownRulesIsCurrent()
    {
        var quest = Quest("quest-a", minimumLevel: 10, requiredFaction: PmcFaction.Usec);
        var profile = Profile(level: 10, faction: PmcFaction.Usec);

        var result = Evaluate(profile, quest)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Current, result.State);
    }

    [Fact]
    public void CompletedQuestIsCompletedInsteadOfCurrent()
    {
        var quest = Quest("quest-a");
        var profile = Profile(
            completedQuestIds: new HashSet<string>(StringComparer.Ordinal) { quest.Id });

        var result = Evaluate(profile, quest)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Completed, result.State);
    }

    [Fact]
    public void KnownUnmetLevelLocksQuest()
    {
        var quest = Quest("quest-a", minimumLevel: 20);
        var profile = Profile(level: 19);

        var result = Evaluate(profile, quest)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Locked, result.State);
        Assert.Contains(result.Reasons, reason => reason.Kind == QuestAvailabilityReasonKind.MinimumLevel);
    }

    [Fact]
    public void CompletedPrerequisiteUnlocksDependentQuest()
    {
        var prerequisite = Quest("quest-a");
        var dependent = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    prerequisite.Id,
                    new[] { QuestRequiredStatus.Complete }),
            ]);
        var profile = Profile(
            completedQuestIds: new HashSet<string>(StringComparer.Ordinal) { prerequisite.Id });

        var result = Evaluate(profile, prerequisite, dependent);

        Assert.Equal(QuestAvailabilityState.Current, result[dependent.Id].State);
    }

    [Fact]
    public void UnlockablePrerequisiteCountsAsActiveBecauseHelperAutoAcceptsAvailableQuests()
    {
        var prerequisite = Quest("quest-a");
        var dependent = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    prerequisite.Id,
                    new[] { QuestRequiredStatus.Active }),
            ]);
        var profile = Profile();

        var result = Evaluate(profile, prerequisite, dependent);

        Assert.Equal(QuestAvailabilityState.Current, result[prerequisite.Id].State);
        Assert.Equal(QuestAvailabilityState.Current, result[dependent.Id].State);
    }

    [Fact]
    public void MissingTraderProgressIsIndeterminateInsteadOfAssumedZero()
    {
        var quest = Quest(
            "quest-a",
            traderStandingRequirements:
            [
                new QuestTraderStandingRequirement(
                    "fence",
                    1.5m,
                    StandingRequirementOperator.AtLeast),
            ]);
        var profile = Profile();

        var result = Evaluate(profile, quest)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Kind == QuestAvailabilityReasonKind.MissingProfileValue &&
                      reason.ReferenceId == "trader-standing:fence");
    }

    [Fact]
    public void KnownLoyaltyDoesNotInventStanding()
    {
        var loyaltyQuest = Quest(
            "loyalty",
            traderLoyaltyRequirements:
            [
                new QuestTraderLoyaltyRequirement("trader-a", 3),
            ]);
        var standingQuest = Quest(
            "standing",
            traderStandingRequirements:
            [
                new QuestTraderStandingRequirement(
                    "trader-a",
                    0.5m,
                    StandingRequirementOperator.AtLeast),
            ]);
        var profile = Profile(
            traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
            {
                ["trader-a"] = new(3, null),
            });

        var result = Evaluate(profile, loyaltyQuest, standingQuest);

        Assert.Equal(QuestAvailabilityState.Current, result[loyaltyQuest.Id].State);
        Assert.Equal(QuestAvailabilityState.Indeterminate, result[standingQuest.Id].State);
    }

    [Fact]
    public void KnownStandingDoesNotInventLoyalty()
    {
        var loyaltyQuest = Quest(
            "loyalty",
            traderLoyaltyRequirements:
            [
                new QuestTraderLoyaltyRequirement("trader-a", 3),
            ]);
        var standingQuest = Quest(
            "standing",
            traderStandingRequirements:
            [
                new QuestTraderStandingRequirement(
                    "trader-a",
                    0.5m,
                    StandingRequirementOperator.AtLeast),
            ]);
        var profile = Profile(
            traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
            {
                ["trader-a"] = new(null, 0.6m),
            });

        var result = Evaluate(profile, loyaltyQuest, standingQuest);

        Assert.Equal(QuestAvailabilityState.Indeterminate, result[loyaltyQuest.Id].State);
        Assert.Equal(QuestAvailabilityState.Current, result[standingQuest.Id].State);
    }

    [Fact]
    public void MaximumTraderStandingRequirementUsesExplicitComparisonDirection()
    {
        var quest = Quest(
            "quest-a",
            traderStandingRequirements:
            [
                new QuestTraderStandingRequirement(
                    "fence",
                    -1.0m,
                    StandingRequirementOperator.AtMost),
            ]);

        var matchingProfile = Profile(
            traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
            {
                ["fence"] = new(1, -1.5m),
            });
        var blockedProfile = Profile(
            traders: new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
            {
                ["fence"] = new(1, 0m),
            });

        Assert.Equal(
            QuestAvailabilityState.Current,
            Evaluate(matchingProfile, quest)[quest.Id].State);
        Assert.Equal(
            QuestAvailabilityState.Locked,
            Evaluate(blockedProfile, quest)[quest.Id].State);
    }

    [Fact]
    public void FailedOnlyPrerequisiteIsLockedUntilFailureOccurs()
    {
        var prerequisite = Quest("quest-a");
        var dependent = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    prerequisite.Id,
                    new[] { QuestRequiredStatus.Failed }),
            ]);

        var result = Evaluate(Profile(), prerequisite, dependent)[dependent.Id];

        Assert.Equal(QuestAvailabilityState.Locked, result.State);
        Assert.Contains(
            result.Reasons,
            reason => reason.Kind == QuestAvailabilityReasonKind.Prerequisite);
    }

    [Fact]
    public void ExplicitPermanentFailureUnlocksFailedOnlyDependentQuest()
    {
        var prerequisite = Quest(
            "quest-a",
            unsupportedFailureConditions: ["shoot"]);
        var dependent = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    prerequisite.Id,
                    new[] { QuestRequiredStatus.Failed }),
            ]);
        var profile = Profile(
            failedQuestIds: new HashSet<string>(StringComparer.Ordinal) { prerequisite.Id });

        var result = Evaluate(profile, prerequisite, dependent);

        Assert.Equal(QuestAvailabilityState.Unavailable, result[prerequisite.Id].State);
        Assert.Equal(QuestAvailabilityState.Current, result[dependent.Id].State);
    }

    [Fact]
    public void CompletedSiblingDeterministicallyFailsMutuallyExclusiveQuest()
    {
        var chosen = Quest("chosen");
        var failedSibling = Quest(
            "failed-sibling",
            completionFailureConditions: [new QuestCompletionFailureCondition(chosen.Id)]);
        var recovery = Quest(
            "recovery",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    failedSibling.Id,
                    new[] { QuestRequiredStatus.Failed }),
            ]);
        var profile = Profile(
            completedQuestIds: new HashSet<string>(StringComparer.Ordinal) { chosen.Id });

        var result = Evaluate(profile, chosen, failedSibling, recovery);

        Assert.Equal(QuestAvailabilityState.Unavailable, result[failedSibling.Id].State);
        Assert.Contains(
            result[failedSibling.Id].Reasons,
            reason => reason.Kind == QuestAvailabilityReasonKind.FailedByQuest &&
                      reason.ReferenceId == chosen.Id);
        Assert.Equal(QuestAvailabilityState.Current, result[recovery.Id].State);
    }

    [Fact]
    public void CompleteOrFailedPrerequisiteIsOrdinaryLockedBranchBeforeTerminalOutcome()
    {
        var prerequisite = Quest("quest-a");
        var dependent = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement(
                    prerequisite.Id,
                    new[] { QuestRequiredStatus.Complete, QuestRequiredStatus.Failed }),
            ]);

        var result = Evaluate(Profile(), prerequisite, dependent);

        Assert.Equal(QuestAvailabilityState.Locked, result[dependent.Id].State);
    }

    [Fact]
    public void DependencyCycleDoesNotGuessAvailability()
    {
        var questA = Quest(
            "quest-a",
            taskRequirements:
            [
                new QuestTaskRequirement("quest-b", new[] { QuestRequiredStatus.Active }),
            ]);
        var questB = Quest(
            "quest-b",
            taskRequirements:
            [
                new QuestTaskRequirement("quest-a", new[] { QuestRequiredStatus.Active }),
            ]);

        var result = Evaluate(Profile(), questA, questB);

        Assert.Equal(QuestAvailabilityState.Indeterminate, result[questA.Id].State);
        Assert.Equal(QuestAvailabilityState.Indeterminate, result[questB.Id].State);
    }

    private static IReadOnlyDictionary<string, QuestAvailabilityResult> Evaluate(
        GameProfileSnapshot profile,
        params QuestDefinition[] quests) =>
        QuestAvailabilityEvaluator.Evaluate(quests, profile);

    private static QuestDefinition Quest(
        string id,
        int minimumLevel = 0,
        PmcFaction? requiredFaction = null,
        int? requiredPrestigeLevel = null,
        IReadOnlyList<QuestTaskRequirement>? taskRequirements = null,
        IReadOnlyList<QuestTraderStandingRequirement>? traderStandingRequirements = null,
        IReadOnlyList<QuestTraderLoyaltyRequirement>? traderLoyaltyRequirements = null,
        bool disabled = false,
        IReadOnlyList<QuestCompletionFailureCondition>? completionFailureConditions = null,
        IReadOnlyList<string>? unsupportedFailureConditions = null) =>
        new(
            Id: id,
            NameKo: null,
            NameEn: null,
            TraderId: null,
            MapId: null,
            WikiUrl: null,
            Experience: null,
            KappaRequired: false,
            LightkeeperRequired: false,
            Disabled: disabled,
            MinimumPlayerLevel: minimumLevel,
            RequiredFaction: requiredFaction,
            RequiredPrestigeLevel: requiredPrestigeLevel,
            TaskRequirements: taskRequirements ?? [],
            TraderStandingRequirements: traderStandingRequirements ?? [],
            TraderLoyaltyRequirements: traderLoyaltyRequirements ?? [],
            CompletionFailureConditionData: completionFailureConditions,
            Restartable: false,
            UnsupportedFailureConditionTypes: unsupportedFailureConditions);

    private static GameProfileSnapshot Profile(
        int level = 1,
        PmcFaction faction = PmcFaction.Usec,
        int? prestigeLevel = null,
        IReadOnlyDictionary<string, TraderProgress>? traders = null,
        IReadOnlySet<string>? completedQuestIds = null,
        IReadOnlySet<string>? failedQuestIds = null) =>
        new()
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = level,
            Faction = faction,
            PrestigeLevel = prestigeLevel,
            Traders = traders ?? new Dictionary<string, TraderProgress>(StringComparer.Ordinal),
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
            FailedQuestIds = failedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
        };
}
