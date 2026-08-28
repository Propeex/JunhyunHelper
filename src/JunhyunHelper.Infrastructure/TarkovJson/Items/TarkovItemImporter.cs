using System.Text.Json;
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

            result.Add(new GameItem(
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
                    : !typeKeys.Contains("noFlea", StringComparer.OrdinalIgnoreCase)));
        }

        return result;
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

        return categories
            .EnumerateArray()
            .Select(TarkovJsonReader.ReferenceId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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
