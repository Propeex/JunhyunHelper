using System.Text.Json;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed record TarkovJsonDocument(
    JsonElement Data,
    IReadOnlyList<string> TranslationPaths)
{
    public static TarkovJsonDocument Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("json.tarkov.dev response root must be an object.");

        if (!root.TryGetProperty("data", out var data) ||
            data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new InvalidDataException("json.tarkov.dev response is missing data.");
        }

        var translationPaths = Array.Empty<string>();
        if (root.TryGetProperty("translations", out var translations))
        {
            if (translations.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("json.tarkov.dev translations must be an array.");

            translationPaths = translations
                .EnumerateArray()
                .Select(static value => value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();
        }

        return new TarkovJsonDocument(data.Clone(), translationPaths);
    }
}
