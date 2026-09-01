namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Counts modeled inventory ownership directly from a Farming Guide snapshot. Raid
/// planning uses snapshot deltas instead of a historical "accepted scan" counter so a
/// later move/replacement/discard automatically changes the owned quantity truth.
/// </summary>
public static class FarmingGuideSnapshotInventoryCounter
{
    public static int Count(FarmingGuideLoadoutSnapshot snapshot, string itemId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        var count = 0;
        foreach (var state in snapshot.Equipment.Values)
            count += CountState(state, itemId);

        if (snapshot.Rig is not null)
            count += CountState(snapshot.Rig, itemId);
        if (snapshot.Backpack is not null)
            count += CountState(snapshot.Backpack, itemId);
        if (snapshot.SecureContainer is not null)
            count += CountState(snapshot.SecureContainer, itemId);

        foreach (var stored in snapshot.StoredItems)
            count += CountState(stored.Item, itemId);

        return count;
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

    private static int CountState(FarmingGuideItemState state, string itemId)
    {
        var count = string.Equals(state.ItemId, itemId, StringComparison.Ordinal) ? 1 : 0;
        foreach (var attachment in state.Attachments.Values)
        {
            if (attachment is not null)
                count += CountState(attachment, itemId);
        }
        foreach (var plate in state.ArmorPlates.Values)
        {
            if (plate is not null)
                count += CountState(plate, itemId);
        }
        return count;
    }
}
