using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Product boundary for the complete-equipment Farming Guide model.
/// Farming Guide may retain source assembly metadata as read-only evidence, but the
/// user-facing runtime model never exposes attachment or armor-plate state. Source-backed
/// inventory grids are different: any item that Tarkov defines as a real storage surface
/// keeps those grids so specialized nested containers remain usable.
/// </summary>
public static class FarmingGuideCompleteEquipmentPolicy
{
    public static GameItem ToRuntimeItem(
        GameItem item,
        IReadOnlyDictionary<string, GameItem> sourceCatalog)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(sourceCatalog);

        var layout = item.FarmingGuideData;
        var runtimeLayout = layout is null
            ? null
            : layout with
            {
                // Keep authoritative storage grids for every real Tarkov container.
                // The grid definitions already include allowed/excluded item/category
                // filters, so Key tool, document/money/card/injector cases and future
                // specialized containers do not need brittle item-name allowlists.
                StorageGrids = layout.StorageGrids,
                AttachmentSlots = Array.Empty<FarmingGuideAttachmentSlotDefinition>(),
                ArmorSlots = Array.Empty<FarmingGuideArmorSlotDefinition>(),
            };

        var completeImageUrl = PreferredCompleteImageUrl(item, sourceCatalog);
        return item with
        {
            IconUrl = completeImageUrl ?? item.IconUrl,
            FarmingGuideData = runtimeLayout,
        };
    }

    public static FarmingGuideItemState NormalizeState(FarmingGuideItemState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        // Complete-equipment normalization removes editable assembly detail, not raid
        // provenance. A newly acquired complete root must remain FIR-acquired after any
        // normalize/render/persist boundary used by the live Farming Guide session.
        return FarmingGuideItemState.Create(state.ItemId, state.RaidAcquired);
    }

    public static bool SupportsNestedStorage(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.FarmingGuideData?.StorageGrids.Count is > 0;
    }

    public static string? PreferredCompleteImageUrl(
        GameItem item,
        IReadOnlyDictionary<string, GameItem> sourceCatalog)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(sourceCatalog);

        var defaultPresetId = item.FarmingGuideAssembly?.DefaultPresetItemId;
        if (!string.IsNullOrWhiteSpace(defaultPresetId) &&
            sourceCatalog.TryGetValue(defaultPresetId, out var preset))
        {
            var presetImage = preset.FarmingGuideAssembly?.Image512Url ??
                              preset.FarmingGuideAssembly?.GridImageUrl ??
                              preset.IconUrl;
            if (!string.IsNullOrWhiteSpace(presetImage))
                return presetImage;
        }

        var ownSourceImage = item.FarmingGuideAssembly?.Image512Url ??
                             item.FarmingGuideAssembly?.GridImageUrl;
        return string.IsNullOrWhiteSpace(ownSourceImage)
            ? item.IconUrl
            : ownSourceImage;
    }
}
