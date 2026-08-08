using System.Text.Json;

namespace JunhyunHelper.Infrastructure.TarkovJson;

internal static class TarkovJsonReader
{
    public static IReadOnlyList<JsonElement> ReadCollection(
        JsonElement data,
        string propertyName)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty(propertyName, out var collection))
        {
            throw new InvalidDataException(
                $"json.tarkov.dev data is missing required collection '{propertyName}'.");
        }

        return ReadCollectionValue(collection, propertyName);
    }

    public static IReadOnlyList<JsonElement> ReadCollectionValue(
        JsonElement collection,
        string description)
    {
        return collection.ValueKind switch
        {
            JsonValueKind.Array => collection
                .EnumerateArray()
                .Select(static value => value.Clone())
                .ToArray(),
            JsonValueKind.Object => collection
                .EnumerateObject()
                .Select(static property => property.Value.Clone())
                .ToArray(),
            _ => throw new InvalidDataException(
                $"json.tarkov.dev collection '{description}' must be an array or object."),
        };
    }

    public static string RequiredString(JsonElement value, string propertyName, string entityName)
    {
        if (value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!;
        }

        throw new InvalidDataException(
            $"{entityName} is missing required string '{propertyName}'.");
    }

    public static string? OptionalString(JsonElement value, string propertyName)
    {
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    public static string? ReferenceId(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Object when value.TryGetProperty("id", out var id) &&
                                      id.ValueKind == JsonValueKind.String => id.GetString(),
            _ => null,
        };
    }
}
