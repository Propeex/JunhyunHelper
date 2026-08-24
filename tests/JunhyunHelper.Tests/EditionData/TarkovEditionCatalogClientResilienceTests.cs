using System.Net;
using System.Text;
using JunhyunHelper.Infrastructure.EditionData;
using Xunit;

namespace JunhyunHelper.Tests.EditionData;

public sealed class TarkovEditionCatalogClientResilienceTests
{
    private const string ValidEditionJson = """
        {
          "editions": {
            "standard": {
              "id": "standard",
              "title": "Standard"
            }
          }
        }
        """;

    [Fact]
    public async Task RetryableServerFailureIsRetriedAndThenSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new SequenceHandler(
            (HttpStatusCode.ServiceUnavailable, "temporary"),
            (HttpStatusCode.OK, ValidEditionJson));
        using var httpClient = new HttpClient(handler);
        var client = new TarkovEditionCatalogClient(httpClient, new Uri("https://example.test/overlay.json"));

        var editions = await client.GetAsync(cancellationToken);

        Assert.Single(editions);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task NonRetryableNotFoundFailsImmediately()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new SequenceHandler((HttpStatusCode.NotFound, "missing"));
        using var httpClient = new HttpClient(handler);
        var client = new TarkovEditionCatalogClient(httpClient, new Uri("https://example.test/overlay.json"));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(cancellationToken));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task TruncatedJsonIsRetriedAndThenSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new SequenceHandler(
            (HttpStatusCode.OK, "{"),
            (HttpStatusCode.OK, ValidEditionJson));
        using var httpClient = new HttpClient(handler);
        var client = new TarkovEditionCatalogClient(httpClient, new Uri("https://example.test/overlay.json"));

        var editions = await client.GetAsync(cancellationToken);

        Assert.Single(editions);
        Assert.Equal(2, handler.RequestCount);
    }

    private sealed class SequenceHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        private int _requestCount;
        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Math.Min(Interlocked.Increment(ref _requestCount) - 1, responses.Length - 1);
            var response = responses[index];
            return Task.FromResult(new HttpResponseMessage(response.Status)
            {
                Content = new StringContent(response.Body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
