using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class ProfileVariableAvailabilityTests
{
    [Fact]
    public void ExactProfileVariableMeetingThresholdMakesQuestCurrent()
    {
        var quest = CreateQuest("pool-x", 3);
        var profile = CreateProfile(new Dictionary<string, int> { ["pool-x"] = 3 });

        var result = QuestAvailabilityEvaluator.Evaluate([quest], profile)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Current, result.State);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public void ExactProfileVariableBelowThresholdKeepsQuestLocked()
    {
        var quest = CreateQuest("pool-x", 3);
        var profile = CreateProfile(new Dictionary<string, int> { ["pool-x"] = 2 });

        var result = QuestAvailabilityEvaluator.Evaluate([quest], profile)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Locked, result.State);
        Assert.Contains(result.Reasons, reason =>
  reason.Kind == QuestAvailabilityReasonKind.ProfileVariable &&
  reason.ReferenceId == "pool-x");
    }

    [Fact]
    public void MissingProfileVariableRemainsIndeterminateInsteadOfAssumingZero()
    {
        var quest = CreateQuest("pool-x", 3);
        var profile = CreateProfile(new Dictionary<string, int>());

        var result = QuestAvailabilityEvaluator.Evaluate([quest], profile)[quest.Id];

        Assert.Equal(QuestAvailabilityState.Indeterminate, result.State);
        Assert.Contains(result.Reasons, reason =>
  reason.Kind == QuestAvailabilityReasonKind.MissingProfileValue &&
  reason.ReferenceId == "profileVariable:pool-x");
    }

    private static QuestDefinition CreateQuest(string variableId, int value) =>
        new(
  Id: "quest-a",
  NameKo: null,
  NameEn: "Quest A",
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
  ProfileVariableRequirementData:
  [
      new QuestProfileVariableRequirement(
variableId,
value,
ProfileVariableRequirementOperator.AtLeast),
  ]);

    private static GameProfileSnapshot CreateProfile(IReadOnlyDictionary<string, int> variables) =>
        new()
        {
  ProfileId = "profile-a",
  GameMode = GameMode.Regular,
  Level = 50,
  Faction = PmcFaction.Usec,
  ProfileVariables = variables,
        };
}
