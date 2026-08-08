namespace JunhyunHelper.Core.Items;

public readonly record struct InventoryQuantity(int Fir, int NonFir)
{
    public int Total => Fir + NonFir;

    public static InventoryQuantity Empty => new(0, 0);

    public InventoryQuantity Normalize() => new(
        Math.Max(0, Fir),
        Math.Max(0, NonFir));
}
