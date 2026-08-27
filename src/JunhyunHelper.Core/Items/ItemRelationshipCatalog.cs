using System.Text.Json.Serialization;

namespace JunhyunHelper.Core.Items;

/// <summary>
/// Canonical item-to-item commerce/crafting relationships built only by the normal
/// Game Content update pipeline. Search and Scanner presentation consume this local
/// snapshot and never perform relationship network I/O.
/// </summary>
public sealed record ItemRelationshipCatalog(
    IReadOnlyList<ItemTraderPurchase> TraderPurchases,
    IReadOnlyList<ItemBarter> Barters,
    IReadOnlyList<ItemCraft> Crafts,
    IReadOnlyList<string> FleaMarketItemIds)
{
    public static ItemRelationshipCatalog Empty { get; } = new(
        Array.Empty<ItemTraderPurchase>(),
        Array.Empty<ItemBarter>(),
        Array.Empty<ItemCraft>(),
        Array.Empty<string>());

    [JsonIgnore]
    public IReadOnlySet<string> FleaMarketItems =>
        FleaMarketItemIds.ToHashSet(StringComparer.Ordinal);
}

public sealed record ItemIngredient(
    string ItemId,
    decimal Count,
    bool IsTool = false);

public sealed record ItemTraderPurchase(
    string ItemId,
    string TraderId,
    int RequiredLevel,
    string? TaskUnlockQuestId = null);

public sealed record ItemBarter(
    string Id,
    string TraderId,
    int RequiredLevel,
    string ProductItemId,
    decimal ProductCount,
    IReadOnlyList<ItemIngredient> RequiredItems,
    string? TaskUnlockQuestId = null);

public sealed record ItemCraft(
    string Id,
    string StationId,
    int RequiredLevel,
    string ProductItemId,
    decimal ProductCount,
    IReadOnlyList<ItemIngredient> RequiredItems,
    string? TaskUnlockQuestId = null);

public sealed record ItemRelationshipSnapshot(
    IReadOnlyList<ItemCraft> CraftsUsingItem,
    IReadOnlyList<ItemBarter> BartersUsingItem,
    IReadOnlyList<ItemTraderPurchase> TraderPurchasesForItem,
    IReadOnlyList<ItemCraft> CraftsForItem,
    IReadOnlyList<ItemBarter> BartersForItem,
    bool FleaMarketAvailable);

public static class ItemRelationshipQuery
{
    public static ItemRelationshipSnapshot ForItem(
        ItemRelationshipCatalog? catalog,
        string? itemId)
    {
        var relationships = catalog ?? ItemRelationshipCatalog.Empty;
        var id = itemId?.Trim() ?? string.Empty;
        if (id.Length == 0)
        {
            return new ItemRelationshipSnapshot([], [], [], [], [], false);
        }

        return new ItemRelationshipSnapshot(
            relationships.Crafts
                .Where(craft => craft.RequiredItems.Any(required =>
                    string.Equals(required.ItemId, id, StringComparison.Ordinal)))
                .OrderBy(craft => craft.StationId, StringComparer.Ordinal)
                .ThenBy(craft => craft.RequiredLevel)
                .ThenBy(craft => craft.ProductItemId, StringComparer.Ordinal)
                .ThenBy(craft => craft.Id, StringComparer.Ordinal)
                .ToArray(),
            relationships.Barters
                .Where(barter => barter.RequiredItems.Any(required =>
                    string.Equals(required.ItemId, id, StringComparison.Ordinal)))
                .OrderBy(barter => barter.TraderId, StringComparer.Ordinal)
                .ThenBy(barter => barter.RequiredLevel)
                .ThenBy(barter => barter.ProductItemId, StringComparer.Ordinal)
                .ThenBy(barter => barter.Id, StringComparer.Ordinal)
                .ToArray(),
            relationships.TraderPurchases
                .Where(purchase => string.Equals(purchase.ItemId, id, StringComparison.Ordinal))
                .OrderBy(purchase => purchase.TraderId, StringComparer.Ordinal)
                .ThenBy(purchase => purchase.RequiredLevel)
                .ToArray(),
            relationships.Crafts
                .Where(craft => string.Equals(craft.ProductItemId, id, StringComparison.Ordinal))
                .OrderBy(craft => craft.StationId, StringComparer.Ordinal)
                .ThenBy(craft => craft.RequiredLevel)
                .ThenBy(craft => craft.Id, StringComparer.Ordinal)
                .ToArray(),
            relationships.Barters
                .Where(barter => string.Equals(barter.ProductItemId, id, StringComparison.Ordinal))
                .OrderBy(barter => barter.TraderId, StringComparer.Ordinal)
                .ThenBy(barter => barter.RequiredLevel)
                .ThenBy(barter => barter.Id, StringComparer.Ordinal)
                .ToArray(),
            relationships.FleaMarketItemIds.Contains(id, StringComparer.Ordinal));
    }
}
