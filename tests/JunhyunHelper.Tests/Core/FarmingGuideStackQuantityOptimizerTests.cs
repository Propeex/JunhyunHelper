using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideStackQuantityOptimizerTests
{
    [Fact]
    public void Optimize_KeepsNeededFirUnitsBeforeMoreValuableOrdinaryUnits()
    {
        var variables = new[]
        {
            new FarmingGuideStackQuantityVariable(
                "needed",
                "quest-ammo",
                MinimumQuantity: 1,
                MaximumQuantity: 3,
                RaidAcquired: true,
                UnitWeightKg: 1m,
                UnitEconomicValue: 1),
            new FarmingGuideStackQuantityVariable(
                "valuable",
                "valuable-ammo",
                MinimumQuantity: 1,
                MaximumQuantity: 3,
                RaidAcquired: false,
                UnitWeightKg: 1m,
                UnitEconomicValue: 100_000),
        };

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            variables,
            fixedWeightKg: 0m,
            maximumWeightKg: 4m,
            fixedRaidAcquiredUnits: new Dictionary<string, int>(),
            remainingFirNeed: id => id == "quest-ammo" ? 3 : 0);

        Assert.True(result.Found);
        Assert.Equal(3, result.Quantities["needed"]);
        Assert.Equal(1, result.Quantities["valuable"]);
        Assert.Equal(3, result.SatisfiedFirUnits);
    }

    [Fact]
    public void Optimize_MaximizesValueAfterFirTie()
    {
        var variables = new[]
        {
            new FarmingGuideStackQuantityVariable(
                "cheap",
                "cheap",
                MinimumQuantity: 1,
                MaximumQuantity: 5,
                RaidAcquired: false,
                UnitWeightKg: 1m,
                UnitEconomicValue: 10),
            new FarmingGuideStackQuantityVariable(
                "expensive",
                "expensive",
                MinimumQuantity: 1,
                MaximumQuantity: 5,
                RaidAcquired: false,
                UnitWeightKg: 1m,
                UnitEconomicValue: 100),
        };

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            variables,
            fixedWeightKg: 0m,
            maximumWeightKg: 6m,
            fixedRaidAcquiredUnits: new Dictionary<string, int>(),
            remainingFirNeed: _ => 0);

        Assert.True(result.Found);
        Assert.Equal(1, result.Quantities["cheap"]);
        Assert.Equal(5, result.Quantities["expensive"]);
    }

    [Fact]
    public void Optimize_CanRetainOnlyPartOfOneStackUnderWeightLimit()
    {
        var variables = new[]
        {
            new FarmingGuideStackQuantityVariable(
                "ammo",
                "ammo",
                MinimumQuantity: 1,
                MaximumQuantity: 60,
                RaidAcquired: false,
                UnitWeightKg: 0.1m,
                UnitEconomicValue: 1_000),
        };

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            variables,
            fixedWeightKg: 5m,
            maximumWeightKg: 7.5m,
            fixedRaidAcquiredUnits: new Dictionary<string, int>(),
            remainingFirNeed: _ => 0);

        Assert.True(result.Found);
        Assert.Equal(25, result.Quantities["ammo"]);
        Assert.Equal(7.5m, result.TotalWeightKg);
    }

    [Fact]
    public void Optimize_UsesAlreadyRetainedFirUnitsWhenCappingNeed()
    {
        var variables = new[]
        {
            new FarmingGuideStackQuantityVariable(
                "ammo",
                "quest-ammo",
                MinimumQuantity: 1,
                MaximumQuantity: 10,
                RaidAcquired: true,
                UnitWeightKg: 1m,
                UnitEconomicValue: 0),
        };
        var fixedFir = new Dictionary<string, int>
        {
            ["quest-ammo"] = 2,
        };

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            variables,
            fixedWeightKg: 0m,
            maximumWeightKg: 4m,
            fixedRaidAcquiredUnits: fixedFir,
            remainingFirNeed: id => id == "quest-ammo" ? 4 : 0);

        Assert.True(result.Found);
        Assert.Equal(2, result.Quantities["ammo"]);
        Assert.Equal(4, result.SatisfiedFirUnits);
    }

    [Fact]
    public void Optimize_ReportsBudgetExceededInsteadOfApproximatingHugeWeightStateSpace()
    {
        var variables = new[]
        {
            new FarmingGuideStackQuantityVariable(
                "currency",
                "currency",
                MinimumQuantity: 1,
                MaximumQuantity: 100_000,
                RaidAcquired: false,
                UnitWeightKg: 0.000001m,
                UnitEconomicValue: 1),
        };

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            variables,
            fixedWeightKg: 0m,
            maximumWeightKg: 100m,
            fixedRaidAcquiredUnits: new Dictionary<string, int>(),
            remainingFirNeed: _ => 0,
            maxCapacityStates: 100);

        Assert.Equal(FarmingGuideStackQuantityOptimizationStatus.BudgetExceeded, result.Status);
        Assert.False(result.ProofComplete);
    }
}
