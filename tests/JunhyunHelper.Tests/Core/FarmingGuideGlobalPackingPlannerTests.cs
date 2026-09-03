using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideGlobalPackingPlannerTests
{
    [Fact]
    public void Plan_RebuildsWholeSelectedSetFromScratch()
    {
        var surface = Surface("root", parent: null, width: 2, height: 2);
        var items = new[]
        {
            Item("wide", Option("root", 2, 1)),
            Item("left", Option("root", 1, 1)),
            Item("right", Option("root", 1, 1)),
        };

        var result = FarmingGuideGlobalPackingPlanner.Plan([surface], items);

        Assert.True(result.Found);
        Assert.Equal(3, result.Placements.Count);
    }

    [Fact]
    public void Plan_AllowsUsingSelectedContainerSurfaceBeforeOwnerPlacement()
    {
        var root = Surface("root", parent: null, width: 2, height: 1);
        var containerSurface = Surface("bag:0", parent: "bag", width: 1, height: 1);
        var items = new[]
        {
            Item("bag", Option("root", 1, 1)),
            Item("loot", Option("bag:0", 1, 1)),
        };

        var result = FarmingGuideGlobalPackingPlanner.Plan([root, containerSurface], items);

        Assert.True(result.Found);
        Assert.Contains(result.Placements, value => value.InstanceId == "bag" && value.SurfaceId == "root");
        Assert.Contains(result.Placements, value => value.InstanceId == "loot" && value.SurfaceId == "bag:0");
    }

    [Fact]
    public void Plan_SurfaceOwnerDependencyCanDifferFromStorageParent()
    {
        var carrierGrid = Surface("backpack-grid", parent: null, width: 1, height: 1);
        var carrierSlot = Surface("backpack-slot", parent: null, width: 1, height: 1);
        var item = Item("backpack", Option("backpack-grid", 1, 1), Option("backpack-slot", 1, 1));
        var owners = new Dictionary<string, string?>
        {
            ["backpack-grid"] = "backpack",
            ["backpack-slot"] = null,
        };

        var result = FarmingGuideGlobalPackingPlanner.Plan(
            [carrierGrid, carrierSlot],
            [item],
            surfaceOwners: owners);

        Assert.True(result.Found);
        Assert.Contains(result.Placements, value =>
            value.InstanceId == "backpack" && value.SurfaceId == "backpack-slot");
    }

    [Fact]
    public void Plan_FinalValidatorRejectsFirstLeafAndSearchesAlternative()
    {
        var first = Surface("first", parent: null, width: 1, height: 1);
        var second = Surface("second", parent: null, width: 1, height: 1);
        var item = Item("loot", Option("first", 1, 1), Option("second", 1, 1));

        var result = FarmingGuideGlobalPackingPlanner.Plan(
            [first, second],
            [item],
            finalValidator: placements =>
                placements.Single().SurfaceId == "second");

        Assert.True(result.Found);
        Assert.Equal("second", Assert.Single(result.Placements).SurfaceId);
    }

    [Fact]
    public void Plan_RejectsMutualContainerCycle()
    {
        var aSurface = Surface("a:0", parent: "a", width: 1, height: 1);
        var bSurface = Surface("b:0", parent: "b", width: 1, height: 1);
        var items = new[]
        {
            Item("a", Option("b:0", 1, 1)),
            Item("b", Option("a:0", 1, 1)),
        };

        var result = FarmingGuideGlobalPackingPlanner.Plan([aSurface, bSurface], items);

        Assert.Equal(FarmingGuideGlobalPackingStatus.NoSolution, result.Status);
    }

    [Fact]
    public void Plan_RejectsCycleExpressedOnlyThroughSurfaceOwnerMap()
    {
        var aSurface = Surface("a-grid", parent: null, width: 1, height: 1);
        var bSurface = Surface("b-grid", parent: null, width: 1, height: 1);
        var owners = new Dictionary<string, string?>
        {
            ["a-grid"] = "a",
            ["b-grid"] = "b",
        };
        var items = new[]
        {
            Item("a", Option("b-grid", 1, 1)),
            Item("b", Option("a-grid", 1, 1)),
        };

        var result = FarmingGuideGlobalPackingPlanner.Plan(
            [aSurface, bSurface],
            items,
            surfaceOwners: owners);

        Assert.Equal(FarmingGuideGlobalPackingStatus.NoSolution, result.Status);
    }

    [Fact]
    public void Plan_ReportsBudgetExceededInsteadOfNoSolution()
    {
        var root = Surface("root", parent: null, width: 3, height: 3);
        var items = Enumerable.Range(0, 6)
            .Select(index => Item($"item-{index}", Option("root", 1, 1)))
            .ToArray();

        var result = FarmingGuideGlobalPackingPlanner.Plan([root], items, maxSearchNodes: 1);

        Assert.Equal(FarmingGuideGlobalPackingStatus.BudgetExceeded, result.Status);
        Assert.False(result.ProofComplete);
    }

    private static FarmingGuideRepackingSurface Surface(
        string id,
        string? parent,
        int width,
        int height) =>
        new(id, parent, width, height, 0, []);

    private static FarmingGuideGlobalPackingItem Item(
        string id,
        params FarmingGuideRepackingOption[] options) =>
        new(id, options);

    private static FarmingGuideRepackingOption Option(
        string surface,
        int width,
        int height) =>
        new(surface, width, height, false, 0);
}
