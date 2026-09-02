using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Source-backed tactical facts used by Farming Guide safety validation. These are hard
/// retention constraints, not economic scores: automatic advice must not consume the last
/// modeled food/drink reserve or reduce loose ammunition usable by the currently carried
/// primary weapon(s). The policy never guesses from localized item names.
/// </summary>
public static class FarmingGuideTacticalResourcePolicy
{
    public static bool ProvidesFood(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var layout = item.FarmingGuideData;
        return string.Equals(
                   layout?.PropertiesType,
                   "ItemPropertiesFoodDrink",
                   StringComparison.OrdinalIgnoreCase) &&
               layout?.Energy is > 0;
    }

    public static bool ProvidesDrink(GameItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var layout = item.FarmingGuideData;
        return string.Equals(
                   layout?.PropertiesType,
                   "ItemPropertiesFoodDrink",
                   StringComparison.OrdinalIgnoreCase) &&
               layout?.Hydration is > 0;
    }

    public static bool IsAmmoForWeapon(GameItem ammo, GameItem weapon)
    {
        ArgumentNullException.ThrowIfNull(ammo);
        ArgumentNullException.ThrowIfNull(weapon);

        var ammoLayout = ammo.FarmingGuideData;
        var weaponLayout = weapon.FarmingGuideData;
        if (!string.Equals(
                ammoLayout?.PropertiesType,
                "ItemPropertiesAmmo",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                weaponLayout?.PropertiesType,
                "ItemPropertiesWeapon",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var allowed = weaponLayout?.AllowedAmmoItemIds ?? Array.Empty<string>();
        if (allowed.Count > 0)
            return allowed.Contains(ammo.Id, StringComparer.Ordinal);

        return !string.IsNullOrWhiteSpace(ammoLayout?.AmmoCaliber) &&
               !string.IsNullOrWhiteSpace(weaponLayout?.WeaponCaliber) &&
               string.Equals(
                   ammoLayout.AmmoCaliber,
                   weaponLayout.WeaponCaliber,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAmmoForAnyWeapon(
        GameItem ammo,
        IEnumerable<GameItem> weapons)
    {
        ArgumentNullException.ThrowIfNull(ammo);
        ArgumentNullException.ThrowIfNull(weapons);
        return weapons.Any(weapon => IsAmmoForWeapon(ammo, weapon));
    }
}
