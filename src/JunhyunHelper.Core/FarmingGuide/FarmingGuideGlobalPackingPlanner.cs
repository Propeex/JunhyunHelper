namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// One item that must be placed somewhere in a complete from-scratch packing proof.
/// Options are already projected through Tarkov storage/filter/rotation rules by the caller.
/// </summary>
public sealed record FarmingGuideGlobalPackingItem(
    string InstanceId,
    IReadOnlyList<FarmingGuideRepackingOption> Options);

public enum FarmingGuideGlobalPackingStatus
{
    Found,
    NoSolution,
    BudgetExceeded,
}

public sealed record FarmingGuideGlobalPackingResult(
    FarmingGuideGlobalPackingStatus Status,
    IReadOnlyList<FarmingGuideRepackingPlacement> Placements,
    int SearchNodes)
{
    public bool Found => Status == FarmingGuideGlobalPackingStatus.Found;
    public bool ProofComplete => Status != FarmingGuideGlobalPackingStatus.BudgetExceeded;
}

/// <summary>
/// Deterministic all-items packing proof for v1.17 Farming Guide.
///
/// Unlike FarmingGuideRepackingPlanner, this planner has no concept of a preferred current
/// placement. Every supplied movable item starts unplaced and the solver proves whether the
/// complete selected set can coexist on the supplied legal surfaces. Container-owned
/// surfaces may exist before their owner is placed; the final parent graph validation rejects
/// self-nesting and cycles. A parent id not present in the movable set is treated as a fixed
/// externally-owned container whose surface was supplied by the caller.
/// </summary>
public static class FarmingGuideGlobalPackingPlanner
{
    public const int DefaultMaxSearchNodes = 250_000;

    public static FarmingGuideGlobalPackingResult Plan(
        IReadOnlyList<FarmingGuideRepackingSurface> surfaces,
        IReadOnlyList<FarmingGuideGlobalPackingItem> items,
        int maxSearchNodes = DefaultMaxSearchNodes)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        ArgumentNullException.ThrowIfNull(items);
        if (maxSearchNodes <= 0)
            return new(FarmingGuideGlobalPackingStatus.BudgetExceeded, [], 0);

        var surfaceMap = new Dictionary<string, FarmingGuideRepackingSurface>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (string.IsNullOrWhiteSpace(surface.Id) ||
                surface.Width <= 0 || surface.Height <= 0 ||
                !surfaceMap.TryAdd(surface.Id, surface) ||
                surface.FixedObstacles.Any(obstacle =>
                    obstacle.Width <= 0 || obstacle.Height <= 0 ||
                    obstacle.X < 0 || obstacle.Y < 0 ||
                    obstacle.X + obstacle.Width > surface.Width ||
                    obstacle.Y + obstacle.Height > surface.Height))
            {
                return new(FarmingGuideGlobalPackingStatus.NoSolution, [], 0);
            }
        }
        if (surfaceMap.Count == 0 && items.Count > 0)
            return new(FarmingGuideGlobalPackingStatus.NoSolution, [], 0);

        var itemIds = new HashSet<string>(StringComparer.Ordinal);
        var candidatesByItem = new Dictionary<string, FarmingGuideRepackingPlacement[]>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.InstanceId) || !itemIds.Add(item.InstanceId))
                return new(FarmingGuideGlobalPackingStatus.NoSolution, [], 0);

            var candidates = EnumeratePlacements(item, surfaceMap).ToArray();
            if (candidates.Length == 0)
                return new(FarmingGuideGlobalPackingStatus.NoSolution, [], 0);
            candidatesByItem[item.InstanceId] = candidates;
        }

        var fixedOccupancy = surfaces
            .SelectMany(surface => surface.FixedObstacles.Select((obstacle, index) => new Occupied(
                $"__fixed__{surface.Id}__{index}",
                surface.Id,
                obstacle.X,
                obstacle.Y,
                obstacle.Width,
                obstacle.Height)))
            .ToArray();

        var ordered = items
            .OrderBy(item => candidatesByItem[item.InstanceId].Length)
            .ThenByDescending(item => item.Options.Count == 0 ? 0 : item.Options.Max(option => option.Width * option.Height))
            .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
            .ToArray();

        var placements = new Dictionary<string, FarmingGuideRepackingPlacement>(StringComparer.Ordinal);
        var occupancy = fixedOccupancy.ToList();
        var nodes = 0;
        var budgetExceeded = false;

        bool Search(int index)
        {
            if (nodes >= maxSearchNodes)
            {
                budgetExceeded = true;
                return false;
            }
            nodes++;

            if (index == ordered.Length)
                return HasValidParentGraph(placements, surfaceMap, itemIds);

            var item = ordered[index];
            foreach (var candidate in candidatesByItem[item.InstanceId])
            {
                if (nodes >= maxSearchNodes)
                {
                    budgetExceeded = true;
                    return false;
                }

                if (occupancy.Any(existing =>
                        string.Equals(existing.SurfaceId, candidate.SurfaceId, StringComparison.Ordinal) &&
                        Overlaps(existing, candidate)))
                {
                    continue;
                }

                placements[item.InstanceId] = candidate;
                occupancy.Add(new Occupied(
                    item.InstanceId,
                    candidate.SurfaceId,
                    candidate.X,
                    candidate.Y,
                    candidate.Width,
                    candidate.Height));

                if (Search(index + 1))
                    return true;

                occupancy.RemoveAt(occupancy.Count - 1);
                placements.Remove(item.InstanceId);
            }

            return false;
        }

        if (Search(0))
        {
            return new(
                FarmingGuideGlobalPackingStatus.Found,
                placements.Values.OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray(),
                nodes);
        }

        return new(
            budgetExceeded
                ? FarmingGuideGlobalPackingStatus.BudgetExceeded
                : FarmingGuideGlobalPackingStatus.NoSolution,
            [],
            nodes);
    }

    private static IEnumerable<FarmingGuideRepackingPlacement> EnumeratePlacements(
        FarmingGuideGlobalPackingItem item,
        IReadOnlyDictionary<string, FarmingGuideRepackingSurface> surfaces)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in item.Options
                     .OrderBy(value => value.Preference)
                     .ThenBy(value => surfaces.TryGetValue(value.SurfaceId, out var surface) ? surface.Priority : int.MaxValue)
                     .ThenBy(value => value.Rotated))
        {
            if (!surfaces.TryGetValue(option.SurfaceId, out var surface) ||
                option.Width <= 0 || option.Height <= 0 ||
                option.Width > surface.Width || option.Height > surface.Height ||
                string.Equals(surface.ParentInstanceId, item.InstanceId, StringComparison.Ordinal))
            {
                continue;
            }

            var optionKey = $"{option.SurfaceId}|{option.Width}|{option.Height}|{option.Rotated}";
            if (!seen.Add(optionKey))
                continue;

            for (var y = 0; y <= surface.Height - option.Height; y++)
            {
                for (var x = 0; x <= surface.Width - option.Width; x++)
                {
                    yield return new FarmingGuideRepackingPlacement(
                        item.InstanceId,
                        option.SurfaceId,
                        x,
                        y,
                        option.Width,
                        option.Height,
                        option.Rotated);
                }
            }
        }
    }

    private static bool HasValidParentGraph(
        IReadOnlyDictionary<string, FarmingGuideRepackingPlacement> placements,
        IReadOnlyDictionary<string, FarmingGuideRepackingSurface> surfaces,
        IReadOnlySet<string> movableItemIds)
    {
        foreach (var itemId in movableItemIds)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal) { itemId };
            var current = itemId;
            while (placements.TryGetValue(current, out var placement) &&
                   surfaces.TryGetValue(placement.SurfaceId, out var surface) &&
                   !string.IsNullOrWhiteSpace(surface.ParentInstanceId))
            {
                var parent = surface.ParentInstanceId!;
                if (!visited.Add(parent))
                    return false;
                if (!movableItemIds.Contains(parent))
                    break;
                current = parent;
            }
        }
        return true;
    }

    private static bool Overlaps(Occupied left, FarmingGuideRepackingPlacement right) =>
        left.X < right.X + right.Width &&
        left.X + left.Width > right.X &&
        left.Y < right.Y + right.Height &&
        left.Y + left.Height > right.Y;

    private sealed record Occupied(
        string InstanceId,
        string SurfaceId,
        int X,
        int Y,
        int Width,
        int Height);
}
