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
        var activation = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.VisibleDropdownActivation.cs");

        Assert.Contains("protected override void OnInitialized(EventArgs e)", lifecycle, StringComparison.Ordinal);
        Assert.Contains("EnsureProductVisibleDropdownInitialization();", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ApplyProductCaliberDropdownPolish();", activation, StringComparison.Ordinal);
        Assert.Contains("_productVisibleDropdownActivatedFromInitialization = true", activation, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterProductVisibleDropdownActivation", activation, StringComparison.Ordinal);
        Assert.Contains("VerifyProductVisibleDropdownInitialization", activation, StringComparison.Ordinal);
        Assert.Contains("FavoriteCaliberMenuButton.Visibility != Visibility.Collapsed", activation, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(CaliberComboBox.ItemTemplate, _productFavoriteCaliberComboBox.ItemTemplate)", activation, StringComparison.Ordinal);
    }

    [Fact]
    public void AmmoPublishedSmoke_CannotRepairMissingProductInitialization()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.VisibleDropdownActivation.cs");

        Assert.Contains("typeof(MainWindow)", source, StringComparison.Ordinal);
        Assert.Contains("window.AmmoPage.VerifyProductVisibleDropdownInitialization();", source, StringComparison.Ordinal);
        Assert.Contains("cannot hide a missing real product initialization", source, StringComparison.Ordinal);
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
        Assert.Contains("page.Dispatcher.BeginInvoke(page.ApplyJunhyunMarkerPanelPolish", loaded, StringComparison.Ordinal);
        Assert.Contains("ActivateProductMarkerPanelBodyLayout();", loaded, StringComparison.Ordinal);

        Assert.DoesNotContain("protected override void OnInitialized", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyJunhyunUiSimplification();", body, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment = VerticalAlignment.Stretch", body, StringComparison.Ordinal);
        Assert.Contains("requestedPanelHeight", body, StringComparison.Ordinal);
        Assert.Contains("panelHeight - headerHeight - verticalChrome", body, StringComparison.Ordinal);
        Assert.Contains("_junhyunMarkerListViewport.Height = listHeight", body, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility = ScrollBarVisibility.Auto", body, StringComparison.Ordinal);
        Assert.Contains("ScrollableHeight > 0.5", body, StringComparison.Ordinal);
        Assert.Contains("ComputedVerticalScrollBarVisibility", body, StringComparison.Ordinal);
        Assert.Contains("marker-list-fills-panel-body=ok", body, StringComparison.Ordinal);
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
