using System.Net;
using System.Text;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovJsonClientResilienceTests
{
    [Fact]
    public async Task RetryableServerFailureIsRetriedAndThenSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new SequenceHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        var document = await client.GetAsync(
            GameMode.Regular,
            TarkovEndpoint.Items,
            cancellationToken: cancellationToken);

        Assert.NotNull(document);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task NonRetryableNotFoundFailsImmediately()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new SequenceHandler(HttpStatusCode.NotFound);
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(
            GameMode.Regular,
            TarkovEndpoint.Items,
            cancellationToken: cancellationToken));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task TruncatedJsonIsRetriedAndThenSucceeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new BodySequenceHandler("{", "{\"data\":{}}");
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        var document = await client.GetAsync(
            GameMode.Regular,
            TarkovEndpoint.Items,
            cancellationToken: cancellationToken);

        Assert.NotNull(document);
        Assert.Equal(2, handler.RequestCount);
    }

    private sealed class SequenceHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _requestCount;
        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Math.Min(Interlocked.Increment(ref _requestCount) - 1, statuses.Length - 1);
            var status = statuses[index];
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent("{\"data\":{}}", Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class BodySequenceHandler(params string[] bodies) : HttpMessageHandler
    {
        private int _requestCount;
        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var index = Math.Min(Interlocked.Increment(ref _requestCount) - 1, bodies.Length - 1);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(bodies[index], Encoding.UTF8, "application/json"),
            });
        }
    }
}
