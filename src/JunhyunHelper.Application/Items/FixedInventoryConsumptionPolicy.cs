using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Application.Items;

public sealed record FixedItemConsumptionRequirement(
    string ItemId,
    int Count,
    bool FoundInRaid);

public sealed record InventoryConsumptionResult(
    IReadOnlyDictionary<string, InventoryQuantity> Inventory,
    InventoryConsumption Consumption);

public static class FixedInventoryConsumptionPolicy
{
    public static InventoryConsumptionResult Consume(
        IReadOnlyDictionary<string, InventoryQuantity> inventory,
        IEnumerable<FixedItemConsumptionRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(requirements);

        var updated = inventory
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value.Normalize().Total > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value.Normalize(), StringComparer.Ordinal);
        var consumed = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);

        foreach (var requirement in requirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.ItemId) || requirement.Count <= 0)
                continue;

            var itemId = requirement.ItemId.Trim();
            updated.TryGetValue(itemId, out var quantity);
            quantity = quantity.Normalize();

            int usedFir;
            int usedNonFir;
            if (requirement.FoundInRaid)
            {
                usedFir = Math.Min(quantity.Fir, requirement.Count);
                usedNonFir = 0;
            }
            else
            {
                usedNonFir = Math.Min(quantity.NonFir, requirement.Count);
                var remaining = requirement.Count - usedNonFir;
                usedFir = Math.Min(quantity.Fir, remaining);
            }

            if (usedFir == 0 && usedNonFir == 0)
                continue;

            var next = new InventoryQuantity(
                quantity.Fir - usedFir,
                quantity.NonFir - usedNonFir);
            if (next.Total == 0)
                updated.Remove(itemId);
            else
                updated[itemId] = next;

            consumed.TryGetValue(itemId, out var alreadyConsumed);
            consumed[itemId] = new InventoryQuantity(
                alreadyConsumed.Fir + usedFir,
                alreadyConsumed.NonFir + usedNonFir);
        }

        return new InventoryConsumptionResult(
            updated,
            new InventoryConsumption(consumed));
    }

    public static IReadOnlyDictionary<string, InventoryQuantity> Restore(
        IReadOnlyDictionary<string, InventoryQuantity> inventory,
        InventoryConsumption consumption)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(consumption);

        var updated = inventory
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value.Normalize().Total > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value.Normalize(), StringComparer.Ordinal);

        foreach (var (itemId, rawQuantity) in consumption.Items)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                continue;

            var restored = rawQuantity.Normalize();
            if (restored.Total == 0)
                continue;

            updated.TryGetValue(itemId, out var currentRaw);
            var current = currentRaw.Normalize();
            updated[itemId] = new InventoryQuantity(
                checked(current.Fir + restored.Fir),
                checked(current.NonFir + restored.NonFir));
        }

        return updated;
    }
}
