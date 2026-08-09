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

        if (layout.UsesLegacyAffineTransform)
            return TryLegacyWorldToSurface(layout, world, surfaceWidth, surfaceHeight, out point);

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

        if (layout.UsesLegacyAffineTransform)
            return TryLegacySurfaceToWorld(layout, point, surfaceWidth, surfaceHeight, height, out world);

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
        if (layout.UsesLegacyAffineTransform)
            return Math.Clamp(layout.SurfaceHeight!.Value / layout.SurfaceWidth!.Value, 0.2, 5.0);

        if (!TryProjectedBounds(layout, out var minX, out var maxX, out var minY, out var maxY))
            return 1;
        var width = Math.Abs(maxX - minX);
        var height = Math.Abs(maxY - minY);
        return width <= 0.000001 ? 1 : Math.Clamp(height / width, 0.2, 5.0);
    }

    public static MapFloorDefinition? FloorForPosition(
        MapLayoutDefinition layout,
        MapWorldPosition position)
    {
        var matching = layout.Floors
            .Where(floor => floor.Contains(position))
            .OrderBy(floor => floor.IsDefault ? 1 : 0)
            .ThenBy(floor => floor.Extents
                .Where(extent => extent.Contains(position))
                .Select(extent => extent.MaxHeight - extent.MinHeight)
                .DefaultIfEmpty(double.MaxValue)
                .Min())
            .ToArray();

        return matching.FirstOrDefault()
               ?? layout.Floors.FirstOrDefault(floor => floor.IsDefault);
    }

    public static MapFloorDefinition? FloorForHeight(MapLayoutDefinition layout, double height) =>
        FloorForPosition(layout, new MapWorldPosition(0, height, 0));

    public static double SurfaceHeading(MapLayoutDefinition layout, double worldHeadingDegrees) =>
        layout.UsesLegacyAffineTransform
            ? NormalizeDegrees(worldHeadingDegrees)
            : NormalizeDegrees(worldHeadingDegrees + layout.CoordinateRotation);

    private static bool TryLegacyWorldToSurface(
        MapLayoutDefinition layout,
        MapWorldPosition world,
        double surfaceWidth,
        double surfaceHeight,
        out Point point)
    {
        point = default;
        if (surfaceWidth <= 0 || surfaceHeight <= 0 ||
            layout.LegacyPlayerTransform is not { Count: 6 } matrix ||
            layout.SurfaceWidth is not > 0 || layout.SurfaceHeight is not > 0)
            return false;

        var rawX = matrix[0] * world.X + matrix[1] * world.Z + matrix[4];
        var rawY = matrix[2] * world.X + matrix[3] * world.Z + matrix[5];
        var x = rawX / layout.SurfaceWidth.Value * surfaceWidth;
        var y = rawY / layout.SurfaceHeight.Value * surfaceHeight;
        if (!double.IsFinite(x) || !double.IsFinite(y))
            return false;

        point = new Point(x, y);
        return true;
    }

    private static bool TryLegacySurfaceToWorld(
        MapLayoutDefinition layout,
        Point point,
        double surfaceWidth,
        double surfaceHeight,
        double height,
        out MapWorldPosition world)
    {
        world = new MapWorldPosition(0, height, 0);
        if (surfaceWidth <= 0 || surfaceHeight <= 0 ||
            layout.LegacyPlayerTransform is not { Count: 6 } matrix ||
            layout.SurfaceWidth is not > 0 || layout.SurfaceHeight is not > 0)
            return false;

        var rawX = point.X / surfaceWidth * layout.SurfaceWidth.Value;
        var rawY = point.Y / surfaceHeight * layout.SurfaceHeight.Value;
        var translatedX = rawX - matrix[4];
        var translatedY = rawY - matrix[5];
        var determinant = matrix[0] * matrix[3] - matrix[1] * matrix[2];
        if (Math.Abs(determinant) < 0.000001)
            return false;

        var x = (matrix[3] * translatedX - matrix[1] * translatedY) / determinant;
        var z = (-matrix[2] * translatedX + matrix[0] * translatedY) / determinant;
        if (!double.IsFinite(x) || !double.IsFinite(z))
            return false;

        world = new MapWorldPosition(x, height, z);
        return true;
    }

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