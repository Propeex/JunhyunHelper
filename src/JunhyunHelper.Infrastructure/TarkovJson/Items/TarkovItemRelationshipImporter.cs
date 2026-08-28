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
    // json.tarkov.dev exposes Bitcoin Farm output through the crafts endpoint even though
    // it is passive hideout production driven by GPU/station state rather than consumable
    // recipe ingredients. Modelling it as a normal zero-cost craft would produce false
    // Scanner relationship information, so only this audited upstream identity is excluded.
    // Every other empty craft remains fail-closed below.
    private const string PassiveBitcoinProductionId = "5d5c205bd582a50d042a3c0e";
    private const string BitcoinFarmStationId = "5d494a445b56502f18c98a10";
    private const string PhysicalBitcoinItemId = "59faff1d86f7746c51718c9c";

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

        // Current json.tarkov.dev can repeat the exact same buyFromTrader offer two or
        // three times for an item. Those rows are byte-for-byte equivalent in all fields
        // represented by the canonical model, so retaining them would fabricate duplicate
        // acquisition paths and trip the canonical uniqueness validator. Normalize only
        // exact record equality here; materially different offers remain separate.
        var canonicalPurchases = purchases
            .Distinct()
            .OrderBy(value => value.ItemId, StringComparer.Ordinal)
            .ThenBy(value => value.TraderId, StringComparer.Ordinal)
            .ThenBy(value => value.RequiredLevel)
            .ToArray();

        return new ItemRelationshipCatalog(
            canonicalPurchases,
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
            {
                if (IsKnownPassiveBitcoinProduction(raw, id, product))
                    continue;

                throw new InvalidDataException($"Craft '{id}' has no required items.");
            }

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

    private static bool IsKnownPassiveBitcoinProduction(JsonElement raw, string id, JsonElement product)
    {
        if (!string.Equals(id, PassiveBitcoinProductionId, StringComparison.Ordinal))
            return false;

        var stationId = RequiredReference(raw, "station", $"Craft '{id}'");
        var productItemId = RequiredReference(product, "item", $"Craft '{id}' product item");
        var productCount = RequiredPositiveDecimal(product, "count", $"Craft '{id}' product item");
        var level = RequiredNonNegativeInt(raw, "level", id);
        var duration = RequiredNonNegativeInt(raw, "duration", id);

        if (!raw.TryGetProperty("requiredQuestItems", out var questRequirements) ||
            questRequirements.ValueKind != JsonValueKind.Array ||
            questRequirements.GetArrayLength() != 0)
        {
            return false;
        }

        return string.Equals(stationId, BitcoinFarmStationId, StringComparison.Ordinal) &&
               string.Equals(productItemId, PhysicalBitcoinItemId, StringComparison.Ordinal) &&
               productCount == 1m &&
               level == 1 &&
               duration > 0;
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
