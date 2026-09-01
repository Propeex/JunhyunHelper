using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Centralizes storage-surface-specific placement semantics. Tarkov special slots are
/// not ordinary 1x1 inventory grids: only items explicitly classified as special-slot
/// compatible may enter them, and a compatible item occupies exactly one special slot
/// regardless of its normal inventory footprint. Nested storage inside an item that
/// happens to sit in a special slot remains an ordinary storage surface.
/// </summary>
public static class FarmingGuideStoragePlacementPolicy
{
    public static bool CanStore(
        FarmingGuideStorageKind storage,
        string? parentInstanceId,
        GameItem item,
        FarmingGuideItemFilter filter)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(filter);
        return FarmingGuideCompatibility.FilterAllows(item, filter) &&
               (!IsSpecialSlotSurface(storage, parentInstanceId) || IsSpecialSlotCompatible(item));
    }

    /// <summary>
    /// A positive source allow-list means the grid exists for a particular family of
    /// items rather than as generic inventory capacity. Raid planning should consume
    /// these dedicated slots before ordinary root storage when the incoming item matches,
    /// e.g. a key should go into a carried key container instead of occupying another
    /// secure-container cell.
    /// </summary>
    public static bool IsDedicatedStorageFor(GameItem item, FarmingGuideItemFilter filter)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.AllowedItemIds.Count == 0 && filter.AllowedCategoryIds.Count == 0)
            return false;

        return FarmingGuideCompatibility.FilterAllows(item, filter);
    }

    public static bool IsSpecialSlotCompatible(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Types.Any(value =>
            string.Equals(Normalize(value), "specialslot", StringComparison.Ordinal));
    }

    public static (int Width, int Height) Footprint(
        FarmingGuideStorageKind storage,
        string? parentInstanceId,
        GameItem item,
        bool rotated)
    {
        ArgumentNullException.ThrowIfNull(item);
        return IsSpecialSlotSurface(storage, parentInstanceId)
            ? (1, 1)
            : FarmingGuidePlacementEngine.Footprint(
                item.Width ?? 1,
                item.Height ?? 1,
                rotated);
    }

    public static bool SupportsRotation(
        FarmingGuideStorageKind storage,
        string? parentInstanceId,
        GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return !IsSpecialSlotSurface(storage, parentInstanceId) &&
               (item.Width ?? 1) != (item.Height ?? 1);
    }

    public static bool IsSpecialSlotSurface(FarmingGuideStorageKind storage, string? parentInstanceId) =>
        storage == FarmingGuideStorageKind.SpecialSlots && string.IsNullOrWhiteSpace(parentInstanceId);

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
