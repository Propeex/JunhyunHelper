using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class TraderProgressPartialFactsTests
{
    [Fact]
    public void KnownLoyaltyWithUnknownStandingOnlyBlocksStandingDecision()
    {
        var loyaltyQuest = Quest(
            "loyalty",
            loyalty: [new QuestTraderLoyaltyRequirement("trader", 3)]);
        var standingQuest = Quest(
            "standing",
            standing:
            [
                new QuestTraderStandingRequirement(
                    "trader",
                    1m,
                    StandingRequirementOperator.AtLeast),
            ]);
        var profile = Profile(new TraderProgress(3, null));

        var results = QuestAvailabilityEvaluator.Evaluate([loyaltyQuest, standingQuest], profile);

        Assert.Equal(QuestAvailabilityState.Current, results["loyalty"].State);
        Assert.Equal(QuestAvailabilityState.Indeterminate, results["standing"].State);
    }

    [Fact]
    public void KnownStandingWithUnknownLoyaltyOnlyBlocksLoyaltyDecision()
    {
        var loyaltyQuest = Quest(
            "loyalty",
            loyalty: [new QuestTraderLoyaltyRequirement("trader", 2)]);
        var standingQuest = Quest(
            "standing",
            standing:
            [
                new QuestTraderStandingRequirement(
                    "trader",
                    1m,
                    StandingRequirementOperator.AtLeast),
            ]);
        var profile = Profile(new TraderProgress(null, 1.5m));

        var results = QuestAvailabilityEvaluator.Evaluate([loyaltyQuest, standingQuest], profile);

        Assert.Equal(QuestAvailabilityState.Indeterminate, results["loyalty"].State);
        Assert.Equal(QuestAvailabilityState.Current, results["standing"].State);
    }

    private static QuestDefinition Quest(
        string id,
        IReadOnlyList<QuestTraderStandingRequirement>? standing = null,
        IReadOnlyList<QuestTraderLoyaltyRequirement>? loyalty = null) =>
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
            Disabled: false,
            MinimumPlayerLevel: 0,
            RequiredFaction: null,
            RequiredPrestigeLevel: null,
            TaskRequirements: [],
            TraderStandingRequirements: standing ?? [],
            TraderLoyaltyRequirements: loyalty ?? [],
            CompletionFailureConditionData: null,
            Restartable: false,
            UnsupportedFailureConditionTypes: null);

    private static GameProfileSnapshot Profile(TraderProgress traderProgress) => new()
    {
        ProfileId = "profile",
        GameMode = GameMode.Regular,
        Level = 1,
        Faction = PmcFaction.Usec,
        Traders = new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
        {
            ["trader"] = traderProgress,
        },
    };
}
