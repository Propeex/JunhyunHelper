using System.Xml.Linq;

namespace JunhyunHelper.Infrastructure.Maps;

public static class MapSvgPresentationTransformer
{
    public const string PresentationRevision = "readable-v1";

    private const string ReadabilityCss = """
        .land { fill:#244B4F !important; }
        .trees { fill:#1E6257 !important; }
        .water { fill:#4F7FAE !important; }
        .rock { fill:#E4D9B9 !important; }
        .wood { fill:#8B5E2E !important; }
        .cement { fill:#D8DDE1 !important; stroke:#343D46 !important; stroke-width:.35 !important; }
        .tarmac { fill:#929DA7 !important; }
        .gravel { fill:#AE8459 !important; }
        .building { fill:#D7DEE5 !important; stroke:#0C1117 !important; stroke-width:.9 !important; }
        .floor { fill:#AEB8C2 !important; stroke:#111820 !important; stroke-width:.6 !important; }
        .locked { fill:#66727E !important; stroke:#111820 !important; stroke-width:.5 !important; }
        .map_border { fill:none !important; stroke:#DDE3E8 !important; stroke-width:2.4 !important; }
        .fence { fill:none !important; stroke:#E7F4E5 !important; stroke-width:1.2 !important; }
        .road_tarmac { fill:none !important; stroke:#C3CBD2 !important; }
        .road_gravel { fill:none !important; stroke:#D7AA78 !important; }
        .railroad { fill:none !important; stroke:#D8755F !important; }
        """;

    public static void CreateReadableCopy(
        string sourcePath,
        string destinationPath,
        IEnumerable<string?> floorLayers,
        string? selectedFloorLayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentNullException.ThrowIfNull(floorLayers);

        var document = XDocument.Load(sourcePath, LoadOptions.PreserveWhitespace);
        var root = document.Root;
        if (root is null || !string.Equals(root.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Map asset '{sourcePath}' is not a valid SVG document.");

        ApplyFloorVisibility(document, floorLayers, selectedFloorLayer);
        ApplyReadabilityStyle(root);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        document.Save(destinationPath, SaveOptions.DisableFormatting);
    }

    private static void ApplyFloorVisibility(
        XDocument document,
        IEnumerable<string?> floorLayers,
        string? selectedFloorLayer)
    {
        var knownLayers = floorLayers
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        if (knownLayers.Count <= 1 || string.IsNullOrWhiteSpace(selectedFloorLayer))
            return;

        foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "g"))
        {
            var id = element.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(id) || !knownLayers.Contains(id))
                continue;
            SetDisplay(element, string.Equals(id, selectedFloorLayer, StringComparison.Ordinal));
        }
    }

    private static void ApplyReadabilityStyle(XElement root)
    {
        foreach (var existing in root.Elements().Where(element =>
                     element.Name.LocalName == "style" &&
                     string.Equals(element.Attribute("id")?.Value, "junhyunhelper_readability", StringComparison.Ordinal)).ToArray())
        {
            existing.Remove();
        }

        var style = new XElement(
            root.Name.Namespace + "style",
            new XAttribute("id", "junhyunhelper_readability"),
            ReadabilityCss);
        root.Add(style);
    }

    private static void SetDisplay(XElement element, bool visible)
    {
        var style = element.Attribute("style")?.Value ?? string.Empty;
        var parts = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("display:", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!visible)
            parts.Add("display:none");
        if (parts.Count == 0)
            element.Attribute("style")?.Remove();
        else
            element.SetAttributeValue("style", string.Join(';', parts));
    }
}
