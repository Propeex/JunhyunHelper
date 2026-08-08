using System.Net;
using System.Text;
using JunhyunHelper.Infrastructure.EditionData;
using Xunit;

namespace JunhyunHelper.Tests.EditionData;

public sealed class TarkovEditionCatalogClientTests
{
    [Fact]
    public async Task ReadsOnlyEditionQuestRulesFromOverlay()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient(new JsonHandler("""
            {
              "tasks": { "ignored": { "disabled": true } },
              "editions": {
                "edge_of_darkness": {
                  "id": "edge_of_darkness",
                  "title": "Edge of Darkness",
                  "defaultStashLevel": 4,
                  "exclusiveTaskIds": ["quest-eod-a", "quest-eod-b"]
                },
                "unheard": {
                  "id": "unheard",
                  "title": "The Unheard",
                  "excludedTaskIds": ["quest-old-patterns"]
                }
              }
            }
            """));
        var client = new TarkovEditionCatalogClient(
            httpClient,
            new Uri("https://example.test/overlay.json"));

        var editions = await client.GetAsync(cancellationToken);

        Assert.Equal(2, editions.Count);
        var eod = Assert.Single(editions, edition => edition.Id == "edge_of_darkness");
        Assert.Contains("quest-eod-a", eod.ExclusiveQuestIds);
        Assert.Contains("quest-eod-b", eod.ExclusiveQuestIds);
        Assert.Empty(eod.ExcludedQuestIds);

        var unheard = Assert.Single(editions, edition => edition.Id == "unheard");
        Assert.Contains("quest-old-patterns", unheard.ExcludedQuestIds);
    }

    [Fact]
    public async Task MissingEditionSectionIsFatalInsteadOfSilentlyDisablingEditionRules()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient(new JsonHandler("{\"tasks\":{}}"));
        var client = new TarkovEditionCatalogClient(
            httpClient,
            new Uri("https://example.test/overlay.json"));

        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetAsync(cancellationToken));
    }

    [Fact]
    public async Task EditionKeyAndIdMismatchIsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var httpClient = new HttpClient(new JsonHandler("""
            {
              "editions": {
                "standard": {
                  "id": "different",
                  "title": "Standard"
                }
              }
            }
            """));
        var client = new TarkovEditionCatalogClient(
            httpClient,
            new Uri("https://example.test/overlay.json"));

        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetAsync(cancellationToken));
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }
}
