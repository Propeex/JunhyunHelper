using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Items whose instance quantity cannot be inferred from the Scanner-confirmed item ID.
/// Ammo is identified from canonical Tarkov type data. Currency uses the three stable
/// canonical Tarkov item IDs rather than localized names.
/// </summary>
public static class FarmingGuideStackQuantityPolicy
{
    private static readonly HashSet<string> CurrencyItemIds = new(StringComparer.Ordinal)
    {
        "5449016a4bdc2d6f028b456f", // Roubles
        "5696686a4bdc2da3298b456a", // Dollars
        "569668774bdc2da2298b4568", // Euros
    };

    public static bool RequiresQuantity(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return IsAmmo(item) || IsCurrency(item);
    }

    public static bool IsCurrency(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return CurrencyItemIds.Contains(item.Id);
    }

    public static bool IsAmmo(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Types.Any(type => string.Equals(type, "ammo", StringComparison.OrdinalIgnoreCase)) ||
               string.Equals(item.FarmingGuideData?.PropertiesType, "ItemPropertiesAmmo", StringComparison.Ordinal);
    }

    public static int NormalizeQuantity(int quantity) => Math.Max(1, quantity);

    /// <summary>
    /// Normalizes a modeled single-stack quantity against the source-backed Tarkov maximum
    /// when that fact is available. Older content snapshots may legitimately have no maximum;
    /// in that case the positive user-confirmed quantity is preserved rather than inventing a
    /// limit. Current ammo data exposes stackMaxSize through ItemPropertiesAmmo.
    /// </summary>
    public static int NormalizeQuantity(GameItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        var normalized = NormalizeQuantity(quantity);
        return item.StackMaxSize is > 0
            ? Math.Min(normalized, item.StackMaxSize.Value)
            : normalized;
    }

    public static bool IsQuantityWithinKnownStackLimit(GameItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (quantity <= 0)
            return false;
        return item.StackMaxSize is not > 0 || quantity <= item.StackMaxSize.Value;
    }
}
