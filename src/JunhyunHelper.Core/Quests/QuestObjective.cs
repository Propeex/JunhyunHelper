namespace JunhyunHelper.Core.Quests;

public enum QuestItemObjectiveKind
{
    Submit,
    FindOrCollect,
    Sell,
    Other,
}

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
    QuestItemObjectiveKind ItemKind);

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
