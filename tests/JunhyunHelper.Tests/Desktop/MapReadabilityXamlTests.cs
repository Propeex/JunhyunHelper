using System.Xml.Linq;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapReadabilityXamlTests
{
    [Fact]
    public void Map_floor_selector_displays_only_floor_name_and_uses_readable_svg_viewbox()
    {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(repositoryRoot, "src", "JunhyunHelper.Desktop", "Map", "MapPage.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace map = "clr-namespace:JunhyunHelper.Desktop.Map";

        var floorCombo = document.Descendants(presentation + "ComboBox")
            .Single(element => element.Attribute(xaml + "Name")?.Value == "FloorComboBox");
        Assert.Equal("Name", floorCombo.Attribute("DisplayMemberPath")?.Value);

        var mapSvg = document.Descendants(map + "ReadableSvgViewbox")
            .Single(element => element.Attribute(xaml + "Name")?.Value == "MapSvg");
        Assert.NotNull(mapSvg);
    }

    [Fact]
    public void MiniMap_uses_same_readable_svg_viewbox()
    {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(repositoryRoot, "src", "JunhyunHelper.Desktop", "Map", "MiniMapWindow.xaml");
        var document = XDocument.Load(path);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace map = "clr-namespace:JunhyunHelper.Desktop.Map";

        var miniSvg = document.Descendants(map + "ReadableSvgViewbox")
            .Single(element => element.Attribute(xaml + "Name")?.Value == "MiniSvg");
        Assert.NotNull(miniSvg);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "JunhyunHelper.slnx")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found from the test output directory.");
    }
}
