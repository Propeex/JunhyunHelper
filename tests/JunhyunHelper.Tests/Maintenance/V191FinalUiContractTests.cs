using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V191FinalUiContractTests
{
    [Fact]
    public void Scanner_favorite_action_matches_wiki_height_and_verifies_after_detail_is_visible()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.V191FinalUiPolish.cs");
        var productUsability = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");

        Assert.Contains("ApplyV191DetailActionAlignment();", productUsability, StringComparison.Ordinal);
        Assert.DoesNotContain("V191DetailActionHandlerRegistered", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EventManager.RegisterClassHandler", source, StringComparison.Ordinal);
        Assert.Contains("private const double V191DetailActionHeight = 34d", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteItemButton.Height = V191DetailActionHeight", source, StringComparison.Ordinal);
        Assert.Contains("WikiButton.Height = V191DetailActionHeight", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteItemButton.Padding = new Thickness(0)", source, StringComparison.Ordinal);
        Assert.Contains("new FontFamily(\"Segoe UI Symbol\")", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteItemButton.HorizontalContentAlignment = HorizontalAlignment.Center", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteItemButton.VerticalContentAlignment = VerticalAlignment.Center", source, StringComparison.Ordinal);
        Assert.Contains("SelectedItemPanel.IsVisibleChanged += SelectedItemPanel_V191SmokeIsVisibleChanged", source, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Render", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatcherPriority.ContextIdle", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_extract_section_has_exactly_three_visible_donor_filters_and_no_visible_master()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunExtractMarkerFilters.cs");

        Assert.Contains("header.Text = \"탈출구\"", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowPmcExtracts, \"PMC 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowScavExtracts, \"Scav 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("NormalizeApprovedExtractCheckBox(ChkShowTransitExtracts, \"트랜짓 탈출구\")", source, StringComparison.Ordinal);
        Assert.Contains("ChkShowExtractMarkers.Visibility = Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("sectionFilters.Length != 3", source, StringComparison.Ordinal);
        Assert.Contains("approved-three-filter-layout=ok", source, StringComparison.Ordinal);
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
