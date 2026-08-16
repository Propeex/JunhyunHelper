using System.Reflection;
using System.Text.Json;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovProfileVariableRequirementTests
{
    [Fact]
    public void GlobalVariablePayloadBecomesStructuredSupportedRequirement()
    {
        using var json = JsonDocument.Parse("{\"tasks\":[{\"id\":\"quest-a\",\"otherRequirements\":[{\"type\":\"globalVariable\",\"variableId\":\"pool-x\",\"compareMethod\":\">=\",\"value\":3}]}]}");
        var quest = CreateQuest();
        var method = typeof(TarkovGameContentImporter).GetMethod(
  "ApplyUnsupportedAvailabilityRequirements",
  BindingFlags.NonPublic | BindingFlags.Static)
  ?? throw new InvalidOperationException("Importer method not found.");

        var result = Assert.IsAssignableFrom<IReadOnlyList<QuestDefinition>>(
  method.Invoke(null, new object[] { new[] { quest }, json.RootElement }));
        var imported = Assert.Single(result);

        var requirement = Assert.Single(imported.ProfileVariableRequirements);
        Assert.Equal("pool-x", requirement.VariableId);
        Assert.Equal(3, requirement.RequiredValue);
        Assert.Equal(ProfileVariableRequirementOperator.AtLeast, requirement.Operator);
        Assert.DoesNotContain("globalVariable", imported.UnsupportedAvailabilityRequirements);
    }

    [Fact]
    public void UnknownGlobalVariableShapeFailsClosedAsUnsupported()
    {
        using var json = JsonDocument.Parse("{\"tasks\":[{\"id\":\"quest-a\",\"otherRequirements\":[{\"type\":\"globalVariable\",\"variableId\":\"pool-x\",\"compareMethod\":\"!=\",\"value\":3}]}]}");
        var method = typeof(TarkovGameContentImporter).GetMethod(
  "ApplyUnsupportedAvailabilityRequirements",
  BindingFlags.NonPublic | BindingFlags.Static)
  ?? throw new InvalidOperationException("Importer method not found.");

        var result = Assert.IsAssignableFrom<IReadOnlyList<QuestDefinition>>(
  method.Invoke(null, new object[] { new[] { CreateQuest() }, json.RootElement }));
        var imported = Assert.Single(result);

        Assert.Empty(imported.ProfileVariableRequirements);
        Assert.Contains("globalVariable", imported.UnsupportedAvailabilityRequirements);
    }

    private static QuestDefinition CreateQuest() =>
        new(
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
  TraderLoyaltyRequirements: []);
}
