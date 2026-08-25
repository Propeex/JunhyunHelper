using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class QuestObjectiveReferenceSemanticsTests
{
    [Fact]
    public void QuestItemObjectiveMayReferenceNonCanonicalQuestItemEntity()
    {
        const string questId = "6524640578137d9edc1628e4";
        const string objectiveId = "objective-6710469f5474276231657a22";
        const string specialQuestItemId = "6662e9aca7e0b43baa3d5f74";

        var content = CreateCatalog(
            new QuestObjective(
                questId,
                objectiveId,
                "findQuestItem",
                "퀘스트 전용 아이템 찾기",
                "Find quest item",
                false,
                1,
                false,
                Array.Empty<string>(),
                Array.Empty<string>(),
                specialQuestItemId,
                QuestItemObjectiveKind.Other));

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, issue =>
            issue.Code == "quest-objective.quest-item.missing" ||
            issue.Message.Contains(specialQuestItemId, StringComparison.Ordinal));
    }

    private static GameContentCatalog CreateCatalog(QuestObjective objective)
    {
        var item = new GameItem(
            "item-0",
            "아이템",
            "Item",
            null,
            null,
            "https://example.com/icon.png",
            "https://example.com/wiki",
            Array.Empty<string>());
        var quest = new QuestDefinition(
            objective.QuestId,
            "퀘스트",
            "Quest",
            "trader-a",
            "map-a",
            "https://example.com/quest",
            100,
            false,
            false,
            false,
            1,
            null,
            null,
            Array.Empty<QuestTaskRequirement>(),
            Array.Empty<QuestTraderStandingRequirement>(),
            Array.Empty<QuestTraderLoyaltyRequirement>());

        return new GameContentCatalog(
            [item],
            [new TraderDefinition("trader-a", "상인", "Trader")],
            [new MapReference("map-a", "맵", "Map", "map-a")],
            [quest],
            [objective],
            [new QuestItemRequirement(objective.QuestId, objective.ObjectiveId, ["item-0"], 1, false)],
            [
                new HideoutStation(
                    "station-a",
                    "시설",
                    "Station",
                    "https://example.com/station.png",
                    [new HideoutLevel("station-a", 1, 1, [new HideoutItemRequirement("station-a", 1, "item-0", 1, false)])]),
            ],
            Ammo:
            [
                new AmmoDefinition(
                    "item-0",
                    "CaliberTest",
                    "bullet",
                    1,
                    50,
                    20,
                    30,
                    0.1m,
                    0.1m,
                    0,
                    0,
                    300,
                    0,
                    0,
                    false,
                    null,
                    Array.Empty<AmmoAcquisition>()),
            ],
            EditionData:
            [
                new EditionDefinition(
                    "edition-a",
                    "Edition A",
                    new HashSet<string>(StringComparer.Ordinal),
                    new HashSet<string>(StringComparer.Ordinal)),
            ]);
    }
}
