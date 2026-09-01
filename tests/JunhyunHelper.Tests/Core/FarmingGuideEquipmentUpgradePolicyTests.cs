using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideEquipmentUpgradePolicyTests
{
    [Fact]
    public void ProtectiveUpgradeRequiresStrictlyHigherSourceArmorClass()
    {
        var class4 = Item("class4", "ItemPropertiesArmor", armorClass: 4);
        var class5 = Item("class5", "ItemPropertiesArmor", armorClass: 5);
        var unknown = Item("unknown", "ItemPropertiesArmor", armorClass: null);

        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsProtectiveUpgrade(class5, class4));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsProtectiveUpgrade(class4, class5));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsProtectiveUpgrade(class4, class4));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsProtectiveUpgrade(unknown, class4));
    }

    [Fact]
    public void HeadsetUpgradeRequiresParetoImprovementWithoutAudioRegression()
    {
        var current = Item(
            "current-headset",
            "ItemPropertiesHeadphone",
            headsetDistance: 1.10m,
            headsetDistortion: 0.20m);
        var dominant = Item(
            "dominant-headset",
            "ItemPropertiesHeadphone",
            headsetDistance: 1.20m,
            headsetDistortion: 0.18m);
        var distanceTradeoff = Item(
            "distance-tradeoff",
            "ItemPropertiesHeadphone",
            headsetDistance: 1.25m,
            headsetDistortion: 0.25m);
        var distortionTradeoff = Item(
            "distortion-tradeoff",
            "ItemPropertiesHeadphone",
            headsetDistance: 1.00m,
            headsetDistortion: 0.10m);
        var unknown = Item("unknown-headset", "ItemPropertiesHeadphone");

        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsHeadsetUpgrade(dominant, current));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsHeadsetUpgrade(distanceTradeoff, current));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsHeadsetUpgrade(distortionTradeoff, current));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsHeadsetUpgrade(current, current));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsHeadsetUpgrade(unknown, current));
    }

    [Fact]
    public void BackpackUpgradeRequiresStrictCapacityIncrease()
    {
        var small = Item("small", "ItemPropertiesBackpack", grids: [(2, 2)]);
        var large = Item("large", "ItemPropertiesBackpack", grids: [(2, 3)]);

        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Backpack,
            large,
            small));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Backpack,
            small,
            large));
    }

    [Fact]
    public void OrdinaryRigUpgradeRequiresStrictCapacityIncrease()
    {
        var small = Item("small-rig", "ItemPropertiesChestRig", grids: [(2, 2)]);
        var large = Item("large-rig", "ItemPropertiesChestRig", grids: [(3, 2)]);

        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            large,
            small));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            small,
            large));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            small,
            small));
    }

    [Fact]
    public void ArmoredRigUpgradeRejectsObjectiveRegression()
    {
        var current = Item("current", "ItemPropertiesChestRig", armorClass: 4, armoredRig: true, grids: [(2, 3)]);
        var higherArmorSmaller = Item("tradeoff", "ItemPropertiesChestRig", armorClass: 5, armoredRig: true, grids: [(2, 2)]);
        var dominant = Item("dominant", "ItemPropertiesChestRig", armorClass: 5, armoredRig: true, grids: [(2, 3)]);
        var ordinary = Item("ordinary", "ItemPropertiesChestRig", armorClass: null, armoredRig: false, grids: [(3, 3)]);

        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            higherArmorSmaller,
            current));
        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            dominant,
            current));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsCarrierUpgrade(
            FarmingGuideStorageKind.Rig,
            ordinary,
            current));
    }

    [Fact]
    public void BodyArmorToArmoredRigUsesStrictProtectionUpgrade()
    {
        var armor4 = Item("armor4", "ItemPropertiesArmor", armorClass: 4);
        var rig4 = Item("rig4", "ItemPropertiesChestRig", armorClass: 4, armoredRig: true, grids: [(2, 2)]);
        var rig5 = Item("rig5", "ItemPropertiesChestRig", armorClass: 5, armoredRig: true, grids: [(2, 2)]);
        var ordinary = Item("ordinary", "ItemPropertiesChestRig", armorClass: null, armoredRig: false, grids: [(2, 2)]);

        Assert.True(FarmingGuideEquipmentUpgradePolicy.IsBodyArmorToArmoredRigUpgrade(rig5, armor4));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsBodyArmorToArmoredRigUpgrade(rig4, armor4));
        Assert.False(FarmingGuideEquipmentUpgradePolicy.IsBodyArmorToArmoredRigUpgrade(ordinary, armor4));
    }

    private static GameItem Item(
        string id,
        string propertiesType,
        int? armorClass = null,
        bool armoredRig = false,
        IReadOnlyList<(int Width, int Height)>? grids = null,
        decimal? headsetDistance = null,
        decimal? headsetDistortion = null)
    {
        var item = new GameItem(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            [],
            1,
            1);
        return item with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                propertiesType,
                (grids ?? []).Select(value => new FarmingGuideStorageGridDefinition(
                    value.Width,
                    value.Height,
                    FarmingGuideItemFilter.Empty)).ToArray(),
                [],
                [],
                [],
                [],
                false,
                armoredRig)
            {
                ArmorClass = armorClass,
                HeadsetDistanceModifier = headsetDistance,
                HeadsetDistortion = headsetDistortion,
            },
        };
    }
}
