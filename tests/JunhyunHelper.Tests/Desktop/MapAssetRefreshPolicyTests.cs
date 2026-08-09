using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Maps;
using JunhyunHelper.Desktop.Services;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapAssetRefreshPolicyTests
{
    [Fact]
    public void Content_fingerprint_is_stable_when_marker_order_changes()
    {
        var first = Marker("a", "map", 10, 20);
        var second = Marker("b", "map", 30, 40);

        var left = Catalog([first, second]);
        var right = Catalog([second, first]);

        Assert.Equal(
            MapAssetRefreshPolicy.ComputeContentFingerprint(left),
            MapAssetRefreshPolicy.ComputeContentFingerprint(right));
    }

    [Fact]
    public void Content_fingerprint_changes_when_a_map_coordinate_changes()
    {
        var before = Catalog([Marker("a", "map", 10, 20)]);
        var after = Catalog([Marker("a", "map", 10.25, 20)]);

        Assert.NotEqual(
            MapAssetRefreshPolicy.ComputeContentFingerprint(before),
            MapAssetRefreshPolicy.ComputeContentFingerprint(after));
    }

    private static GameContentCatalog Catalog(IReadOnlyList<MapMarkerDefinition> markers) =>
        new(
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            MapMarkerData: markers);

    private static MapMarkerDefinition Marker(
        string id,
        string mapId,
        double x,
        double z) =>
        new(
            id,
            mapId,
            MapMarkerKind.PmcExtract,
            id,
            new MapWorldPosition(x, 0, z),
            [],
            null,
            null,
            null);
}
