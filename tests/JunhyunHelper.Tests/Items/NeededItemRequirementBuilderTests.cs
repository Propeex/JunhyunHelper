using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Items;

public sealed class NeededItemRequirementBuilderTests
{
    [Fact]
    public void SingleAcceptedQuestItemBecomesFixedRequirement()
    {
        var result = NeededItemRequirementBuilder.Build(
            [new QuestItemRequirement("quest-a", "objective-a", ["item-a"], 3, true)],
            []);

        var requirement = Assert.Single(result.FixedRequirements);
        Assert.Equal("item-a", requirement.ItemId);
        Assert.Equal(3, requirement.RequiredTotal);
        Assert.Equal(3, requirement.RequiredFir);
        Assert.Equal(ItemRequirementSourceKind.Quest, requirement.Source.Kind);
        Assert.Empty(result.AlternativeQuestRequirements);
    }

    [Fact]
    public void AlternativeQuestItemsAreNotArbitrarilyAssignedToOneItem()
    {
        var source = new QuestItemRequirement(
            "quest-a",
            "objective-a",
            ["item-a", "item-b"],
            2,
            false);

        var result = NeededItemRequirementBuilder.Build([source], []);

        Assert.Empty(result.FixedRequirements);
        Assert.Same(source, Assert.Single(result.AlternativeQuestRequirements));
    }

    [Fact]
    public void HideoutRequirementBecomesFixedRequirementWithoutChoosingScope()
    {
        var result = NeededItemRequirementBuilder.Build(
            [],
            [new HideoutItemRequirement("workbench", 3, "item-a", 5, false)]);

        var requirement = Assert.Single(result.FixedRequirements);
        Assert.Equal("item-a", requirement.ItemId);
        Assert.Equal(5, requirement.RequiredTotal);
        Assert.Equal(0, requirement.RequiredFir);
        Assert.Equal(ItemRequirementSourceKind.Hideout, requirement.Source.Kind);
        Assert.Equal("workbench", requirement.Source.SourceId);
        Assert.Equal("3", requirement.Source.DetailId);
    }

    [Fact]
    public void InvalidRequirementIsRejectedInsteadOfNormalizedSilently()
    {
        Assert.Throws<InvalidDataException>(() => NeededItemRequirementBuilder.Build(
            [new QuestItemRequirement("quest-a", "objective-a", ["item-a"], 0, false)],
            []));
    }
}
