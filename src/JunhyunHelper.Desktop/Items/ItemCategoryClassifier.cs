using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.Items;

internal enum ItemDisplayCategory
{
    Weapons,
    WeaponParts,
    Gear,
    Ammo,
    Medical,
    Provisions,
    Barter,
    Keys,
    Info,
    Special,
    Quest,
    Money,
    Maps,
    Other,
}

internal static class ItemCategoryClassifier
{
    public static ItemDisplayCategory Classify(GameItem? item)
    {
        if (item is null)
            return ItemDisplayCategory.Other;

        var keys = item.Categories
            .Select(Normalize)
            .Where(static key => key.Length > 0)
            .ToArray();

        // More specific groups must win over broad ancestors such as Gear or Weapons.
        if (Has(keys, "weapon-mod", "weapon-parts", "mod", "mods"))
            return ItemDisplayCategory.WeaponParts;
        if (Has(keys, "ammo", "ammunition", "round", "rounds", "ammo-pack", "ammo-packs"))
            return ItemDisplayCategory.Ammo;
        if (Has(keys, "meds", "medicine", "medical-treatment", "injector", "stimulator"))
            return ItemDisplayCategory.Medical;
        if (Has(keys, "food-and-drink", "food", "drink", "provisions"))
            return ItemDisplayCategory.Provisions;
        if (Has(keys, "key", "keys", "keycard"))
            return ItemDisplayCategory.Keys;
        if (Has(keys, "info", "info-item", "info-items"))
            return ItemDisplayCategory.Info;
        if (Has(keys, "special-item", "special-items", "special-equipment"))
            return ItemDisplayCategory.Special;
        if (Has(keys, "quest-item", "quest-items"))
            return ItemDisplayCategory.Quest;
        if (Has(keys, "money", "currency"))
            return ItemDisplayCategory.Money;
        if (Has(keys, "map", "maps"))
            return ItemDisplayCategory.Maps;
        if (Has(keys, "barter-item", "barter-items", "barter"))
            return ItemDisplayCategory.Barter;
        if (Has(keys, "weapon", "weapons", "throwable-weapon"))
            return ItemDisplayCategory.Weapons;
        if (Has(keys, "gear", "equipment", "armor", "armored-equipment", "backpack", "rig", "helmet"))
            return ItemDisplayCategory.Gear;

        return ItemDisplayCategory.Other;
    }

    public static string Label(ItemDisplayCategory category) => category switch
    {
        ItemDisplayCategory.Weapons => "무기",
        ItemDisplayCategory.WeaponParts => "무기 부품",
        ItemDisplayCategory.Gear => "장비",
        ItemDisplayCategory.Ammo => "탄약",
        ItemDisplayCategory.Medical => "의약품",
        ItemDisplayCategory.Provisions => "식량/음료",
        ItemDisplayCategory.Barter => "물물교환",
        ItemDisplayCategory.Keys => "열쇠",
        ItemDisplayCategory.Info => "정보",
        ItemDisplayCategory.Special => "특수 장비",
        ItemDisplayCategory.Quest => "퀘스트 아이템",
        ItemDisplayCategory.Money => "화폐",
        ItemDisplayCategory.Maps => "지도",
        _ => "기타",
    };

    public static int Order(ItemDisplayCategory category) => category switch
    {
        ItemDisplayCategory.Weapons => 0,
        ItemDisplayCategory.WeaponParts => 1,
        ItemDisplayCategory.Gear => 2,
        ItemDisplayCategory.Ammo => 3,
        ItemDisplayCategory.Medical => 4,
        ItemDisplayCategory.Provisions => 5,
        ItemDisplayCategory.Barter => 6,
        ItemDisplayCategory.Keys => 7,
        ItemDisplayCategory.Info => 8,
        ItemDisplayCategory.Special => 9,
        ItemDisplayCategory.Quest => 10,
        ItemDisplayCategory.Money => 11,
        ItemDisplayCategory.Maps => 12,
        _ => 99,
    };

    private static bool Has(IEnumerable<string> keys, params string[] candidates) =>
        keys.Any(key => candidates.Any(candidate =>
            key.Equals(candidate, StringComparison.Ordinal) ||
            key.Contains(candidate, StringComparison.Ordinal)));

    private static string Normalize(string value) =>
        value.Trim()
            .ToLowerInvariant()
            .Replace('_', '-')
            .Replace(' ', '-');
}
