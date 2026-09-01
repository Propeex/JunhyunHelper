namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Counts modeled inventory ownership directly from a Farming Guide snapshot. Raid
/// planning uses snapshot deltas instead of a historical "accepted scan" truth so a
/// later move/replacement/discard automatically changes the owned quantity.
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
            AddState(counts, state);
        if (snapshot.Rig is not null)
            AddState(counts, snapshot.Rig);
        if (snapshot.Backpack is not null)
            AddState(counts, snapshot.Backpack);
        if (snapshot.SecureContainer is not null)
            AddState(counts, snapshot.SecureContainer);
        foreach (var stored in snapshot.StoredItems)
            AddState(counts, stored.Item);

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

    private static void AddState(Dictionary<string, int> counts, FarmingGuideItemState state)
    {
        counts[state.ItemId] = counts.GetValueOrDefault(state.ItemId) + 1;
        foreach (var attachment in state.Attachments.Values)
        {
            if (attachment is not null)
                AddState(counts, attachment);
        }
        foreach (var plate in state.ArmorPlates.Values)
        {
            if (plate is not null)
                AddState(counts, plate);
        }
    }
}
