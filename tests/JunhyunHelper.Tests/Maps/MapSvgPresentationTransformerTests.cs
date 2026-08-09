using System.Xml.Linq;
using JunhyunHelper.Infrastructure.Maps;
using Xunit;

namespace JunhyunHelper.Tests.Maps;

public sealed class MapSvgPresentationTransformerTests
{
    [Fact]
    public void Readable_copy_adds_high_contrast_style_and_keeps_only_selected_floor_visible()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"junhyunhelper-map-svg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "source.svg");
        var destination = Path.Combine(directory, "rendered.svg");

        try
        {
            File.WriteAllText(source, """
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
                  <style id="style_common">.building { fill:#1a2632 }.land { fill:#1f5054 }</style>
                  <g id="Ground_Level"><rect class="building" width="20" height="20" /></g>
                  <g id="Second_Floor"><rect class="floor" width="10" height="10" /></g>
                </svg>
                """);

            MapSvgPresentationTransformer.CreateReadableCopy(
                source,
                destination,
                ["Ground_Level", "Second_Floor"],
                "Ground_Level");

            var document = XDocument.Load(destination);
            var readability = document.Root!.Elements()
                .Single(element => element.Name.LocalName == "style" &&
                                   element.Attribute("id")?.Value == "junhyunhelper_readability");
            Assert.Contains(".building { fill:#D7DEE5", readability.Value, StringComparison.Ordinal);
            Assert.Contains(".road_tarmac", readability.Value, StringComparison.Ordinal);

            var ground = document.Descendants().Single(element => element.Attribute("id")?.Value == "Ground_Level");
            var second = document.Descendants().Single(element => element.Attribute("id")?.Value == "Second_Floor");
            Assert.DoesNotContain("display:none", ground.Attribute("style")?.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("display:none", second.Attribute("style")?.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Single_floor_map_still_receives_readability_style()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"junhyunhelper-map-svg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "source.svg");
        var destination = Path.Combine(directory, "rendered.svg");

        try
        {
            File.WriteAllText(source, "<svg xmlns=\"http://www.w3.org/2000/svg\"><path class=\"building\" /></svg>");
            MapSvgPresentationTransformer.CreateReadableCopy(source, destination, ["Ground_Level"], "Ground_Level");

            var document = XDocument.Load(destination);
            Assert.Contains(document.Root!.Elements(), element =>
                element.Name.LocalName == "style" &&
                element.Attribute("id")?.Value == "junhyunhelper_readability");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
