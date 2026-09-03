namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Counts modeled inventory ownership directly from a Farming Guide snapshot. Stackable
/// stored items contribute their explicit quantity; equipment and legacy assembly states
/// remain single instances.
/// </summary>
public static class FarmingGuideSnapshotInventoryCounter
{
    public static int Count(FarmingGuideLoadoutSnapshot snapshot, string itemId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return CountAll(snapshot).GetValueOrDefault(itemId);
    }

    public static IReadOnlyDictionary<string, int> CountAll(FarmingGuideLoadoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return CountMatching(snapshot, CountFilter.All);
    }

    /// <summary>
    /// Counts item units acquired during the active modeled raid. This is acquisition history,
    /// not Tarkov FIR eligibility.
    /// </summary>
    public static IReadOnlyDictionary<string, int> CountRaidAcquiredAll(FarmingGuideLoadoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return CountMatching(snapshot, CountFilter.RaidAcquired);
    }

    public static int CountRaidAcquired(FarmingGuideLoadoutSnapshot snapshot, string itemId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return CountRaidAcquiredAll(snapshot).GetValueOrDefault(itemId);
    }

    /// <summary>
    /// Counts only item units explicitly proven to carry Tarkov Found-in-Raid provenance.
    /// Unknown and NotFoundInRaid units are deliberately excluded.
    /// </summary>
    public static IReadOnlyDictionary<string, int> CountFoundInRaidAll(FarmingGuideLoadoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return CountMatching(snapshot, CountFilter.FoundInRaid);
    }

    public static int CountFoundInRaid(FarmingGuideLoadoutSnapshot snapshot, string itemId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return CountFoundInRaidAll(snapshot).GetValueOrDefault(itemId);
    }

    /// <summary>
    /// Compatibility helper for historical callers that need active-raid acquisition counts.
    /// Explicit acquisition provenance wins over baseline item-id subtraction.
    /// </summary>
    public static int AcquiredSince(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot current,
        string itemId)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        var explicitAcquired = CountRaidAcquired(current, itemId);
        if (explicitAcquired > 0)
            return explicitAcquired;
        return Math.Max(0, Count(current, itemId) - Count(baseline, itemId));
    }

    /// <summary>
    /// Compatibility helper for historical callers. Explicit acquisition provenance wins
    /// whenever it is present; baseline deltas remain available for older snapshots.
    /// </summary>
    public static IReadOnlyDictionary<string, int> AcquiredSinceAll(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var explicitCounts = CountRaidAcquiredAll(current);
        if (explicitCounts.Count > 0)
            return explicitCounts;

        var baselineCounts = CountAll(baseline);
        var currentCounts = CountAll(current);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in currentCounts)
        {
            var delta = pair.Value - baselineCounts.GetValueOrDefault(pair.Key);
            if (delta > 0)
                result[pair.Key] = delta;
        }
        return result;
    }

    private static IReadOnlyDictionary<string, int> CountMatching(
        FarmingGuideLoadoutSnapshot snapshot,
        CountFilter filter)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        EnumerateRoots(snapshot, (state, quantity) => AddState(counts, state, quantity, filter));
        return counts;
    }

    private static void EnumerateRoots(
        FarmingGuideLoadoutSnapshot snapshot,
        Action<FarmingGuideItemState, int> add)
    {
        foreach (var state in snapshot.Equipment.Values)
            add(state, 1);
        if (snapshot.Rig is not null)
            add(snapshot.Rig, 1);
        if (snapshot.Backpack is not null)
            add(snapshot.Backpack, 1);
        if (snapshot.SecureContainer is not null)
            add(snapshot.SecureContainer, 1);
        foreach (var stored in snapshot.StoredItems)
            add(stored.Item, stored.NormalizedQuantity);
    }

    private static void AddState(
        Dictionary<string, int> counts,
        FarmingGuideItemState state,
        int rootQuantity,
        CountFilter filter)
    {
        var include = filter switch
        {
            CountFilter.All => true,
            CountFilter.RaidAcquired => state.RaidAcquired,
            CountFilter.FoundInRaid => state.IsFirQualified,
            _ => false,
        };
        if (include)
        {
            counts[state.ItemId] = checked(
                counts.GetValueOrDefault(state.ItemId) + Math.Max(1, rootQuantity));
        }

        foreach (var attachment in state.Attachments.Values)
        {
            if (attachment is not null)
                AddState(counts, attachment, 1, filter);
        }
        foreach (var plate in state.ArmorPlates.Values)
        {
            if (plate is not null)
                AddState(counts, plate, 1, filter);
        }
    }

    private enum CountFilter
    {
        All,
        RaidAcquired,
        FoundInRaid,
    }
}
