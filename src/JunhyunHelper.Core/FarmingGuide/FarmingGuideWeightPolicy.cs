using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Current Tarkov carrying-weight facts used by Farming Guide. Strength increases maximum
/// carrying weight by 0.6% per level from a 77 kg base; elite (51) is treated as 100 kg.
/// At elite, weapons equipped on sling, back and holster do not contribute to character
/// weight. Instance-specific stimulant effects are intentionally outside the product
/// boundary because Scanner cannot observe them.
/// </summary>
public static class FarmingGuideWeightPolicy
{
    private const decimal BaseMaximumKg = 77m;
    private const decimal PerStrengthLevel = 0.006m;
    private const decimal EliteMaximumKg = 100m;

    public static decimal MaximumCarryWeightKg(FarmingGuideWeightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var level = settings.Normalized().StrengthLevel;
        return level >= 51
            ? EliteMaximumKg
            : BaseMaximumKg * (1m + PerStrengthLevel * level);
    }

    public static decimal ItemWeightKg(GameItem item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Math.Max(0m, item.WeightKg ?? 0m) * Math.Max(1, quantity);
    }

    public static bool EquipmentCountsTowardWeight(
        FarmingGuideEquipmentSlot slot,
        FarmingGuideWeightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Normalized().StrengthLevel < 51)
            return true;

        return slot is not (
            FarmingGuideEquipmentSlot.PrimaryWeapon1 or
            FarmingGuideEquipmentSlot.PrimaryWeapon2 or
            FarmingGuideEquipmentSlot.Holster);
    }

    public static bool IsWithinLimit(decimal totalWeightKg, FarmingGuideWeightSettings settings) =>
        Math.Max(0m, totalWeightKg) <= MaximumCarryWeightKg(settings);
}
