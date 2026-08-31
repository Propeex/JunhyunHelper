using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V114MiniMapLifecycleContractTests
{
    [Fact]
    public void Main_map_selection_is_published_synchronously_before_first_minimap_creation()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapSelectionConsistencyBridge.cs");

        var handlerStart = source.IndexOf("private void MapSelector_SelectionChanged", StringComparison.Ordinal);
        Assert.True(handlerStart >= 0);
        var immediate = source.IndexOf("_ = SynchronizeCore();", handlerStart, StringComparison.Ordinal);
        var queued = source.IndexOf("QueueSynchronize();", immediate, StringComparison.Ordinal);

        Assert.True(immediate > handlerStart);
        Assert.True(queued > immediate);
        Assert.Contains("first Loaded", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Player_marker_size_update_does_not_run_whole_minimap_view_refresh()
    {
        var root = FindRepositoryRoot();
        var isolation = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.PlayerMarkerSizeIsolation.cs");
        var registry = Read(root, "src", "JunhyunHelper.Desktop", "Map", "JunhyunMiniMapProductRegistry.cs");

        Assert.Contains("ApplyJunhyunPlayerMarkerSizeOnly", isolation, StringComparison.Ordinal);
        Assert.Contains("PlayerMarkerScale.ScaleX = markerSize", isolation, StringComparison.Ordinal);
        Assert.Contains("PlayerMarkerScale.ScaleY = markerSize", isolation, StringComparison.Ordinal);
        Assert.Contains("SaveSettings();", isolation, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateMapView", isolation, StringComparison.Ordinal);

        Assert.Contains("window.ApplyJunhyunPlayerMarkerSizeOnly(mapPixelSize)", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("window.ApplySharedPlayerMarkerSize(mapPixelSize)", registry, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_standard_marker_layer_is_rebuilt_from_loaded_data_without_starting_another_refresh()
    {
        var root = FindRepositoryRoot();
        var recovery = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.MarkerRefreshRecovery.cs");

        Assert.Contains("HasExpectedStandardMarkers()", recovery, StringComparison.Ordinal);
        Assert.Contains("MapMarkerDbService.Instance", recovery, StringComparison.Ordinal);
        Assert.Contains("MiniMapMarkerVisibilityState.Capture(MapSettings.Instance)", recovery, StringComparison.Ordinal);
        Assert.Contains("RebuildStandardMarkerLayerFromLoadedData();", recovery, StringComparison.Ordinal);
        Assert.Contains("CreateMapMarkerElement(marker, screenX, screenY, isCurrentFloor)", recovery, StringComparison.Ordinal);
        Assert.Contains("SynchronizeGeneralMarkerScale(force: true)", recovery, StringComparison.Ordinal);
        Assert.DoesNotContain("QueueMarkerRefresh();", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Mini_scanner_has_no_right_click_context_menu_and_keeps_left_drag_surface()
    {
        var root = FindRepositoryRoot();
        var xamlPath = Path.Combine(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml");
        var correctionPath = Path.Combine(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.Correction.cs");
        var xaml = File.ReadAllText(xamlPath);

        Assert.DoesNotContain("<ContextMenu", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentCorrectionMenuItem_Click", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonDown=\"DragSurface_PreviewMouseLeftButtonDown\"", xaml, StringComparison.Ordinal);
        Assert.False(File.Exists(correctionPath));
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
