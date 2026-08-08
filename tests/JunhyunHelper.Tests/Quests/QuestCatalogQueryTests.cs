using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestCatalogQueryTests
{
    [Fact]
    public void CurrentReturnsOnlyDeterministicallyCurrentQuests()
    {
        var current = Quest("current", minimumLevel: 1);
        var locked = Quest("locked", minimumLevel: 10);
        var unknown = Quest("unknown", unsupported: ["dialogue"]);
        var completed = Quest("completed");
        var profile = new GameProfileSnapshot
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            CompletedQuestIds = new HashSet<string>(StringComparer.Ordinal) { completed.Id },
        };

        var result = QuestCatalogQuery.Current(
            Content([current, locked, unknown, completed]),
            profile);

        Assert.Equal("current", Assert.Single(result).Quest.Id);
    }

    [Fact]
    public void EvaluateKeepsIndeterminateVisibleAsItsOwnState()
    {
        var quest = Quest("unknown", unsupported: ["dialogue"]);
        var profile = new GameProfileSnapshot
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
        };

        var result = Assert.Single(QuestCatalogQuery.Evaluate(Content([quest]), profile));

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.Availability.State);
    }

    [Fact]
    public void CurrentCannotAccidentallyIgnoreEditionRules()
    {
        var exclusiveQuest = Quest("eod-only");
        var editions = new[]
        {
            new EditionDefinition(
                "standard",
                "Standard",
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal)),
            new EditionDefinition(
                "edge_of_darkness",
                "Edge of Darkness",
                new HashSet<string>(StringComparer.Ordinal) { exclusiveQuest.Id },
                new HashSet<string>(StringComparer.Ordinal)),
        };
        var standardProfile = new GameProfileSnapshot
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            EditionId = "standard",
        };

        var result = QuestCatalogQuery.Current(
            Content([exclusiveQuest], editions),
            standardProfile);

        Assert.Empty(result);
        var evaluated = Assert.Single(
            QuestCatalogQuery.Evaluate(Content([exclusiveQuest], editions), standardProfile));
        Assert.Equal(QuestAvailabilityState.Unavailable, evaluated.Availability.State);
        Assert.Contains(evaluated.Availability.Reasons, reason =>
            reason.Kind == QuestAvailabilityReasonKind.Edition);
    }

    private static GameContentCatalog Content(
        IReadOnlyList<QuestDefinition> quests,
        IReadOnlyList<EditionDefinition>? editions = null) =>
        new(
            Items: [],
            Traders: [],
            Maps: [],
            Quests: quests,
            QuestObjectives: [],
            QuestItemRequirements: [],
            HideoutStations: [],
            Ammo: [],
            EditionData: editions ?? []);

    private static QuestDefinition Quest(
        string id,
        int minimumLevel = 0,
        IReadOnlyList<string>? unsupported = null) =>
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
            minimumLevel,
            null,
            null,
            [],
            [],
            [],
            unsupported);
}
