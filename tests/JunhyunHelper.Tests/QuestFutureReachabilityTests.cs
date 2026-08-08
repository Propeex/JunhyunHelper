using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class QuestFutureReachabilityTests
{
    [Fact]
    public void LevelGate_RemainsPotentialForFuturePlanning()
    {
        var result = Evaluate([Quest("q", minimumLevel: 40)], Profile(level: 10))["q"];
        Assert.Equal(QuestFutureReachabilityState.Potential, result.State);
    }

    [Fact]
    public void FactionMismatch_IsUnavailable()
    {
        var result = Evaluate([Quest("q", requiredFaction: PmcFaction.Bear)], Profile())["q"];
        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
        Assert.False(result.IncludeFutureRequirements);
    }

    [Fact]
    public void EditionExclusion_IsUnavailable()
    {
        var edition = new EditionDefinition(
            "standard", "Standard",
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(["q"], StringComparer.Ordinal));

        var result = QuestFutureReachabilityEvaluator.Evaluate(
            [Quest("q")], Profile(editionId: "standard"), [edition])["q"];

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
    }

    [Fact]
    public void CompletedQuest_IsNotFutureRequirement()
    {
        var result = Evaluate(
            [Quest("q")],
            Profile(completedQuestIds: new HashSet<string>(["q"], StringComparer.Ordinal)))["q"];

        Assert.Equal(QuestFutureReachabilityState.Completed, result.State);
        Assert.False(result.IncludeFutureRequirements);
    }

    [Fact]
    public void CompletedPrerequisite_ClosesFailedOnlyBranch()
    {
        var source = Quest("source");
        var branch = Quest("branch", requirements: [FailedRequirement("source")]);
        var result = Evaluate(
            [source, branch],
            Profile(completedQuestIds: new HashSet<string>(["source"], StringComparer.Ordinal)))["branch"];

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
    }

    [Fact]
    public void FailedOnlyBranch_IsNormalFuturePotentialBeforeOutcome()
    {
        var source = Quest("source");
        var branch = Quest("branch", requirements: [FailedRequirement("source")]);

        var result = Evaluate([source, branch], Profile())["branch"];

        Assert.Equal(QuestFutureReachabilityState.Potential, result.State);
        Assert.True(result.IncludeFutureRequirements);
    }

    [Fact]
    public void ExplicitFailedSource_UnlocksFailedOnlyFutureBranch()
    {
        var source = Quest("source", unsupportedFailure: ["shoot"]);
        var branch = Quest("branch", requirements: [FailedRequirement("source")]);
        var profile = Profile(failedQuestIds: new HashSet<string>(["source"], StringComparer.Ordinal));

        var result = Evaluate([source, branch], profile);

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result["source"].State);
        Assert.Equal(QuestFutureReachabilityState.Potential, result["branch"].State);
    }

    [Fact]
    public void CompleteOrFailedPrerequisite_IsNormalPotentialBeforeOutcome()
    {
        var source = Quest("source");
        var branch = Quest(
            "branch",
            requirements:
            [new QuestTaskRequirement(
                "source",
                new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Complete, QuestRequiredStatus.Failed]))]);

        var result = Evaluate([source, branch], Profile())["branch"];

        Assert.Equal(QuestFutureReachabilityState.Potential, result.State);
    }

    [Fact]
    public void CompletedSibling_AutoFailsMutuallyExclusiveSource_AndKeepsRecoveryPotential()
    {
        var chosen = Quest("chosen");
        var source = Quest(
            "source",
            completionFailureConditions: [new QuestCompletionFailureCondition("chosen")]);
        var recovery = Quest("recovery", requirements: [FailedRequirement("source")]);
        var profile = Profile(
            completedQuestIds: new HashSet<string>(["chosen"], StringComparer.Ordinal));

        var result = Evaluate([chosen, source, recovery], profile);

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result["source"].State);
        Assert.Equal(QuestFutureReachabilityState.Potential, result["recovery"].State);
    }

    [Fact]
    public void UnsupportedAvailabilityCondition_RemainsIndeterminatePotential()
    {
        var result = Evaluate([Quest("q", unsupportedAvailability: ["dialogue"])], Profile())["q"];
        Assert.Equal(QuestFutureReachabilityState.IndeterminatePotential, result.State);
    }

    [Fact]
    public void PermanentlyUnavailablePrerequisite_ClosesDependentQuest()
    {
        var bearOnly = Quest("bear", requiredFaction: PmcFaction.Bear);
        var dependent = Quest(
            "dependent",
            requirements:
            [new QuestTaskRequirement(
                "bear",
                new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Complete]))]);

        var result = Evaluate([bearOnly, dependent], Profile())["dependent"];
        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
    }

    private static QuestTaskRequirement FailedRequirement(string questId) =>
        new(questId, new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Failed]));

    private static IReadOnlyDictionary<string, QuestFutureReachabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile) =>
        QuestFutureReachabilityEvaluator.Evaluate(quests, profile);

    private static QuestDefinition Quest(
        string id,
        int minimumLevel = 1,
        PmcFaction? requiredFaction = null,
        IReadOnlyList<QuestTaskRequirement>? requirements = null,
        IReadOnlyList<string>? unsupportedAvailability = null,
        IReadOnlyList<QuestCompletionFailureCondition>? completionFailureConditions = null,
        IReadOnlyList<string>? unsupportedFailure = null) =>
        new(
            id, id, id, null, null, null, null,
            false, false, false,
            minimumLevel, requiredFaction, null,
            requirements ?? [], [], [],
            unsupportedAvailability,
            completionFailureConditions,
            false,
            unsupportedFailure);

    private static GameProfileSnapshot Profile(
        int level = 1,
        PmcFaction faction = PmcFaction.Usec,
        string? editionId = null,
        IReadOnlySet<string>? completedQuestIds = null,
        IReadOnlySet<string>? failedQuestIds = null) =>
        new()
        {
            ProfileId = "test",
            GameMode = GameMode.Regular,
            Level = level,
            Faction = faction,
            EditionId = editionId,
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
            FailedQuestIds = failedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
        };
}
