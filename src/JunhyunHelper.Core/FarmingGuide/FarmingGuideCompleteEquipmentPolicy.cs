using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Product boundary for the v1.15.2 complete-equipment model.
/// Farming Guide may retain source assembly metadata as read-only evidence, but the
/// user-facing runtime model never exposes attachment or armor-plate state.
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
        return FarmingGuideItemState.Create(state.ItemId);
    }

    public static bool SupportsNestedStorage(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.FarmingGuideData?.StorageGrids.Count is not > 0)
            return false;

        return FarmingGuideCompatibility.IsStorageCarrierCompatible(FarmingGuideStorageKind.Backpack, item) ||
               FarmingGuideCompatibility.IsStorageCarrierCompatible(FarmingGuideStorageKind.Rig, item);
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
