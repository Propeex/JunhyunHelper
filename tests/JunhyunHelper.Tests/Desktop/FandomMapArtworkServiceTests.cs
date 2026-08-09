using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Services;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class FandomMapArtworkServiceTests
{
    [Fact]
    public async Task Builds_aligned_svg_when_wiki_markers_match_canonical_markers()
    {
        var wikiJson = """
        {
          "mapImage": "Customs Interactive Map Base.png",
          "coordinateOrder": "xy",
          "mapBounds": [[0, 0], [1000, 1000]],
          "origin": "top-left",
          "markers": [
            { "id": "1", "categoryId": "1", "position": [100, 200], "popup": { "title": "Crossroads" } },
            { "id": "2", "categoryId": "1", "position": [800, 200], "popup": { "title": "ZB-1011" } },
            { "id": "3", "categoryId": "1", "position": [100, 800], "popup": { "title": "Trailer Park" } },
            { "id": "4", "categoryId": "1", "position": [800, 800], "popup": { "title": "Old Gas Station" } }
          ]
        }
        """;
        using var client = new HttpClient(new FakeHandler(wikiJson));
        var service = new FandomMapArtworkService(client);
        var layout = Layout();
        var markers = new[]
        {
            Marker("1", "Crossroads", 10, 20),
            Marker("2", "ZB-1011", 80, 20),
            Marker("3", "Trailer Park", 10, 80),
            Marker("4", "Old Gas Station", 80, 80),
        };
        var root = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "customs.svg");

        try
        {
            var result = await service.TryBuildAlignedSvgAsync(
                layout,
                markers,
                destination,
                TestContext.Current.CancellationToken);

            Assert.True(result.Applied, result.Warning);
            Assert.Equal(4, result.MatchedMarkers);
            Assert.Equal(4, result.InlierMarkers);
            Assert.True(result.Residual < 0.000001);
            Assert.Contains("Escape from Tarkov Wiki", result.Attribution);
            Assert.True(File.Exists(destination));

            var document = XDocument.Load(destination);
            var image = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "image");
            Assert.Contains("matrix(", image.Attribute("transform")?.Value);
            Assert.StartsWith("data:image/png;base64,", image.Attribute("href")?.Value);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_wiki_background_when_too_few_markers_can_prove_alignment()
    {
        var wikiJson = """
        {
          "mapImage": "Customs Interactive Map Base.png",
          "coordinateOrder": "xy",
          "mapBounds": [[0, 0], [1000, 1000]],
          "origin": "top-left",
          "markers": [
            { "id": "1", "categoryId": "1", "position": [100, 200], "popup": { "title": "Crossroads" } }
          ]
        }
        """;
        using var client = new HttpClient(new FakeHandler(wikiJson));
        var service = new FandomMapArtworkService(client);
        var root = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "customs.svg");

        try
        {
            var result = await service.TryBuildAlignedSvgAsync(
                Layout(),
                new[] { Marker("1", "Crossroads", 10, 20) },
                destination,
                TestContext.Current.CancellationToken);

            Assert.False(result.Applied);
            Assert.False(File.Exists(destination));
            Assert.Contains("at least", result.Warning, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static MapLayoutDefinition Layout() =>
        new(
            "customs-id",
            "customs",
            "customs",
            0,
            4,
            new[] { 1d, 0d, -1d, 0d },
            0,
            new[] { new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100) },
            new[] { new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100) },
            "https://example.test/customs.svg",
            null,
            new[] { new MapFloorDefinition("main", "기본층", null, -1000, 1000, true) },
            "fallback",
            "https://example.test");

    private static MapMarkerDefinition Marker(string id, string name, double x, double z) =>
        new(
            id,
            "customs-id",
            MapMarkerKind.PmcExtract,
            name,
            new MapWorldPosition(x, 0, z),
            Array.Empty<MapOutlinePoint>(),
            null,
            null,
            null);

    private sealed class FakeHandler(string wikiJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Query.Contains("action=raw", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(wikiJson, Encoding.UTF8, "application/json"),
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 }),
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        }
    }
}
