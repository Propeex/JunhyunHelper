using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideCompatibilityTests
{
    [Fact]
    public void PistolUsesHolsterInsteadOfPrimaryWeaponSlots()
    {
        var pistol = Item("pistol", categories: ["Pistols"], types: ["weapon"]);

        Assert.True(FarmingGuideCompatibility.IsEquipmentSlotCompatible(FarmingGuideEquipmentSlot.Holster, pistol));
        Assert.False(FarmingGuideCompatibility.IsEquipmentSlotCompatible(FarmingGuideEquipmentSlot.PrimaryWeapon1, pistol));
        Assert.False(FarmingGuideCompatibility.IsEquipmentSlotCompatible(FarmingGuideEquipmentSlot.PrimaryWeapon2, pistol));
    }

    [Theory]
    [InlineData(FarmingGuideStorageKind.Rig, "Tactical rigs")]
    [InlineData(FarmingGuideStorageKind.Backpack, "Backpacks")]
    [InlineData(FarmingGuideStorageKind.SecureContainer, "Secure containers")]
    public void StorageCarrierAcceptsCanonicalCategoryFallback(
        FarmingGuideStorageKind kind,
        string category)
    {
        var item = Item(kind.ToString(), categories: [category]);

        Assert.True(FarmingGuideCompatibility.IsStorageCarrierCompatible(kind, item));
    }

    [Fact]
    public void BodyArmorAcceptsCanonicalBodyArmorCategory()
    {
        var armor = Item("armor", categories: ["Body armor"]);

        Assert.True(FarmingGuideCompatibility.IsEquipmentSlotCompatible(FarmingGuideEquipmentSlot.BodyArmor, armor));
    }

    private static GameItem Item(
        string id,
        IReadOnlyList<string>? categories = null,
        IReadOnlyList<string>? types = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            categories ?? [],
            types ?? [],
            1,
            1);
}
