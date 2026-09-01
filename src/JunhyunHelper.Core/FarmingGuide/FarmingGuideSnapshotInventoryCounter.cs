namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Counts modeled inventory ownership directly from a Farming Guide snapshot. Stackable
/// stored items contribute their explicit quantity; equipment and legacy assembly states
/// remain single instances. Raid planning therefore derives FIR progress from the current
/// accepted snapshot rather than historical scan events.
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
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var state in snapshot.Equipment.Values)
            AddState(counts, state, 1);
        if (snapshot.Rig is not null)
            AddState(counts, snapshot.Rig, 1);
        if (snapshot.Backpack is not null)
            AddState(counts, snapshot.Backpack, 1);
        if (snapshot.SecureContainer is not null)
            AddState(counts, snapshot.SecureContainer, 1);
        foreach (var stored in snapshot.StoredItems)
            AddState(counts, stored.Item, stored.NormalizedQuantity);

        return counts;
    }

    public static int AcquiredSince(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot current,
        string itemId)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        return Math.Max(0, Count(current, itemId) - Count(baseline, itemId));
    }

    public static IReadOnlyDictionary<string, int> AcquiredSinceAll(
        FarmingGuideLoadoutSnapshot baseline,
        FarmingGuideLoadoutSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

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

    private static void AddState(
        Dictionary<string, int> counts,
        FarmingGuideItemState state,
        int rootQuantity)
    {
        counts[state.ItemId] = checked(counts.GetValueOrDefault(state.ItemId) + Math.Max(1, rootQuantity));
        foreach (var attachment in state.Attachments.Values)
        {
            if (attachment is not null)
                AddState(counts, attachment, 1);
        }
        foreach (var plate in state.ArmorPlates.Values)
        {
            if (plate is not null)
                AddState(counts, plate, 1);
        }
    }
}
