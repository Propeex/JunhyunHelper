namespace JunhyunHelper.Core.Hideout;

public sealed record HideoutStation(
    string Id,
    string? NameKo,
    string? NameEn,
    string? ImageUrl,
    IReadOnlyList<HideoutLevel> Levels);

public sealed record HideoutLevel(
    string StationId,
    int Level,
    int? ConstructionTimeSeconds,
    IReadOnlyList<HideoutItemRequirement> ItemRequirements);

public sealed record HideoutItemRequirement(
    string StationId,
    int TargetLevel,
    string ItemId,
    int Count,
    bool FoundInRaid);
