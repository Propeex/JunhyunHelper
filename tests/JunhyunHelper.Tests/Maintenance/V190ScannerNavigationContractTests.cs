using System.Runtime.CompilerServices;
using Xunit;

namespace JunhyunHelper.Tests.Maintenance;

public sealed class V190ScannerNavigationContractTests
{
    [Fact]
    public void Every_scanner_item_open_route_converges_on_one_product_boundary()
    {
        var root = FindRepositoryRoot();
        var page = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.xaml.cs");
        var relationships = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ItemRelationships.cs");
        var favorites = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.FavoritesRecents.cs");
        var usability = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.ProductUsability.cs");
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.PublishedItemDetailSmoke.cs");

        Assert.Contains("private void OpenScannerItemDetails(ScannerItemSearchDetails details)", page, StringComparison.Ordinal);
        Assert.Contains("RenderSearchDetails(details);\n        RenderProductItemExtensions(details);\n        OnScannerItemOpened(details);", Normalize(page), StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(page, "OnScannerItemOpened(details);"));

        Assert.Contains("OpenScannerItemDetails(details);", page, StringComparison.Ordinal);
        Assert.Contains("OpenScannerItemDetails(details);", relationships, StringComparison.Ordinal);
        Assert.Contains("SelectSearchItemById(itemId);", relationships, StringComparison.Ordinal);
        Assert.True(CountOccurrences(favorites, "SelectSearchItemById(itemId);") >= 2);

        Assert.DoesNotContain("RefreshProductItemExtensions", usability, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSearchResultList_PreviewMouseUp", usability, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductItemSearchBox_PreviewKeyDown", usability, StringComparison.Ordinal);

        Assert.Contains("OpenScannerItemDetails(details);", smoke, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildItemRelationshipPresentation();", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void Saved_item_lists_resolve_current_mode_presentation_without_building_relationships()
    {
        var root = FindRepositoryRoot();
        var coordinator = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerCoordinator.Search.cs");
        var favorites = Read(root, "src", "JunhyunHelper.Desktop", "Scanner", "ScannerPage.FavoritesRecents.cs");

        Assert.Contains("public ScannerItemSearchHit? GetSearchItemHit", coordinator, StringComparison.Ordinal);
        Assert.Contains("_coordinator.GetSearchItemHit(itemId)", favorites, StringComparison.Ordinal);
        Assert.DoesNotContain("_coordinator.GetSearchItemDetails(itemId)", favorites, StringComparison.Ordinal);
    }

    [Fact]
    public void Published_ammo_smoke_verifies_real_lifecycle_instead_of_initializing_the_feature()
    {
        var root = FindRepositoryRoot();
        var smoke = Read(root, "src", "JunhyunHelper.Desktop", "Ammo", "AmmoPage.PublishedRuntimeSmoke.cs");

        Assert.Contains("if (!_productCaliberDropdownApplied", smoke, StringComparison.Ordinal);
        Assert.Contains("ProductCaliberIconCycleInterval != TimeSpan.FromMilliseconds(700)", smoke, StringComparison.Ordinal);
        Assert.Contains("shared-cycle-ms=700", smoke, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyProductCaliberDropdownPolish();", smoke, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value) =>
        source.Split(value, StringSplitOptions.None).Length - 1;

    private static string Normalize(string source) => source.Replace("\r\n", "\n", StringComparison.Ordinal);

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
