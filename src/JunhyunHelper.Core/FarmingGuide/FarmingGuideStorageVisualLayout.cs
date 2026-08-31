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
/// Profiles are activated only when the current live grid count and geometry are
/// compatible; otherwise callers must use their ordinary procedural layout.
/// </summary>
public static class FarmingGuideStorageVisualLayoutResolver
{
    private const double TarkovCellSize = 64d;
    private const double OverlapEpsilon = 0.01d;

    private sealed record SourcePosition(double X, double Y);

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

    // Coordinates are independently retained factual UI metadata from verified
    // Tarkov compound-grid templates. EFT coordinates use +X right and -Y down.
    private static readonly IReadOnlyDictionary<string, SourcePosition[]> Profiles =
        new Dictionary<string, SourcePosition[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["mbss_rig"] =
            [
                new(210, 3),
                new(210, -65),
                new(210, -133),
                new(0, -69.3),
                new(70, -69.3),
                new(4.3, -0.3),
                new(140, -2),
            ],
            ["ANA Tactical M1"] =
            [
                new(0, 0),
                new(70, 0),
                new(140, 0),
                new(210, 0),
                new(7, -133),
                new(140, -133),
                new(0, -266),
                new(70, -266),
                new(140, -266),
                new(210, -266),
            ],
            ["A18"] =
            [
                new(0, 0),
                new(70, 0),
                new(140, 0),
                new(210, 0),
                new(280, 0),
                new(0, -133),
                new(70, -133),
                new(140, -133),
                new(210, -133),
                new(280, -133),
                new(0, -266),
                new(70, -266),
                new(140, -266),
                new(210, -266),
                new(280, -266),
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

        var scale = cellSize / TarkovCellSize;
        var rawLeft = source.Select(position => position.X * scale).ToArray();
        var rawTop = source.Select(position => -position.Y * scale).ToArray();
        var minLeft = rawLeft.Min();
        var minTop = rawTop.Min();

        var grids = new FarmingGuideStorageVisualGrid[source.Length];
        var rectangles = new (double Left, double Top, double Right, double Bottom)[source.Length];
        var width = 0d;
        var height = 0d;
        for (var index = 0; index < source.Length; index++)
        {
            var definition = liveGrids[index];
            if (definition.Width <= 0 || definition.Height <= 0)
                return false;

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
