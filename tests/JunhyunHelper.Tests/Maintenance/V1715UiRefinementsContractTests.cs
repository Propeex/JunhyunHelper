using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V1715UiRefinementsContractTests
{
    [Fact]
    public void Header_ShowsVersionOnlyAndUsesItemsCleanupDot()
    {
        var root = FindRepositoryRoot();
        var xaml = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.xaml");
        var source = Read(root, "src", "JunhyunHelper.Desktop", "MainWindow.ItemsCleanupIndicator.cs");

        Assert.DoesNotContain("StatusText", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ItemsCleanupIndicator\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Fill=\"#F59E0B\"", xaml, StringComparison.Ordinal);
        Assert.Contains("_activeItemsWorkspace?.Plan.CleanupItems.Count", source, StringComparison.Ordinal);
        Assert.Contains("Visibility.Visible", source, StringComparison.Ordinal);
        Assert.Contains("Visibility.Collapsed", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Ammo_CaliberAndFavoritesShareOneAnimatedIconTemplate()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.CaliberDropdownPolish.cs");

        Assert.Contains("static AmmoPage()", source, StringComparison.Ordinal);
        Assert.Contains("ProductCaliberDropdownHandlerRegistered", source, StringComparison.Ordinal);
        Assert.Contains("CaliberComboBox.ItemTemplate = template", source, StringComparison.Ordinal);
        Assert.Contains("ItemTemplate = template", source, StringComparison.Ordinal);
        Assert.Contains("FavoriteCaliberMenuButton.Visibility = Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("_productFavoriteCaliberComboBox = new ComboBox", source, StringComparison.Ordinal);
        Assert.Contains("VerifyProductCaliberDropdownRuntimeContract", source, StringComparison.Ordinal);
        Assert.Contains("junhyun-ammo-ui-smoke-success.txt", source, StringComparison.Ordinal);
        Assert.Contains("ProductCaliberIconCycleInterval = TimeSpan.FromMilliseconds(700)", source, StringComparison.Ordinal);
        Assert.Contains("Interval = ProductCaliberIconCycleInterval", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeSpan.FromMilliseconds(1400)", source, StringComparison.Ordinal);
        Assert.Contains("GroupBy(row => row.RawCaliber", source, StringComparison.Ordinal);
        Assert.Contains("rows[index].Icon", source, StringComparison.Ordinal);
        Assert.Contains("AdvanceProductCaliberIcons", source, StringComparison.Ordinal);
        Assert.DoesNotContain("representative", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MapMarkerPanel_UsesContentHeightAndClosesOnOutsideClick()
    {
        var root = FindRepositoryRoot();
        var source = Read(root, "src", "JunhyunHelper.Desktop", "Map", "MapPage.JunhyunMarkerPanelPolish.cs");

        Assert.Contains("new ScrollViewer", source, StringComparison.Ordinal);
        Assert.Contains("MapMarkersContent.DesiredSize.Height", source, StringComparison.Ordinal);
        Assert.Contains("ScrollBarVisibility.Hidden", source, StringComparison.Ordinal);
        Assert.Contains("ScrollBarVisibility.Auto", source, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonDown += JunhyunMarkerPanel_PreviewMouseLeftButtonDown", source, StringComparison.Ordinal);
        Assert.Contains("IsWithinJunhyunMarkerPanel", source, StringComparison.Ordinal);
        Assert.Contains("MapMarkersContent.Visibility = Visibility.Collapsed", source, StringComparison.Ordinal);
        Assert.Contains("Do not mark the event handled", source, StringComparison.Ordinal);
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
