using System.Text.Json;
using JunhyunHelper.Core.Editions;

namespace JunhyunHelper.Infrastructure.EditionData;

public sealed class TarkovEditionCatalogClient
{
    public static readonly Uri DefaultSourceUri = new(
        "https://cdn.jsdelivr.net/gh/tarkovtracker-org/tarkov-data-overlay@main/dist/overlay.json");

    private readonly HttpClient _httpClient;
    private readonly Uri _sourceUri;

    public TarkovEditionCatalogClient(HttpClient httpClient, Uri? sourceUri = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sourceUri = sourceUri ?? DefaultSourceUri;
    }

    public async Task<IReadOnlyList<EditionDefinition>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(_sourceUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return Parse(document.RootElement);
    }

    internal static IReadOnlyList<EditionDefinition> Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("editions", out var editions) ||
            editions.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Edition overlay is missing the 'editions' object.");
        }

        var result = new List<EditionDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in editions.EnumerateObject())
        {
            var raw = property.Value;
            if (raw.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException($"Edition '{property.Name}' must be an object.");

            var id = RequiredString(raw, "id", property.Name);
            if (!string.Equals(id, property.Name, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Edition key '{property.Name}' does not match id '{id}'.");
            }

            if (!ids.Add(id))
                throw new InvalidDataException($"Duplicate edition id '{id}'.");

            result.Add(new EditionDefinition(
                id,
                RequiredString(raw, "title", id),
                ReadTaskIds(raw, "exclusiveTaskIds", id),
                ReadTaskIds(raw, "excludedTaskIds", id)));
        }

        if (result.Count == 0)
            throw new InvalidDataException("Edition overlay contains no editions.");

        return result;
    }

    private static IReadOnlySet<string> ReadTaskIds(
        JsonElement edition,
        string propertyName,
        string editionId)
    {
        if (!edition.TryGetProperty(propertyName, out var raw) ||
            raw.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (raw.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Edition '{editionId}' field '{propertyName}' must be an array.");
        }

        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in raw.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(value.GetString()))
            {
                throw new InvalidDataException(
                    $"Edition '{editionId}' field '{propertyName}' contains an invalid task id.");
            }

            var taskId = value.GetString()!;
            if (!result.Add(taskId))
            {
                throw new InvalidDataException(
                    $"Edition '{editionId}' field '{propertyName}' contains duplicate task id '{taskId}'.");
            }
        }

        return result;
    }

    private static string RequiredString(JsonElement value, string propertyName, string entityName)
    {
        if (value.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
        {
            return property.GetString()!;
        }

        throw new InvalidDataException(
            $"Edition '{entityName}' is missing required string '{propertyName}'.");
    }
}
