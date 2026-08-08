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
    IReadOnlyList<string>? UnsupportedAvailabilityRequirements = null);
