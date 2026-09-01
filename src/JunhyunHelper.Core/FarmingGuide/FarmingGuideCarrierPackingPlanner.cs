namespace JunhyunHelper.Core.FarmingGuide;

public sealed record FarmingGuideCarrierPackingSurface(
    string Id,
    int Width,
    int Height,
    int Priority,
    IReadOnlyList<FarmingGuideGridPlacement> FixedObstacles);

public sealed record FarmingGuideCarrierPackingOption(
    string SurfaceId,
    int Width,
    int Height,
    bool Rotated,
    int Preference);

public sealed record FarmingGuideCarrierPackingItem(
    string InstanceId,
    string? PreferredSurfaceId,
    int PreferredX,
    int PreferredY,
    bool PreferredRotated,
    bool Fixed,
    IReadOnlyList<FarmingGuideCarrierPackingOption> Options);

public sealed record FarmingGuideCarrierPackingPlacement(
    string InstanceId,
    string SurfaceId,
    int X,
    int Y,
    int Width,
    int Height,
    bool Rotated);

public sealed record FarmingGuideCarrierPackingPlan(
    IReadOnlyList<FarmingGuideCarrierPackingPlacement> Placements,
    int MovedCount,
    int SearchNodes);

/// <summary>
/// Deterministic bounded packing search for replacing one physical carrier while keeping
/// every modeled root item. Unlike raid repacking, the destination carrier starts empty:
/// old coordinates are preferences, not initial occupancy. Locked roots are fixed to their
/// exact old address and therefore make an unsafe carrier swap fail closed.
/// </summary>
public static class FarmingGuideCarrierPackingPlanner
{
    public const int DefaultMaxSearchNodes = 80000;

    public static FarmingGuideCarrierPackingPlan? TryPack(
        IReadOnlyList<FarmingGuideCarrierPackingSurface> surfaces,
        IReadOnlyList<FarmingGuideCarrierPackingItem> items,
        int maxSearchNodes = DefaultMaxSearchNodes)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        ArgumentNullException.ThrowIfNull(items);
        if (maxSearchNodes <= 0)
            return null;

        var surfaceMap = new Dictionary<string, FarmingGuideCarrierPackingSurface>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (string.IsNullOrWhiteSpace(surface.Id) || surface.Width <= 0 || surface.Height <= 0 ||
                !surfaceMap.TryAdd(surface.Id, surface))
            {
                return null;
            }
        }
        if (surfaceMap.Count == 0)
            return items.Count == 0 ? new FarmingGuideCarrierPackingPlan([], 0, 0) : null;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.InstanceId) || !ids.Add(item.InstanceId) || item.Options.Count == 0)
                return null;
            if (item.Options.Any(option => !surfaceMap.ContainsKey(option.SurfaceId) || option.Width <= 0 || option.Height <= 0))
                return null;
        }

        var occupied = new List<Occupied>();
        foreach (var surface in surfaces)
        {
            for (var index = 0; index < surface.FixedObstacles.Count; index++)
            {
                var obstacle = surface.FixedObstacles[index];
                if (!Inside(surface, obstacle.X, obstacle.Y, obstacle.Width, obstacle.Height))
                    return null;
                occupied.Add(new Occupied(
                    $"__fixed__{surface.Id}__{index}",
                    surface.Id,
                    obstacle.X,
                    obstacle.Y,
                    obstacle.Width,
                    obstacle.Height));
            }
        }

        var placements = new Dictionary<string, FarmingGuideCarrierPackingPlacement>(StringComparer.Ordinal);
        foreach (var item in items.Where(static value => value.Fixed).OrderBy(value => value.InstanceId, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(item.PreferredSurfaceId) ||
                !surfaceMap.TryGetValue(item.PreferredSurfaceId, out var surface))
            {
                return null;
            }

            var option = item.Options.FirstOrDefault(value =>
                string.Equals(value.SurfaceId, item.PreferredSurfaceId, StringComparison.Ordinal) &&
                value.Rotated == item.PreferredRotated);
            if (option is null || !Inside(surface, item.PreferredX, item.PreferredY, option.Width, option.Height))
                return null;

            var fixedPlacement = new FarmingGuideCarrierPackingPlacement(
                item.InstanceId,
                surface.Id,
                item.PreferredX,
                item.PreferredY,
                option.Width,
                option.Height,
                option.Rotated);
            if (OverlapsAny(occupied, fixedPlacement))
                return null;

            placements[item.InstanceId] = fixedPlacement;
            occupied.Add(ToOccupied(fixedPlacement));
        }

        var movable = items
            .Where(static value => !value.Fixed)
            .OrderByDescending(MaxArea)
            .ThenBy(value => value.Options.Count)
            .ThenBy(value => value.InstanceId, StringComparer.Ordinal)
            .ToArray();

        FarmingGuideCarrierPackingPlan? best = null;
        var searchNodes = 0;
        Search(
            0,
            movable,
            surfaceMap,
            occupied,
            placements,
            ref best,
            ref searchNodes,
            maxSearchNodes);
        return best;
    }

    private static void Search(
        int index,
        IReadOnlyList<FarmingGuideCarrierPackingItem> items,
        IReadOnlyDictionary<string, FarmingGuideCarrierPackingSurface> surfaces,
        List<Occupied> occupied,
        Dictionary<string, FarmingGuideCarrierPackingPlacement> placements,
        ref FarmingGuideCarrierPackingPlan? best,
        ref int searchNodes,
        int maxSearchNodes)
    {
        if (searchNodes >= maxSearchNodes)
            return;
        searchNodes++;

        if (index >= items.Count)
        {
            var result = placements.Values
                .OrderBy(value => value.InstanceId, StringComparer.Ordinal)
                .ToArray();
            var movedCount = items.Count(item =>
            {
                if (!placements.TryGetValue(item.InstanceId, out var placement))
                    return true;
                return !IsPreferred(item, placement);
            });
            var candidate = new FarmingGuideCarrierPackingPlan(result, movedCount, searchNodes);
            if (best is null || Compare(candidate, best) < 0)
                best = candidate;
            return;
        }

        var item = items[index];
        foreach (var candidate in EnumerateCandidates(item, surfaces))
        {
            if (searchNodes >= maxSearchNodes)
                return;
            if (OverlapsAny(occupied, candidate))
                continue;

            placements[item.InstanceId] = candidate;
            occupied.Add(ToOccupied(candidate));
            Search(index + 1, items, surfaces, occupied, placements, ref best, ref searchNodes, maxSearchNodes);
            occupied.RemoveAt(occupied.Count - 1);
            placements.Remove(item.InstanceId);

            // Zero moved movable roots is globally optimal. Once found there is no reason
            // to enumerate equivalent later candidates.
            if (best is { MovedCount: 0 })
                return;
        }
    }

    private static IEnumerable<FarmingGuideCarrierPackingPlacement> EnumerateCandidates(
        FarmingGuideCarrierPackingItem item,
        IReadOnlyDictionary<string, FarmingGuideCarrierPackingSurface> surfaces)
    {
        var candidates = new List<(FarmingGuideCarrierPackingPlacement Placement, int Preferred, int Preference, int SurfacePriority)>();
        foreach (var option in item.Options)
        {
            if (!surfaces.TryGetValue(option.SurfaceId, out var surface) ||
                option.Width > surface.Width || option.Height > surface.Height)
            {
                continue;
            }

            for (var y = 0; y <= surface.Height - option.Height; y++)
            {
                for (var x = 0; x <= surface.Width - option.Width; x++)
                {
                    var placement = new FarmingGuideCarrierPackingPlacement(
                        item.InstanceId,
                        surface.Id,
                        x,
                        y,
                        option.Width,
                        option.Height,
                        option.Rotated);
                    candidates.Add((
                        placement,
                        IsPreferred(item, placement) ? 0 : 1,
                        option.Preference,
                        surface.Priority));
                }
            }
        }

        return candidates
            .OrderBy(value => value.Preferred)
            .ThenBy(value => value.Preference)
            .ThenBy(value => value.SurfacePriority)
            .ThenBy(value => value.Placement.Y)
            .ThenBy(value => value.Placement.X)
            .ThenBy(value => value.Placement.Rotated)
            .Select(value => value.Placement);
    }

    private static bool IsPreferred(
        FarmingGuideCarrierPackingItem item,
        FarmingGuideCarrierPackingPlacement placement) =>
        !string.IsNullOrWhiteSpace(item.PreferredSurfaceId) &&
        string.Equals(item.PreferredSurfaceId, placement.SurfaceId, StringComparison.Ordinal) &&
        item.PreferredX == placement.X &&
        item.PreferredY == placement.Y &&
        item.PreferredRotated == placement.Rotated;

    private static int MaxArea(FarmingGuideCarrierPackingItem item) =>
        item.Options.Count == 0 ? 0 : item.Options.Max(value => value.Width * value.Height);

    private static int Compare(FarmingGuideCarrierPackingPlan left, FarmingGuideCarrierPackingPlan right)
    {
        var result = left.MovedCount.CompareTo(right.MovedCount);
        if (result != 0)
            return result;
        return string.CompareOrdinal(Signature(left.Placements), Signature(right.Placements));
    }

    private static string Signature(IEnumerable<FarmingGuideCarrierPackingPlacement> placements) =>
        string.Join(";", placements
            .OrderBy(value => value.InstanceId, StringComparer.Ordinal)
            .Select(value => $"{value.InstanceId}:{value.SurfaceId}:{value.X}:{value.Y}:{value.Rotated}"));

    private static bool Inside(
        FarmingGuideCarrierPackingSurface surface,
        int x,
        int y,
        int width,
        int height) =>
        width > 0 && height > 0 && x >= 0 && y >= 0 &&
        x + width <= surface.Width && y + height <= surface.Height;

    private static bool OverlapsAny(
        IReadOnlyList<Occupied> occupied,
        FarmingGuideCarrierPackingPlacement placement) =>
        occupied.Any(value =>
            string.Equals(value.SurfaceId, placement.SurfaceId, StringComparison.Ordinal) &&
            placement.X < value.X + value.Width &&
            placement.X + placement.Width > value.X &&
            placement.Y < value.Y + value.Height &&
            placement.Y + placement.Height > value.Y);

    private static Occupied ToOccupied(FarmingGuideCarrierPackingPlacement placement) =>
        new(
            placement.InstanceId,
            placement.SurfaceId,
            placement.X,
            placement.Y,
            placement.Width,
            placement.Height);

    private sealed record Occupied(
        string InstanceId,
        string SurfaceId,
        int X,
        int Y,
        int Width,
        int Height);
}
