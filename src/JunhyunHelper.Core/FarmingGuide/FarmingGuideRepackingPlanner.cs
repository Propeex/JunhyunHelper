namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// One physical storage surface available to the raid repacking planner. The planner is
/// deliberately ignorant of WPF, Scanner and Tarkov item names; Desktop projects current
/// source-backed storage/filter rules into legal per-item options before calling it.
/// </summary>
public sealed record FarmingGuideRepackingSurface(
    string Id,
    string? ParentInstanceId,
    int Width,
    int Height,
    int Priority,
    IReadOnlyList<FarmingGuideGridPlacement> FixedObstacles);

/// <summary>
/// One legal footprint for an item on a storage surface. Width/Height are already the
/// context-specific footprint (including Special Slot compression) so the search engine
/// never needs to duplicate placement policy.
/// </summary>
public sealed record FarmingGuideRepackingOption(
    string SurfaceId,
    int Width,
    int Height,
    bool Rotated,
    int Preference);

/// <summary>
/// Existing stored item projected into repacking space. Immovable items remain hard
/// obstacles. Movable items begin at their known current placement and are displaced only
/// when required by the incoming item or by another necessary move.
/// </summary>
public sealed record FarmingGuideRepackingItem(
    string InstanceId,
    string CurrentSurfaceId,
    int CurrentX,
    int CurrentY,
    int CurrentWidth,
    int CurrentHeight,
    bool CurrentRotated,
    bool Movable,
    IReadOnlyList<FarmingGuideRepackingOption> Options);

public sealed record FarmingGuideRepackingIncoming(
    string InstanceId,
    IReadOnlyList<FarmingGuideRepackingOption> Options);

public sealed record FarmingGuideRepackingPlacement(
    string InstanceId,
    string SurfaceId,
    int X,
    int Y,
    int Width,
    int Height,
    bool Rotated);

public sealed record FarmingGuideRepackingPlan(
    FarmingGuideRepackingPlacement Incoming,
    IReadOnlyList<FarmingGuideRepackingPlacement> ExistingPlacements,
    IReadOnlyList<string> MovedInstanceIds,
    int SearchNodes);

/// <summary>
/// Bounded deterministic displacement search used only after the normal direct-fit path
/// fails. Existing placements remain in place until they actually block a required move;
/// the search can then cascade through additional unlocked items. This avoids the old
/// false "discard" result caused by fragmented but sufficient storage while keeping the
/// cost proportional to the local conflict rather than globally repacking every raid item.
/// </summary>
public static class FarmingGuideRepackingPlanner
{
    public const int DefaultMaxSearchNodes = 60000;

    public static FarmingGuideRepackingPlan? TryPlan(
        IReadOnlyList<FarmingGuideRepackingSurface> surfaces,
        IReadOnlyList<FarmingGuideRepackingItem> items,
        FarmingGuideRepackingIncoming incoming,
        int maxSearchNodes = DefaultMaxSearchNodes)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(incoming);
        if (maxSearchNodes <= 0)
            return null;

        var surfaceMap = new Dictionary<string, FarmingGuideRepackingSurface>(StringComparer.Ordinal);
        foreach (var surface in surfaces)
        {
            if (string.IsNullOrWhiteSpace(surface.Id) || surface.Width <= 0 || surface.Height <= 0 ||
                !surfaceMap.TryAdd(surface.Id, surface))
            {
                return null;
            }
        }
        if (surfaceMap.Count == 0)
            return null;

        var itemMap = new Dictionary<string, FarmingGuideRepackingItem>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.InstanceId) ||
                string.Equals(item.InstanceId, incoming.InstanceId, StringComparison.Ordinal) ||
                !surfaceMap.ContainsKey(item.CurrentSurfaceId) ||
                item.CurrentWidth <= 0 || item.CurrentHeight <= 0 ||
                !itemMap.TryAdd(item.InstanceId, item))
            {
                return null;
            }
        }

        var baseOccupancy = new List<OccupiedPlacement>();
        foreach (var surface in surfaces)
        {
            for (var index = 0; index < surface.FixedObstacles.Count; index++)
            {
                var fixedPlacement = surface.FixedObstacles[index];
                if (fixedPlacement.Width <= 0 || fixedPlacement.Height <= 0)
                    continue;
                baseOccupancy.Add(new OccupiedPlacement(
                    $"__fixed__{surface.Id}__{index}",
                    surface.Id,
                    fixedPlacement.X,
                    fixedPlacement.Y,
                    fixedPlacement.Width,
                    fixedPlacement.Height,
                    Fixed: true,
                    Incoming: false,
                    Moved: false));
            }
        }

        foreach (var item in items)
        {
            baseOccupancy.Add(new OccupiedPlacement(
                item.InstanceId,
                item.CurrentSurfaceId,
                item.CurrentX,
                item.CurrentY,
                item.CurrentWidth,
                item.CurrentHeight,
                Fixed: !item.Movable,
                Incoming: false,
                Moved: false));
        }

        var incomingCandidates = EnumeratePlacements(incoming.InstanceId, incoming.Options, surfaceMap)
            .Select(candidate =>
            {
                var overlaps = FindOverlaps(baseOccupancy, candidate);
                var blockedByHardObstacle = overlaps.Any(value =>
                    value.Fixed || !itemMap.TryGetValue(value.InstanceId, out var existing) || !existing.Movable);
                var blockers = blockedByHardObstacle
                    ? []
                    : overlaps
                        .Select(value => value.InstanceId)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                var option = FindOption(incoming.Options, candidate);
                var surface = surfaceMap[candidate.SurfaceId];
                return new IncomingCandidate(
                    candidate,
                    blockers,
                    blockedByHardObstacle,
                    option?.Preference ?? int.MaxValue,
                    surface.Priority);
            })
            .Where(static value => !value.BlockedByHardObstacle)
            .OrderBy(value => value.Blockers.Count)
            .ThenBy(value => value.Preference)
            .ThenBy(value => value.SurfacePriority)
            .ThenBy(value => value.Placement.Y)
            .ThenBy(value => value.Placement.X)
            .ThenBy(value => value.Placement.Rotated)
            .ToArray();

        CandidatePlan? best = null;
        var searchNodes = 0;

        foreach (var incomingCandidate in incomingCandidates)
        {
            if (searchNodes >= maxSearchNodes)
                break;
            if (best is not null && incomingCandidate.Blockers.Count > best.Score.MovedCount)
                break;

            var occupancy = baseOccupancy
                .Where(value => !incomingCandidate.Blockers.Contains(value.InstanceId, StringComparer.Ordinal))
                .ToList();
            occupancy.Add(new OccupiedPlacement(
                incoming.InstanceId,
                incomingCandidate.Placement.SurfaceId,
                incomingCandidate.Placement.X,
                incomingCandidate.Placement.Y,
                incomingCandidate.Placement.Width,
                incomingCandidate.Placement.Height,
                Fixed: true,
                Incoming: true,
                Moved: true));

            var released = incomingCandidate.Blockers.ToHashSet(StringComparer.Ordinal);
            var unplaced = incomingCandidate.Blockers.ToHashSet(StringComparer.Ordinal);
            var movedPlacements = new Dictionary<string, FarmingGuideRepackingPlacement>(StringComparer.Ordinal);
            CandidatePlan? candidateBest = null;
            var candidateLowerBound = incomingCandidate.Blockers.Count;
            var optimalMovedCountFound = false;

            Search(
                occupancy,
                released,
                unplaced,
                movedPlacements,
                incomingCandidate,
                candidateLowerBound,
                ref candidateBest,
                ref optimalMovedCountFound,
                ref searchNodes,
                maxSearchNodes,
                itemMap,
                surfaceMap);

            if (candidateBest is not null && (best is null || Compare(candidateBest.Score, best.Score) < 0))
                best = candidateBest;
        }

        if (best is null)
            return null;

        return new FarmingGuideRepackingPlan(
            best.Incoming,
            best.ExistingPlacements,
            best.MovedInstanceIds,
            searchNodes);
    }

    private static void Search(
        List<OccupiedPlacement> occupancy,
        HashSet<string> released,
        HashSet<string> unplaced,
        Dictionary<string, FarmingGuideRepackingPlacement> movedPlacements,
        IncomingCandidate incomingCandidate,
        int candidateLowerBound,
        ref CandidatePlan? candidateBest,
        ref bool optimalMovedCountFound,
        ref int searchNodes,
        int maxSearchNodes,
        IReadOnlyDictionary<string, FarmingGuideRepackingItem> itemMap,
        IReadOnlyDictionary<string, FarmingGuideRepackingSurface> surfaceMap)
    {
        if (optimalMovedCountFound || searchNodes >= maxSearchNodes)
            return;
        searchNodes++;

        if (unplaced.Count == 0)
        {
            var finalPlacements = new Dictionary<string, FarmingGuideRepackingPlacement>(StringComparer.Ordinal);
            foreach (var item in itemMap.Values)
            {
                finalPlacements[item.InstanceId] = movedPlacements.TryGetValue(item.InstanceId, out var moved)
                    ? moved
                    : CurrentPlacement(item);
            }

            if (!HasValidParentGraph(finalPlacements, surfaceMap, itemMap))
                return;

            var movedIds = finalPlacements.Values
                .Where(value => !SamePlacement(value, CurrentPlacement(itemMap[value.InstanceId])))
                .Select(value => value.InstanceId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var score = BuildScore(
                movedIds,
                finalPlacements,
                incomingCandidate,
                itemMap);
            var plan = new CandidatePlan(
                incomingCandidate.Placement,
                finalPlacements.Values.OrderBy(value => value.InstanceId, StringComparer.Ordinal).ToArray(),
                movedIds,
                score);
            if (candidateBest is null || Compare(score, candidateBest.Score) < 0)
                candidateBest = plan;
            if (score.MovedCount == candidateLowerBound)
                optimalMovedCountFound = true;
            return;
        }

        var nextId = unplaced
            .Select(id => itemMap[id])
            .OrderByDescending(item => item.CurrentWidth * item.CurrentHeight)
            .ThenBy(item => item.Options.Count)
            .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
            .Select(item => item.InstanceId)
            .First();
        var next = itemMap[nextId];

        var placementCandidates = EnumeratePlacements(next.InstanceId, next.Options, surfaceMap)
            .Select(placement =>
            {
                var overlaps = FindOverlaps(occupancy, placement);
                var invalid = overlaps.Any(value => value.Fixed || value.Incoming || value.Moved);
                var displacementCount = invalid
                    ? int.MaxValue
                    : overlaps.Count(value => itemMap.TryGetValue(value.InstanceId, out var conflict) && conflict.Movable);
                var option = FindOption(next.Options, placement);
                var sameSurface = string.Equals(placement.SurfaceId, next.CurrentSurfaceId, StringComparison.Ordinal);
                var distance = sameSurface
                    ? Math.Abs(placement.X - next.CurrentX) + Math.Abs(placement.Y - next.CurrentY)
                    : 1000;
                return new ExistingCandidate(
                    placement,
                    overlaps,
                    invalid,
                    displacementCount,
                    sameSurface ? 0 : 1,
                    option?.Preference ?? int.MaxValue,
                    distance,
                    surfaceMap[placement.SurfaceId].Priority);
            })
            .Where(static value => !value.Invalid)
            .OrderBy(value => value.DisplacementCount)
            .ThenBy(value => value.CrossSurface)
            .ThenBy(value => value.Preference)
            .ThenBy(value => value.Distance)
            .ThenBy(value => value.SurfacePriority)
            .ThenBy(value => value.Placement.Y)
            .ThenBy(value => value.Placement.X)
            .ThenBy(value => value.Placement.Rotated)
            .ToArray();

        foreach (var candidate in placementCandidates)
        {
            if (optimalMovedCountFound || searchNodes >= maxSearchNodes)
                return;

            var displaced = candidate.Overlaps
                .Where(value => itemMap.TryGetValue(value.InstanceId, out var conflict) && conflict.Movable)
                .Select(value => value.InstanceId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (displaced.Any(movedPlacements.ContainsKey))
                continue;

            var nextOccupancy = occupancy
                .Where(value => !displaced.Contains(value.InstanceId, StringComparer.Ordinal))
                .ToList();
            nextOccupancy.Add(new OccupiedPlacement(
                next.InstanceId,
                candidate.Placement.SurfaceId,
                candidate.Placement.X,
                candidate.Placement.Y,
                candidate.Placement.Width,
                candidate.Placement.Height,
                Fixed: false,
                Incoming: false,
                Moved: true));

            var nextReleased = new HashSet<string>(released, StringComparer.Ordinal) { next.InstanceId };
            nextReleased.UnionWith(displaced);
            var nextUnplaced = new HashSet<string>(unplaced, StringComparer.Ordinal);
            nextUnplaced.Remove(next.InstanceId);
            nextUnplaced.UnionWith(displaced);
            var nextMoved = new Dictionary<string, FarmingGuideRepackingPlacement>(movedPlacements, StringComparer.Ordinal)
            {
                [next.InstanceId] = candidate.Placement,
            };

            Search(
                nextOccupancy,
                nextReleased,
                nextUnplaced,
                nextMoved,
                incomingCandidate,
                candidateLowerBound,
                ref candidateBest,
                ref optimalMovedCountFound,
                ref searchNodes,
                maxSearchNodes,
                itemMap,
                surfaceMap);
        }
    }

    private static IEnumerable<FarmingGuideRepackingPlacement> EnumeratePlacements(
        string instanceId,
        IReadOnlyList<FarmingGuideRepackingOption> options,
        IReadOnlyDictionary<string, FarmingGuideRepackingSurface> surfaceMap)
    {
        foreach (var option in options
                     .OrderBy(value => value.Preference)
                     .ThenBy(value => surfaceMap.TryGetValue(value.SurfaceId, out var surface) ? surface.Priority : int.MaxValue)
                     .ThenBy(value => value.Rotated))
        {
            if (!surfaceMap.TryGetValue(option.SurfaceId, out var surface) ||
                option.Width <= 0 || option.Height <= 0 ||
                option.Width > surface.Width || option.Height > surface.Height)
            {
                continue;
            }

            for (var y = 0; y <= surface.Height - option.Height; y++)
            {
                for (var x = 0; x <= surface.Width - option.Width; x++)
                {
                    yield return new FarmingGuideRepackingPlacement(
                        instanceId,
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

    private static FarmingGuideRepackingOption? FindOption(
        IReadOnlyList<FarmingGuideRepackingOption> options,
        FarmingGuideRepackingPlacement placement) =>
        options.FirstOrDefault(value =>
            string.Equals(value.SurfaceId, placement.SurfaceId, StringComparison.Ordinal) &&
            value.Width == placement.Width &&
            value.Height == placement.Height &&
            value.Rotated == placement.Rotated);

    private static IReadOnlyList<OccupiedPlacement> FindOverlaps(
        IReadOnlyList<OccupiedPlacement> occupancy,
        FarmingGuideRepackingPlacement placement) =>
        occupancy
            .Where(value =>
                string.Equals(value.SurfaceId, placement.SurfaceId, StringComparison.Ordinal) &&
                Overlaps(
                    placement.X,
                    placement.Y,
                    placement.Width,
                    placement.Height,
                    value.X,
                    value.Y,
                    value.Width,
                    value.Height))
            .ToArray();

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

    private static FarmingGuideRepackingPlacement CurrentPlacement(FarmingGuideRepackingItem item) =>
        new(
            item.InstanceId,
            item.CurrentSurfaceId,
            item.CurrentX,
            item.CurrentY,
            item.CurrentWidth,
            item.CurrentHeight,
            item.CurrentRotated);

    private static bool SamePlacement(
        FarmingGuideRepackingPlacement left,
        FarmingGuideRepackingPlacement right) =>
        string.Equals(left.SurfaceId, right.SurfaceId, StringComparison.Ordinal) &&
        left.X == right.X &&
        left.Y == right.Y &&
        left.Rotated == right.Rotated;

    private static bool HasValidParentGraph(
        IReadOnlyDictionary<string, FarmingGuideRepackingPlacement> finalPlacements,
        IReadOnlyDictionary<string, FarmingGuideRepackingSurface> surfaceMap,
        IReadOnlyDictionary<string, FarmingGuideRepackingItem> itemMap)
    {
        var parentById = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var placement in finalPlacements.Values)
        {
            if (!surfaceMap.TryGetValue(placement.SurfaceId, out var surface))
                return false;
            var parent = surface.ParentInstanceId;
            if (parent is not null && !itemMap.ContainsKey(parent))
                return false;
            parentById[placement.InstanceId] = parent;
        }

        foreach (var instanceId in parentById.Keys)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            string? current = instanceId;
            while (current is not null)
            {
                if (!visited.Add(current))
                    return false;
                if (!parentById.TryGetValue(current, out current))
                    return false;
            }
        }
        return true;
    }

    private static PlanScore BuildScore(
        IReadOnlyList<string> movedIds,
        IReadOnlyDictionary<string, FarmingGuideRepackingPlacement> finalPlacements,
        IncomingCandidate incomingCandidate,
        IReadOnlyDictionary<string, FarmingGuideRepackingItem> itemMap)
    {
        var crossSurface = 0;
        var distance = 0;
        foreach (var id in movedIds)
        {
            var current = itemMap[id];
            var final = finalPlacements[id];
            if (!string.Equals(current.CurrentSurfaceId, final.SurfaceId, StringComparison.Ordinal))
            {
                crossSurface++;
                distance += 1000;
            }
            else
            {
                distance += Math.Abs(current.CurrentX - final.X) + Math.Abs(current.CurrentY - final.Y);
            }
        }

        return new PlanScore(
            movedIds.Count,
            incomingCandidate.Preference,
            crossSurface,
            distance,
            incomingCandidate.SurfacePriority,
            incomingCandidate.Placement.Y,
            incomingCandidate.Placement.X,
            incomingCandidate.Placement.Rotated ? 1 : 0);
    }

    private static int Compare(PlanScore left, PlanScore right)
    {
        var values = new[]
        {
            left.MovedCount.CompareTo(right.MovedCount),
            left.IncomingPreference.CompareTo(right.IncomingPreference),
            left.CrossSurfaceMoves.CompareTo(right.CrossSurfaceMoves),
            left.TotalDistance.CompareTo(right.TotalDistance),
            left.IncomingSurfacePriority.CompareTo(right.IncomingSurfacePriority),
            left.IncomingY.CompareTo(right.IncomingY),
            left.IncomingX.CompareTo(right.IncomingX),
            left.IncomingRotated.CompareTo(right.IncomingRotated),
        };
        return values.FirstOrDefault(static value => value != 0);
    }

    private sealed record OccupiedPlacement(
        string InstanceId,
        string SurfaceId,
        int X,
        int Y,
        int Width,
        int Height,
        bool Fixed,
        bool Incoming,
        bool Moved);

    private sealed record IncomingCandidate(
        FarmingGuideRepackingPlacement Placement,
        IReadOnlyList<string> Blockers,
        bool BlockedByHardObstacle,
        int Preference,
        int SurfacePriority);

    private sealed record ExistingCandidate(
        FarmingGuideRepackingPlacement Placement,
        IReadOnlyList<OccupiedPlacement> Overlaps,
        bool Invalid,
        int DisplacementCount,
        int CrossSurface,
        int Preference,
        int Distance,
        int SurfacePriority);

    private sealed record PlanScore(
        int MovedCount,
        int IncomingPreference,
        int CrossSurfaceMoves,
        int TotalDistance,
        int IncomingSurfacePriority,
        int IncomingY,
        int IncomingX,
        int IncomingRotated);

    private sealed record CandidatePlan(
        FarmingGuideRepackingPlacement Incoming,
        IReadOnlyList<FarmingGuideRepackingPlacement> ExistingPlacements,
        IReadOnlyList<string> MovedInstanceIds,
        PlanScore Score);
}
