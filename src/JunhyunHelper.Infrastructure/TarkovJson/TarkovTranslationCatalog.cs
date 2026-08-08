using System.Text.Json;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed class TarkovTranslationCatalog
{
    private readonly IReadOnlyDictionary<string, string> _values;

    private TarkovTranslationCatalog(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public static TarkovTranslationCatalog Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    public static TarkovTranslationCatalog FromDocument(TarkovJsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Data.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "json.tarkov.dev translation document data must be an object.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.Data.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
                continue;

            var value = property.Value.GetString();
            if (value is not null)
                values[property.Name] = value;
        }

        return new TarkovTranslationCatalog(values);
    }

    public string? Resolve(string? translationKey)
    {
        if (string.IsNullOrWhiteSpace(translationKey))
            return null;

        return _values.TryGetValue(translationKey, out var value)
            ? value
            : null;
    }
}

public readonly record struct LocalizedText(string? Korean, string? English)
{
    public string? Preferred => Korean ?? English;
}

public sealed class TarkovLocalization
{
    private readonly TarkovTranslationCatalog _korean;
    private readonly TarkovTranslationCatalog _english;

    public TarkovLocalization(
        TarkovTranslationCatalog? korean = null,
        TarkovTranslationCatalog? english = null)
    {
        _korean = korean ?? TarkovTranslationCatalog.Empty;
        _english = english ?? TarkovTranslationCatalog.Empty;
    }

    public LocalizedText Resolve(string? translationKey)
    {
        if (string.IsNullOrWhiteSpace(translationKey))
            return default;

        return new LocalizedText(
            _korean.Resolve(translationKey),
            _english.Resolve(translationKey) ?? translationKey);
    }
}
