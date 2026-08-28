using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V183VisibleUiActivationContractTests
{
    [Fact]
    public void AmmoVisibleDropdowns_ActivateFromRoutedLoadedWithoutOriginalSourceGate()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.VisibleDropdownActivation.cs");

        Assert.Contains("[ModuleInitializer]", source, StringComparison.Ordinal);
        Assert.Contains("FrameworkElement.LoadedEvent", source, StringComparison.Ordinal);
        Assert.Contains("if (sender is not AmmoPage page)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReferenceEquals(e.OriginalSource, page)", source, StringComparison.Ordinal);
        Assert.Contains("page.ApplyProductCaliberDropdownPolish();", source, StringComparison.Ordinal);
        Assert.Contains("VerifyProductVisibleDropdownLoadedActivation", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteCaliberMenuButton.Visibility != Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(CaliberComboBox.ItemTemplate, _productFavoriteCaliberComboBox.ItemTemplate)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AmmoPublishedSmoke_ChecksLoadedActivationBeforeLegacySmokeCanRepairIt()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.VisibleDropdownActivation.cs");

        Assert.Contains("typeof(MainWindow)", source, StringComparison.Ordinal);
        Assert.Contains("window.AmmoPage.VerifyProductVisibleDropdownLoadedActivation();", source, StringComparison.Ordinal);
        Assert.Contains("older published Ammo smoke", source, StringComparison.Ordinal);
        Assert.Contains("direct initializer cannot set this flag", source, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(88)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MapMarkerCheckboxViewport_UsesTheWholeAvailablePanelBody()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunMarkerPanelBodyLayout.cs");

        Assert.Contains("[ModuleInitializer]", source, StringComparison.Ordinal);
        Assert.Contains("if (sender is not MapPage page)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReferenceEquals(e.OriginalSource, page)", source, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment = VerticalAlignment.Stretch", source, StringComparison.Ordinal);
        Assert.Contains("requestedPanelHeight", source, StringComparison.Ordinal);
        Assert.Contains("panelHeight - headerHeight - verticalChrome", source, StringComparison.Ordinal);
        Assert.Contains("_junhyunMarkerListViewport.Height = listHeight", source, StringComparison.Ordinal);
        Assert.Contains("contentHeight <= listHeight + 0.5", source, StringComparison.Ordinal);
        Assert.Contains("marker-list-fills-panel-body=ok", source, StringComparison.Ordinal);
        Assert.Contains("Environment.Exit(89)", source, StringComparison.Ordinal);
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
