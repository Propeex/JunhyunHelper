using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideGlobalPackingPlannerTests
{
    [Fact]
    public void RebuildsAllUnlockedPlacementsFromScratch()
    {
        var surfaces = new[]
        {
            Surface("root", width: 3, height: 1),
        };
        var items = new[]
        {
            Item("a", "root", currentX: 0),
            Item("b", "root", currentX: 1),
            Item("incoming", "root", currentX: null),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(surfaces, items);

        Assert.True(result.Found);
        Assert.Equal(3, result.Placements.Count);
        Assert.Equal(3, result.Placements.Select(value => value.X).Distinct().Count());
    }

    [Fact]
    public void SelectedContainerMayContributeItsOwnedSurface()
    {
        var surfaces = new[]
        {
            Surface("root", width: 1, height: 1),
            Surface("bag-grid", width: 1, height: 1, owner: "bag"),
        };
        var items = new[]
        {
            Item("bag", "root", currentX: null),
            new FarmingGuideGlobalPackingItem(
                "loot",
                Fixed: false,
                CurrentPlacement: null,
                [new FarmingGuideGlobalPackingOption("bag-grid", 1, 1, false, 0)]),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(surfaces, items);

        Assert.True(result.Found);
        Assert.Equal("bag-grid", result.Placements.Single(value => value.InstanceId == "loot").SurfaceId);
    }

    [Fact]
    public void RejectsContainerOwnershipCycle()
    {
        var surfaces = new[]
        {
            Surface("a-grid", 1, 1, owner: "a"),
            Surface("b-grid", 1, 1, owner: "b"),
        };
        var items = new[]
        {
            new FarmingGuideGlobalPackingItem(
                "a", false, null,
                [new FarmingGuideGlobalPackingOption("b-grid", 1, 1, false, 0)]),
            new FarmingGuideGlobalPackingItem(
                "b", false, null,
                [new FarmingGuideGlobalPackingOption("a-grid", 1, 1, false, 0)]),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(surfaces, items);

        Assert.Equal(FarmingGuideGlobalPackingStatus.NoSolution, result.Status);
    }

    [Fact]
    public void FixedItemKeepsExactPlacement()
    {
        var surfaces = new[] { Surface("root", 2, 1) };
        var fixedPlacement = new FarmingGuideGlobalPackingPlacement("fixed", "root", 1, 0, 1, 1, false);
        var items = new[]
        {
            new FarmingGuideGlobalPackingItem(
                "fixed",
                Fixed: true,
                fixedPlacement,
                [new FarmingGuideGlobalPackingOption("root", 1, 1, false, 0)]),
            Item("other", "root", currentX: null),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(surfaces, items);

        Assert.True(result.Found);
        Assert.Equal(fixedPlacement, result.Placements.Single(value => value.InstanceId == "fixed"));
    }

    [Fact]
    public void FinalValidatorCanRejectFirstGeometricArrangementAndContinueSearch()
    {
        var surfaces = new[] { Surface("root", 2, 1) };
        var items = new[]
        {
            Item("a", "root", currentX: 0),
            Item("b", "root", currentX: 1),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(
            surfaces,
            items,
            placements => placements["a"].X == 1);

        Assert.True(result.Found);
        Assert.Equal(1, result.Placements.Single(value => value.InstanceId == "a").X);
    }

    [Fact]
    public void ReportsBudgetExceededInsteadOfClaimingNoSolution()
    {
        var surfaces = new[] { Surface("root", 4, 1) };
        var items = new[]
        {
            Item("a", "root", null),
            Item("b", "root", null),
            Item("c", "root", null),
            Item("d", "root", null),
        };

        var result = FarmingGuideGlobalPackingPlanner.TryPlan(
            surfaces,
            items,
            finalValidator: _ => false,
            maxSearchNodes: 2);

        Assert.Equal(FarmingGuideGlobalPackingStatus.BudgetExceeded, result.Status);
        Assert.False(result.ProofComplete);
    }

    private static FarmingGuideGlobalPackingSurface Surface(
        string id,
        int width,
        int height,
        string? owner = null) =>
        new(id, owner, width, height, 0, []);

    private static FarmingGuideGlobalPackingItem Item(
        string id,
        string surface,
        int? currentX) =>
        new(
            id,
            Fixed: false,
            CurrentPlacement: currentX is null
                ? null
                : new FarmingGuideGlobalPackingPlacement(id, surface, currentX.Value, 0, 1, 1, false),
            [new FarmingGuideGlobalPackingOption(surface, 1, 1, false, 0)]);
}
