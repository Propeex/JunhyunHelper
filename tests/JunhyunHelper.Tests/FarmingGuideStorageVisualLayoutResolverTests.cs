using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class FarmingGuideStorageVisualLayoutResolverTests
{
    [Fact]
    public void TryResolve_UsesVerifiedItemAlias_WhenNormalizedContentHasNoLayoutName()
    {
        var resolved = FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            A18Grids(),
            cellSize: 32,
            out var layout);

        Assert.True(resolved);
        Assert.Equal(15, layout.Grids.Count);
        Assert.Equal(0, layout.Grids[0].Left);
        Assert.Equal(0, layout.Grids[0].Top);
        Assert.True(layout.Grids[5].Top > layout.Grids[0].Top);
        Assert.True(layout.Grids[10].Top > layout.Grids[5].Top);
    }

    [Fact]
    public void TryResolve_UsesExplicitLayoutName_WithoutDependingOnItemAlias()
    {
        var resolved = FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "future-item",
            "ANA Tactical M1",
            AnaM1Grids(),
            cellSize: 29,
            out var layout);

        Assert.True(resolved);
        Assert.Equal(10, layout.Grids.Count);
        Assert.True(layout.Width > 0);
        Assert.True(layout.Height > 0);
    }

    [Fact]
    public void TryResolve_UsesVerifiedMbssProfileSignature()
    {
        var resolved = FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "64a5366719bab53bd203bf33",
            layoutName: null,
            MbssGrids(),
            cellSize: 32,
            out var layout);

        Assert.True(resolved);
        Assert.Equal(7, layout.Grids.Count);
        Assert.True(layout.Width > 0);
        Assert.True(layout.Height > 0);
    }

    [Fact]
    public void TryResolve_RejectsStaleProfile_WhenLiveGridCountChanged()
    {
        var grids = A18Grids().Take(14).ToArray();

        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            grids,
            cellSize: 32,
            out _));
    }

    [Fact]
    public void TryResolve_RejectsStaleProfile_WhenLiveGridDimensionsChangeWithoutOverlap()
    {
        var grids = A18Grids();
        grids[10] = Grid(1, 2);

        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            grids,
            cellSize: 64,
            out _));
    }

    [Fact]
    public void TryResolve_RejectsStaleProfile_WhenLiveGridWidthChanges()
    {
        var grids = AnaM1Grids();
        grids[6] = Grid(2, 1);

        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5c0e722886f7740458316a57",
            layoutName: null,
            grids,
            cellSize: 29,
            out _));
    }

    [Fact]
    public void TryResolve_RejectsUnknownLayout_AndPreservesProceduralFallbackContract()
    {
        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "unknown-item",
            "unknown-layout",
            [Grid(1, 1)],
            cellSize: 29,
            out _));
    }

    private static FarmingGuideStorageGridDefinition[] A18Grids() =>
    [
        Grid(1, 2), Grid(1, 2), Grid(1, 2), Grid(1, 2), Grid(1, 2),
        Grid(1, 2), Grid(1, 2), Grid(1, 2), Grid(1, 2), Grid(1, 2),
        Grid(1, 1), Grid(1, 1), Grid(1, 1), Grid(1, 1), Grid(1, 1),
    ];

    private static FarmingGuideStorageGridDefinition[] AnaM1Grids() =>
    [
        Grid(1, 2), Grid(1, 2), Grid(1, 2), Grid(1, 2),
        Grid(2, 2), Grid(2, 2),
        Grid(1, 1), Grid(1, 1), Grid(1, 1), Grid(1, 1),
    ];

    private static FarmingGuideStorageGridDefinition[] MbssGrids() =>
    [
        Grid(1, 1), Grid(1, 1), Grid(1, 1),
        Grid(1, 2), Grid(1, 2), Grid(2, 1), Grid(1, 3),
    ];

    private static FarmingGuideStorageGridDefinition Grid(int width, int height) =>
        new(width, height, FarmingGuideItemFilter.Empty);
}
