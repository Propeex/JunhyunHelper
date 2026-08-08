using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed record TarkovEndpointLoadResult(
    TarkovEndpointSource Source,
    IReadOnlyList<string> Warnings);

public sealed class TarkovEndpointSourceLoader
{
    private readonly TarkovJsonClient _client;

    public TarkovEndpointSourceLoader(TarkovJsonClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<TarkovEndpointLoadResult> LoadAsync(
        GameMode gameMode,
        TarkovEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        var baseDocument = await _client.GetAsync(
            gameMode,
            endpoint,
            cancellationToken: cancellationToken);

        if (!SupportsTranslations(endpoint))
        {
            return new TarkovEndpointLoadResult(
                new TarkovEndpointSource(baseDocument, new TarkovLocalization()),
                Array.Empty<string>());
        }

        var warnings = new List<string>();
        var korean = await TryLoadTranslationAsync(
            gameMode,
            endpoint,
            "ko",
            warnings,
            cancellationToken);
        var english = await TryLoadTranslationAsync(
            gameMode,
            endpoint,
            "en",
            warnings,
            cancellationToken);

        return new TarkovEndpointLoadResult(
            new TarkovEndpointSource(
                baseDocument,
                new TarkovLocalization(korean, english)),
            warnings);
    }

    private async Task<TarkovTranslationCatalog> TryLoadTranslationAsync(
        GameMode gameMode,
        TarkovEndpoint endpoint,
        string language,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await _client.GetAsync(
                gameMode,
                endpoint,
                language,
                cancellationToken);
            return TarkovTranslationCatalog.FromDocument(document);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            warnings.Add(
                $"Could not load {language} translation for {endpoint}: {exception.Message}");
            return TarkovTranslationCatalog.Empty;
        }
    }

    private static bool SupportsTranslations(TarkovEndpoint endpoint) => endpoint is
        TarkovEndpoint.Tasks or
        TarkovEndpoint.Hideout or
        TarkovEndpoint.Items or
        TarkovEndpoint.Traders or
        TarkovEndpoint.Maps;
}
