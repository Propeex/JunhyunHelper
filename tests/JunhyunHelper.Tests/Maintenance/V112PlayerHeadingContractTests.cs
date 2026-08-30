using System.Runtime.CompilerServices;
using System.Text.Json;
using JunhyunHelper.Core.Maps;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V112PlayerHeadingContractTests
{
    [Theory]
    [InlineData(0.0, -2.0, 0.0, 0.0, 2.0, 0.0)]
    [InlineData(90.0, -2.0, 0.0, 0.0, 2.0, 90.0)]
    [InlineData(0.0, 0.0, -10.0, -10.0, 0.0, 90.0)]
    [InlineData(90.0, 0.0, -10.0, -10.0, 0.0, 180.0)]
    [InlineData(0.0, 0.0, 10.0, 10.0, 0.0, 270.0)]
    [InlineData(90.0, 0.0, 10.0, 10.0, 0.0, 0.0)]
    [InlineData(0.0, -1.932, 0.518, 0.517, 1.932, 344.991)]
    [InlineData(0.0, 0.0159, 9.863, 9.8655, 0.0502, 270.292)]
    public void Affine_projection_matches_known_map_orientations(
        double rawAngle,
        double a,
        double b,
        double c,
        double d,
        double expected)
    {
        var actual = PlayerHeadingProjection.Project(rawAngle, a, b, c, d);
        AssertAngle(expected, actual, 0.02);
    }

    [Fact]
    public void Current_player_map_transforms_all_produce_finite_normalized_headings()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "vendor",
            "Tarkov-Helper",
            "TarkovHelper",
            "Assets",
            "DB",
            "Data",
            "map_configs.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var maps = document.RootElement.GetProperty("maps").EnumerateArray().ToArray();

        Assert.NotEmpty(maps);
        foreach (var map in maps)
        {
            var key = map.GetProperty("key").GetString();
            Assert.True(map.TryGetProperty("playerMarkerTransform", out var transformElement),
                $"Map '{key}' has no playerMarkerTransform.");
            var transform = transformElement.EnumerateArray().Select(value => value.GetDouble()).ToArray();
            Assert.True(transform.Length >= 6, $"Map '{key}' has an incomplete playerMarkerTransform.");

            foreach (var rawAngle in new[] { 0.0, 90.0, 180.0, 270.0 })
            {
                var projected = PlayerHeadingProjection.Project(
                    rawAngle,
                    transform[0],
                    transform[1],
                    transform[2],
                    transform[3]);
                Assert.True(double.IsFinite(projected), $"Map '{key}' produced a non-finite heading.");
                Assert.InRange(projected, 0.0, 360.0);
            }
        }
    }

    [Fact]
    public void Runtime_applies_the_same_projected_heading_after_main_map_and_minimap_donor_rendering()
    {
        var root = FindRepositoryRoot();
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyPlayerHeadingBridge.cs");
        var runtime = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapProductRuntime.cs");
        var registry = Read(root, "src", "JunhyunHelper.Desktop", "Map", "JunhyunMiniMapProductRegistry.cs");
        var mapPartial = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunPlayerHeading.cs");
        var miniMapPartial = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.JunhyunPlayerHeading.cs");

        Assert.Contains("PlayerMarkerTransform", bridge, StringComparison.Ordinal);
        Assert.Contains("PlayerHeadingProjection.Project", bridge, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.ContextIdle", bridge, StringComparison.Ordinal);
        Assert.Contains("_page.ApplyJunhyunPlayerHeading(projectedAngle);", bridge, StringComparison.Ordinal);
        Assert.Contains("JunhyunMiniMapProductRegistry.ApplyPlayerHeadingAfterDonor(projectedAngle);", bridge, StringComparison.Ordinal);

        Assert.Contains("_playerHeadingBridge = new LegacyPlayerHeadingBridge(page);", runtime, StringComparison.Ordinal);
        Assert.Contains("_playerHeadingBridge.Dispose();", runtime, StringComparison.Ordinal);
        Assert.Contains("ApplyPlayerHeadingAfterDonor", registry, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.ContextIdle", registry, StringComparison.Ordinal);
        Assert.Contains("window.ApplyJunhyunPlayerHeading(angle)", registry, StringComparison.Ordinal);
        Assert.Contains("MarkerRotation.Angle = angle", mapPartial, StringComparison.Ordinal);
        Assert.Contains("PlayerRotation.Angle = angle", miniMapPartial, StringComparison.Ordinal);
    }

    private static void AssertAngle(double expected, double actual, double tolerance)
    {
        var delta = Math.Abs(expected - actual) % 360.0;
        if (delta > 180.0)
            delta = 360.0 - delta;
        Assert.InRange(delta, 0.0, tolerance);
    }

    private static string Read(string root, params string[] path) =>
        File.ReadAllText(Path.Combine([root, .. path]));

    private static string FindRepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)
            ?? throw new InvalidOperationException("Test source path is unavailable."));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "src")) &&
                Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the JunhyunHelper repository root.");
    }
}
