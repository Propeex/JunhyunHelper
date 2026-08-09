namespace JunhyunHelper.Core.Items;

public sealed record InventoryConsumption(
    IReadOnlyDictionary<string, InventoryQuantity> Items)
{
    public static InventoryConsumption Empty { get; } =
        new(new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal));

    public bool IsEmpty => Items.Count == 0 || Items.Values.All(quantity => quantity.Total <= 0);
}
