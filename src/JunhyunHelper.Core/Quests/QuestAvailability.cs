namespace JunhyunHelper.Core.Quests;

public enum QuestAvailabilityState
{
    Completed,
    Current,
    Locked,
    Unavailable,
    Indeterminate,
}

public enum QuestAvailabilityReasonKind
{
    Disabled,
    Failed,
    FailedByQuest,
    MinimumLevel,
    Faction,
    Edition,
    Prestige,
    TraderStanding,
    TraderLoyalty,
    Prerequisite,
    PrerequisiteUnavailable,
    MissingProfileValue,
    UnsupportedAvailabilityRequirement,
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
