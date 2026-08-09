using System.Text.Json.Serialization;

namespace JunhyunHelper.Core.Quests;

public enum QuestItemObjectiveKind
{
    Submit,
    FindOrCollect,
    Sell,
    Other,
}

/// <summary>
/// Quest-only world geometry used by the Map subsystem. Y is nullable because some
/// external location records define X/Z without a reliable height. Unknown height
/// must not be guessed as zero and accidentally assigned to the wrong floor.
/// </summary>
public sealed record QuestWorldPosition(double X, double? Y, double Z);

public sealed record QuestOutlinePoint(double X, double Z);

public enum QuestMapLocationKind
{
    PossibleLocation,
    Zone,
}

public sealed record QuestMapLocation(
    string MapId,
    QuestMapLocationKind Kind,
    QuestWorldPosition Position,
    IReadOnlyList<QuestOutlinePoint> Outline,
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
