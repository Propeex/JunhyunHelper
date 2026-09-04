using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V191FinalUiContractTests
{
    [Fact]
    public void Scanner_favorite_action_matches_wiki_height_and_verifies_after_detail_is_visible()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.xaml");
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.DetailActionSmoke.cs");
        var productUsability = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");
        var retiredPolish = Path.Combine(
            root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.V191FinalUiPolish.cs");

        Assert.Contains("Height=\"34\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Padding=\"0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontFamily=\"Segoe UI Symbol\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalContentAlignment=\"Center\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalContentAlignment=\"Center\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ArmDetailActionAlignmentSmoke();", productUsability, StringComparison.Ordinal);
        Assert.DoesNotContain("FavoriteItemButton.Height =", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WikiButton.Height =", source, StringComparison.Ordinal);
        Assert.Contains("SelectedItemPanel.IsVisibleChanged += SelectedItemPanel_DetailActionSmokeIsVisibleChanged", source, StringComparison.Ordinal);
        Assert.Contains("SelectedItemPanel.Visibility != Visibility.Visible", source, StringComparison.Ordinal);
        Assert.Contains("new DispatcherTimer(", source, StringComparison.Ordinal);
        Assert.Contains("Visibility = Visibility.Visible", source, StringComparison.Ordinal);
        Assert.Contains("RestoreDetailActionSmokePageVisibility();", source, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Render", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatcherPriority.ContextIdle", source, StringComparison.Ordinal);
        Assert.False(File.Exists(retiredPolish));
    }

    [Fact]
    public void Map_extract_section_has_exactly_three_visible_donor_filters_and_no_visible_master()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunExtractMarkerFilters.cs");
        var v114Smoke = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunV114MiniMapSmoke.cs");

        Assert.Contains("header.Text = \"탈출구\"", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowPmcExtracts, \"PMC 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowScavExtracts, \"Scav 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowTransitExtracts, \"트랜짓 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("ChkShowExtractMarkers.Visibility = Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("sectionFilters.Length != 3", source, StringComparison.Ordinal);
        Assert.Contains("RunJunhyunV114MiniMapSmokeAndWriteExtractEvidenceAsync(marker)", source, StringComparison.Ordinal);
        Assert.Contains("approved-three-filter-layout=ok", v114Smoke, StringComparison.Ordinal);
        Assert.Contains("actual-transit-marker-render=ok", v114Smoke, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveExistingExtractFilter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapMarkersContent.Children.Add", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MiniMap_initialization_uses_the_current_visible_main_map_selection()
    {
        var root = FindRepositoryRoot();
        var bridge = Read(root, "src", "JunhyunHelper.Desktop", "Map", "LegacyMapSelectionConsistencyBridge.cs");
        var registry = Read(root, "src", "JunhyunHelper.Desktop", "Map", "JunhyunMiniMapProductRegistry.cs");
        var window = Read(root, "src", "JunhyunHelper.Desktop", "Map", "OverlayMiniMapWindow.MapSelectionSync.cs");

        Assert.Contains("_mapSelector.SelectedItem is not ComboBoxItem selected", bridge, StringComparison.Ordinal);
        Assert.Contains("_tracker.SetCurrentMap(canonicalKey)", bridge, StringComparison.Ordinal);
        Assert.Contains("JunhyunMiniMapProductRegistry.SynchronizeMapSelection(canonicalKey)", bridge, StringComparison.Ordinal);

        var registerStart = registry.IndexOf(
            "public static void Register(TarkovHelper.Windows.OverlayMiniMapWindow window)",
            StringComparison.Ordinal);
        var unregisterStart = registry.IndexOf(
            "public static void Unregister(TarkovHelper.Windows.OverlayMiniMapWindow window)",
            StringComparison.Ordinal);
        Assert.True(registerStart >= 0 && unregisterStart > registerStart,
            "Could not isolate the MiniMap product registration boundary.");

        var registerBody = registry[registerStart..unregisterStart];
        var synchronizeIndex = registerBody.IndexOf(
            "_ = LegacyMapSelectionConsistencyBridge.SynchronizeCurrentSelectionNow();",
            StringComparison.Ordinal);
        var activeRegistrationIndex = registerBody.IndexOf(
            "_active = new WeakReference<TarkovHelper.Windows.OverlayMiniMapWindow>(window);",
            StringComparison.Ordinal);
        Assert.True(synchronizeIndex >= 0 && activeRegistrationIndex >= 0 && synchronizeIndex < activeRegistrationIndex,
            "The visible Main Map selection must reach MapTrackerService before the MiniMap becomes the active product window.");

        Assert.Contains("internal void SynchronizeJunhyunMapSelection(string mapKey)", window, StringComparison.Ordinal);
        Assert.Contains("LoadMap(canonical)", window, StringComparison.Ordinal);
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
