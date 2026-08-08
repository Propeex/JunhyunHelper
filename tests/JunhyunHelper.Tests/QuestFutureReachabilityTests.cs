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
        var quest = Quest("q", minimumLevel: 40);
        var result = Evaluate([quest], Profile(level: 10))["q"];

        Assert.Equal(QuestFutureReachabilityState.Potential, result.State);
        Assert.True(result.IncludeFutureRequirements);
    }

    [Fact]
    public void FactionMismatch_IsUnavailable()
    {
        var quest = Quest("q", requiredFaction: PmcFaction.Bear);
        var result = Evaluate([quest], Profile(faction: PmcFaction.Usec))["q"];

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
        Assert.False(result.IncludeFutureRequirements);
    }

    [Fact]
    public void EditionExclusion_IsUnavailable()
    {
        var quest = Quest("q");
        var edition = new EditionDefinition(
            "standard",
            "Standard",
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(["q"], StringComparer.Ordinal));

        var result = QuestFutureReachabilityEvaluator.Evaluate(
            [quest],
            Profile(editionId: "standard"),
            [edition])["q"];

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
        var branch = Quest(
            "branch",
            requirements:
            [
                new QuestTaskRequirement(
                    "source",
                    new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Failed])),
            ]);

        var result = Evaluate(
            [source, branch],
            Profile(completedQuestIds: new HashSet<string>(["source"], StringComparer.Ordinal)))["branch"];

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
        Assert.Contains(result.Reasons, reason =>
            reason.Kind == QuestFutureReachabilityReasonKind.PrerequisiteUnavailable &&
            reason.ReferenceId == "source");
    }

    [Fact]
    public void UntrackedFailedPrerequisite_RemainsIndeterminatePotential()
    {
        var source = Quest("source");
        var branch = Quest(
            "branch",
            requirements:
            [
                new QuestTaskRequirement(
                    "source",
                    new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Failed])),
            ]);

        var result = Evaluate([source, branch], Profile())["branch"];

        Assert.Equal(QuestFutureReachabilityState.IndeterminatePotential, result.State);
        Assert.True(result.IncludeFutureRequirements);
    }

    [Fact]
    public void UnsupportedCondition_RemainsIndeterminatePotential()
    {
        var quest = Quest("q", unsupported: ["dialogue"]);
        var result = Evaluate([quest], Profile())["q"];

        Assert.Equal(QuestFutureReachabilityState.IndeterminatePotential, result.State);
        Assert.True(result.IncludeFutureRequirements);
    }

    [Fact]
    public void PermanentlyUnavailablePrerequisite_ClosesDependentQuest()
    {
        var bearOnly = Quest("bear", requiredFaction: PmcFaction.Bear);
        var dependent = Quest(
            "dependent",
            requirements:
            [
                new QuestTaskRequirement(
                    "bear",
                    new HashSet<QuestRequiredStatus>([QuestRequiredStatus.Complete])),
            ]);

        var result = Evaluate([bearOnly, dependent], Profile(faction: PmcFaction.Usec))["dependent"];

        Assert.Equal(QuestFutureReachabilityState.Unavailable, result.State);
    }

    private static IReadOnlyDictionary<string, QuestFutureReachabilityResult> Evaluate(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile) =>
        QuestFutureReachabilityEvaluator.Evaluate(quests, profile);

    private static QuestDefinition Quest(
        string id,
        int minimumLevel = 1,
        PmcFaction? requiredFaction = null,
        IReadOnlyList<QuestTaskRequirement>? requirements = null,
        IReadOnlyList<string>? unsupported = null) =>
        new(
            id,
            id,
            id,
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            minimumLevel,
            requiredFaction,
            null,
            requirements ?? Array.Empty<QuestTaskRequirement>(),
            Array.Empty<QuestTraderStandingRequirement>(),
            Array.Empty<QuestTraderLoyaltyRequirement>(),
            unsupported);

    private static GameProfileSnapshot Profile(
        int level = 1,
        PmcFaction faction = PmcFaction.Usec,
        string? editionId = null,
        IReadOnlySet<string>? completedQuestIds = null) =>
        new()
        {
            ProfileId = "test",
            GameMode = GameMode.Regular,
            Level = level,
            Faction = faction,
            EditionId = editionId,
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
        };
}
