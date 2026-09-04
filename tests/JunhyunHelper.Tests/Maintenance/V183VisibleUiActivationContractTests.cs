using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V183VisibleUiActivationContractTests
{
    [Fact]
    public void AmmoVisibleDropdowns_ActivateDuringPageInitialization()
    {
        var root = FindRepositoryRoot();
        var lifecycle = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.ProductGridFixes.cs");
        var activation = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.CaliberSelectorSmoke.cs");

        Assert.Contains("protected override void OnInitialized(EventArgs e)", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ApplyProductCaliberDropdownPolish();", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureProductVisibleDropdownInitialization", lifecycle, StringComparison.Ordinal);
        Assert.DoesNotContain("_productVisibleDropdownActivatedFromInitialization", activation, StringComparison.Ordinal);
        Assert.Contains("VerifyProductCaliberSelectorInitialization", activation, StringComparison.Ordinal);
        Assert.DoesNotContain("FavoriteCaliberMenuButton", activation, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(CaliberComboBox.ItemTemplate, FavoriteCaliberComboBox.ItemTemplate)", activation, StringComparison.Ordinal);
    }

    [Fact]
    public void AmmoPublishedSmoke_CannotRepairMissingProductInitialization()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.CaliberSelectorSmoke.cs");

        Assert.Contains("typeof(MainWindow)", source, StringComparison.Ordinal);
        Assert.Contains("window.AmmoPage.VerifyProductCaliberSelectorInitialization();", source, StringComparison.Ordinal);
        Assert.Contains("cannot repair a missing real product path", source, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(88)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MapMarkerCheckboxViewport_ActivatesFromRealLoadedWithoutAdvancingDonorConstruction()
    {
        var root = FindRepositoryRoot();
        var loaded = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunMarkerPanelPolish.cs");
        var body = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunMarkerPanelBodyLayout.cs");

        Assert.Contains("FrameworkElement.LoadedEvent", loaded, StringComparison.Ordinal);
        Assert.Contains("if (sender is not MapPage page)", loaded, StringComparison.Ordinal);
        Assert.DoesNotContain("ReferenceEquals(e.OriginalSource, page)", loaded, StringComparison.Ordinal);
        Assert.Contains("page.ScheduleJunhyunMarkerPanelPolish(DispatcherPriority.Loaded);", loaded, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.ContextIdle", loaded, StringComparison.Ordinal);
        Assert.Contains("TryResolveOrWrapJunhyunMarkerListViewport()", loaded, StringComparison.Ordinal);
        Assert.Contains("MapMarkersOverlay.Child is not Panel overlayContent", loaded, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(child.Content, MapMarkersContent)", loaded, StringComparison.Ordinal);
        Assert.Contains("overlayContent.Children.IndexOf(MapMarkersContent)", loaded, StringComparison.Ordinal);
        Assert.Contains("FailJunhyunMarkerPanelActivationSmoke();", loaded, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(89)", loaded, StringComparison.Ordinal);
        Assert.Contains("ActivateProductMarkerPanelBodyLayout();", loaded, StringComparison.Ordinal);

        var wrapGuard = loaded.IndexOf("if (!TryResolveOrWrapJunhyunMarkerListViewport())", StringComparison.Ordinal);
        var applied = loaded.IndexOf("_junhyunMarkerPanelPolishApplied = true;", StringComparison.Ordinal);
        var activate = loaded.IndexOf("ActivateProductMarkerPanelBodyLayout();", StringComparison.Ordinal);
        Assert.True(wrapGuard >= 0 && applied > wrapGuard && activate > applied,
            "Map marker polish must not mark activation complete or activate body layout before viewport insertion succeeds.");

        Assert.DoesNotContain("protected override void OnInitialized", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyJunhyunUiSimplification();", body, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment = VerticalAlignment.Stretch", body, StringComparison.Ordinal);
        Assert.Contains("var maximumPanelHeight = Math.Max(120, mapHeight - 16);", body, StringComparison.Ordinal);
        Assert.Contains("var panelHeight = maximumPanelHeight;", body, StringComparison.Ordinal);
        Assert.Contains("panelHeight - headerHeight - verticalChrome", body, StringComparison.Ordinal);
        Assert.Contains("_junhyunMarkerListViewport.Height = listHeight", body, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility = ScrollBarVisibility.Auto", body, StringComparison.Ordinal);
        Assert.Contains("var expectedPanelHeight = Math.Max(120, mapHeight - 16);", body, StringComparison.Ordinal);
        Assert.Contains("ScrollableHeight > 0.5", body, StringComparison.Ordinal);
        Assert.Contains("ComputedVerticalScrollBarVisibility", body, StringComparison.Ordinal);
        Assert.Contains("marker-panel-uses-available-height=ok", body, StringComparison.Ordinal);
        Assert.Contains("marker-list-fills-panel-body=ok", body, StringComparison.Ordinal);
        Assert.Contains("scrollbar-only-on-real-overflow=ok", body, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(89)", body, StringComparison.Ordinal);
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
