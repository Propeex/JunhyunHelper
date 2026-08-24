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
        var content = CreateCatalog() with { Ammunition = [] };

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
