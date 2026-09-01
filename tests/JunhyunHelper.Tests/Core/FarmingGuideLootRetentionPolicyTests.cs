using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideLootRetentionPolicyTests
{
    [Fact]
    public void FirNeededItemMayReplaceMultipleOrdinaryVictims()
    {
        var preserved = Metrics(needed: 1, fleaValue: 10_000, slots: 2);
        var victims = new[]
        {
            Metrics(needed: 0, fleaValue: 100_000, slots: 1),
            Metrics(needed: 0, fleaValue: 100_000, slots: 1),
        };

        Assert.True(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void FirNeededVictimIsNeverAutomaticallySacrificed()
    {
        var preserved = Metrics(needed: 1, fleaValue: 500_000, slots: 4);
        var victims = new[] { Metrics(needed: 1, fleaValue: 1_000, slots: 1) };

        Assert.False(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void OrdinaryItemMayReplaceSeveralCheaperVictimsByAggregateFleaValue()
    {
        var preserved = Metrics(needed: 0, fleaValue: 100_000, slots: 4);
        var victims = new[]
        {
            Metrics(needed: 0, fleaValue: 20_000, slots: 1),
            Metrics(needed: 0, fleaValue: 30_000, slots: 1),
        };

        Assert.True(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void EqualOrMoreValuableVictimSetIsPreserved()
    {
        var preserved = Metrics(needed: 0, fleaValue: 50_000, slots: 2);
        var victims = new[]
        {
            Metrics(needed: 0, fleaValue: 20_000, slots: 1),
            Metrics(needed: 0, fleaValue: 30_000, slots: 1),
        };

        Assert.False(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, victims));
    }

    [Fact]
    public void StackQuantityContributesItsWholeFleaValue()
    {
        var preserved = Metrics(needed: 0, fleaValue: 60_000, slots: 1);
        var victimStack = Metrics(needed: 0, fleaValue: 4_000, slots: 1) with { Quantity = 20 };

        Assert.False(FarmingGuideLootRetentionPolicy.CanSacrificeFor(preserved, [victimStack]));
    }

    [Fact]
    public void TwoCheapVictimsArePreferredOverOneMoreExpensiveVictim()
    {
        var twoCheap = new[]
        {
            Metrics(needed: 0, fleaValue: 10_000, slots: 1),
            Metrics(needed: 0, fleaValue: 15_000, slots: 1),
        };
        var oneExpensive = new[] { Metrics(needed: 0, fleaValue: 50_000, slots: 1) };

        Assert.True(FarmingGuideLootRetentionPolicy.IsPreferredVictimSet(twoCheap, oneExpensive));
        Assert.False(FarmingGuideLootRetentionPolicy.IsPreferredVictimSet(oneExpensive, twoCheap));
    }

    [Fact]
    public void AnyFirNeededVictimMakesAPlanWorseThanOrdinaryVictims()
    {
        var ordinary = new[] { Metrics(needed: 0, fleaValue: 100_000, slots: 1) };
        var required = new[] { Metrics(needed: 1, fleaValue: 1, slots: 1) };

        Assert.True(FarmingGuideLootRetentionPolicy.IsPreferredVictimSet(ordinary, required));
    }

    private static FarmingGuideLootMetrics Metrics(int needed, int fleaValue, int slots) =>
        new(needed, TraderSellPrice: 999_999, FleaAveragePrice: fleaValue, slots);
}
