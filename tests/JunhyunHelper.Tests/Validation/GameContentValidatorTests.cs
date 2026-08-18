using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Validation;
using Xunit;

namespace JunhyunHelper.Tests.Validation;

public sealed class GameContentValidatorTests
{
    [Fact]
    public void ValidConnectedContentPasses()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "quest-prereq");

        var result = new GameContentValidator().Validate(content);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void MissingNeededItemReferenceIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "missing-item", prerequisiteId: "quest-prereq");

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Severity == ContentValidationSeverity.Fatal &&
            issue.Code == "quest-item.item.missing");
    }

    [Fact]
    public void EmptyAcceptedQuestItemSetIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "quest-prereq") with
        {
            QuestItemRequirements =
            [
                new QuestItemRequirement(
                    "quest-a",
                    "objective-a",
                    Array.Empty<string>(),
                    2,
                    true),
            ],
        };

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest-item.items-empty");
    }

    [Fact]
    public void NonPositiveQuestItemCountIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "quest-prereq") with
        {
            QuestItemRequirements =
            [
                new QuestItemRequirement(
                    "quest-a",
                    "objective-a",
                    new[] { "item-a" },
                    0,
                    true),
            ],
        };

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest-item.count.nonpositive");
    }

    [Fact]
    public void NonPositiveHideoutItemCountIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "quest-prereq");
        var station = content.HideoutStations.Single() with
        {
            Levels =
            [
                new HideoutLevel(
                    "station-a",
                    1,
                    null,
                    new[]
                    {
                        new HideoutItemRequirement(
                            "station-a",
                            1,
                            "item-a",
                            0,
                            false),
                    }),
            ],
        };
        content = content with { HideoutStations = [station] };

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "hideout-item.count.nonpositive");
    }

    [Fact]
    public void MissingPrerequisiteQuestIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "missing-quest");

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "quest.prerequisite.missing");
    }

    [Fact]
    public void MissingQuestFailureTriggerIsFatal()
    {
        var content = CreateCatalog(itemRequirementId: "item-a", prerequisiteId: "quest-prereq");
        var quest = content.Quests.Single(candidate => candidate.Id == "quest-a") with
        {
            CompletionFailureConditionData =
                [new QuestCompletionFailureCondition("missing-trigger")],
        };
        content = content with
        {
            Quests = content.Quests
                .Select(candidate => candidate.Id == quest.Id ? quest : candidate)
                .ToArray(),
        };

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Severity == ContentValidationSeverity.Fatal &&
            issue.Code == "quest.failure-trigger.missing");
    }

    [Fact]
    public void ConflictingEditionQuestRuleIsFatal()
    {
        var content = CreateCatalog(
            "item-a",
            "quest-prereq",
            [
                new EditionDefinition(
                    "edition-a",
                    "Edition A",
                    new HashSet<string>(StringComparer.Ordinal) { "quest-a" },
                    new HashSet<string>(StringComparer.Ordinal) { "quest-a" }),
            ]);

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Severity == ContentValidationSeverity.Fatal &&
            issue.Code == "edition.quest-rule.conflict");
    }

    [Fact]
    public void EditionQuestMissingFromThisModeIsWarningNotFatal()
    {
        var content = CreateCatalog(
            "item-a",
            "quest-prereq",
            [
                new EditionDefinition(
                    "edition-a",
                    "Edition A",
                    new HashSet<string>(StringComparer.Ordinal) { "quest-other-mode" },
                    new HashSet<string>(StringComparer.Ordinal)),
            ]);

        var result = new GameContentValidator().Validate(content);

        Assert.True(result.IsValid);
        Assert.Contains(result.Issues, issue =>
            issue.Severity == ContentValidationSeverity.Warning &&
            issue.Code == "edition.quest.missing-in-mode");
    }

    [Fact]
    public void DuplicateEditionIdIsFatalEvenIfSnapshotWasConstructedDirectly()
    {
        var content = CreateCatalog(
            "item-a",
            "quest-prereq",
            [
                new EditionDefinition(
                    "edition-a",
                    "Edition A",
                    new HashSet<string>(StringComparer.Ordinal),
                    new HashSet<string>(StringComparer.Ordinal)),
                new EditionDefinition(
                    "edition-a",
                    "Edition A duplicate",
                    new HashSet<string>(StringComparer.Ordinal),
                    new HashSet<string>(StringComparer.Ordinal)),
            ]);

        var result = new GameContentValidator().Validate(content);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "edition.duplicate");
    }

    private static GameContentCatalog CreateCatalog(
        string itemRequirementId,
        string prerequisiteId,
        IReadOnlyList<EditionDefinition>? editions = null)
    {
        var quest = new QuestDefinition(
            "quest-a",
            "퀘스트 A",
            "Quest A",
            "trader-a",
            "map-a",
            null,
            null,
            false,
            false,
            false,
            1,
            null,
            null,
            new[]
            {
                new QuestTaskRequirement(
                    prerequisiteId,
                    new HashSet<QuestRequiredStatus> { QuestRequiredStatus.Complete }),
            },
            Array.Empty<QuestTraderStandingRequirement>(),
            Array.Empty<QuestTraderLoyaltyRequirement>());

        return new GameContentCatalog(
            new[]
            {
                new GameItem(
                    "item-a",
                    "아이템 A",
                    "Item A",
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<string>()),
            },
            new[] { new TraderDefinition("trader-a", "상인 A", "Trader A") },
            new[] { new MapReference("map-a", "맵 A", "Map A", "map-a") },
            new[]
            {
                quest,
                new QuestDefinition(
                    "quest-prereq",
                    "선행 퀘스트",
                    "Prerequisite Quest",
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    false,
                    1,
                    null,
                    null,
                    Array.Empty<QuestTaskRequirement>(),
                    Array.Empty<QuestTraderStandingRequirement>(),
                    Array.Empty<QuestTraderLoyaltyRequirement>()),
            },
            Array.Empty<QuestObjective>(),
            new[]
            {
                new QuestItemRequirement(
                    "quest-a",
                    "objective-a",
                    new[] { itemRequirementId },
                    2,
                    true),
            },
            new[]
            {
                new HideoutStation(
                    "station-a",
                    "시설 A",
                    "Station A",
                    null,
                    new[]
                    {
                        new HideoutLevel(
                            "station-a",
                            1,
                            null,
                            new[]
                            {
                                new HideoutItemRequirement(
                                    "station-a",
                                    1,
                                    "item-a",
                                    3,
                                    false),
                            }),
                    }),
            },
            Ammo: [],
            EditionData: editions ?? []);
    }
}
