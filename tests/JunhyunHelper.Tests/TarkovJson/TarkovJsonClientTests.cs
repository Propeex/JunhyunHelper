using System.Net;
using System.Text;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovJsonClientTests
{
    [Fact]
    public async Task SeasonalKoreanTaskRequestUsesExpectedPath()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""
            {
              "data": { "tasks": {} },
              "translations": ["$.data.tasks.*.name"]
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        var result = await client.GetAsync(
            GameMode.PvpSeason,
            TarkovEndpoint.Tasks,
            "ko",
            cancellationToken);

        Assert.Equal(
            new Uri("https://example.test/pvp-season/tasks_ko"),
            handler.LastRequestUri);
        Assert.Single(result.TranslationPaths);
        Assert.Equal("$.data.tasks.*.name", result.TranslationPaths[0]);
    }

    [Theory]
    [InlineData(GameMode.Regular, "regular")]
    [InlineData(GameMode.Pve, "pve")]
    [InlineData(GameMode.PvpSeason, "pvp-season")]
    public async Task GameModesMapToStableSourceSegments(GameMode gameMode, string expectedSegment)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("{\"data\":{}}");
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        await client.GetAsync(
            gameMode,
            TarkovEndpoint.Items,
            cancellationToken: cancellationToken);

        Assert.Equal(
            new Uri($"https://example.test/{expectedSegment}/items"),
            handler.LastRequestUri);
    }

    [Fact]
    public async Task MissingDataIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("{\"translations\":[]}");
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.GetAsync(
                GameMode.Regular,
                TarkovEndpoint.Tasks,
                cancellationToken: cancellationToken));
    }

    [Fact]
    public async Task NonStringTranslationEntryIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHandler("""
            {
              "data": {},
              "translations": [123]
            }
            """);
        using var httpClient = new HttpClient(handler);
        var client = new TarkovJsonClient(httpClient, new Uri("https://example.test/"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.GetAsync(
                GameMode.Regular,
                TarkovEndpoint.Tasks,
                cancellationToken: cancellationToken));
    }

    private sealed class RecordingHandler(string responseJson) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }
}
