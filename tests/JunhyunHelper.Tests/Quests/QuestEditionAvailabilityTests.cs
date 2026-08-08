using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestEditionAvailabilityTests
{
    private static readonly EditionDefinition Standard = new(
        "standard",
        "Standard",
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal));

    private static readonly EditionDefinition Eod = new(
        "edge_of_darkness",
        "Edge of Darkness",
        new HashSet<string>(StringComparer.Ordinal) { "eod-only" },
        new HashSet<string>(StringComparer.Ordinal));

    private static readonly EditionDefinition Unheard = new(
        "unheard",
        "The Unheard",
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal) { "old-patterns" });

    [Fact]
    public void ExclusiveQuestIsLockedForOtherEdition()
    {
        var result = Evaluate(Quest("eod-only"), Profile("standard"));

        Assert.Equal(QuestAvailabilityState.Locked, result.State);
        Assert.Contains(result.Reasons, reason => reason.Kind == QuestAvailabilityReasonKind.Edition);
    }

    [Fact]
    public void ExclusiveQuestIsCurrentForOwningEdition()
    {
        var result = Evaluate(Quest("eod-only"), Profile("edge_of_darkness"));

        Assert.Equal(QuestAvailabilityState.Current, result.State);
    }

    [Fact]
    public void ExcludedQuestIsLockedForExcludedEdition()
    {
        var result = Evaluate(Quest("old-patterns"), Profile("unheard"));

        Assert.Equal(QuestAvailabilityState.Locked, result.State);
        Assert.Contains(result.Reasons, reason => reason.Kind == QuestAvailabilityReasonKind.Edition);
    }

    [Fact]
    public void ExcludedQuestRemainsCurrentForOtherEdition()
    {
        var result = Evaluate(Quest("old-patterns"), Profile("standard"));

        Assert.Equal(QuestAvailabilityState.Current, result.State);
    }

    [Fact]
    public void EditionSensitiveQuestWithoutEditionInputIsIndeterminate()
    {
        var result = Evaluate(Quest("eod-only"), Profile(null));

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.State);
        Assert.Contains(result.Reasons, reason =>
            reason.Kind == QuestAvailabilityReasonKind.MissingProfileValue &&
            reason.ReferenceId == "edition");
    }

    [Fact]
    public void EditionSensitiveQuestWithUnknownEditionIsIndeterminate()
    {
        var result = Evaluate(Quest("eod-only"), Profile("unknown-edition"));

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.State);
    }

    [Fact]
    public void NormalQuestDoesNotRequireEditionInput()
    {
        var result = Evaluate(Quest("normal"), Profile(null));

        Assert.Equal(QuestAvailabilityState.Current, result.State);
    }

    private static QuestAvailabilityResult Evaluate(
        QuestDefinition quest,
        GameProfileSnapshot profile) =>
        QuestAvailabilityEvaluator.Evaluate(
            [quest],
            profile,
            [Standard, Eod, Unheard])[quest.Id];

    private static QuestDefinition Quest(string id) =>
        new(
            id,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            0,
            null,
            null,
            [],
            [],
            []);

    private static GameProfileSnapshot Profile(string? editionId) =>
        new()
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            EditionId = editionId,
        };
}
