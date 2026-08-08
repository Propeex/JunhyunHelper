using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class InventoryCleanupChangeDetectorTests
{
    [Fact]
    public void NewlySurplusInventory_IsReported()
    {
        var previous = Plan(Cleanup("wire", 0, 2));
        var current = Plan(Cleanup("wire", 0, 8));

        var change = Assert.Single(InventoryCleanupChangeDetector.FindIncreases(previous, current));

        Assert.Equal("wire", change.ItemId);
        Assert.Equal(6, change.IncreasedBy);
        Assert.Equal(8, change.CurrentSurplusTotal);
    }

    [Fact]
    public void DecreasedOrUnchangedSurplus_IsNotReported()
    {
        var previous = Plan(Cleanup("wire", 0, 8));
        var current = Plan(Cleanup("wire", 0, 4));

        Assert.Empty(InventoryCleanupChangeDetector.FindIncreases(previous, current));
    }

    private static FutureNeededItemsPlan Plan(params InventorySurplusItem[] cleanup) =>
        new(
            Array.Empty<NeededItem>(),
            cleanup,
            Array.Empty<QuestItemRequirement>(),
            Array.Empty<CleanupProtection>(),
            Array.Empty<string>(),
            new Dictionary<string, QuestFutureReachabilityResult>(StringComparer.Ordinal));

    private static InventorySurplusItem Cleanup(string itemId, int fir, int nonFir) =>
        new(
            itemId,
            0,
            0,
            fir,
            nonFir,
            fir,
            nonFir,
            Array.Empty<ItemRequirementSource>());
}
