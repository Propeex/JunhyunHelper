using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Tests.Items;

public sealed class NeededItemCalculatorTests
{
    private static readonly ItemRequirementSource QuestSource =
        new(ItemRequirementSourceKind.Quest, "quest-a", "objective-a");

    [Fact]
    public void FirSubsetAloneDoesNotSatisfyMixedRequirement()
    {
        var result = CalculateSingle(requiredTotal: 15, requiredFir: 5, ownedFir: 5, ownedNonFir: 0);

        Assert.False(result.IsFulfilled);
        Assert.Equal(10, result.RemainingTotal);
        Assert.Equal(0, result.RemainingFir);
    }

    [Fact]
    public void ExactFirAndNonFirInventorySatisfiesMixedRequirement()
    {
        var result = CalculateSingle(requiredTotal: 15, requiredFir: 5, ownedFir: 5, ownedNonFir: 10);

        Assert.True(result.IsFulfilled);
        Assert.Equal(0, result.RemainingTotal);
        Assert.Equal(0, result.RemainingFir);
    }

    [Fact]
    public void SurplusFirCanSatisfyUnrestrictedRemainder()
    {
        var result = CalculateSingle(requiredTotal: 15, requiredFir: 5, ownedFir: 15, ownedNonFir: 0);

        Assert.True(result.IsFulfilled);
        Assert.Equal(0, result.RemainingTotal);
    }

    [Fact]
    public void TotalQuantityCannotReplaceMissingFir()
    {
        var result = CalculateSingle(requiredTotal: 15, requiredFir: 5, ownedFir: 4, ownedNonFir: 11);

        Assert.False(result.IsFulfilled);
        Assert.Equal(1, result.RemainingTotal);
        Assert.Equal(1, result.RemainingFir);
    }

    [Fact]
    public void QuestAndHideoutRequirementsAreAggregatedWithoutLosingSources()
    {
        var requirements = new[]
        {
            new ItemRequirement(
                "item-a",
                RequiredTotal: 3,
                RequiredFir: 3,
                new ItemRequirementSource(ItemRequirementSourceKind.Quest, "quest-a", "objective-a")),
            new ItemRequirement(
                "item-a",
                RequiredTotal: 5,
                RequiredFir: 0,
                new ItemRequirementSource(ItemRequirementSourceKind.Hideout, "workbench", "level-2")),
        };

        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["item-a"] = new(Fir: 3, NonFir: 2),
        };

        var result = Assert.Single(NeededItemCalculator.Calculate(requirements, inventory));

        Assert.Equal(8, result.RequiredTotal);
        Assert.Equal(3, result.RequiredFir);
        Assert.Equal(3, result.RemainingTotal);
        Assert.Equal(2, result.Sources.Count);
        Assert.Contains(result.Sources, source => source.Kind == ItemRequirementSourceKind.Quest);
        Assert.Contains(result.Sources, source => source.Kind == ItemRequirementSourceKind.Hideout);
    }

    private static NeededItem CalculateSingle(
        int requiredTotal,
        int requiredFir,
        int ownedFir,
        int ownedNonFir)
    {
        var requirements = new[]
        {
            new ItemRequirement("item-a", requiredTotal, requiredFir, QuestSource),
        };

        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["item-a"] = new(ownedFir, ownedNonFir),
        };

        return Assert.Single(NeededItemCalculator.Calculate(requirements, inventory));
    }
}
