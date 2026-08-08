using System.Net.Http.Headers;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed class TarkovJsonClient
{
    public static readonly Uri DefaultBaseUri = new("https://json.tarkov.dev/");

    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public TarkovJsonClient(HttpClient httpClient, Uri? baseUri = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUri = baseUri ?? DefaultBaseUri;
    }

    public async Task<TarkovJsonDocument> GetAsync(
        GameMode gameMode,
        TarkovEndpoint endpoint,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var modeSegment = TarkovSourcePath.GameModeSegment(gameMode);
        var endpointSegment = TarkovSourcePath.EndpointSegment(endpoint);
        var languageSuffix = string.IsNullOrWhiteSpace(language)
            ? string.Empty
            : $"_{language.Trim().ToLowerInvariant()}";

        var requestUri = new Uri(_baseUri, $"{modeSegment}/{endpointSegment}{languageSuffix}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return TarkovJsonDocument.Parse(document.RootElement);
    }
}
