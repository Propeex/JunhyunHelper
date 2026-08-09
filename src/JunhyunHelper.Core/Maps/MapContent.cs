using System.Text.Json.Serialization;

namespace JunhyunHelper.Core.Maps;

public sealed record MapWorldPosition(double X, double Y, double Z);

public sealed record MapOutlinePoint(double X, double Z);

public enum MapMarkerKind
{
    PmcExtract,
    ScavExtract,
    SharedExtract,
    Transit,
    PmcSpawn,
    ScavSpawn,
    SniperScav,
    Boss,
    SpecialAi,
    Hazard,
    Lock,
    Switch,
    StationaryWeapon,
    BtrStop,
    LootContainer,
    LooseLoot,
}

public sealed record MapMarkerDefinition(
    string Id,
    string MapId,
    MapMarkerKind Kind,
    string Name,
    MapWorldPosition Position,
    IReadOnlyList<MapOutlinePoint> Outline,
    double? Top,
    double? Bottom,
    string? Detail);

public sealed record MapBoundsPoint(double X, double Z);

public sealed record MapWorldBounds(
    MapBoundsPoint First,
    MapBoundsPoint Second)
{
    public bool Contains(double x, double z)
    {
        var minX = Math.Min(First.X, Second.X);
        var maxX = Math.Max(First.X, Second.X);
        var minZ = Math.Min(First.Z, Second.Z);
        var maxZ = Math.Max(First.Z, Second.Z);
        return x >= minX && x <= maxX && z >= minZ && z <= maxZ;
    }
}

public sealed record MapFloorExtent(
    double MinHeight,
    double MaxHeight,
    IReadOnlyList<MapWorldBounds> Bounds)
{
    public bool Contains(MapWorldPosition position) =>
        position.Y >= MinHeight && position.Y < MaxHeight &&
        (Bounds.Count == 0 || Bounds.Any(bounds => bounds.Contains(position.X, position.Z)));
}

public sealed record MapFloorDefinition(
    string Id,
    string Name,
    string? SvgLayer,
    double MinHeight,
    double MaxHeight,
    bool IsDefault,
    IReadOnlyList<MapFloorExtent>? ExtentData = null)
{
    [JsonIgnore]
    public IReadOnlyList<MapFloorExtent> Extents =>
        ExtentData is { Count: > 0 }
            ? ExtentData
            : [new MapFloorExtent(MinHeight, MaxHeight, Array.Empty<MapWorldBounds>())];

    public bool Contains(MapWorldPosition position) =>
        Extents.Any(extent => extent.Contains(position));
}

public sealed record MapLayoutDefinition(
    string MapId,
    string Key,
    string NormalizedName,
    double MinZoom,
    double MaxZoom,
    IReadOnlyList<double> Transform,
    double CoordinateRotation,
    IReadOnlyList<MapBoundsPoint> Bounds,
    IReadOnlyList<MapBoundsPoint> SvgBounds,
    string SvgUrl,
    string? BaseSvgLayer,
    IReadOnlyList<MapFloorDefinition> Floors,
    string? Attribution,
    string? AttributionUrl);
