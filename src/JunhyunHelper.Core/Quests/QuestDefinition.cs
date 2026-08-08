using System.Text.Json.Serialization;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

public enum QuestRequiredStatus
{
    Complete,
    Active,
    Failed,
}

public enum StandingRequirementOperator
{
    AtLeast,
    AtMost,
    LessThan,
}

public sealed record QuestTaskRequirement(
    string RequiredQuestId,
    IReadOnlyCollection<QuestRequiredStatus> AcceptedStatuses);

public sealed record QuestCompletionFailureCondition(string TriggerQuestId);

public sealed record QuestTraderStandingRequirement(
    string TraderId,
    decimal RequiredStanding,
    StandingRequirementOperator Operator);

public sealed record QuestTraderLoyaltyRequirement(
    string TraderId,
    int RequiredLoyaltyLevel);

public sealed record QuestDefinition(
    string Id,
    string? NameKo,
    string? NameEn,
    string? TraderId,
    string? MapId,
    string? WikiUrl,
    int? Experience,
    bool KappaRequired,
    bool LightkeeperRequired,
    bool Disabled,
    int MinimumPlayerLevel,
    PmcFaction? RequiredFaction,
    int? RequiredPrestigeLevel,
    IReadOnlyList<QuestTaskRequirement> TaskRequirements,
    IReadOnlyList<QuestTraderStandingRequirement> TraderStandingRequirements,
    IReadOnlyList<QuestTraderLoyaltyRequirement> TraderLoyaltyRequirements,
    IReadOnlyList<string>? UnsupportedAvailabilityRequirementTypes = null,
    IReadOnlyList<QuestCompletionFailureCondition>? CompletionFailureConditionData = null,
    bool Restartable = false,
    IReadOnlyList<string>? UnsupportedFailureConditionTypes = null)
{
    [JsonIgnore]
    public IReadOnlyList<string> UnsupportedAvailabilityRequirements =>
        UnsupportedAvailabilityRequirementTypes ?? Array.Empty<string>();

    [JsonIgnore]
    public IReadOnlyList<QuestCompletionFailureCondition> CompletionFailureConditions =>
        CompletionFailureConditionData ?? Array.Empty<QuestCompletionFailureCondition>();

    [JsonIgnore]
    public IReadOnlyList<string> UnsupportedFailureConditions =>
        UnsupportedFailureConditionTypes ?? Array.Empty<string>();

    [JsonIgnore]
    public bool RequiresExplicitFailureInput =>
        !Restartable && UnsupportedFailureConditions.Count > 0;
}
