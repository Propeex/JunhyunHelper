using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class FarmingGuideStorageVisualLayoutResolverTests
{
    [Fact]
    public void TryResolve_UsesVerifiedItemAlias_WhenNormalizedContentHasNoLayoutName()
    {
        var grids = Enumerable.Range(0, 15)
            .Select(_ => Grid(1, 1))
            .ToArray();

        var resolved = FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            grids,
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
        var grids = Enumerable.Range(0, 10)
            .Select(_ => Grid(1, 1))
            .ToArray();

        var resolved = FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "future-item",
            "ANA Tactical M1",
            grids,
            cellSize: 29,
            out var layout);

        Assert.True(resolved);
        Assert.Equal(10, layout.Grids.Count);
        Assert.True(layout.Width > 0);
        Assert.True(layout.Height > 0);
    }

    [Fact]
    public void TryResolve_RejectsStaleProfile_WhenLiveGridCountChanged()
    {
        var grids = Enumerable.Range(0, 14)
            .Select(_ => Grid(1, 1))
            .ToArray();

        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            grids,
            cellSize: 32,
            out _));
    }

    [Fact]
    public void TryResolve_RejectsProfile_WhenLiveGridGeometryWouldOverlap()
    {
        var grids = Enumerable.Range(0, 15)
            .Select(_ => Grid(1, 1))
            .ToArray();
        grids[0] = Grid(2, 1);

        Assert.False(FarmingGuideStorageVisualLayoutResolver.TryResolve(
            "5d5d87f786f77427997cfaef",
            layoutName: null,
            grids,
            cellSize: 64,
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

    private static FarmingGuideStorageGridDefinition Grid(int width, int height) =>
        new(width, height, FarmingGuideItemFilter.Empty);
}
