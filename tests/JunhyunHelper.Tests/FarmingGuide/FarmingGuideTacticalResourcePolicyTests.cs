using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideTacticalResourcePolicyTests
{
    [Fact]
    public void ProvisionFacts_ClassifyFoodAndDrinkWithoutNameGuessing()
    {
        var both = Item("opaque-a", Layout("ItemPropertiesFoodDrink") with
        {
            Energy = 15,
            Hydration = 25,
        });
        var foodOnly = Item("opaque-b", Layout("ItemPropertiesFoodDrink") with
        {
            Energy = 20,
            Hydration = -10,
        });
        var drinkOnly = Item("opaque-c", Layout("ItemPropertiesFoodDrink") with
        {
            Energy = 0,
            Hydration = 60,
        });

        Assert.True(FarmingGuideTacticalResourcePolicy.ProvidesFood(both));
        Assert.True(FarmingGuideTacticalResourcePolicy.ProvidesDrink(both));
        Assert.True(FarmingGuideTacticalResourcePolicy.ProvidesFood(foodOnly));
        Assert.False(FarmingGuideTacticalResourcePolicy.ProvidesDrink(foodOnly));
        Assert.False(FarmingGuideTacticalResourcePolicy.ProvidesFood(drinkOnly));
        Assert.True(FarmingGuideTacticalResourcePolicy.ProvidesDrink(drinkOnly));
    }

    [Fact]
    public void WeaponAllowedAmmo_IsAuthoritativeOverCaliberFallback()
    {
        var accepted = Item("ammo-accepted", Layout("ItemPropertiesAmmo") with
        {
            AmmoCaliber = "caliber-a",
        });
        var sameCaliberButRejected = Item("ammo-other", Layout("ItemPropertiesAmmo") with
        {
            AmmoCaliber = "caliber-a",
        });
        var weapon = Item("weapon", Layout("ItemPropertiesWeapon") with
        {
            WeaponCaliber = "caliber-a",
            AllowedAmmoItemIds = ["ammo-accepted"],
        });

        Assert.True(FarmingGuideTacticalResourcePolicy.IsAmmoForWeapon(accepted, weapon));
        Assert.False(FarmingGuideTacticalResourcePolicy.IsAmmoForWeapon(sameCaliberButRejected, weapon));
    }

    [Fact]
    public void WeaponCaliber_IsSafeFallbackWhenAllowedAmmoListIsUnavailable()
    {
        var compatible = Item("ammo-a", Layout("ItemPropertiesAmmo") with
        {
            AmmoCaliber = "caliber-a",
        });
        var incompatible = Item("ammo-b", Layout("ItemPropertiesAmmo") with
        {
            AmmoCaliber = "caliber-b",
        });
        var weapon = Item("weapon", Layout("ItemPropertiesWeapon") with
        {
            WeaponCaliber = "caliber-a",
        });

        Assert.True(FarmingGuideTacticalResourcePolicy.IsAmmoForWeapon(compatible, weapon));
        Assert.False(FarmingGuideTacticalResourcePolicy.IsAmmoForWeapon(incompatible, weapon));
    }

    private static GameItem Item(string id, FarmingGuideItemLayout layout) =>
        new(id, id, id, id, id, null, null, [], [], [], 1, 1)
        {
            FarmingGuideData = layout,
        };

    private static FarmingGuideItemLayout Layout(string propertiesType) =>
        new(
            propertiesType,
            [],
            [],
            [],
            [],
            [],
            false,
            false);
}
