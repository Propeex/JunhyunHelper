using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Services;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapArtworkProviderPipelineTests
{
    [Fact]
    public async Task Falls_through_rejected_provider_and_keeps_next_valid_provider()
    {
        var root = Path.Combine(Path.GetTempPath(), "JunhyunHelper.Tests", Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "map.svg");
        Directory.CreateDirectory(root);

        try
        {
            var pipeline = new MapArtworkProviderPipeline(
            [
                new FakeProvider("preferred", applied: false, writesPartialFile: true),
                new FakeProvider("fallback", applied: true, writesPartialFile: false),
            ]);

            var result = await pipeline.TryBuildAlignedSvgAsync(
                Layout(),
                [],
                destination,
                TestContext.Current.CancellationToken);

            Assert.True(result.Applied);
            Assert.Equal("fallback", result.ProviderId);
            Assert.Equal("fallback", await File.ReadAllTextAsync(
                destination,
                TestContext.Current.CancellationToken));
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
            "map",
            "map",
            0,
            4,
            [1d, 0d, 1d, 0d],
            0,
            [new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100)],
            [new MapBoundsPoint(0, 0), new MapBoundsPoint(100, 100)],
            "https://example.test/map.svg",
            null,
            [new MapFloorDefinition("main", "기본층", null, -1000, 1000, true)],
            null,
            null);

    private sealed class FakeProvider(
        string id,
        bool applied,
        bool writesPartialFile) : IMapArtworkProvider
    {
        public string ProviderId => id;

        public async Task<MapArtworkProviderResult> TryBuildAlignedSvgAsync(
            MapLayoutDefinition layout,
            IReadOnlyList<MapMarkerDefinition> canonicalMarkers,
            string destination,
            CancellationToken cancellationToken = default)
        {
            if (writesPartialFile)
                await File.WriteAllTextAsync(destination, "partial", cancellationToken);
            if (applied)
                await File.WriteAllTextAsync(destination, id, cancellationToken);

            return new MapArtworkProviderResult(
                applied,
                applied ? id : null,
                "test-revision",
                applied ? id : null,
                null,
                applied ? null : "rejected");
        }
    }
}
