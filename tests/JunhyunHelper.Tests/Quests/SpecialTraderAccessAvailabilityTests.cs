using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class SpecialTraderAccessAvailabilityTests
{
    private const string TraderId = "lightkeeper";
    private const string UnlockId = "getting-acquainted";
    private const string FollowupId = "lightkeeper-followup";

    [Fact]
    public void InitialUnlockCompletionAutomaticallyAllowsAccess()
    {
        var result = Evaluate(Profile(completed: [UnlockId]));

        Assert.Equal(QuestAvailabilityState.Current, result[FollowupId].State);
    }

    [Fact]
    public void FailedInitialUnlockKeepsRecoverableAccessLockedNotUnavailable()
    {
        var result = Evaluate(Profile(failed: [UnlockId]));

        Assert.Equal(QuestAvailabilityState.Locked, result[FollowupId].State);
        Assert.Contains(
            result[FollowupId].Reasons,
            reason => reason.Kind == QuestAvailabilityReasonKind.SpecialTraderAccess);
    }

    [Fact]
    public void ManualRecoveryFactAllowsAccessAfterInitialUnlockFailure()
    {
        var result = Evaluate(Profile(
            failed: [UnlockId],
            overrides: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                [TraderId] = true,
            }));

        Assert.Equal(QuestAvailabilityState.Current, result[FollowupId].State);
    }

    [Fact]
    public void ManualLossFactLocksAccessAfterInitialUnlockCompletion()
    {
        var result = Evaluate(Profile(
            completed: [UnlockId],
            overrides: new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                [TraderId] = false,
            }));

        Assert.Equal(QuestAvailabilityState.Locked, result[FollowupId].State);
    }

    private static IReadOnlyDictionary<string, QuestAvailabilityResult> Evaluate(GameProfileSnapshot profile) =>
        QuestAvailabilityEvaluator.Evaluate([UnlockQuest(), FollowupQuest()], profile);

    private static QuestDefinition UnlockQuest() => new(
        Id: UnlockId,
        NameKo: UnlockId,
        NameEn: UnlockId,
        TraderId: null,
        MapId: null,
        WikiUrl: null,
        Experience: null,
        KappaRequired: false,
        LightkeeperRequired: false,
        Disabled: false,
        MinimumPlayerLevel: 1,
        RequiredFaction: null,
        RequiredPrestigeLevel: null,
        TaskRequirements: [],
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: [],
        Restartable: false,
        UnsupportedFailureConditionTypes: ["traderStanding"]);

    private static QuestDefinition FollowupQuest() => new(
        Id: FollowupId,
        NameKo: FollowupId,
        NameEn: FollowupId,
        TraderId: TraderId,
        MapId: null,
        WikiUrl: null,
        Experience: null,
        KappaRequired: false,
        LightkeeperRequired: true,
        Disabled: false,
        MinimumPlayerLevel: 1,
        RequiredFaction: null,
        RequiredPrestigeLevel: null,
        TaskRequirements: [],
        TraderStandingRequirements: [],
        TraderLoyaltyRequirements: [],
        SpecialTraderAccessRequirement: new QuestSpecialTraderAccessRequirement(
            TraderId,
            UnlockId,
            new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete },
            AllowManualOverride: true));

    private static GameProfileSnapshot Profile(
        IReadOnlySet<string>? completed = null,
        IReadOnlySet<string>? failed = null,
        IReadOnlyDictionary<string, bool>? overrides = null) => new()
        {
            ProfileId = "profile",
            GameMode = GameMode.Regular,
            Level = 60,
            Faction = PmcFaction.Usec,
            CompletedQuestIds = completed ?? new HashSet<string>(StringComparer.Ordinal),
            FailedQuestIds = failed ?? new HashSet<string>(StringComparer.Ordinal),
            SpecialTraderAccessOverrides = overrides ?? new Dictionary<string, bool>(StringComparer.Ordinal),
        };
}
