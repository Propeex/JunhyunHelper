using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V111MapMaintenanceContractTests
{
    [Fact]
    public void First_minimap_open_replays_latest_main_map_selection()
    {
        var root = FindRepositoryRoot();
        var registry = Read(root, "src", "JunhyunHelper.Desktop", "Map", "JunhyunMiniMapProductRegistry.cs");

        Assert.Contains("private static string? _latestMapKey;", registry, StringComparison.Ordinal);
        Assert.Contains("_latestMapKey = mapKey;", registry, StringComparison.Ordinal);
        Assert.Contains("latestMapKey = _latestMapKey;", registry, StringComparison.Ordinal);
        Assert.Contains("window.SynchronizeJunhyunMapSelection(latestMapKey);", registry, StringComparison.Ordinal);

        var unregisterStart = registry.IndexOf("public static void Unregister", StringComparison.Ordinal);
        var unregisterEnd = registry.IndexOf("public static void ZoomIn", unregisterStart, StringComparison.Ordinal);
        Assert.True(unregisterStart >= 0 && unregisterEnd > unregisterStart);
        var unregisterBody = registry[unregisterStart..unregisterEnd];
        Assert.DoesNotContain("_latestMapKey = null", unregisterBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Extract_filter_rows_retry_until_late_donor_controls_exist()
    {
        var root = FindRepositoryRoot();
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapMarkerSettingsV2Bridge.cs");
        var recovery = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.MarkerRefreshRecovery.cs");

        Assert.Contains("TryMoveExtractRows();", bridge, StringComparison.Ordinal);
        Assert.Contains("var extractsReady = TryMoveExtractRows();", bridge, StringComparison.Ordinal);
        Assert.Contains("if (extractsReady || _retries >= 24)", bridge, StringComparison.Ordinal);
        Assert.Contains("IsDescendantOf(checkBox, destination)", bridge, StringComparison.Ordinal);

        Assert.Contains("RepairEmptyExtractProjectionIfNeeded();", recovery, StringComparison.Ordinal);
        Assert.Contains("SynchronizeExtractPresentation(force: true);", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_marker_resize_reapplies_minimap_presentation_and_empty_marker_layer_recovers_once()
    {
        var root = FindRepositoryRoot();
        var registry = Read(root, "src", "JunhyunHelper.Desktop", "Map", "JunhyunMiniMapProductRegistry.cs");
        var repair = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.MarkerPresentationRepair.cs");
        var recovery = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.MarkerRefreshRecovery.cs");

        Assert.Contains("window.ApplySharedPlayerMarkerSize(mapPixelSize);", registry, StringComparison.Ordinal);
        Assert.Contains("window.ReapplyJunhyunMarkerPresentationAfterDonorMapView();", registry, StringComparison.Ordinal);
        Assert.Contains("SynchronizeGeneralMarkerScale(force: true);", repair, StringComparison.Ordinal);
        Assert.Contains("ApplyJunhyunAdditionalMarkerScale(child);", repair, StringComparison.Ordinal);
        Assert.Contains("ApplyJunhyunMarkerVisualScale(child);", repair, StringComparison.Ordinal);

        Assert.Contains("_junhyunLastStableStandardMarkerCount", recovery, StringComparison.Ordinal);
        Assert.Contains("if (_junhyunEmptyStandardMarkerTicks < 2)", recovery, StringComparison.Ordinal);
        Assert.Contains("_junhyunStandardMarkerRecoveryAttempted = true;", recovery, StringComparison.Ordinal);
        Assert.Contains("QueueMarkerRefresh();", recovery, StringComparison.Ordinal);
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
