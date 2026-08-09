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
    public async Task LoadAsync_UsesLegacyGroundZeroArtworkAndPreservesSpatialFloorExtents()
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
        Assert.Equal("legacy-GroundZero", layout.Key);
        Assert.True(layout.UsesLegacyAffineTransform);
        Assert.Equal([ -2d, 0d, 0d, 2d, 1600d, 1301.5d ], layout.LegacyPlayerTransform);
        Assert.Equal(2800, layout.SurfaceWidth);
        Assert.Equal(3100, layout.SurfaceHeight);
        Assert.Equal(4, layout.Floors.Count);

        var basement = Assert.Single(layout.Floors, floor => floor.Id == "basement");
        Assert.Equal("basement", basement.SvgLayer);
        var extent = Assert.Single(basement.Extents);
        var bounds = Assert.Single(extent.Bounds);
        Assert.True(bounds.Contains(0, 0));
        Assert.False(bounds.Contains(50, 0));

        Assert.EndsWith("/GroundZero.svg", layout.SvgUrl, StringComparison.Ordinal);
        Assert.Contains("Propeex/Tarkov-Helper", layout.SvgUrl, StringComparison.Ordinal);
        Assert.DoesNotContain("tarkov-dev-svg-maps", layout.SvgUrl, StringComparison.Ordinal);
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