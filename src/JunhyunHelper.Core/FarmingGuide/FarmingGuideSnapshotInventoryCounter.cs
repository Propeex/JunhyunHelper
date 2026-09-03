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
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var state in snapshot.Equipment.Values)
            AddState(counts, state, 1, raidAcquiredOnly: false);
        if (snapshot.Rig is not null)
            AddState(counts, snapshot.Rig, 1, raidAcquiredOnly: false);
        if (snapshot.Backpack is not null)
            AddState(counts, snapshot.Backpack, 1, raidAcquiredOnly: false);
        if (snapshot.SecureContainer is not null)
            AddState(counts, snapshot.SecureContainer, 1, raidAcquiredOnly: false);
        foreach (var stored in snapshot.StoredItems)
            AddState(counts, stored.Item, stored.NormalizedQuantity, raidAcquiredOnly: false);

        return counts;
    }

    /// <summary>
    /// Counts only items that entered the modeled active raid through a confirmed Scanner
    /// identification. The v1.17 product rule treats every such incoming item as FIR.
    /// Baseline item counts are irrelevant: explicit provenance survives identical-item
    /// replacement and disappears naturally if the acquired item is later discarded.
    /// </summary>
    public static IReadOnlyDictionary<string, int> CountRaidAcquiredAll(FarmingGuideLoadoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var state in snapshot.Equipment.Values)
            AddState(counts, state, 1, raidAcquiredOnly: true);
        if (snapshot.Rig is not null)
            AddState(counts, snapshot.Rig, 1, raidAcquiredOnly: true);
        if (snapshot.Backpack is not null)
            AddState(counts, snapshot.Backpack, 1, raidAcquiredOnly: true);
        if (snapshot.SecureContainer is not null)
            AddState(counts, snapshot.SecureContainer, 1, raidAcquiredOnly: true);
        foreach (var stored in snapshot.StoredItems)
            AddState(counts, stored.Item, stored.NormalizedQuantity, raidAcquiredOnly: true);

        return counts;
    }

    public static int CountRaidAcquired(FarmingGuideLoadoutSnapshot snapshot, string itemId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        return CountRaidAcquiredAll(snapshot).GetValueOrDefault(itemId);
    }

    // Retained for compatibility with older maintenance/tests. New v1.17 decision code must
    // use explicit raid-acquired provenance instead of deriving FIR ownership from net count.
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
        int rootQuantity,
        bool raidAcquiredOnly)
    {
        if (!raidAcquiredOnly || state.RaidAcquired)
        {
            counts[state.ItemId] = checked(
                counts.GetValueOrDefault(state.ItemId) + Math.Max(1, rootQuantity));
        }

        // Complete-equipment runtime is root-only, but keep legacy assembly traversal for
        // compatibility. Child states have their own provenance if they ever exist.
        foreach (var attachment in state.Attachments.Values)
        {
            if (attachment is not null)
                AddState(counts, attachment, 1, raidAcquiredOnly);
        }
        foreach (var plate in state.ArmorPlates.Values)
        {
            if (plate is not null)
                AddState(counts, plate, 1, raidAcquiredOnly);
        }
    }
}
