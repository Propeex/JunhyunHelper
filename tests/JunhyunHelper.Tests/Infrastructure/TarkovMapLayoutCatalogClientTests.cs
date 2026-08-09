using System.Net;
using System.Net.Http;
using System.Text;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Content;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovMapLayoutCatalogClientTests
{
    [Fact]
    public async Task LoadAsync_MatchesAltMapAndPreservesSpatialFloorExtents()
    {
        const string metadata = """
            [
              {
                "normalizedName": "ground-zero",
                "maps": [
                  {
                    "key": "ground-zero",
                    "projection": "interactive",
                    "altMaps": ["ground-zero-21"],
                    "minZoom": 0.5,
                    "maxZoom": 7,
                    "transform": [1.5, 20, 1.5, 40],
                    "coordinateRotation": 5,
                    "bounds": [[-100, -200], [100, 200]],
                    "svgBounds": [[-90, -190], [90, 190]],
                    "svgPath": "https://assets.tarkov.dev/maps/svg/ground-zero.svg",
                    "svgLayer": "level_ground",
                    "heightRange": [-1000, 28],
                    "author": "Map Author",
                    "authorLink": "https://example.test/map-author",
                    "layers": [
                      {
                        "name": "Garage",
                        "svgLayer": "level_garage",
                        "extents": [
                          {
                            "height": [-1000, 21],
                            "bounds": [
                              [[-20, -30], [20, 30], "garage"]
                            ]
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            ]
            """;

        using var httpClient = new HttpClient(new StaticJsonHandler(metadata));
        var client = new TarkovMapLayoutCatalogClient(httpClient);
        var result = await client.LoadAsync(
            [new MapReference("map-gz-21", null, "Ground Zero 21+", "ground-zero-21")],
            TestContext.Current.CancellationToken);

        var layout = Assert.Single(result.Layouts);
        Assert.Equal("map-gz-21", layout.MapId);
        Assert.Equal("ground-zero", layout.NormalizedName);
        Assert.Equal(2, layout.Floors.Count);
        var garage = Assert.Single(layout.Floors, floor => floor.SvgLayer == "level_garage");
        var extent = Assert.Single(garage.Extents);
        var bounds = Assert.Single(extent.Bounds);
        Assert.True(bounds.Contains(0, 0));
        Assert.False(bounds.Contains(50, 0));
        Assert.EndsWith("ground-zero.svg", layout.SvgUrl, StringComparison.Ordinal);
        Assert.Contains("tarkov-dev-svg-maps", layout.SvgUrl, StringComparison.Ordinal);
    }

    private sealed class StaticJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            };
            return Task.FromResult(response);
        }
    }
}
