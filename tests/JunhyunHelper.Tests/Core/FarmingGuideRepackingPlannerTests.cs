using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideRepackingPlannerTests
{
    [Fact]
    public void RepackingMovesOneSmallBlockerInsteadOfRejectingLargeIncomingItem()
    {
        var surface = Surface("bag", width: 3, height: 3);
        var blocker = Item(
            "small",
            "bag",
            x: 1,
            y: 0,
            width: 1,
            height: 1,
            movable: true,
            [Option("bag", 1, 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("bag", 2, 3)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([surface], [blocker], incoming);

        Assert.NotNull(plan);
        Assert.Equal(["small"], plan!.MovedInstanceIds);
        Assert.Equal("bag", plan.Incoming.SurfaceId);
        Assert.Equal(2, plan.Incoming.Width);
        Assert.Equal(3, plan.Incoming.Height);
        var moved = Assert.Single(plan.ExistingPlacements);
        Assert.Equal("small", moved.InstanceId);
        Assert.False(moved.X == 1 && moved.Y == 0);
    }

    [Fact]
    public void RepackingCanMoveMultipleBlockersWhenFragmentationRequiresIt()
    {
        var surface = Surface("bag", width: 3, height: 3);
        var first = Item(
            "first",
            "bag",
            x: 1,
            y: 0,
            width: 1,
            height: 1,
            movable: true,
            [Option("bag", 1, 1)]);
        var second = Item(
            "second",
            "bag",
            x: 1,
            y: 1,
            width: 1,
            height: 1,
            movable: true,
            [Option("bag", 1, 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("bag", 2, 3)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([surface], [first, second], incoming);

        Assert.NotNull(plan);
        Assert.Equal(2, plan!.MovedInstanceIds.Count);
        Assert.Contains("first", plan.MovedInstanceIds);
        Assert.Contains("second", plan.MovedInstanceIds);
    }

    [Fact]
    public void RepackingCanCascadeDisplacementAcrossStorageSurfaces()
    {
        var bag = Surface("bag", width: 3, height: 2, priority: 0);
        var pockets = Surface("pockets", width: 1, height: 2, priority: 1);
        var directBlocker = Item(
            "small",
            "bag",
            x: 1,
            y: 0,
            width: 1,
            height: 1,
            movable: true,
            [Option("bag", 1, 1)]);
        var columnBlocker = Item(
            "column",
            "bag",
            x: 2,
            y: 0,
            width: 1,
            height: 2,
            movable: true,
            [Option("bag", 1, 2), Option("pockets", 1, 2, preference: 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("bag", 2, 2)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([bag, pockets], [directBlocker, columnBlocker], incoming);

        Assert.NotNull(plan);
        Assert.Equal(2, plan!.MovedInstanceIds.Count);
        Assert.Contains("small", plan.MovedInstanceIds);
        Assert.Contains("column", plan.MovedInstanceIds);
        var column = plan.ExistingPlacements.Single(value => value.InstanceId == "column");
        Assert.Equal("pockets", column.SurfaceId);
    }

    [Fact]
    public void LockedBlockerRemainsHardObstacle()
    {
        var surface = Surface("bag", width: 3, height: 3);
        var locked = Item(
            "locked",
            "bag",
            x: 1,
            y: 0,
            width: 1,
            height: 1,
            movable: false,
            [Option("bag", 1, 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("bag", 2, 3)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([surface], [locked], incoming);

        Assert.Null(plan);
    }

    [Fact]
    public void ReservedCellsRemainHardObstacles()
    {
        var surface = Surface(
            "bag",
            width: 2,
            height: 2,
            fixedObstacles: [new FarmingGuideGridPlacement("reserved", 0, 0, 1, 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("bag", 2, 2)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([surface], [], incoming);

        Assert.Null(plan);
    }

    [Fact]
    public void FinalParentGraphRejectsContainerCycles()
    {
        var root = Surface("root", width: 2, height: 1, parentInstanceId: null);
        var insideA = Surface("inside-a", width: 1, height: 1, parentInstanceId: "a", priority: 1);
        var insideB = Surface("inside-b", width: 1, height: 1, parentInstanceId: "b", priority: 1);
        var a = Item(
            "a",
            "root",
            x: 0,
            y: 0,
            width: 1,
            height: 1,
            movable: true,
            [Option("inside-b", 1, 1)]);
        var b = Item(
            "b",
            "root",
            x: 1,
            y: 0,
            width: 1,
            height: 1,
            movable: true,
            [Option("inside-a", 1, 1)]);
        var incoming = new FarmingGuideRepackingIncoming(
            "incoming",
            [Option("root", 2, 1)]);

        var plan = FarmingGuideRepackingPlanner.TryPlan([root, insideA, insideB], [a, b], incoming);

        Assert.Null(plan);
    }

    private static FarmingGuideRepackingSurface Surface(
        string id,
        int width,
        int height,
        string? parentInstanceId = null,
        int priority = 0,
        IReadOnlyList<FarmingGuideGridPlacement>? fixedObstacles = null) =>
        new(
            id,
            parentInstanceId,
            width,
            height,
            priority,
            fixedObstacles ?? []);

    private static FarmingGuideRepackingItem Item(
        string id,
        string surfaceId,
        int x,
        int y,
        int width,
        int height,
        bool movable,
        IReadOnlyList<FarmingGuideRepackingOption> options) =>
        new(
            id,
            surfaceId,
            x,
            y,
            width,
            height,
            CurrentRotated: false,
            movable,
            options);

    private static FarmingGuideRepackingOption Option(
        string surfaceId,
        int width,
        int height,
        bool rotated = false,
        int preference = 0) =>
        new(surfaceId, width, height, rotated, preference);
}
