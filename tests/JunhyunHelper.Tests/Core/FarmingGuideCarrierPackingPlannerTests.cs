using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideCarrierPackingPlannerTests
{
    [Fact]
    public void TryPack_RearrangesFragmentedContentsWithoutDroppingAnyItem()
    {
        var surfaces = new[]
        {
            new FarmingGuideCarrierPackingSurface("grid0", 2, 3, 0, []),
        };
        var oneByTwo = Item("wide", "grid0", 0, 0, width: 1, height: 2);
        var oneByOne = Item("small", "grid0", 1, 1, width: 1, height: 1);

        var plan = FarmingGuideCarrierPackingPlanner.TryPack(surfaces, [oneByTwo, oneByOne]);

        Assert.NotNull(plan);
        Assert.Equal(2, plan.Placements.Count);
        Assert.Equal(new[] { "small", "wide" }, plan.Placements.Select(value => value.InstanceId).Order().ToArray());
        Assert.True(plan.SearchNodes > 0);
    }

    [Fact]
    public void TryPack_KeepsLockedItemAtExactAddress()
    {
        var surfaces = new[]
        {
            new FarmingGuideCarrierPackingSurface("grid0", 2, 2, 0, []),
        };
        var locked = Item("locked", "grid0", 1, 1, width: 1, height: 1, fixedItem: true);
        var movable = Item("movable", "grid0", 1, 1, width: 1, height: 1);

        var plan = FarmingGuideCarrierPackingPlanner.TryPack(surfaces, [locked, movable]);

        Assert.NotNull(plan);
        var fixedPlacement = Assert.Single(plan.Placements.Where(value => value.InstanceId == "locked"));
        Assert.Equal((1, 1), (fixedPlacement.X, fixedPlacement.Y));
        var moved = Assert.Single(plan.Placements.Where(value => value.InstanceId == "movable"));
        Assert.NotEqual((1, 1), (moved.X, moved.Y));
    }

    [Fact]
    public void TryPack_FailsWhenLockedAddressConflictsWithReservedCell()
    {
        var surfaces = new[]
        {
            new FarmingGuideCarrierPackingSurface(
                "grid0",
                2,
                2,
                0,
                [new FarmingGuideGridPlacement("reserved", 1, 1, 1, 1)]),
        };
        var locked = Item("locked", "grid0", 1, 1, width: 1, height: 1, fixedItem: true);

        Assert.Null(FarmingGuideCarrierPackingPlanner.TryPack(surfaces, [locked]));
    }

    [Fact]
    public void TryPack_UsesAlternateGridWhenPreferredGridCannotFit()
    {
        var surfaces = new[]
        {
            new FarmingGuideCarrierPackingSurface("grid0", 1, 1, 0, []),
            new FarmingGuideCarrierPackingSurface("grid1", 2, 2, 1, []),
        };
        var item = new FarmingGuideCarrierPackingItem(
            "item",
            "grid0",
            0,
            0,
            false,
            Fixed: false,
            [new FarmingGuideCarrierPackingOption("grid1", 2, 1, false, 1)]);

        var plan = FarmingGuideCarrierPackingPlanner.TryPack(surfaces, [item]);

        Assert.NotNull(plan);
        Assert.Equal("grid1", Assert.Single(plan.Placements).SurfaceId);
        Assert.Equal(1, plan.MovedCount);
    }

    private static FarmingGuideCarrierPackingItem Item(
        string id,
        string surface,
        int x,
        int y,
        int width,
        int height,
        bool fixedItem = false) =>
        new(
            id,
            surface,
            x,
            y,
            false,
            fixedItem,
            [
                new FarmingGuideCarrierPackingOption(surface, width, height, false, 0),
                new FarmingGuideCarrierPackingOption(surface, height, width, true, 1),
            ]);
}
