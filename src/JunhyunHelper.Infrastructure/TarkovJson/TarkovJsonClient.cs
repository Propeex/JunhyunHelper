using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Infrastructure.TarkovJson;

public sealed class TarkovJsonClient
{
    public static readonly Uri DefaultBaseUri = new("https://json.tarkov.dev/");

    private const int MaximumAttempts = 3;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SecondRetryDelay = TimeSpan.FromMilliseconds(750);

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

        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTimeout.CancelAfter(RequestTimeout);

            try
            {
                return await GetOnceAsync(requestUri, requestTimeout.Token);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastFailure = new TimeoutException(
                    $"Tarkov data request timed out after {RequestTimeout.TotalSeconds:0.#} seconds: {requestUri}",
                    exception);
            }
            catch (HttpRequestException exception) when (IsRetryableHttpFailure(exception))
            {
                lastFailure = exception;
            }
            catch (JsonException exception)
            {
                // A truncated 200 response is indistinguishable from schema corruption at
                // this boundary. Retry a bounded number of times, then fail closed.
                lastFailure = exception;
            }
            catch (InvalidDataException exception)
            {
                lastFailure = exception;
            }

            if (attempt >= MaximumAttempts)
                break;

            await Task.Delay(GetRetryDelay(attempt), cancellationToken);
        }

        throw lastFailure ?? new HttpRequestException($"Tarkov data request failed: {requestUri}");
    }

    private async Task<TarkovJsonDocument> GetOnceAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
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

    private static bool IsRetryableHttpFailure(HttpRequestException exception)
    {
        if (exception.StatusCode is null)
            return true;

        return exception.StatusCode.Value is HttpStatusCode.RequestTimeout or
               HttpStatusCode.TooManyRequests ||
               (int)exception.StatusCode.Value >= 500;
    }

    private static TimeSpan GetRetryDelay(int completedAttempt) => completedAttempt switch
    {
        <= 1 => FirstRetryDelay,
        _ => SecondRetryDelay,
    };
}
