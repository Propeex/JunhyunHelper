using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Services;
using SkiaSharp;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class GroundZeroRe3mrArtworkProviderV2Tests
{
    private static readonly (string Name, double U, double V)[] Anchors =
    [
        ("Emercom Checkpoint", 0.600, 0.137),
        ("Scav Checkpoint (Co-Op)", 0.758, 0.137),
        ("Mira Ave", 0.506, 0.357),
        ("Police Cordon V-Ex", 0.824, 0.477),
        ("Nakatani Basement Stairs", 0.807, 0.849),
    ];

    [Fact]
    public async Task Builds_re3mr_ground_floor_and_keeps_online_alternate_floors()
    {
        var image = BuildArtwork();
        var schematic = BuildSchematic();
        using var client = new HttpClient(new FakeHandler(image, schematic));
        var provider = new GroundZeroRe3mrArtworkProviderV2(client);
        var root = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "map-cache", "candidate", "svg", "map-id-groundzero.svg");

        try
        {
            var markers = Anchors
                .Select((anchor, index) => new MapMarkerDefinition(
                    $"marker-{index}",
                    "map-id",
                    MapMarkerKind.SharedExtract,
                    anchor.Name,
                    new MapWorldPosition(anchor.U * 100, 0, anchor.V * 100),
                    [],
                    null,
                    null,
                    null))
                .ToArray();

            var result = await provider.TryBuildAlignedSvgAsync(
                Layout(),
                markers,
                destination,
                TestContext.Current.CancellationToken);

            Assert.True(result.Applied, result.Warning);
            Assert.Equal("re3mr-groundzero-floor-aware", result.ProviderId);
            Assert.Contains("0.3C", result.SourceRevision);

            var document = XDocument.Load(destination);
            var groups = document
                .Descendants()
                .Where(element => element.Name.LocalName == "g")
                .Select(element => element.Attribute("id")?.Value)
                .Where(value => value is not null)
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains("Ground_Level", groups);
            Assert.Contains("Second_Floor", groups);
            Assert.Contains("Third_Floor", groups);
            Assert.Contains("Underground_Level", groups);
            Assert.Contains("re3mr-floor-aware-v2", document.Root?.Attribute("data-junhyun-helper-artwork")?.Value);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static MapLayoutDefinition Layout() =>
        new(
            "map-id",
            "groundzero",
            "groundzero",
            0,
            4,
            [1d, 0d, -1d, 0d],
            0,
            [new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100)],
            [new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100)],
            "https://example.test/GroundZero.svg",
            null,
            [
                new MapFloorDefinition("main", "기본층", "Ground_Level", -1000, 28, true),
                new MapFloorDefinition("second", "2층", "Second_Floor", 28, 32.3, false),
                new MapFloorDefinition("third", "3층", "Third_Floor", 32.3, 1000, false),
                new MapFloorDefinition("garage", "지하", "Underground_Level", -1000, 21, false),
            ],
            null,
            null);

    private static byte[] BuildArtwork()
    {
        using var bitmap = new SKBitmap(1000, 1000);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(20, 20, 20));
        using var road = new SKPaint { Color = new SKColor(90, 90, 90), IsAntialias = false };
        using var building = new SKPaint { Color = new SKColor(150, 145, 130), IsAntialias = false };
        using var anchorPaint = new SKPaint { Color = new SKColor(10, 220, 205), IsAntialias = false };

        canvas.DrawRect(180, 100, 520, 50, road);
        canvas.DrawRect(360, 130, 70, 680, road);
        canvas.DrawRect(460, 220, 210, 190, building);
        canvas.DrawRect(230, 470, 190, 250, building);
        canvas.DrawRect(610, 550, 180, 260, building);

        foreach (var anchor in Anchors)
            canvas.DrawCircle((float)(anchor.U * 999), (float)(anchor.V * 999), 18, anchorPaint);

        using var encoded = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static byte[] BuildSchematic()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
              <rect width="100" height="100" fill="#111"/>
              <g id="Ground_Level" style="display:block"><rect x="10" y="10" width="20" height="20" fill="#555"/></g>
              <g id="Second_Floor" style="display:none"><rect x="30" y="30" width="20" height="20" fill="#777"/></g>
              <g id="Third_Floor" style="display:none"><rect x="50" y="50" width="20" height="20" fill="#999"/></g>
              <g id="Underground_Level" style="display:none"><rect x="70" y="70" width="20" height="20" fill="#bbb"/></g>
            </svg>
            """;
        return Encoding.UTF8.GetBytes(svg);
    }

    private sealed class FakeHandler(byte[] image, byte[] schematic) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException();
            if (uri.Host.Equals("reemr.se", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "<html><body><p>Version 0.3C</p><a href=\"https://www.re3mr.com/maps/Groundzero/GroundZero.png\">map</a></body></html>",
                        Encoding.UTF8,
                        "text/html"),
                });
            }

            if (uri.Host.Equals("example.test", StringComparison.OrdinalIgnoreCase))
            {
                var svgContent = new ByteArrayContent(schematic);
                svgContent.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = svgContent,
                });
            }

            var imageContent = new ByteArrayContent(image);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = imageContent,
            });
        }
    }
}
