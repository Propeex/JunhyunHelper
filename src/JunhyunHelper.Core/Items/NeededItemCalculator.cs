namespace JunhyunHelper.Core.Items;

public static class NeededItemCalculator
{
    public static IReadOnlyList<NeededItem> Calculate(
        IEnumerable<ItemRequirement> requirements,
        IReadOnlyDictionary<string, InventoryQuantity>? inventory = null)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        inventory ??= new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);

        return requirements
            .Select(static requirement => requirement.Normalize())
            .Where(static requirement =>
                !string.IsNullOrWhiteSpace(requirement.ItemId) && requirement.RequiredTotal > 0)
            .GroupBy(static requirement => requirement.ItemId, StringComparer.Ordinal)
            .Select(group => BuildNeededItem(group.Key, group, inventory))
            .OrderBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private static NeededItem BuildNeededItem(
        string itemId,
        IEnumerable<ItemRequirement> requirements,
        IReadOnlyDictionary<string, InventoryQuantity> inventory)
    {
        var requirementList = requirements.ToArray();
        var requiredTotal = requirementList.Sum(static requirement => requirement.RequiredTotal);
        var requiredFir = requirementList.Sum(static requirement => requirement.RequiredFir);

        var owned = inventory.TryGetValue(itemId, out var quantity)
            ? quantity.Normalize()
            : InventoryQuantity.Empty;

        var firSatisfied = Math.Min(owned.Fir, requiredFir);
        var remainingFir = requiredFir - firSatisfied;

        var unrestrictedRequired = requiredTotal - requiredFir;
        var unrestrictedAvailable = owned.NonFir + Math.Max(0, owned.Fir - requiredFir);
        var unrestrictedSatisfied = Math.Min(unrestrictedRequired, unrestrictedAvailable);
        var remainingUnrestricted = unrestrictedRequired - unrestrictedSatisfied;

        return new NeededItem(
            itemId,
            requiredTotal,
            requiredFir,
            owned.Fir,
            owned.NonFir,
            remainingFir + remainingUnrestricted,
            remainingFir,
            requirementList.Select(static requirement => requirement.Source).ToArray());
    }
}
