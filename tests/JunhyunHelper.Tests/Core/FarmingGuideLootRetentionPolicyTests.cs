using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideLootRetentionPolicyTests
{
    [Fact]
    public void NeededItemMayReplaceMultipleNonNeededVictims()
    {
        var preserved = Metrics(needed: 1, value: 10_000, slots: 2);
        var victims = new[]
        {
            Metrics(needed: 0, value: 100_000, slots: 1),
            Metrics(needed: 0, value: 100_000, slots: 1),
        };

        Assert.True(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void RequiredVictimIsNeverAutomaticallySacrificed()
    {
        var preserved = Metrics(needed: 1, value: 500_000, slots: 4);
        var victims = new[] { Metrics(needed: 1, value: 1_000, slots: 1) };

        Assert.False(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void NonNeededItemMayReplaceSeveralCheaperVictimsByAggregateValue()
    {
        var preserved = Metrics(needed: 0, value: 100_000, slots: 4);
        var victims = new[]
        {
            Metrics(needed: 0, value: 20_000, slots: 1),
            Metrics(needed: 0, value: 30_000, slots: 1),
        };

        Assert.True(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void EqualOrMoreValuableVictimSetIsPreserved()
    {
        var preserved = Metrics(needed: 0, value: 50_000, slots: 2);
        var victims = new[]
        {
            Metrics(needed: 0, value: 20_000, slots: 1),
            Metrics(needed: 0, value: 30_000, slots: 1),
        };

        Assert.False(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    private static FarmingGuideLootMetrics Metrics(int needed, int value, int slots) =>
        new(needed, value, null, slots);
}
