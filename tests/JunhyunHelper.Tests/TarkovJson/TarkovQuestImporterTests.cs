using System.Text.Json;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovQuestImporterTests
{
    [Fact]
    public void PreservesQuestAvailabilityRuleMeanings()
    {
        var document = Document("""
            {
              "data": {
                "prestige": [
                  { "id": "prestige-2", "prestigeLevel": 2 }
                ],
                "tasks": [
                  {
                    "id": "quest-a",
                    "name": "quest-a Name",
                    "trader": "trader-prapor",
                    "map": { "id": "map-customs" },
                    "minPlayerLevel": 15,
                    "factionName": "USEC",
                    "requiredPrestige": "prestige-2",
                    "taskRequirements": [
                      {
                        "task": "quest-prereq",
                        "status": ["complete", "active"]
                      }
                    ],
                    "traderRequirements": [
                      {
                        "trader": "trader-fence",
                        "value": 1.5
                      }
                    ],
                    "traderLevelRequirements": [
                      {
                        "trader": "trader-prapor",
                        "level": 3
                      }
                    ]
                  }
                ]
              }
            }
            """);

        var quest = Assert.Single(
            new TarkovQuestImporter().Import(document, new TarkovLocalization()));

        Assert.Equal("quest-a", quest.Id);
        Assert.Equal("trader-prapor", quest.TraderId);
        Assert.Equal("map-customs", quest.MapId);
        Assert.Equal(15, quest.MinimumPlayerLevel);
        Assert.Equal(PmcFaction.Usec, quest.RequiredFaction);
        Assert.Equal(2, quest.RequiredPrestigeLevel);

        var prerequisite = Assert.Single(quest.TaskRequirements);
        Assert.Contains(QuestRequiredStatus.Complete, prerequisite.AcceptedStatuses);
        Assert.Contains(QuestRequiredStatus.Active, prerequisite.AcceptedStatuses);

        Assert.Equal(1.5m, Assert.Single(quest.TraderStandingRequirements).RequiredStanding);
        Assert.Equal(3, Assert.Single(quest.TraderLoyaltyRequirements).RequiredLoyaltyLevel);
    }

    [Fact]
    public void MissingPrerequisiteStatusDefaultsToComplete()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  {
                    "id": "quest-a",
                    "taskRequirements": [
                      { "task": "quest-prereq" }
                    ]
                  },
                  { "id": "quest-prereq" }
                ]
              }
            }
            """);

        var quest = new TarkovQuestImporter()
            .Import(document, new TarkovLocalization())
            .Single(quest => quest.Id == "quest-a");

        var statuses = Assert.Single(quest.TaskRequirements).AcceptedStatuses;
        Assert.Single(statuses);
        Assert.Contains(QuestRequiredStatus.Complete, statuses);
    }

    [Fact]
    public void NeutralFactionBecomesNoRestriction()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  { "id": "quest-a", "factionName": "Any Target" }
                ]
              }
            }
            """);

        var quest = Assert.Single(
            new TarkovQuestImporter().Import(document, new TarkovLocalization()));

        Assert.Null(quest.RequiredFaction);
    }

    [Fact]
    public void UnknownPrerequisiteStatusIsFatalInsteadOfGuessed()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  {
                    "id": "quest-a",
                    "taskRequirements": [
                      { "task": "quest-prereq", "status": ["new-future-status"] }
                    ]
                  }
                ]
              }
            }
            """);

        Assert.Throws<InvalidDataException>(() =>
            new TarkovQuestImporter().Import(document, new TarkovLocalization()));
    }

    private static TarkovJsonDocument Document(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(parsed.RootElement);
    }
}
