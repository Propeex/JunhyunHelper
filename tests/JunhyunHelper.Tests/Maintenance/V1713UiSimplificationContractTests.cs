using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1713UiSimplificationContractTests
{
    [Fact]
    public void AmmoDetails_DefaultCollapsed_AndPublishedSmokeChecksFullRoundTrip()
    {
        var root = FindRepositoryRoot();
        var simplification = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.ProductUiSimplification.cs");
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.xaml");
        var code = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.xaml.cs");
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.ProductUiLayoutSmoke.cs");

        Assert.Contains("_productDetailsExpanded = false;", simplification, StringComparison.Ordinal);
        Assert.DoesNotContain("SummaryText", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SummaryText", code, StringComparison.Ordinal);

        Assert.Contains("initial != \"▲\" || AmmoPage.ProductDetailHost.Visibility != Visibility.Collapsed", smoke, StringComparison.Ordinal);
        Assert.Contains("toggle.Content as string != \"▼\" || AmmoPage.ProductDetailHost.Visibility != Visibility.Visible", smoke, StringComparison.Ordinal);
        Assert.Contains("toggle.Content as string != \"▲\" || AmmoPage.ProductDetailHost.Visibility != Visibility.Collapsed", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemsUsageFilter_IsRemovedRatherThanHidden()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.xaml");
        var code = Read(root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.xaml.cs");
        var retiredShim = Path.Combine(
            root, "src", "JunhyunHelper.Desktop", "Items", "ItemsPage.ProductSimplification.cs");

        Assert.DoesNotContain("UsageComboBox", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemUsageFilter", code, StringComparison.Ordinal);
        Assert.DoesNotContain("MatchesUsage", code, StringComparison.Ordinal);
        Assert.False(File.Exists(retiredShim));
    }

    [Fact]
    public void ScannerNeededSources_JoinAuthoritativeItemsWorkspacePlan()
    {
        var root = FindRepositoryRoot();
        var sources = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.ScannerItemSources.cs");

        Assert.Contains("_activeItemsWorkspace.Plan.NeededItems", sources, StringComparison.Ordinal);
        Assert.Contains("needed.Sources", sources, StringComparison.Ordinal);
        Assert.Contains("needed.RemainingTotal", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("new ItemsApplicationService", sources, StringComparison.Ordinal);
        Assert.DoesNotContain("new ItemRequirement", sources, StringComparison.Ordinal);
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
