using System.Xml.Linq;
using Xunit;

namespace JunhyunHelper.Tests.Desktop;

public sealed class MapXamlInitializationTests
{
    [Fact]
    public void Map_marker_checkboxes_do_not_set_IsChecked_during_Xaml_construction()
    {
        var repositoryRoot = FindRepositoryRoot();
        var path = Path.Combine(
            repositoryRoot,
            "src",
            "JunhyunHelper.Desktop",
            "Map",
            "MapPage.xaml");

        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var markerCheckBoxes = document
            .Descendants(presentation + "CheckBox")
            .Where(element => element.Attribute("Tag") is not null)
            .ToArray();

        Assert.NotEmpty(markerCheckBoxes);
        Assert.All(markerCheckBoxes, element =>
            Assert.Null(element.Attribute("IsChecked")));
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
