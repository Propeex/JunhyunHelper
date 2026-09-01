namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Visual-only placement for one Tarkov compound-storage grid. Coordinates are
/// WPF pixels relative to the owning storage surface; storage mechanics continue
/// to come from the live <see cref="FarmingGuideStorageGridDefinition"/> data.
/// </summary>
public sealed record FarmingGuideStorageVisualGrid(int GridIndex, double Left, double Top);

public sealed record FarmingGuideStorageVisualLayout(
    double Width,
    double Height,
    IReadOnlyList<FarmingGuideStorageVisualGrid> Grids);

/// <summary>
/// Resolves verified Tarkov compound-grid UI templates without allowing stale
/// visual metadata to affect inventory compatibility or placement mechanics.
/// Profiles are activated only when the current live grid count and each grid's
/// width/height match the verified profile signature; otherwise callers must use
/// their ordinary procedural layout.
/// </summary>
public static class FarmingGuideStorageVisualLayoutResolver
{
    private const double TarkovCellSize = 64d;
    private const double OverlapEpsilon = 0.01d;

    private sealed record SourceGrid(
        double X,
        double Y,
        int ExpectedWidth,
        int ExpectedHeight);

    // Item ids are stable EFT template ids. These aliases make the first verified
    // profiles usable even when the normalized tarkov.dev payload omits the raw
    // GridLayoutName/RigLayoutName field.
    private static readonly IReadOnlyDictionary<string, string> ItemLayoutNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["64a5366719bab53bd203bf33"] = "mbss_rig",
            ["5d5d87f786f77427997cfaef"] = "A18",
            ["5c0e722886f7740458316a57"] = "ANA Tactical M1",
        };

    // Coordinates and expected grid dimensions are independently retained factual
    // metadata for the small product-owned set of verified Tarkov compound-grid
    // templates. EFT coordinates use +X right and -Y down. The expected dimensions
    // are part of the profile identity: if current Tarkov mechanics drift at any
    // grid index, exact coordinates are rejected and presentation falls back.
    private static readonly IReadOnlyDictionary<string, SourceGrid[]> Profiles =
        new Dictionary<string, SourceGrid[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["mbss_rig"] =
            [
                new(210, 3, 1, 1),
                new(210, -65, 1, 1),
                new(210, -133, 1, 1),
                new(0, -69.3, 1, 2),
                new(70, -69.3, 1, 2),
                new(4.3, -0.3, 2, 1),
                new(140, -2, 1, 3),
            ],
            ["ANA Tactical M1"] =
            [
                new(0, 0, 1, 2),
                new(70, 0, 1, 2),
                new(140, 0, 1, 2),
                new(210, 0, 1, 2),
                new(7, -133, 2, 2),
                new(140, -133, 2, 2),
                new(0, -266, 1, 1),
                new(70, -266, 1, 1),
                new(140, -266, 1, 1),
                new(210, -266, 1, 1),
            ],
            ["A18"] =
            [
                new(0, 0, 1, 2),
                new(70, 0, 1, 2),
                new(140, 0, 1, 2),
                new(210, 0, 1, 2),
                new(280, 0, 1, 2),
                new(0, -133, 1, 2),
                new(70, -133, 1, 2),
                new(140, -133, 1, 2),
                new(210, -133, 1, 2),
                new(280, -133, 1, 2),
                new(0, -266, 1, 1),
                new(70, -266, 1, 1),
                new(140, -266, 1, 1),
                new(210, -266, 1, 1),
                new(280, -266, 1, 1),
            ],
        };

    public static bool TryResolve(
        string itemId,
        string? layoutName,
        IReadOnlyList<FarmingGuideStorageGridDefinition> liveGrids,
        double cellSize,
        out FarmingGuideStorageVisualLayout layout)
    {
        layout = new FarmingGuideStorageVisualLayout(0, 0, []);
        if (liveGrids.Count == 0 || !double.IsFinite(cellSize) || cellSize <= 0)
            return false;

        var resolvedName = string.IsNullOrWhiteSpace(layoutName)
            ? ItemLayoutNames.GetValueOrDefault(itemId)
            : layoutName.Trim();
        if (string.IsNullOrWhiteSpace(resolvedName) ||
            !Profiles.TryGetValue(resolvedName, out var source) ||
            source.Length != liveGrids.Count)
        {
            return false;
        }

        for (var index = 0; index < source.Length; index++)
        {
            var definition = liveGrids[index];
            var expected = source[index];
            if (definition.Width != expected.ExpectedWidth ||
                definition.Height != expected.ExpectedHeight)
            {
                return false;
            }
        }

        var scale = cellSize / TarkovCellSize;
        var rawLeft = source.Select(grid => grid.X * scale).ToArray();
        var rawTop = source.Select(grid => -grid.Y * scale).ToArray();
        var minLeft = rawLeft.Min();
        var minTop = rawTop.Min();

        var grids = new FarmingGuideStorageVisualGrid[source.Length];
        var rectangles = new (double Left, double Top, double Right, double Bottom)[source.Length];
        var width = 0d;
        var height = 0d;
        for (var index = 0; index < source.Length; index++)
        {
            var definition = liveGrids[index];
            var left = rawLeft[index] - minLeft;
            var top = rawTop[index] - minTop;
            var right = left + definition.Width * cellSize;
            var bottom = top + definition.Height * cellSize;
            if (!double.IsFinite(left) || !double.IsFinite(top) ||
                !double.IsFinite(right) || !double.IsFinite(bottom))
            {
                return false;
            }

            grids[index] = new FarmingGuideStorageVisualGrid(index, left, top);
            rectangles[index] = (left, top, right, bottom);
            width = Math.Max(width, right);
            height = Math.Max(height, bottom);
        }

        for (var leftIndex = 0; leftIndex < rectangles.Length; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < rectangles.Length; rightIndex++)
            {
                if (Overlaps(rectangles[leftIndex], rectangles[rightIndex]))
                    return false;
            }
        }

        layout = new FarmingGuideStorageVisualLayout(width, height, grids);
        return true;
    }

    private static bool Overlaps(
        (double Left, double Top, double Right, double Bottom) first,
        (double Left, double Top, double Right, double Bottom) second) =>
        first.Left < second.Right - OverlapEpsilon &&
        first.Right > second.Left + OverlapEpsilon &&
        first.Top < second.Bottom - OverlapEpsilon &&
        first.Bottom > second.Top + OverlapEpsilon;
}
