using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

public static class FarmingGuideCompatibility
{
    public static bool IsEquipmentSlotCompatible(FarmingGuideEquipmentSlot slot, GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var propertyType = item.FarmingGuideData?.PropertiesType ?? string.Empty;
        var keys = item.Types
            .Concat(item.Categories)
            .Select(Normalize)
            .Where(static value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var isPistol = ContainsAny(keys, "pistol", "pistols", "revolver", "revolvers", "handgun", "handguns");
        var isWeapon = propertyType.Equals("ItemPropertiesWeapon", StringComparison.OrdinalIgnoreCase) ||
                       ContainsAny(keys, "weapon", "weapons");

        return slot switch
        {
            FarmingGuideEquipmentSlot.Headset =>
                propertyType.Equals("ItemPropertiesHeadphone", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "headphone", "headphones", "headset", "earpiece"),
            FarmingGuideEquipmentSlot.Helmet =>
                propertyType.Equals("ItemPropertiesHelmet", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "helmet", "helmets"),
            FarmingGuideEquipmentSlot.FaceCover => ContainsAny(keys, "facecover", "facecovers", "mask"),
            FarmingGuideEquipmentSlot.Armband => ContainsAny(keys, "armband", "armbands"),
            FarmingGuideEquipmentSlot.BodyArmor =>
                propertyType.Equals("ItemPropertiesArmor", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "bodyarmor", "bodyarmors", "armorvest", "armorvests"),
            FarmingGuideEquipmentSlot.Eyewear =>
                propertyType.Equals("ItemPropertiesGlasses", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "eyewear", "glasses"),
            FarmingGuideEquipmentSlot.PrimaryWeapon1 or FarmingGuideEquipmentSlot.PrimaryWeapon2 =>
                isWeapon && !isPistol,
            FarmingGuideEquipmentSlot.Holster => isPistol,
            FarmingGuideEquipmentSlot.Melee => ContainsAny(keys, "melee", "meleeweapon", "knife", "knives"),
            FarmingGuideEquipmentSlot.Dogtag => ContainsAny(keys, "dogtag", "dogtags"),
            _ => false,
        };
    }

    public static bool IsStorageCarrierCompatible(FarmingGuideStorageKind storage, GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var propertyType = item.FarmingGuideData?.PropertiesType ?? string.Empty;
        var keys = item.Types
            .Concat(item.Categories)
            .Select(Normalize)
            .Where(static value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

        return storage switch
        {
            FarmingGuideStorageKind.Rig =>
                propertyType.Equals("ItemPropertiesChestRig", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "rig", "rigs", "chestRig", "chestRigs", "tacticalRig", "tacticalRigs"),
            FarmingGuideStorageKind.Backpack =>
                propertyType.Equals("ItemPropertiesBackpack", StringComparison.OrdinalIgnoreCase) ||
                ContainsAny(keys, "backpack", "backpacks"),
            FarmingGuideStorageKind.SecureContainer =>
                (propertyType.Equals("ItemPropertiesContainer", StringComparison.OrdinalIgnoreCase) ||
                 ContainsAny(keys, "container", "containers")) &&
                ContainsAny(keys, "securecontainer", "securecontainers", "securedcontainer", "securedcontainers", "pouch", "pouches"),
            _ => false,
        };
    }

    public static bool FilterAllows(GameItem item, FarmingGuideItemFilter filter)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.ExcludedItemIds.Contains(item.Id, StringComparer.Ordinal) ||
            item.CategoryIds.Any(id => filter.ExcludedCategoryIds.Contains(id, StringComparer.Ordinal)))
        {
            return false;
        }

        if (filter.AllowedItemIds.Count == 0 && filter.AllowedCategoryIds.Count == 0)
            return true;

        return filter.AllowedItemIds.Contains(item.Id, StringComparer.Ordinal) ||
               item.CategoryIds.Any(id => filter.AllowedCategoryIds.Contains(id, StringComparer.Ordinal));
    }

    public static bool ItemsConflict(GameItem left, GameItem right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.FarmingGuideData?.ConflictingItemIds.Contains(right.Id, StringComparer.Ordinal) == true ||
               right.FarmingGuideData?.ConflictingItemIds.Contains(left.Id, StringComparer.Ordinal) == true;
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool ContainsAny(HashSet<string> keys, params string[] candidates) =>
        candidates.Select(Normalize).Any(candidate => keys.Contains(candidate));
}
