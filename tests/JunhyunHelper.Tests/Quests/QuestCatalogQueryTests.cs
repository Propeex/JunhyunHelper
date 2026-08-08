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
            [current, locked, unknown, completed],
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

        var result = Assert.Single(QuestCatalogQuery.Evaluate([quest], profile));

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.Availability.State);
    }

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
