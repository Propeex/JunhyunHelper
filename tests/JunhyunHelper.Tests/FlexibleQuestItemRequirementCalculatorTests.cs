using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class FlexibleQuestItemRequirementCalculatorTests
{
    [Fact]
    public void CombinesAcceptedItemInventoryIntoOneObjectiveProgress()
    {
        var requirement = Requirement(["a", "b", "c"], count: 5, foundInRaid: false);
        var inventory = Inventory(
            ("a", new InventoryQuantity(Fir: 1, NonFir: 1)),
            ("b", new InventoryQuantity(Fir: 0, NonFir: 1)),
            ("other", new InventoryQuantity(Fir: 0, NonFir: 99)));

        var progress = FlexibleQuestItemRequirementCalculator.Calculate(requirement, inventory);

        Assert.Equal(5, progress.RequiredTotal);
        Assert.Equal(3, progress.OwnedTotal);
        Assert.Equal(2, progress.RemainingTotal);
        Assert.Equal(0, progress.RemainingFir);
    }

    [Fact]
    public void FoundInRaidRequirementDoesNotUseNonFirAlternatives()
    {
        var requirement = Requirement(["a", "b"], count: 3, foundInRaid: true);
        var inventory = Inventory(
            ("a", new InventoryQuantity(Fir: 2, NonFir: 0)),
            ("b", new InventoryQuantity(Fir: 0, NonFir: 10)));

        var progress = FlexibleQuestItemRequirementCalculator.Calculate(requirement, inventory);

        Assert.Equal(3, progress.RequiredFir);
        Assert.Equal(1, progress.RemainingTotal);
        Assert.Equal(1, progress.RemainingFir);
    }

    [Fact]
    public void ExcessAcrossFlexibleCandidatesOnlyMarksGroupFulfilled()
    {
        var requirement = Requirement(["a", "b"], count: 5, foundInRaid: false);
        var inventory = Inventory(
            ("a", new InventoryQuantity(Fir: 0, NonFir: 5)),
            ("b", new InventoryQuantity(Fir: 0, NonFir: 5)));

        var progress = FlexibleQuestItemRequirementCalculator.Calculate(requirement, inventory);

        Assert.True(progress.IsFulfilled);
        Assert.Equal(0, progress.RemainingTotal);
        Assert.Equal(10, progress.OwnedTotal);
    }

    [Fact]
    public void RejectsSingleItemRequirementBecauseItIsNotFlexible()
    {
        var requirement = Requirement(["a"], count: 1, foundInRaid: false);

        Assert.Throws<InvalidDataException>(() =>
            FlexibleQuestItemRequirementCalculator.Calculate(requirement));
    }

    private static QuestItemRequirement Requirement(
        IReadOnlyList<string> acceptedItemIds,
        int count,
        bool foundInRaid) =>
        new("quest", "objective", acceptedItemIds, count, foundInRaid);

    private static IReadOnlyDictionary<string, InventoryQuantity> Inventory(
        params (string ItemId, InventoryQuantity Quantity)[] items) =>
        items.ToDictionary(item => item.ItemId, item => item.Quantity, StringComparer.Ordinal);
}
