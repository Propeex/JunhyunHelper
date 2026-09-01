using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Centralizes storage-surface-specific placement semantics. Tarkov special slots are
/// not ordinary 1x1 inventory grids: only items explicitly classified as special-slot
/// compatible may enter them, and a compatible item occupies exactly one special slot
/// regardless of its normal inventory footprint.
/// </summary>
public static class FarmingGuideStoragePlacementPolicy
{
    public static bool CanStore(
        FarmingGuideStorageKind storage,
        GameItem item,
        FarmingGuideItemFilter filter)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(filter);
        return FarmingGuideCompatibility.FilterAllows(item, filter) &&
               (storage != FarmingGuideStorageKind.SpecialSlots || IsSpecialSlotCompatible(item));
    }

    public static bool IsSpecialSlotCompatible(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Types.Any(value =>
            string.Equals(Normalize(value), "specialslot", StringComparison.Ordinal));
    }

    public static (int Width, int Height) Footprint(
        FarmingGuideStorageKind storage,
        GameItem item,
        bool rotated)
    {
        ArgumentNullException.ThrowIfNull(item);
        return storage == FarmingGuideStorageKind.SpecialSlots
            ? (1, 1)
            : FarmingGuidePlacementEngine.Footprint(
                item.Width ?? 1,
                item.Height ?? 1,
                rotated);
    }

    public static bool SupportsRotation(FarmingGuideStorageKind storage, GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return storage != FarmingGuideStorageKind.SpecialSlots &&
               (item.Width ?? 1) != (item.Height ?? 1);
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
