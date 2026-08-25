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

public sealed class GameContentIntegrityValidatorTests
{
    private const string RegressionQuestId = "6524640578137d9edc1628e4";
    private const string RegressionObjectiveId = "objective-6710469f5474276231657a22";
    private const string RegressionSpecialDogtagId = "6662e9aca7e0b43baa3d5f74";

    [Fact]
    public void CompleteConnectedCatalogPasses()
    {
        var result = new GameContentIntegrityValidator().Validate(CreateCatalog());

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, issue => issue.Severity == ContentValidationSeverity.Fatal);
    }

    [Fact]
    public void DuplicateItemIdIsFatal()
    {
        var content = CreateCatalog();
        content = content with { Items = [content.Items[0], content.Items[0]] };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "item.duplicate");
    }

    [Fact]
    public void EmptyCriticalDomainIsFatal()
    {
        var content = CreateCatalog() with { Ammo = [] };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "domain.ammo.empty");
    }

    [Fact]
    public void QuestItemRequirementMustPointToRealObjective()
    {
        var content = CreateCatalog();
        content = content with
        {
            QuestItemRequirements =
            [
                new QuestItemRequirement("quest-a", "missing-objective", ["item-0"], 1, true),
            ],
        };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest-item.objective.missing");
    }

    [Fact]
    public void OtherObjectiveStillRejectsDanglingCanonicalItemReference()
    {
        var content = CreateCatalog();
        var regressionQuest = content.Quests[0] with { Id = RegressionQuestId };
        var regressionObjective = new QuestObjective(
            RegressionQuestId,
            RegressionObjectiveId,
            "specialCondition",
            "특수 아이템 조건",
            "Special item condition",
            false,
            1,
            false,
            Array.Empty<string>(),
            [RegressionSpecialDogtagId],
            null,
            QuestItemObjectiveKind.Other);

        content = content with
        {
            Quests = [regressionQuest],
            QuestObjectives = [regressionObjective],
            QuestItemRequirements =
            [
                new QuestItemRequirement(RegressionQuestId, RegressionObjectiveId, ["item-0"], 1, false),
            ],
        };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Code == "quest-objective.item.missing" &&
            issue.Message.Contains(RegressionSpecialDogtagId, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(QuestItemObjectiveKind.Submit, "giveItem")]
    [InlineData(QuestItemObjectiveKind.FindOrCollect, "findItem")]
    [InlineData(QuestItemObjectiveKind.Sell, "sellItem")]
    public void CanonicalItemObjectiveStillRejectsDanglingItemReference(
        QuestItemObjectiveKind itemKind,
        string type)
    {
        const string danglingItemId = "actual-dangling-canonical-item";
        var content = CreateCatalog();
        var objective = content.QuestObjectives[0] with
        {
            Type = type,
            ItemIds = [danglingItemId],
            ItemKind = itemKind,
        };

        content = content with
        {
            QuestObjectives = [objective],
            QuestItemRequirements = itemKind == QuestItemObjectiveKind.Submit
                ? [new QuestItemRequirement("quest-a", "objective-a", [danglingItemId], 1, true)]
                : content.QuestItemRequirements,
        };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Code == "quest-objective.item.missing" &&
            issue.Message.Contains(danglingItemId, StringComparison.Ordinal));
    }

    [Fact]
    public void OrdinaryQuestItemRequirementStillRejectsMissingCanonicalItem()
    {
        const string danglingItemId = "missing-material-item";
        var content = CreateCatalog() with
        {
            QuestItemRequirements =
            [
                new QuestItemRequirement("quest-a", "objective-a", [danglingItemId], 1, true),
            ],
        };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest-item.item.missing");
    }

    [Fact]
    public void InvalidItemWebUrlIsFatal()
    {
        var content = CreateCatalog();
        content = content with
        {
            Items = content.Items
                .Select((item, index) => index == 0 ? item with { WikiUrl = "not-a-web-url" } : item)
                .ToArray(),
        };

        var result = new GameContentIntegrityValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "item.wiki.invalid");
    }

    [Fact]
    public void SuspiciousDomainShrinkIsRejectedAgainstHealthyBaseline()
    {
        var baseline = CreateCatalog(itemCount: 10);
        var candidate = CreateCatalog(itemCount: 4);

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "update.items.suspicious-shrink");
    }

    [Fact]
    public void OrdinaryDomainChurnIsAcceptedAgainstHealthyBaseline()
    {
        var baseline = CreateCatalog(itemCount: 10);
        var candidate = CreateCatalog(itemCount: 8);

        var result = new ContentUpdateCompletenessGuard().Validate(candidate, baseline);

        Assert.True(result.IsValid);
    }

    private static GameContentCatalog CreateCatalog(int itemCount = 1)
    {
        var items = Enumerable.Range(0, Math.Max(1, itemCount))
            .Select(index => new GameItem(
                $"item-{index}",
                $"아이템 {index}",
                $"Item {index}",
                null,
                null,
                "https://example.com/icon.png",
                "https://example.com/wiki",
                Array.Empty<string>()))
            .ToArray();

        var quest = new QuestDefinition(
            "quest-a",
            "퀘스트 A",
            "Quest A",
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
            items,
            [new TraderDefinition("trader-a", "상인 A", "Trader A")],
            [new MapReference("map-a", "맵 A", "Map A", "map-a")],
            [quest],
            [
                new QuestObjective(
                    "quest-a",
                    "objective-a",
                    "giveItem",
                    "아이템 제출",
                    "Hand over item",
                    false,
                    1,
                    true,
                    ["map-a"],
                    ["item-0"],
                    null,
                    QuestItemObjectiveKind.Submit),
            ],
            [new QuestItemRequirement("quest-a", "objective-a", ["item-0"], 1, true)],
            [
                new HideoutStation(
                    "station-a",
                    "시설 A",
                    "Station A",
                    "https://example.com/station.png",
                    [
                        new HideoutLevel(
                            "station-a",
                            1,
                            10,
                            [new HideoutItemRequirement("station-a", 1, "item-0", 1, false)]),
                    ]),
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
