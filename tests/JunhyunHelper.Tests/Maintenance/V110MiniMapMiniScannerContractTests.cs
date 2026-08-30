using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V110MiniMapMiniScannerContractTests
{
    [Fact]
    public void MiniMap_reused_window_show_boundary_resynchronizes_visible_main_map()
    {
        var root = FindRepositoryRoot();
        var runtime = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapProductRuntime.cs");
        var donorService = Read(root, "vendor", "Tarkov-Helper", "TarkovHelper", "Services", "OverlayMiniMapService.cs");

        Assert.Contains("_overlay.OverlayVisibilityChanged += Overlay_VisibilityChanged;", runtime, StringComparison.Ordinal);
        Assert.Contains("private void Overlay_VisibilityChanged(bool visible)", runtime, StringComparison.Ordinal);
        Assert.Contains("if (_disposed || !visible)", runtime, StringComparison.Ordinal);
        Assert.Contains("LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow();", runtime, StringComparison.Ordinal);
        Assert.Contains("_overlay.OverlayVisibilityChanged -= Overlay_VisibilityChanged;", runtime, StringComparison.Ordinal);

        // This donor behavior is the regression trigger: Hide keeps the loaded Window alive,
        // and ShowOverlayCore reuses it. SourceInitialized/Loaded therefore cannot be the only
        // synchronization boundary.
        Assert.Contains("_overlayWindow?.Hide();", donorService, StringComparison.Ordinal);
        Assert.Contains("if (_overlayWindow == null)", donorService, StringComparison.Ordinal);
        Assert.Contains("_overlayWindow!.Show();", donorService, StringComparison.Ordinal);
    }

    [Fact]
    public void MiniScanner_flea_minimum_remains_compatibility_data_but_is_not_presented()
    {
        var root = FindRepositoryRoot();
        var catalogItem = Read(root, "src", "JunhyunHelper.Core", "Scanner", "ScannerCatalogItem.cs");
        var displaySettings = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerDisplaySettings.cs");
        var window = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml.cs");
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "MiniScannerWindow.xaml");
        var catalogService = Read(root, "src", "JunhyunHelper.Infrastructure", "Scanner", "ScannerCatalogService.cs");

        Assert.Contains("public int? FleaMinimumPrice { get; init; }", catalogItem, StringComparison.Ordinal);
        Assert.Contains("CurrentCacheSchemaVersion = 4", catalogService, StringComparison.Ordinal);
        Assert.Contains("GetInt(raw, \"lastLowPrice\")", catalogService, StringComparison.Ordinal);
        Assert.Contains("CurrentSchemaVersion = 8", displaySettings, StringComparison.Ordinal);
        Assert.Contains("FleaMinimumPriceField = \"flea_minimum_price\"", displaySettings, StringComparison.Ordinal);
        Assert.Contains("ScannerInfoOrderPolicy.Normalize", displaySettings, StringComparison.Ordinal);
        Assert.Contains("ShowFleaMinimumPrice", displaySettings, StringComparison.Ordinal);

        // v1.11 removes only the product presentation. The compatibility field remains in
        // persisted settings and Scanner catalog data so old settings/data can migrate
        // without destructive schema churn.
        Assert.DoesNotContain("x:Name=\"FleaMinimumPriceText\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("플리 최저", window, StringComparison.Ordinal);
        Assert.DoesNotContain("[ScannerDisplaySettings.FleaMinimumPriceField]", window, StringComparison.Ordinal);
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
