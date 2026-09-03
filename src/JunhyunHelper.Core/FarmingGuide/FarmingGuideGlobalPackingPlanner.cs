namespace JunhyunHelper.Core.FarmingGuide;

public sealed record FarmingGuideGlobalPackingSurface(
    string Id,
    string? OwnerInstanceId,
    int Width,
    int Height,
    int Priority,
    IReadOnlyList<FarmingGuideGridPlacement> FixedObstacles);

public sealed record FarmingGuideGlobalPackingOption(
    string SurfaceId,
    int Width,
    int Height,
    bool Rotated,
    int Preference);

public sealed record FarmingGuideGlobalPackingPlacement(
    string InstanceId,
    string SurfaceId,
    int X,
    int Y,
    int Width,
    int Height,
    bool Rotated);

public sealed record FarmingGuideGlobalPackingItem(
    string InstanceId,
    bool Fixed,
    FarmingGuideGlobalPackingPlacement? CurrentPlacement,
    IReadOnlyList<FarmingGuideGlobalPackingOption> Options);

public enum FarmingGuideGlobalPackingStatus
{
    Found,
    NoSolution,
    BudgetExceeded,
}

public sealed record FarmingGuideGlobalPackingResult(
    FarmingGuideGlobalPackingStatus Status,
    IReadOnlyList<FarmingGuideGlobalPackingPlacement> Placements,
    int SearchNodes)
{
    public bool Found => Status == FarmingGuideGlobalPackingStatus.Found;
    public bool ProofComplete => Status != FarmingGuideGlobalPackingStatus.BudgetExceeded;
}

/// <summary>
/// Deterministic from-scratch packing proof for one already-selected set of retained roots.
///
/// Unlike the historical local displacement planner, unlocked current placement has no
/// semantic privilege. It is only enumerated first as a stability tie after the retained set
/// has already been chosen by the Farming Guide objective. Fixed items keep their exact
/// placement. Caller-supplied surfaces/options already encode Tarkov storage/equipment rules.
/// Owned surfaces model nested/carrier storage; a final owner graph check rejects self/cycles.
///
/// The optional final validator lets Desktop enforce cross-surface Tarkov rules such as
/// mutually incompatible equipped roots without teaching this generic grid solver item types.
/// Budget exhaustion is explicit and must never be mistaken for a proven NoSolution.
/// </summary>
public static class FarmingGuideGlobalPackingPlanner
{
    public const int DefaultMaxSearchNodes = 250_000;

    public static FarmingGuideGlobalPackingResult TryPlan(
        IReadOnlyList<FarmingGuideGlobalPackingSurface> surfaces,
        IReadOnlyList<FarmingGuideGlobalPackingItem> items,
        Func<IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement>, bool>? finalValidator = null,
        int maxSearchNodes = DefaultMaxSearchNodes)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        ArgumentNullException.ThrowIfNull(items);
        if (maxSearchNodes <= 0)
            return BudgetExceeded(0);

        var surfaceMap = new Dictionary<string, FarmingGuideGlobalPackingSurface>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (string.IsNullOrWhiteSpace(surface.Id) ||
                surface.Width <= 0 || surface.Height <= 0 ||
                !surfaceMap.TryAdd(surface.Id, surface))
            {
                return NoSolution(0);
            }
        }
        if (surfaceMap.Count == 0)
            return NoSolution(0);

        var itemMap = new Dictionary<string, FarmingGuideGlobalPackingItem>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.InstanceId) || !itemMap.TryAdd(item.InstanceId, item))
                return NoSolution(0);
            if (item.Fixed && item.CurrentPlacement is null)
                return NoSolution(0);
        }

        foreach (var surface in surfaces)
        {
            if (surface.OwnerInstanceId is { Length: > 0 } owner && !itemMap.ContainsKey(owner))
                return NoSolution(0);
        }

        var occupancy = new Dictionary<string, List<RectPlacement>>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            var list = new List<RectPlacement>();
            foreach (var obstacle in surface.FixedObstacles)
            {
                if (!Inside(surface, obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height))
                    return NoSolution(0);
                if (list.Any(value => Overlaps(value.X, value.Y, value.Width, value.Height,
                        obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height)))
                {
                    return NoSolution(0);
                }
                list.Add(new RectPlacement(
                    $"__fixed__{surface.Id}__{list.Count}",
                    obstacle.X,
                    obstacle.Y,
                    obstacle.Width,
                    obstacle.Height));
            }
            occupancy[surface.Id] = list;
        }

        var placements = new Dictionary<string, FarmingGuideGlobalPackingPlacement>(StringComparer.Ordinal);
        foreach (var item in items.Where(static value => value.Fixed).OrderBy(value => value.InstanceId, StringComparer.Ordinal))
        {
            var current = item.CurrentPlacement!;
            if (!surfaceMap.TryGetValue(current.SurfaceId, out var surface) ||
                !MatchesAnyOption(item.Options, current) ||
                !CanOccupy(surface, occupancy[current.SurfaceId], current.X, current.Y, current.Width, current.Height))
            {
                return NoSolution(0);
            }

            placements[item.InstanceId] = current;
            occupancy[current.SurfaceId].Add(new RectPlacement(
                item.InstanceId,
                current.X,
                current.Y,
                current.Width,
                current.Height));
        }

        var candidates = new Dictionary<string, FarmingGuideGlobalPackingPlacement[]>(StringComparer.Ordinal);
        foreach (var item in items.Where(static value => !value.Fixed))
        {
            var values = EnumerateCandidates(item, surfaceMap).ToArray();
            if (values.Length == 0)
                return NoSolution(0);
            candidates[item.InstanceId] = values;
        }

        var remaining = items
            .Where(static value => !value.Fixed)
            .OrderBy(value => candidates[value.InstanceId].Length)
            .ThenByDescending(value => MaximumArea(value.Options))
            .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
            .Select(value => value.InstanceId)
            .ToArray();

        var nodes = 0;
        var exhausted = false;
        if (Search(
                index: 0,
                remaining,
                candidates,
                surfaceMap,
                occupancy,
                placements,
                itemMap,
                finalValidator,
                ref nodes,
                maxSearchNodes,
                ref exhausted,
                out var result))
        {
            return new FarmingGuideGlobalPackingResult(
                FarmingGuideGlobalPackingStatus.Found,
                result.Values.OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray(),
                nodes);
        }

        return exhausted ? BudgetExceeded(nodes) : NoSolution(nodes);
    }

    private static bool Search(
        int index,
        IReadOnlyList<string> remaining,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement[]> candidates,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingSurface> surfaces,
        Dictionary<string, List<RectPlacement>> occupancy,
        Dictionary<string, FarmingGuideGlobalPackingPlacement> placements,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingItem> itemMap,
        Func<IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement>, bool>? finalValidator,
        ref int nodes,
        int maxNodes,
        ref bool exhausted,
        out Dictionary<string, FarmingGuideGlobalPackingPlacement> result)
    {
        result = placements;
        if (nodes >= maxNodes)
        {
            exhausted = true;
            return false;
        }
        nodes++;

        if (index >= remaining.Count)
        {
            if (!HasValidOwnerGraph(placements, surfaces) ||
                (finalValidator is not null && !finalValidator(placements)))
            {
                return false;
            }

            result = new Dictionary<string, FarmingGuideGlobalPackingPlacement>(placements, StringComparer.Ordinal);
            return true;
        }

        var id = remaining[index];
        var item = itemMap[id];
        foreach (var candidate in candidates[id])
        {
            if (nodes >= maxNodes)
            {
                exhausted = true;
                return false;
            }
            if (!surfaces.TryGetValue(candidate.SurfaceId, out var surface) ||
                !CanOccupy(surface, occupancy[candidate.SurfaceId],
                    candidate.X, candidate.Y, candidate.Width, candidate.Height))
            {
                continue;
            }

            // A root can never live inside its own storage surface. Full indirect cycles are
            // checked at the leaf when all owner relationships are known.
            if (string.Equals(surface.OwnerInstanceId, id, StringComparison.Ordinal))
                continue;

            placements[id] = candidate;
            occupancy[candidate.SurfaceId].Add(new RectPlacement(
                id, candidate.X, candidate.Y, candidate.Width, candidate.Height));

            if (Search(
                    index + 1,
                    remaining,
                    candidates,
                    surfaces,
                    occupancy,
                    placements,
                    itemMap,
                    finalValidator,
                    ref nodes,
                    maxNodes,
                    ref exhausted,
                    out result))
            {
                return true;
            }

            occupancy[candidate.SurfaceId].RemoveAt(occupancy[candidate.SurfaceId].Count - 1);
            placements.Remove(id);
            if (exhausted)
                return false;
        }

        return false;
    }

    private static IEnumerable<FarmingGuideGlobalPackingPlacement> EnumerateCandidates(
        FarmingGuideGlobalPackingItem item,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingSurface> surfaces)
    {
        var all = new List<(FarmingGuideGlobalPackingPlacement Placement, int Preference, int SurfacePriority, int Stability)>();
        foreach (var option in item.Options)
        {
            if (!surfaces.TryGetValue(option.SurfaceId, out var surface) ||
                option.Width <= 0 || option.Height <= 0 ||
                option.Width > surface.Width || option.Height > surface.Height)
            {
                continue;
            }

            for (var y = 0; y <= surface.Height - option.Height; y++)
            {
                for (var x = 0; x <= surface.Width - option.Width; x++)
                {
                    var placement = new FarmingGuideGlobalPackingPlacement(
                        item.InstanceId,
                        option.SurfaceId,
                        x,
                        y,
                        option.Width,
                        option.Height,
                        option.Rotated);
                    var stability = SamePhysicalPlacement(item.CurrentPlacement, placement) ? 0 : 1;
                    all.Add((placement, option.Preference, surface.Priority, stability));
                }
            }
        }

        return all
            .OrderBy(value => value.Stability)
            .ThenBy(value => value.Preference)
            .ThenBy(value => value.SurfacePriority)
            .ThenBy(value => value.Placement.Y)
            .ThenBy(value => value.Placement.X)
            .ThenBy(value => value.Placement.Rotated)
            .Select(value => value.Placement)
            .DistinctBy(value => new
            {
                value.SurfaceId,
                value.X,
                value.Y,
                value.Width,
                value.Height,
                value.Rotated,
            });
    }

    private static bool HasValidOwnerGraph(
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingPlacement> placements,
        IReadOnlyDictionary<string, FarmingGuideGlobalPackingSurface> surfaces)
    {
        foreach (var itemId in placements.Keys)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { itemId };
            var current = itemId;
            while (placements.TryGetValue(current, out var placement) &&
                   surfaces.TryGetValue(placement.SurfaceId, out var surface) &&
                   surface.OwnerInstanceId is { Length: > 0 } owner)
            {
                if (!placements.ContainsKey(owner) || !seen.Add(owner))
                    return false;
                current = owner;
            }
        }
        return true;
    }

    private static bool MatchesAnyOption(
        IReadOnlyList<FarmingGuideGlobalPackingOption> options,
        FarmingGuideGlobalPackingPlacement placement) =>
        options.Any(option =>
            string.Equals(option.SurfaceId, placement.SurfaceId, StringComparison.Ordinal) &&
            option.Width == placement.Width &&
            option.Height == placement.Height &&
            option.Rotated == placement.Rotated);

    private static bool CanOccupy(
        FarmingGuideGlobalPackingSurface surface,
        IReadOnlyList<RectPlacement> occupied,
        int x,
        int y,
        int width,
        int height) =>
        Inside(surface, x, y, width, height) &&
        occupied.All(value => !Overlaps(
            value.X, value.Y, value.Width, value.Height,
            x, y, width, height));

    private static bool Inside(
        FarmingGuideGlobalPackingSurface surface,
        int x,
        int y,
        int width,
        int height) =>
        width > 0 && height > 0 && x >= 0 && y >= 0 &&
        x + width <= surface.Width && y + height <= surface.Height;

    private static bool Overlaps(
        int leftX,
        int leftY,
        int leftWidth,
        int leftHeight,
        int rightX,
        int rightY,
        int rightWidth,
        int rightHeight) =>
        leftX < rightX + rightWidth &&
        leftX + leftWidth > rightX &&
        leftY < rightY + rightHeight &&
        leftY + leftHeight > rightY;

    private static bool SamePhysicalPlacement(
        FarmingGuideGlobalPackingPlacement? left,
        FarmingGuideGlobalPackingPlacement right) =>
        left is not null &&
        string.Equals(left.SurfaceId, right.SurfaceId, StringComparison.Ordinal) &&
        left.X == right.X && left.Y == right.Y &&
        left.Width == right.Width && left.Height == right.Height &&
        left.Rotated == right.Rotated;

    private static int MaximumArea(IReadOnlyList<FarmingGuideGlobalPackingOption> options) =>
        options.Count == 0 ? 0 : options.Max(value => checked(value.Width * value.Height));

    private static FarmingGuideGlobalPackingResult NoSolution(int nodes) =>
        new(FarmingGuideGlobalPackingStatus.NoSolution, [], nodes);

    private static FarmingGuideGlobalPackingResult BudgetExceeded(int nodes) =>
        new(FarmingGuideGlobalPackingStatus.BudgetExceeded, [], nodes);

    private sealed record RectPlacement(string Id, int X, int Y, int Width, int Height);
}
