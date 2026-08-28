using System.Text.Json;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Infrastructure.TarkovJson.Items;

/// <summary>
/// Imports the item commerce/crafting graph used by Scanner item search. This importer
/// consumes the same already-downloaded Items/Barters/Crafts documents as the normal
/// Game Content build and performs no independent network access.
/// </summary>
public sealed class TarkovItemRelationshipImporter
{
    public ItemRelationshipCatalog Import(
        TarkovJsonDocument itemsDocument,
        TarkovJsonDocument bartersDocument,
        TarkovJsonDocument craftsDocument)
    {
        ArgumentNullException.ThrowIfNull(itemsDocument);
        ArgumentNullException.ThrowIfNull(bartersDocument);
        ArgumentNullException.ThrowIfNull(craftsDocument);

        var purchases = new List<ItemTraderPurchase>();
        var fleaItems = new List<string>();

        foreach (var item in TarkovJsonReader.ReadCollection(itemsDocument.Data, "items"))
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Item relationship entries must be objects.");

            var itemId = TarkovJsonReader.RequiredString(item, "id", "Item relationship");
            if (IsFleaMarketAvailable(item))
                fleaItems.Add(itemId);

            if (!item.TryGetProperty("buyFromTrader", out var rawOffers) ||
                rawOffers.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            foreach (var rawOffer in TarkovJsonReader.ReadCollectionValue(rawOffers, $"item {itemId} trader purchases"))
            {
                purchases.Add(new ItemTraderPurchase(
                    itemId,
                    RequiredReference(rawOffer, "trader", $"Item '{itemId}' trader purchase"),
                    RequiredNonNegativeInt(rawOffer, "minTraderLevel", itemId),
                    OptionalReference(rawOffer, "taskUnlock"),
                    RequiredPositiveDecimal(rawOffer, "price", $"Item '{itemId}' trader purchase"),
                    RequiredReference(rawOffer, "currencyItem", $"Item '{itemId}' trader purchase"),
                    TarkovJsonReader.OptionalString(rawOffer, "currency"),
                    OptionalNonNegativeInt(rawOffer, "buyLimit", $"Item '{itemId}' trader purchase")));
            }
        }

        return new ItemRelationshipCatalog(
            purchases.OrderBy(value => value.ItemId, StringComparer.Ordinal)
                .ThenBy(value => value.TraderId, StringComparer.Ordinal)
                .ThenBy(value => value.RequiredLevel).ToArray(),
            ReadBarters(bartersDocument.Data),
            ReadCrafts(craftsDocument.Data),
            fleaItems.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }

    private static IReadOnlyList<ItemBarter> ReadBarters(JsonElement data)
    {
        var result = new List<ItemBarter>();
        foreach (var raw in TarkovJsonReader.ReadCollectionValue(data, "barters"))
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Barter entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Barter");
            var offered = RequiredObject(raw, "offeredItem", $"Barter '{id}'");
            var requirements = ReadRequirements(raw, "requiredItems", $"Barter '{id}'", allowTool: false);
            if (requirements.Count == 0)
                throw new InvalidDataException($"Barter '{id}' has no required items.");

            result.Add(new ItemBarter(
                id,
                RequiredReference(raw, "trader", $"Barter '{id}'"),
                RequiredNonNegativeInt(raw, "minTraderLevel", id),
                RequiredReference(offered, "item", $"Barter '{id}' offered item"),
                RequiredPositiveDecimal(offered, "count", $"Barter '{id}' offered item"),
                requirements,
                OptionalReference(raw, "taskUnlock"),
                OptionalNonNegativeInt(raw, "buyLimit", $"Barter '{id}'")));
        }

        return result.OrderBy(value => value.TraderId, StringComparer.Ordinal)
            .ThenBy(value => value.RequiredLevel)
            .ThenBy(value => value.ProductItemId, StringComparer.Ordinal)
            .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ItemCraft> ReadCrafts(JsonElement data)
    {
        var result = new List<ItemCraft>();
        foreach (var raw in TarkovJsonReader.ReadCollectionValue(data, "crafts"))
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Craft entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Craft");
            var product = RequiredObject(raw, "productItem", $"Craft '{id}'");
            var requirements = ReadRequirements(raw, "requiredItems", $"Craft '{id}'", allowTool: true);
            if (requirements.Count == 0)
                throw new InvalidDataException($"Craft '{id}' has no required items.");

            result.Add(new ItemCraft(
                id,
                RequiredReference(raw, "station", $"Craft '{id}'"),
                RequiredNonNegativeInt(raw, "level", id),
                RequiredReference(product, "item", $"Craft '{id}' product item"),
                RequiredPositiveDecimal(product, "count", $"Craft '{id}' product item"),
                requirements,
                OptionalReference(raw, "taskUnlock"),
                RequiredNonNegativeInt(raw, "duration", id)));
        }

        return result.OrderBy(value => value.StationId, StringComparer.Ordinal)
            .ThenBy(value => value.RequiredLevel)
            .ThenBy(value => value.ProductItemId, StringComparer.Ordinal)
            .ThenBy(value => value.Id, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ItemIngredient> ReadRequirements(
        JsonElement parent, string propertyName, string entityName, bool allowTool)
    {
        if (!parent.TryGetProperty(propertyName, out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<ItemIngredient>();
        }

        return TarkovJsonReader.ReadCollectionValue(rawRequirements, $"{entityName} {propertyName}")
            .Select(raw =>
            {
                var isTool = false;
                if (allowTool && raw.TryGetProperty("attributes", out var attributes) &&
                    attributes.ValueKind == JsonValueKind.Object)
                {
                    isTool = TarkovJsonReader.OptionalBool(attributes, "tool") ?? false;
                }

                return new ItemIngredient(
                    RequiredReference(raw, "item", entityName),
                    RequiredPositiveDecimal(raw, "count", entityName),
                    isTool);
            }).ToArray();
    }

    private static bool IsFleaMarketAvailable(JsonElement item)
    {
        if (item.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array)
        {
            foreach (var type in types.EnumerateArray())
            {
                if (type.ValueKind == JsonValueKind.String &&
                    string.Equals(type.GetString(), "noFlea", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        return (TarkovJsonReader.OptionalInt(item, "lastLowPrice") ?? 0) > 0 ||
               (TarkovJsonReader.OptionalInt(item, "avg24hPrice") ?? 0) > 0;
    }

    private static JsonElement RequiredObject(JsonElement parent, string propertyName, string entityName)
    {
        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.Object)
            return value;
        throw new InvalidDataException($"{entityName} is missing object '{propertyName}'.");
    }

    private static string RequiredReference(JsonElement parent, string propertyName, string entityName)
    {
        if (!parent.TryGetProperty(propertyName, out var raw))
            throw new InvalidDataException($"{entityName} is missing reference '{propertyName}'.");
        var id = TarkovJsonReader.ReferenceId(raw);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"{entityName} has invalid reference '{propertyName}'.");
        return id;
    }

    private static string? OptionalReference(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var raw) || raw.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        var id = TarkovJsonReader.ReferenceId(raw);
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"Invalid optional reference '{propertyName}'.");
        return id;
    }

    private static int RequiredNonNegativeInt(JsonElement parent, string propertyName, string entityName)
    {
        var value = TarkovJsonReader.RequiredInt(parent, propertyName, entityName);
        if (value < 0)
            throw new InvalidDataException($"{entityName} has negative '{propertyName}'.");
        return value;
    }

    private static int? OptionalNonNegativeInt(JsonElement parent, string propertyName, string entityName)
    {
        var value = TarkovJsonReader.OptionalInt(parent, propertyName);
        if (value < 0)
            throw new InvalidDataException($"{entityName} has negative '{propertyName}'.");
        return value;
    }

    private static decimal RequiredPositiveDecimal(JsonElement parent, string propertyName, string entityName)
    {
        var value = TarkovJsonReader.RequiredDecimal(parent, propertyName, entityName);
        if (value <= 0)
            throw new InvalidDataException($"{entityName} has non-positive '{propertyName}'.");
        return value;
    }
}
