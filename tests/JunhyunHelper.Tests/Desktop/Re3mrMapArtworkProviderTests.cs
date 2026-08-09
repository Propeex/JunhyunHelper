using System.Net;
using System.Net.Http.Headers;
using System.Text;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Services;
using SkiaSharp;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class Re3mrMapArtworkProviderTests
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
    public async Task Builds_ground_zero_from_online_revision_and_canonical_markers()
    {
        var image = BuildArtwork();
        using var client = new HttpClient(new FakeHandler(image));
        var provider = new Re3mrMapArtworkProvider(client);
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
            Assert.Equal("re3mr", result.ProviderId);
            Assert.Contains("0.3C", result.SourceRevision);
            Assert.Contains("RE3MR", result.Attribution);
            Assert.True(File.Exists(destination));
            Assert.True(File.Exists(Path.Combine(
                root,
                "map-cache",
                "candidate",
                "providers",
                "re3mr",
                "groundzero",
                "state.json")));
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
            "https://example.test/groundzero.svg",
            null,
            [new MapFloorDefinition("main", "기본층", null, -1000, 1000, true)],
            null,
            null);

    private static byte[] BuildArtwork()
    {
        using var bitmap = new SKBitmap(1000, 1000);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(24, 26, 30));
        using var road = new SKPaint { Color = new SKColor(85, 89, 96), IsAntialias = false };
        using var building = new SKPaint { Color = new SKColor(150, 165, 145), IsAntialias = false };
        using var anchorPaint = new SKPaint { Color = new SKColor(20, 185, 180), IsAntialias = false };

        canvas.DrawRect(180, 100, 520, 50, road);
        canvas.DrawRect(360, 130, 70, 680, road);
        canvas.DrawRect(460, 220, 210, 190, building);
        canvas.DrawRect(230, 470, 190, 250, building);
        canvas.DrawRect(610, 550, 180, 260, building);

        foreach (var anchor in Anchors)
            canvas.DrawCircle((float)(anchor.U * 999), (float)(anchor.V * 999), 17, anchorPaint);

        using var encoded = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private sealed class FakeHandler(byte[] image) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host.Equals("reemr.se", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "<html><body><p>Version 0.3C</p><a href=\"https://www.re3mr.com/maps/Groundzero/GroundZero.png\">map</a></body></html>",
                        Encoding.UTF8,
                        "text/html"),
                });
            }

            var content = new ByteArrayContent(image);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content,
            });
        }
    }
}
