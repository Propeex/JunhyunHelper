using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Map;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class LegacyMapCoordinateTransformerTests
{
    [Fact]
    public void GroundZeroLegacyAffineTransform_RoundTripsWorldAndSurfaceCoordinates()
    {
        var layout = CreateGroundZeroLayout();
        var world = new MapWorldPosition(42.5, 17, -63.25);

        Assert.True(MapCoordinateTransformer.TryWorldToSurface(
            layout,
            world,
            layout.SurfaceWidth!.Value,
            layout.SurfaceHeight!.Value,
            out var surface));

        Assert.Equal(-2 * world.X + 1600, surface.X, 8);
        Assert.Equal(2 * world.Z + 1301.5, surface.Y, 8);

        Assert.True(MapCoordinateTransformer.TrySurfaceToWorld(
            layout,
            surface,
            layout.SurfaceWidth.Value,
            layout.SurfaceHeight.Value,
            world.Y,
            out var restored));
        Assert.Equal(world.X, restored.X, 8);
        Assert.Equal(world.Y, restored.Y, 8);
        Assert.Equal(world.Z, restored.Z, 8);
    }

    [Fact]
    public void LegacyMap_UsesArtworkAspectRatioAndLegacyHeadingSemantics()
    {
        var layout = CreateGroundZeroLayout();

        Assert.Equal(3100d / 2800d, MapCoordinateTransformer.SurfaceAspectRatio(layout), 8);
        Assert.Equal(271.5, MapCoordinateTransformer.SurfaceHeading(layout, 271.5), 8);
    }

    private static MapLayoutDefinition CreateGroundZeroLayout() =>
        new(
            "map-ground-zero",
            "legacy-GroundZero",
            "ground-zero",
            0.5,
            5,
            [1, 0, 1, 0],
            0,
            [new MapBoundsPoint(-100, -100), new MapBoundsPoint(100, 100)],
            [new MapBoundsPoint(-100, -100), new MapBoundsPoint(100, 100)],
            "https://example.invalid/GroundZero.svg",
            "main",
            [new MapFloorDefinition("main", "Ground Floor", "main", double.MinValue, double.MaxValue, true)],
            null,
            null,
            [-2, 0, 0, 2, 1600, 1301.5],
            2800,
            3100);
}