using System.Net;
using System.Net.Http;
using System.Text;
using JunhyunHelper.Core.Reference;
using JunhyunHelper.Infrastructure.Content;
using Xunit;

namespace JunhyunHelper.Tests.Infrastructure;

public sealed class TarkovMapLayoutCatalogClientTests
{
    private const string Revision = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task LoadAsync_UsesAtomicLatestLegacyBundleAndPreservesSpatialFloorExtents()
    {
        using var httpClient = new HttpClient(new RoutingHandler(latestBundleAvailable: true));
        var client = new TarkovMapLayoutCatalogClient(httpClient);
        var result = await client.LoadAsync(
            [new MapReference("map-gz-21", null, "Ground Zero 21+", "ground-zero-21")],
            TestContext.Current.CancellationToken);

        var layout = Assert.Single(result.Layouts);
        Assert.Equal("map-gz-21", layout.MapId);
        Assert.Equal("ground-zero", layout.NormalizedName);
        Assert.Equal("legacy-GroundZero", layout.Key);
        Assert.True(layout.UsesLegacyAffineTransform);

        // These values intentionally differ from the pinned fallback. If this assertion
        // passes, the same resolved upstream revision supplied both config/calibration
        // and the SVG URL instead of silently using the built-in known-good template.
        Assert.Equal([-3d, 0d, 0d, 3d, 1700d, 1400d], layout.LegacyPlayerTransform);
        Assert.Equal(3000, layout.SurfaceWidth);
        Assert.Equal(3300, layout.SurfaceHeight);
        Assert.Contains($"/{Revision}/", layout.SvgUrl, StringComparison.Ordinal);
        Assert.EndsWith("/GroundZero.svg", layout.SvgUrl, StringComparison.Ordinal);

        Assert.Equal(4, layout.Floors.Count);
        var basement = Assert.Single(layout.Floors, floor => floor.Id == "basement");
        Assert.Equal("basement", basement.SvgLayer);
        var extent = Assert.Single(basement.Extents);
        var bounds = Assert.Single(extent.Bounds);
        Assert.True(bounds.Contains(0, 0));
        Assert.False(bounds.Contains(50, 0));

        Assert.Contains(Revision[..8], layout.Attribution, StringComparison.Ordinal);
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Contains("pinned known-good", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LoadAsync_WhenLatestLegacyBundleFails_KeepsPinnedKnownGoodBundle()
    {
        using var httpClient = new HttpClient(new RoutingHandler(latestBundleAvailable: false));
        var client = new TarkovMapLayoutCatalogClient(httpClient);
        var result = await client.LoadAsync(
            [new MapReference("map-gz", null, "Ground Zero", "ground-zero")],
            TestContext.Current.CancellationToken);

        var layout = Assert.Single(result.Layouts);
        Assert.Equal([-2d, 0d, 0d, 2d, 1600d, 1301.5d], layout.LegacyPlayerTransform);
        Assert.Equal(2800, layout.SurfaceWidth);
        Assert.Equal(3100, layout.SurfaceHeight);
        Assert.Contains("9371c4769d8da8acb9df864a2c88f83ecdd42818", layout.SvgUrl, StringComparison.Ordinal);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("pinned known-good", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RoutingHandler(bool latestBundleAvailable) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            if (string.Equals(url, TarkovMapLayoutCatalogClient.MetadataUrl, StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, TarkovMetadata(), request));

            if (url.Contains("api.github.com/repos/Propeex/Tarkov-Helper/commits/main", StringComparison.Ordinal))
            {
                return Task.FromResult(latestBundleAvailable
                    ? Json(HttpStatusCode.OK, $$"""{"sha":"{{Revision}}"}""", request)
                    : Json(HttpStatusCode.ServiceUnavailable, "{}", request));
            }

            if (url.Contains($"/{Revision}/TarkovHelper/Assets/DB/Data/map_configs.json", StringComparison.Ordinal))
                return Task.FromResult(Json(HttpStatusCode.OK, LegacyConfig(), request));

            return Task.FromResult(Json(HttpStatusCode.NotFound, "{}", request));
        }

        private static HttpResponseMessage Json(
            HttpStatusCode status,
            string json,
            HttpRequestMessage request) =>
            new(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                RequestMessage = request,
            };
    }

    private static string TarkovMetadata() => """
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

    private static string LegacyConfig() => """
        {
          "maps": [
            {
              "key": "GroundZero",
              "displayName": "Ground Zero",
              "svgFileName": "GroundZero.svg",
              "imageWidth": 3000,
              "imageHeight": 3300,
              "aliases": ["groundzero", "ground-zero", "ground-zero-21", "Sandbox"],
              "playerMarkerTransform": [-3, 0, 0, 3, 1700, 1400],
              "floors": [
                {"layerId":"basement","displayName":"Basement","order":-1,"isDefault":false},
                {"layerId":"main","displayName":"Ground Floor","order":0,"isDefault":true},
                {"layerId":"level2","displayName":"Level 2","order":1,"isDefault":false},
                {"layerId":"level3","displayName":"Level 3","order":2,"isDefault":false}
              ]
            }
          ]
        }
        """;
}
