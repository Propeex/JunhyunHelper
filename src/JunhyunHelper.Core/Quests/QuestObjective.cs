using System.Text.Json.Serialization;
using JunhyunHelper.Core.Maps;

namespace JunhyunHelper.Core.Quests;

public enum QuestItemObjectiveKind
{
    Submit,
    FindOrCollect,
    Sell,
    Other,
}

public enum QuestMapLocationKind
{
    PossibleLocation,
    Zone,
}

public sealed record QuestMapLocation(
    string MapId,
    QuestMapLocationKind Kind,
    MapWorldPosition Position,
    IReadOnlyList<MapOutlinePoint> Outline,
    double? Top,
    double? Bottom);

public sealed record QuestObjective(
    string QuestId,
    string ObjectiveId,
    string Type,
    string? DescriptionKo,
    string? DescriptionEn,
    bool Optional,
    int? Count,
    bool FoundInRaid,
    IReadOnlyList<string> MapIds,
    IReadOnlyList<string> ItemIds,
    string? QuestItemId,
    QuestItemObjectiveKind ItemKind,
    IReadOnlyList<QuestMapLocation>? MapLocationData = null)
{
    [JsonIgnore]
    public IReadOnlyList<QuestMapLocation> MapLocations =>
        MapLocationData ?? Array.Empty<QuestMapLocation>();
}

public sealed record QuestItemRequirement(
    string QuestId,
    string ObjectiveId,
    IReadOnlyList<string> AcceptedItemIds,
    int Count,
    bool FoundInRaid)
{
    public bool HasAlternatives => AcceptedItemIds.Count > 1;
}

public sealed record QuestObjectiveImport(
    IReadOnlyList<QuestObjective> Objectives,
    IReadOnlyList<QuestItemRequirement> ItemRequirements);
