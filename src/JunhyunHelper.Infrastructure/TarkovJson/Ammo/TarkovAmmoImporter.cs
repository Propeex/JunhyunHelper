using System.Text.Json;
using JunhyunHelper.Core.Ammo;

namespace JunhyunHelper.Infrastructure.TarkovJson.Ammo;

public sealed class TarkovAmmoImporter
{
    private const string AmmoPropertiesType = "ItemPropertiesAmmo";

    public IReadOnlyList<AmmoDefinition> Import(
        TarkovJsonDocument itemsDocument,
        TarkovJsonDocument bartersDocument,
        TarkovJsonDocument craftsDocument)
    {
        ArgumentNullException.ThrowIfNull(itemsDocument);
        ArgumentNullException.ThrowIfNull(bartersDocument);
        ArgumentNullException.ThrowIfNull(craftsDocument);

        var rawItems = TarkovJsonReader.ReadCollection(itemsDocument.Data, "items");
        var ammoItems = rawItems
            .Where(IsAmmoItem)
            .ToDictionary(
                item => TarkovJsonReader.RequiredString(item, "id", "Ammo item"),
                StringComparer.Ordinal);

        var purchasesByAmmo = ReadTraderPurchases(ammoItems);
        var bartersByAmmo = ReadBarters(bartersDocument.Data, ammoItems.Keys);
        var craftsByAmmo = ReadCrafts(craftsDocument.Data, ammoItems.Keys);

        var result = new List<AmmoDefinition>(ammoItems.Count);
        foreach (var (itemId, rawItem) in ammoItems.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            var properties = RequiredObject(rawItem, "properties", $"Ammo '{itemId}'");
            var acquisitions = new[]
                {
                    purchasesByAmmo.GetValueOrDefault(itemId) ?? [],
                    bartersByAmmo.GetValueOrDefault(itemId) ?? [],
                    craftsByAmmo.GetValueOrDefault(itemId) ?? [],
                }
                .SelectMany(static values => values)
                .ToArray();

            result.Add(new AmmoDefinition(
                itemId,
                TarkovJsonReader.RequiredString(properties, "caliber", $"Ammo '{itemId}' properties"),
                TarkovJsonReader.OptionalString(properties, "ammoType"),
                RequiredNonNegativeInt(properties, "projectileCount", itemId),
                RequiredNonNegativeInt(properties, "damage", itemId),
                RequiredNonNegativeInt(properties, "armorDamage", itemId),
                RequiredNonNegativeInt(properties, "penetrationPower", itemId),
                RequiredDecimal(properties, "fragmentationChance", itemId),
                RequiredDecimal(properties, "ricochetChance", itemId),
                RequiredDecimal(properties, "accuracyModifier", itemId),
                RequiredDecimal(properties, "recoilModifier", itemId),
                RequiredDecimal(properties, "initialSpeed", itemId),
                RequiredDecimal(properties, "heavyBleedModifier", itemId),
                RequiredDecimal(properties, "lightBleedModifier", itemId),
                TarkovJsonReader.OptionalBool(properties, "tracer") ?? false,
                TarkovJsonReader.OptionalString(properties, "tracerColor"),
                acquisitions));
        }

        return result;
    }

    private static bool IsAmmoItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object ||
            !item.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return TarkovJsonReader.OptionalString(properties, "propertiesType") == AmmoPropertiesType;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<AmmoAcquisition>> ReadTraderPurchases(
        IReadOnlyDictionary<string, JsonElement> ammoItems)
    {
        var result = new Dictionary<string, IReadOnlyList<AmmoAcquisition>>(StringComparer.Ordinal);

        foreach (var (itemId, item) in ammoItems)
        {
            if (!item.TryGetProperty("buyFromTrader", out var rawOffers) ||
                rawOffers.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                continue;
            }

            var offers = TarkovJsonReader.ReadCollectionValue(rawOffers, $"ammo {itemId} trader offers")
                .Select(raw => ReadTraderPurchase(raw, itemId))
                .ToArray();
            if (offers.Length > 0)
                result[itemId] = offers;
        }

        return result;
    }

    private static AmmoAcquisition ReadTraderPurchase(JsonElement raw, string itemId)
    {
        var traderId = RequiredReference(raw, "trader", $"Ammo '{itemId}' trader purchase");
        var currencyItemId = RequiredReference(raw, "currencyItem", $"Ammo '{itemId}' trader purchase");
        var price = RequiredPositiveDecimal(raw, "price", $"Ammo '{itemId}' trader purchase");
        var minTraderLevel = RequiredNonNegativeInt(raw, "minTraderLevel", itemId);

        return new AmmoAcquisition(
            AmmoAcquisitionKind.TraderPurchase,
            ReferenceId: null,
            TraderId: traderId,
            StationId: null,
            RequiredLevel: minTraderLevel,
            TaskUnlockQuestId: OptionalReference(raw, "taskUnlock"),
            OutputCount: 1,
            Price: price,
            CurrencyItemId: currencyItemId,
            CurrencyCode: TarkovJsonReader.RequiredString(raw, "currency", $"Ammo '{itemId}' trader purchase"),
            DurationSeconds: null,
            BuyLimit: TarkovJsonReader.OptionalInt(raw, "buyLimit"),
            Requirements: Array.Empty<AmmoAcquisitionRequirement>());
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<AmmoAcquisition>> ReadBarters(
        JsonElement barterData,
        IEnumerable<string> ammoItemIds)
    {
        var ammoIds = ammoItemIds.ToHashSet(StringComparer.Ordinal);
        var result = new Dictionary<string, List<AmmoAcquisition>>(StringComparer.Ordinal);

        foreach (var raw in TarkovJsonReader.ReadCollectionValue(barterData, "barters"))
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Barter entries must be objects.");

            var offered = RequiredObject(raw, "offeredItem", "Barter");
            var itemId = RequiredReference(offered, "item", "Barter offered item");
            if (!ammoIds.Contains(itemId))
                continue;

            var barterId = TarkovJsonReader.RequiredString(raw, "id", "Barter");
            var requirements = ReadRequirements(raw, "requiredItems", $"Barter '{barterId}'", allowTool: false);
            if (requirements.Count == 0)
                throw new InvalidDataException($"Ammo barter '{barterId}' has no required items.");

            var acquisition = new AmmoAcquisition(
                AmmoAcquisitionKind.TraderBarter,
                barterId,
                RequiredReference(raw, "trader", $"Barter '{barterId}'"),
                StationId: null,
                RequiredLevel: RequiredNonNegativeInt(raw, "minTraderLevel", barterId),
                TaskUnlockQuestId: OptionalReference(raw, "taskUnlock"),
                OutputCount: RequiredPositiveDecimal(offered, "count", $"Barter '{barterId}' offered item"),
                Price: null,
                CurrencyItemId: null,
                CurrencyCode: null,
                DurationSeconds: null,
                BuyLimit: TarkovJsonReader.OptionalInt(raw, "buyLimit"),
                Requirements: requirements);

            Add(result, itemId, acquisition);
        }

        return result.ToDictionary(
            static entry => entry.Key,
            static entry => (IReadOnlyList<AmmoAcquisition>)entry.Value,
            StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<AmmoAcquisition>> ReadCrafts(
        JsonElement craftData,
        IEnumerable<string> ammoItemIds)
    {
        var ammoIds = ammoItemIds.ToHashSet(StringComparer.Ordinal);
        var result = new Dictionary<string, List<AmmoAcquisition>>(StringComparer.Ordinal);

        foreach (var raw in TarkovJsonReader.ReadCollectionValue(craftData, "crafts"))
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Craft entries must be objects.");

            var product = RequiredObject(raw, "productItem", "Craft");
            var itemId = RequiredReference(product, "item", "Craft product item");
            if (!ammoIds.Contains(itemId))
                continue;

            var craftId = TarkovJsonReader.RequiredString(raw, "id", "Craft");
            var acquisition = new AmmoAcquisition(
                AmmoAcquisitionKind.HideoutCraft,
                craftId,
                TraderId: null,
                StationId: RequiredReference(raw, "station", $"Craft '{craftId}'"),
                RequiredLevel: RequiredNonNegativeInt(raw, "level", craftId),
                TaskUnlockQuestId: OptionalReference(raw, "taskUnlock"),
                OutputCount: RequiredPositiveDecimal(product, "count", $"Craft '{craftId}' product"),
                Price: null,
                CurrencyItemId: null,
                CurrencyCode: null,
                DurationSeconds: RequiredNonNegativeInt(raw, "duration", craftId),
                BuyLimit: null,
                Requirements: ReadRequirements(raw, "requiredItems", $"Craft '{craftId}'", allowTool: true));

            Add(result, itemId, acquisition);
        }

        return result.ToDictionary(
            static entry => entry.Key,
            static entry => (IReadOnlyList<AmmoAcquisition>)entry.Value,
            StringComparer.Ordinal);
    }

    private static IReadOnlyList<AmmoAcquisitionRequirement> ReadRequirements(
        JsonElement parent,
        string propertyName,
        string entityName,
        bool allowTool)
    {
        if (!parent.TryGetProperty(propertyName, out var rawRequirements) ||
            rawRequirements.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<AmmoAcquisitionRequirement>();
        }

        return TarkovJsonReader.ReadCollectionValue(rawRequirements, $"{entityName} {propertyName}")
            .Select(raw =>
            {
                var itemId = RequiredReference(raw, "item", entityName);
                var count = RequiredPositiveDecimal(raw, "count", entityName);
                var isTool = false;
                if (allowTool && raw.TryGetProperty("attributes", out var attributes) &&
                    attributes.ValueKind == JsonValueKind.Object)
                {
                    isTool = TarkovJsonReader.OptionalBool(attributes, "tool") ?? false;
                }

                return new AmmoAcquisitionRequirement(itemId, count, isTool);
            })
            .ToArray();
    }

    private static JsonElement RequiredObject(JsonElement parent, string propertyName, string entityName)
    {
        if (parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

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
        if (!parent.TryGetProperty(propertyName, out var raw) ||
            raw.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

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

    private static decimal RequiredDecimal(JsonElement parent, string propertyName, string entityName) =>
        TarkovJsonReader.RequiredDecimal(parent, propertyName, $"Ammo '{entityName}' properties");

    private static decimal RequiredPositiveDecimal(JsonElement parent, string propertyName, string entityName)
    {
        var value = TarkovJsonReader.RequiredDecimal(parent, propertyName, entityName);
        if (value <= 0)
            throw new InvalidDataException($"{entityName} has non-positive '{propertyName}'.");
        return value;
    }

    private static void Add(
        IDictionary<string, List<AmmoAcquisition>> result,
        string itemId,
        AmmoAcquisition acquisition)
    {
        if (!result.TryGetValue(itemId, out var list))
        {
            list = [];
            result[itemId] = list;
        }

        list.Add(acquisition);
    }
}
