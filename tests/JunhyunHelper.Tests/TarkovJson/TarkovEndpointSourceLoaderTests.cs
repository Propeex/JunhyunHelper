using System.Net;
using System.Text;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovEndpointSourceLoaderTests
{
    [Fact]
    public async Task MissingKoreanTranslationBecomesWarningButBaseContentStillLoads()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RoutingHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/regular/items" => Json(HttpStatusCode.OK, "{\"data\":{\"items\":[]}}"),
            "/regular/items_ko" => Json(HttpStatusCode.ServiceUnavailable, "{}"),
            "/regular/items_en" => Json(HttpStatusCode.OK, "{\"data\":{\"item Name\":\"Item\"}}"),
            _ => Json(HttpStatusCode.NotFound, "{}"),
        });
        using var httpClient = new HttpClient(handler);
        var loader = new TarkovEndpointSourceLoader(
            new TarkovJsonClient(httpClient, new Uri("https://example.test/")));

        var result = await loader.LoadAsync(
            GameMode.Regular,
            TarkovEndpoint.Items,
            cancellationToken);

        Assert.Single(result.Warnings);
        Assert.Equal("Item", result.Source.Localization.Resolve("item Name").English);
        Assert.Null(result.Source.Localization.Resolve("item Name").Korean);
    }

    [Fact]
    public async Task NonLocalizedEndpointDoesNotRequestLanguageDocuments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var requestedPaths = new List<string>();
        var handler = new RoutingHandler(request =>
        {
            requestedPaths.Add(request.RequestUri!.AbsolutePath);
            return Json(HttpStatusCode.OK, "{\"data\":[]}");
        });
        using var httpClient = new HttpClient(handler);
        var loader = new TarkovEndpointSourceLoader(
            new TarkovJsonClient(httpClient, new Uri("https://example.test/")));

        await loader.LoadAsync(
            GameMode.Pve,
            TarkovEndpoint.Barters,
            cancellationToken);

        Assert.Equal(new[] { "/pve/barters" }, requestedPaths);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class RoutingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> route) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(route(request));
    }
}
