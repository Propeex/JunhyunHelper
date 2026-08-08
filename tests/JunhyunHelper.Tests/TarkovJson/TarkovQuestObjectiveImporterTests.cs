using System.Text.Json;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Infrastructure.TarkovJson;
using JunhyunHelper.Infrastructure.TarkovJson.Quests;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovQuestObjectiveImporterTests
{
    [Fact]
    public void OnlySubmitObjectivesBecomeNeededMaterialRequirements()
    {
        var document = Document("""
            {
              "data": {
                "tasks": {
                  "quest-a": {
                    "id": "quest-a",
                    "objectives": [
                      {
                        "id": "objective-find",
                        "type": "findItem",
                        "description": "find Description",
                        "items": ["item-a"],
                        "count": 2,
                        "foundInRaid": true
                      },
                      {
                        "id": "objective-give",
                        "type": "giveItem",
                        "description": "give Description",
                        "items": ["item-a"],
                        "count": 2,
                        "foundInRaid": true
                      },
                      {
                        "id": "objective-sell",
                        "type": "sellItem",
                        "description": "sell Description",
                        "items": ["item-a", "item-b"],
                        "count": 99
                      }
                    ]
                  }
                }
              }
            }
            """);

        var imported = new TarkovQuestObjectiveImporter().Import(
            document,
            new TarkovLocalization());

        Assert.Equal(3, imported.Objectives.Count);
        var requirement = Assert.Single(imported.ItemRequirements);
        Assert.Equal("objective-give", requirement.ObjectiveId);
        Assert.Equal(2, requirement.Count);
        Assert.True(requirement.FoundInRaid);
        Assert.Equal("item-a", Assert.Single(requirement.AcceptedItemIds));
    }

    [Fact]
    public void AlternativeSubmitItemsArePreservedAsOneRequirementGroup()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  {
                    "id": "quest-a",
                    "objectives": [
                      {
                        "id": "objective-give",
                        "type": "giveItem",
                        "items": ["item-a", "item-b"],
                        "count": 3
                      }
                    ]
                  }
                ]
              }
            }
            """);

        var requirement = Assert.Single(
            new TarkovQuestObjectiveImporter()
                .Import(document, new TarkovLocalization())
                .ItemRequirements);

        Assert.True(requirement.HasAlternatives);
        Assert.Equal(2, requirement.AcceptedItemIds.Count);
    }

    [Fact]
    public void SameObjectiveIdCanExistInDifferentQuests()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  {
                    "id": "quest-a",
                    "objectives": [
                      { "id": "shared-objective", "type": "visit" }
                    ]
                  },
                  {
                    "id": "quest-b",
                    "objectives": [
                      { "id": "shared-objective", "type": "visit" }
                    ]
                  }
                ]
              }
            }
            """);

        var imported = new TarkovQuestObjectiveImporter().Import(
            document,
            new TarkovLocalization());

        Assert.Equal(2, imported.Objectives.Count);
        Assert.Contains(imported.Objectives, objective =>
            objective.QuestId == "quest-a" && objective.ObjectiveId == "shared-objective");
        Assert.Contains(imported.Objectives, objective =>
            objective.QuestId == "quest-b" && objective.ObjectiveId == "shared-objective");
    }

    [Fact]
    public void DuplicateObjectiveIdInsideSameQuestIsFatal()
    {
        var document = Document("""
            {
              "data": {
                "tasks": [
                  {
                    "id": "quest-a",
                    "objectives": [
                      { "id": "duplicate", "type": "visit" },
                      { "id": "duplicate", "type": "visit" }
                    ]
                  }
                ]
              }
            }
            """);

        Assert.Throws<InvalidDataException>(() =>
            new TarkovQuestObjectiveImporter().Import(document, new TarkovLocalization()));
    }

    private static TarkovJsonDocument Document(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovJsonDocument.Parse(parsed.RootElement);
    }
}
