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

            var paths = new List<string>();
            foreach (var value in translations.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        "json.tarkov.dev translations entries must be strings.");
                }

                var path = value.GetString();
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }

            translationPaths = paths.ToArray();
        }

        return new TarkovJsonDocument(data.Clone(), translationPaths);
    }
}
