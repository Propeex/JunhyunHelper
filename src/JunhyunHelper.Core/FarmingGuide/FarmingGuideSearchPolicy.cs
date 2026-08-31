using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

public static class FarmingGuideSearchPolicy
{
    /// <summary>
    /// Farming Guide edits concrete raid-start inventory items. The upstream item feed
    /// also exposes assembled weapon presets as Item records; those are recipes/configs,
    /// not additional physical base weapons, and must not appear as duplicate draggable
    /// items in this editor.
    /// </summary>
    public static bool IsDraggableInventoryItem(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (string.Equals(
                item.FarmingGuideData?.PropertiesType,
                "ItemPropertiesPreset",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !item.Types.Any(type =>
            string.Equals(type.Trim(), "preset", StringComparison.OrdinalIgnoreCase));
    }
}
