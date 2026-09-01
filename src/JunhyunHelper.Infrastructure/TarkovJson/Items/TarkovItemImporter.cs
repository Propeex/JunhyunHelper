using System.Text.Json;
using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Infrastructure.TarkovJson.Items;

public sealed class TarkovItemImporter
{
    public IReadOnlyList<GameItem> Import(
        TarkovJsonDocument baseDocument,
        TarkovLocalization localization)
    {
        ArgumentNullException.ThrowIfNull(baseDocument);
        ArgumentNullException.ThrowIfNull(localization);

        var items = TarkovJsonReader.ReadCollection(baseDocument.Data, "items");
        var categoryKeysById = ReadCategoryKeys(baseDocument.Data);
        var result = new List<GameItem>(items.Count);
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in items)
        {
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Item entries must be objects.");

            var id = TarkovJsonReader.RequiredString(raw, "id", "Item");
            if (!ids.Add(id))
                throw new InvalidDataException($"Duplicate item id '{id}'.");

            var name = localization.Resolve(TarkovJsonReader.OptionalString(raw, "name"));
            var shortName = localization.Resolve(TarkovJsonReader.OptionalString(raw, "shortName"));
            var categoryIds = ReadCategoryIds(raw);
            var categoryKeys = categoryIds
                .Where(categoryKeysById.ContainsKey)
                .Select(categoryId => categoryKeysById[categoryId])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var typeKeys = ReadStringArray(raw, "types");

            var item = new GameItem(
                id,
                name.Korean,
                name.English,
                shortName.Korean,
                shortName.English,
                TarkovJsonReader.OptionalString(raw, "iconLink"),
                TarkovJsonReader.OptionalString(raw, "wikiLink"),
                categoryIds,
                categoryKeys,
                typeKeys,
                TarkovJsonReader.OptionalInt(raw, "width"),
                TarkovJsonReader.OptionalInt(raw, "height"),
                OptionalDecimal(raw, "weight"),
                TarkovJsonReader.OptionalInt(raw, "basePrice"),
                typeKeys.Length == 0
                    ? null
                    : !typeKeys.Contains("noFlea", StringComparer.OrdinalIgnoreCase))
            {
                FarmingGuideData = ReadFarmingGuideLayout(raw),
                FarmingGuideAssembly = ReadAssemblySource(raw),
            };

            result.Add(item);
        }

        return result;
    }

    private static FarmingGuideAssemblySource? ReadAssemblySource(JsonElement item)
    {
        var gridImageUrl = TarkovJsonReader.OptionalString(item, "gridImageLink");
        var image512Url = TarkovJsonReader.OptionalString(item, "image512pxLink");
        var containedItemIds = ReadContainedItemIds(item);
        string? defaultPresetItemId = null;

        if (item.TryGetProperty("properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object &&
            properties.TryGetProperty("defaultPreset", out var defaultPreset))
        {
            defaultPresetItemId = ReferenceId(defaultPreset);
        }

        if (string.IsNullOrWhiteSpace(gridImageUrl) &&
            string.IsNullOrWhiteSpace(image512Url) &&
            string.IsNullOrWhiteSpace(defaultPresetItemId) &&
            containedItemIds.Count == 0)
        {
            return null;
        }

        return new FarmingGuideAssemblySource(
            gridImageUrl,
            image512Url,
            defaultPresetItemId,
            containedItemIds);
    }

    private static IReadOnlyList<string> ReadContainedItemIds(JsonElement item)
    {
        if (!item.TryGetProperty("containsItems", out var values) || values.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<string>();
        foreach (var value in values.EnumerateArray())
        {
            string? id = null;
            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("item", out var nestedItem))
                id = ReferenceId(nestedItem);
            id ??= ReferenceId(value);
            if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id, StringComparer.Ordinal))
                result.Add(id);
        }
        return result;
    }

    private static FarmingGuideItemLayout? ReadFarmingGuideLayout(JsonElement item)
    {
        var blocksHeadphones = OptionalBool(item, "blocksHeadphones") ?? false;
        var conflictingItems = ReadReferenceArray(item, "conflictingItems");
        var conflictingSlotIds = ReadStringArray(item, "conflictingSlotIds");

        if (!item.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            return blocksHeadphones || conflictingItems.Count > 0 || conflictingSlotIds.Length > 0
                ? new FarmingGuideItemLayout(
                    null,
                    [],
                    [],
                    [],
                    conflictingItems,
                    conflictingSlotIds,
                    blocksHeadphones,
                    false)
                : null;
        }

        var propertiesType = TarkovJsonReader.OptionalString(properties, "propertiesType")
                             ?? TarkovJsonReader.OptionalString(properties, "__typename");
        var storageLayoutName = ReadStorageLayoutName(properties);
        var grids = ReadStorageGrids(properties);
        var slots = ReadAttachmentSlots(properties);
        var armorSlots = ReadArmorSlots(properties);
        var armorClass = TarkovJsonReader.OptionalInt(properties, "class") ?? 0;
        var isChestRig = string.Equals(
            propertiesType,
            "ItemPropertiesChestRig",
            StringComparison.OrdinalIgnoreCase);
        var isArmoredRig = isChestRig && (armorSlots.Count > 0 || armorClass > 0);

        if (string.IsNullOrWhiteSpace(propertiesType) &&
            string.IsNullOrWhiteSpace(storageLayoutName) &&
            grids.Count == 0 &&
            slots.Count == 0 &&
            armorSlots.Count == 0 &&
            armorClass <= 0 &&
            !blocksHeadphones &&
            conflictingItems.Count == 0 &&
            conflictingSlotIds.Length == 0)
        {
            return null;
        }

        return new FarmingGuideItemLayout(
            propertiesType,
            grids,
            slots,
            armorSlots,
            conflictingItems,
            conflictingSlotIds,
            blocksHeadphones,
            isArmoredRig)
        {
            StorageLayoutName = storageLayoutName,
            ArmorClass = armorClass > 0 ? armorClass : null,
        };
    }

    private static string? ReadStorageLayoutName(JsonElement properties) =>
        TarkovJsonReader.OptionalString(properties, "gridLayoutName")
        ?? TarkovJsonReader.OptionalString(properties, "GridLayoutName")
        ?? TarkovJsonReader.OptionalString(properties, "rigLayoutName")
        ?? TarkovJsonReader.OptionalString(properties, "RigLayoutName");

    private static IReadOnlyList<FarmingGuideStorageGridDefinition> ReadStorageGrids(JsonElement properties)
    {
        if (!properties.TryGetProperty("grids", out var grids) || grids.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<FarmingGuideStorageGridDefinition>();
        foreach (var grid in grids.EnumerateArray())
        {
            if (grid.ValueKind != JsonValueKind.Object)
                continue;

            var width = TarkovJsonReader.OptionalInt(grid, "width") ?? 0;
            var height = TarkovJsonReader.OptionalInt(grid, "height") ?? 0;
            if (width <= 0 || height <= 0)
                continue;

            result.Add(new FarmingGuideStorageGridDefinition(
                width,
                height,
                ReadFilter(grid)));
        }

        return result;
    }

    private static IReadOnlyList<FarmingGuideAttachmentSlotDefinition> ReadAttachmentSlots(JsonElement properties)
    {
        if (!properties.TryGetProperty("slots", out var slots) || slots.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<FarmingGuideAttachmentSlotDefinition>();
        foreach (var slot in slots.EnumerateArray())
        {
            if (slot.ValueKind != JsonValueKind.Object)
                continue;

            var id = TarkovJsonReader.OptionalString(slot, "id")
                     ?? TarkovJsonReader.OptionalString(slot, "nameId");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            result.Add(new FarmingGuideAttachmentSlotDefinition(
                id,
                TarkovJsonReader.OptionalString(slot, "nameId") ?? id,
                TarkovJsonReader.OptionalString(slot, "name"),
                OptionalBool(slot, "required") ?? false,
                ReadFilter(slot)));
        }

        return result;
    }

    private static IReadOnlyList<FarmingGuideArmorSlotDefinition> ReadArmorSlots(JsonElement properties)
    {
        if (!properties.TryGetProperty("armorSlots", out var slots) || slots.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<FarmingGuideArmorSlotDefinition>();
        var index = 0;
        foreach (var slot in slots.EnumerateArray())
        {
            if (slot.ValueKind != JsonValueKind.Object)
                continue;

            var nameId = TarkovJsonReader.OptionalString(slot, "nameId") ?? $"armor-slot-{index}";
            var id = TarkovJsonReader.OptionalString(slot, "id") ?? nameId;
            var hasAllowedPlates = slot.TryGetProperty("allowedPlates", out var allowedPlates) &&
                                   allowedPlates.ValueKind == JsonValueKind.Array;
            var plateIds = hasAllowedPlates
                ? ReadReferences(allowedPlates)
                : Array.Empty<string>();

            result.Add(new FarmingGuideArmorSlotDefinition(
                id,
                nameId,
                TarkovJsonReader.OptionalString(slot, "name"),
                Locked: !hasAllowedPlates,
                plateIds));
            index++;
        }

        return result;
    }

    private static FarmingGuideItemFilter ReadFilter(JsonElement owner)
    {
        if (!owner.TryGetProperty("filters", out var filter) || filter.ValueKind != JsonValueKind.Object)
            return FarmingGuideItemFilter.Empty;

        return new FarmingGuideItemFilter(
            ReadReferenceArray(filter, "allowedCategories"),
            ReadReferenceArray(filter, "allowedItems"),
            ReadReferenceArray(filter, "excludedCategories"),
            ReadReferenceArray(filter, "excludedItems"));
    }

    private static IReadOnlyDictionary<string, string> ReadCategoryKeys(JsonElement data)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("itemCategories", out var categories) ||
            categories.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return result;
        }

        IEnumerable<(string? FallbackId, JsonElement Value)> values = categories.ValueKind switch
        {
            JsonValueKind.Object => categories.EnumerateObject()
                .Select(property => ((string?)property.Name, property.Value)),
            JsonValueKind.Array => categories.EnumerateArray()
                .Select(value => ((string?)null, value)),
            _ => Array.Empty<(string?, JsonElement)>(),
        };

        foreach (var (fallbackId, value) in values)
        {
            if (value.ValueKind != JsonValueKind.Object)
                continue;

            var id = TarkovJsonReader.OptionalString(value, "id") ?? fallbackId;
            var key = TarkovJsonReader.OptionalString(value, "normalizedName")
                      ?? TarkovJsonReader.OptionalString(value, "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(key))
                continue;

            result[id] = key.Trim().ToLowerInvariant();
        }

        return result;
    }

    private static IReadOnlyList<string> ReadCategoryIds(JsonElement item)
    {
        if (!item.TryGetProperty("categories", out var categories) ||
            categories.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<string>();
        }

        if (categories.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Item categories must be an array when present.");

        return ReadReferences(categories);
    }

    private static string[] ReadStringArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var values) ||
            values.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (values.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Item '{propertyName}' must be an array when present.");

        return values.EnumerateArray()
            .Where(static value => value.ValueKind == JsonValueKind.String)
            .Select(static value => value.GetString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadReferenceArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            return [];

        return ReadReferences(values);
    }

    private static string[] ReadReferences(JsonElement values)
    {
        return values.EnumerateArray()
            .Select(ReferenceId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string? ReferenceId(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
            return value.GetString();
        if (value.ValueKind != JsonValueKind.Object)
            return null;

        return TarkovJsonReader.OptionalString(value, "id")
               ?? TarkovJsonReader.OptionalString(value, "_id");
    }

    private static bool? OptionalBool(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static decimal? OptionalDecimal(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : null;
    }
}
