using JunhyunHelper.Application.Items;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Application;

public sealed class FixedInventoryConsumptionPolicyTests
{
    [Fact]
    public void GeneralRequirementUsesGeneralBeforeInRaid()
    {
        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["item"] = new(Fir: 3, NonFir: 2),
        };

        var result = FixedInventoryConsumptionPolicy.Consume(
            inventory,
            [new FixedItemConsumptionRequirement("item", Count: 4, FoundInRaid: false)]);

        Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 0), result.Inventory["item"]);
        Assert.Equal(new InventoryQuantity(Fir: 2, NonFir: 2), result.Consumption.Items["item"]);
    }

    [Fact]
    public void InRaidRequirementNeverConsumesGeneralInventory()
    {
        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["item"] = new(Fir: 1, NonFir: 5),
        };

        var result = FixedInventoryConsumptionPolicy.Consume(
            inventory,
            [new FixedItemConsumptionRequirement("item", Count: 3, FoundInRaid: true)]);

        Assert.Equal(new InventoryQuantity(Fir: 0, NonFir: 5), result.Inventory["item"]);
        Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 0), result.Consumption.Items["item"]);
    }

    [Fact]
    public void ConsumptionNeverCreatesNegativeInventoryAndRestoreIsExact()
    {
        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            ["item"] = new(Fir: 1, NonFir: 1),
        };

        var consumed = FixedInventoryConsumptionPolicy.Consume(
            inventory,
            [new FixedItemConsumptionRequirement("item", Count: 10, FoundInRaid: false)]);

        Assert.DoesNotContain("item", consumed.Inventory.Keys);
        Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 1), consumed.Consumption.Items["item"]);

        var restored = FixedInventoryConsumptionPolicy.Restore(consumed.Inventory, consumed.Consumption);
        Assert.Equal(new InventoryQuantity(Fir: 1, NonFir: 1), restored["item"]);
    }
}
