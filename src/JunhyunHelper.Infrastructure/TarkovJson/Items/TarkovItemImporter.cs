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

            result.Add(new GameItem(
                id,
                name.Korean,
                name.English,
                shortName.Korean,
                shortName.English,
                TarkovJsonReader.OptionalString(raw, "iconLink"),
                TarkovJsonReader.OptionalString(raw, "wikiLink"),
                ReadCategoryIds(raw)));
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
}
