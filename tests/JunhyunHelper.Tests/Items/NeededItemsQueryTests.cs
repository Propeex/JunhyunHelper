using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Items;

public sealed class NeededItemsQueryTests
{
    [Fact]
    public void CalculatesOnlyFixedRequirementsAndReturnsAlternativesSeparately()
    {
        var result = NeededItemsQuery.Calculate(
            [
                new QuestItemRequirement("quest-a", "fixed", ["item-a"], 3, true),
                new QuestItemRequirement("quest-b", "choice", ["item-b", "item-c"], 2, false),
            ],
            [new HideoutItemRequirement("workbench", 2, "item-a", 4, false)],
            new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
            {
                ["item-a"] = new(Fir: 2, NonFir: 1),
            });

        var fixedItem = Assert.Single(result.FixedItems);
        Assert.Equal("item-a", fixedItem.ItemId);
        Assert.Equal(7, fixedItem.RequiredTotal);
        Assert.Equal(3, fixedItem.RequiredFir);
        Assert.Equal(4, fixedItem.RemainingTotal);
        Assert.Equal(1, fixedItem.RemainingFir);

        var alternative = Assert.Single(result.AlternativeQuestRequirements);
        Assert.Equal("choice", alternative.ObjectiveId);
        Assert.Equal(new[] { "item-b", "item-c" }, alternative.AcceptedItemIds);
    }
}
