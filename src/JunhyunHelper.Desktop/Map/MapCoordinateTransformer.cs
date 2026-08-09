using System.Windows;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Desktop.Map;

public static class MapCoordinateTransformer
{
    public static bool TryWorldToSurface(
        MapLayoutDefinition layout,
        MapWorldPosition world,
        double surfaceWidth,
        double surfaceHeight,
        out Point point)
    {
        point = default;
        if (!TryProjectedBounds(layout, out var minX, out var maxX, out var minY, out var maxY))
            return false;
        var projected = Project(layout, world.X, world.Z);
        var width = maxX - minX;
        var height = maxY - minY;
        if (Math.Abs(width) < 0.000001 || Math.Abs(height) < 0.000001)
            return false;

        var x = (projected.X - minX) / width * surfaceWidth;
        var y = (projected.Y - minY) / height * surfaceHeight;
        if (!double.IsFinite(x) || !double.IsFinite(y))
            return false;
        point = new Point(x, y);
        return true;
    }

    public static bool TrySurfaceToWorld(
        MapLayoutDefinition layout,
        Point point,
        double surfaceWidth,
        double surfaceHeight,
        double height,
        out MapWorldPosition world)
    {
        world = new MapWorldPosition(0, height, 0);
        if (surfaceWidth <= 0 || surfaceHeight <= 0 ||
            !TryProjectedBounds(layout, out var minX, out var maxX, out var minY, out var maxY))
            return false;

        var projectedX = minX + point.X / surfaceWidth * (maxX - minX);
        var projectedY = minY + point.Y / surfaceHeight * (maxY - minY);
        var scaleX = layout.Transform[0];
        var marginX = layout.Transform[1];
        var scaleY = layout.Transform[2] * -1;
        var marginY = layout.Transform[3];
        if (Math.Abs(scaleX) < 0.000001 || Math.Abs(scaleY) < 0.000001)
            return false;

        var rotatedX = (projectedX - marginX) / scaleX;
        var rotatedZ = (projectedY - marginY) / scaleY;
        var radians = -layout.CoordinateRotation * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var x = rotatedX * cos - rotatedZ * sin;
        var z = rotatedX * sin + rotatedZ * cos;
        if (!double.IsFinite(x) || !double.IsFinite(z))
            return false;
        world = new MapWorldPosition(x, height, z);
        return true;
    }

    public static double SurfaceAspectRatio(MapLayoutDefinition layout)
    {
        if (!TryProjectedBounds(layout, out var minX, out var maxX, out var minY, out var maxY))
            return 1;
        var width = Math.Abs(maxX - minX);
        var height = Math.Abs(maxY - minY);
        return width <= 0.000001 ? 1 : Math.Clamp(height / width, 0.2, 5.0);
    }

    public static MapFloorDefinition? FloorForHeight(MapLayoutDefinition layout, double height) =>
        layout.Floors
            .Where(floor => height >= floor.MinHeight && height < floor.MaxHeight)
            .OrderBy(floor => floor.MaxHeight - floor.MinHeight)
            .FirstOrDefault();

    public static double SurfaceHeading(MapLayoutDefinition layout, double worldHeadingDegrees) =>
        NormalizeDegrees(worldHeadingDegrees + layout.CoordinateRotation);

    private static bool TryProjectedBounds(
        MapLayoutDefinition layout,
        out double minX,
        out double maxX,
        out double minY,
        out double maxY)
    {
        minX = maxX = minY = maxY = 0;
        if (layout.Transform.Count != 4 || layout.SvgBounds.Count != 2)
            return false;
        var first = Project(layout, layout.SvgBounds[0].X, layout.SvgBounds[0].Z);
        var second = Project(layout, layout.SvgBounds[1].X, layout.SvgBounds[1].Z);
        minX = Math.Min(first.X, second.X);
        maxX = Math.Max(first.X, second.X);
        minY = Math.Min(first.Y, second.Y);
        maxY = Math.Max(first.Y, second.Y);
        return double.IsFinite(minX) && double.IsFinite(maxX) && double.IsFinite(minY) && double.IsFinite(maxY);
    }

    private static Point Project(MapLayoutDefinition layout, double x, double z)
    {
        var radians = layout.CoordinateRotation * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        var rotatedX = x * cos - z * sin;
        var rotatedZ = x * sin + z * cos;
        var projectedX = layout.Transform[0] * rotatedX + layout.Transform[1];
        var projectedY = layout.Transform[2] * -1 * rotatedZ + layout.Transform[3];
        return new Point(projectedX, projectedY);
    }

    private static double NormalizeDegrees(double value) =>
        (value % 360.0 + 360.0) % 360.0;
}
