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

public sealed record MapFloorDefinition(
    string Id,
    string Name,
    string? SvgLayer,
    double MinHeight,
    double MaxHeight,
    bool IsDefault);

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
