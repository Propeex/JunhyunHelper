namespace JunhyunHelper.Core.Quests;

public enum QuestAvailabilityState
{
    Completed,
    Current,
    Locked,
    Indeterminate,
}

public enum QuestAvailabilityReasonKind
{
    Disabled,
    MinimumLevel,
    Faction,
    Prestige,
    TraderStanding,
    TraderLoyalty,
    Prerequisite,
    MissingProfileValue,
    FailedPrerequisiteStateNotTracked,
    MissingReferencedQuest,
    DependencyCycle,
}

public sealed record QuestAvailabilityReason(
    QuestAvailabilityReasonKind Kind,
    string? ReferenceId = null);

public sealed record QuestAvailabilityResult(
    string QuestId,
    QuestAvailabilityState State,
    IReadOnlyList<QuestAvailabilityReason> Reasons);
