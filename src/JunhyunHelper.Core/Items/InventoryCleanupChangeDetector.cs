namespace JunhyunHelper.Core.Items;

public sealed record InventoryCleanupIncrease(
    string ItemId,
    int PreviousSurplusFir,
    int PreviousSurplusNonFir,
    int CurrentSurplusFir,
    int CurrentSurplusNonFir)
{
    public int PreviousSurplusTotal => PreviousSurplusFir + PreviousSurplusNonFir;

    public int CurrentSurplusTotal => CurrentSurplusFir + CurrentSurplusNonFir;

    public int IncreasedBy => CurrentSurplusTotal - PreviousSurplusTotal;
}

public static class InventoryCleanupChangeDetector
{
    public static IReadOnlyList<InventoryCleanupIncrease> FindIncreases(
        FutureNeededItemsPlan previous,
        FutureNeededItemsPlan current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var previousById = previous.CleanupItems.ToDictionary(
            static item => item.ItemId,
            StringComparer.Ordinal);

        return current.CleanupItems
            .Select(item =>
            {
                previousById.TryGetValue(item.ItemId, out var old);
                return new InventoryCleanupIncrease(
                    item.ItemId,
                    old?.SurplusFir ?? 0,
                    old?.SurplusNonFir ?? 0,
                    item.SurplusFir,
                    item.SurplusNonFir);
            })
            .Where(static change => change.IncreasedBy > 0)
            .OrderByDescending(static change => change.IncreasedBy)
            .ThenBy(static change => change.ItemId, StringComparer.Ordinal)
            .ToArray();
    }
}
