using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuidePlacementEngineTests
{
    [Fact]
    public void RotationSwapsActualItemFootprint()
    {
        Assert.Equal((1, 2), FarmingGuidePlacementEngine.Footprint(1, 2, rotated: false));
        Assert.Equal((2, 1), FarmingGuidePlacementEngine.Footprint(1, 2, rotated: true));
    }

    [Fact]
    public void RejectsOverlapAndOutOfBounds()
    {
        var occupied = new[] { new FarmingGuideGridPlacement("a", 1, 1, 2, 2) };

        Assert.False(FarmingGuidePlacementEngine.CanPlace(5, 5, 2, 2, 2, 2, false, occupied));
        Assert.False(FarmingGuidePlacementEngine.CanPlace(5, 5, 4, 4, 2, 2, false, occupied));
        Assert.True(FarmingGuidePlacementEngine.CanPlace(5, 5, 3, 0, 2, 2, false, occupied));
    }

    [Fact]
    public void MovingItemMayReuseItsOwnCells()
    {
        var occupied = new[] { new FarmingGuideGridPlacement("moving", 1, 1, 2, 2) };

        Assert.True(FarmingGuidePlacementEngine.CanPlace(
            5, 5, 1, 1, 2, 2, false, occupied, ignoredInstanceId: "moving"));
    }

    [Fact]
    public void FindsFirstContiguousFitInsteadOfOnlyCountingFreeCells()
    {
        var occupied = new[]
        {
            new FarmingGuideGridPlacement("left", 0, 0, 1, 2),
            new FarmingGuideGridPlacement("middle", 2, 0, 1, 2),
        };

        Assert.Null(FarmingGuidePlacementEngine.FindFirstFit(3, 2, 1, 2, false, occupied));
        Assert.Equal((1, 0), FarmingGuidePlacementEngine.FindFirstFit(3, 2, 1, 1, false, occupied));
    }
}
