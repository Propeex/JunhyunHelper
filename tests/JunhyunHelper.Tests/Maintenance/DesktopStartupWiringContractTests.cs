using System.Runtime.CompilerServices;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class DesktopStartupWiringContractTests
{
    [Fact]
    public void PageInfrastructure_IsOwnedByProductInitialization_NotPageLoadedOrder()
    {
        var root = FindRepositoryRoot();
        var lifecycle = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.ProductLifecycle.cs"));
        var images = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.Images.cs"));
        var xaml = File.ReadAllText(Path.Combine(
            root,
            "src",
            "JunhyunHelper.Desktop",
            "MainWindow.xaml"));

        Assert.Contains("QuestPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("HideoutPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("ItemsPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AmmoPage.SetImageCache(_services.Images);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AmmoPage.SetFavoriteStore(_services.AmmoFavorites);", lifecycle, StringComparison.Ordinal);
        Assert.Contains("AttachContentNavigation();", lifecycle, StringComparison.Ordinal);

        Assert.DoesNotContain("Loaded=\"ItemsPage_Loaded\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Loaded=\"HideoutPage_Loaded\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Loaded=\"AmmoPage_Loaded\"", xaml, StringComparison.Ordinal);

        Assert.DoesNotContain("ItemsPage_Loaded", images, StringComparison.Ordinal);
        Assert.DoesNotContain("HideoutPage_Loaded", images, StringComparison.Ordinal);
        Assert.DoesNotContain("AmmoPage_Loaded", images, StringComparison.Ordinal);
    }

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
