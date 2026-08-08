using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestUnsupportedAvailabilityTests
{
    [Fact]
    public void UnsupportedAdditionalRequirementIsIndeterminateInsteadOfIgnored()
    {
        var quest = new QuestDefinition(
            Id: "quest-a",
            NameKo: null,
            NameEn: null,
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
            UnsupportedAvailabilityRequirementTypes: ["dialogue"]);
        var profile = new GameProfileSnapshot
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
        };

        var result = QuestAvailabilityEvaluator.Evaluate([quest], profile)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.State);
        Assert.Contains(result.Reasons, reason =>
            reason.Kind == QuestAvailabilityReasonKind.UnsupportedAvailabilityRequirement &&
            reason.ReferenceId == "dialogue");
    }
}
