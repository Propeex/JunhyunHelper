using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideSearchPolicyTests
{
    [Fact]
    public void WeaponPresetIsNotDraggableInventoryItem()
    {
        var preset = Item("preset") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesPreset",
                [],
                [],
                [],
                [],
                [],
                false,
                false),
        };

        Assert.False(FarmingGuideSearchPolicy.IsDraggableInventoryItem(preset));
    }

    [Fact]
    public void PresetTypeFallbackIsNotDraggableInventoryItem()
    {
        Assert.False(FarmingGuideSearchPolicy.IsDraggableInventoryItem(Item("preset", ["preset"])));
    }

    [Fact]
    public void BaseWeaponRemainsDraggableInventoryItem()
    {
        var weapon = Item("weapon", ["gun"]) with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [new FarmingGuideAttachmentSlotDefinition(
                    "mod_magazine",
                    "mod_magazine",
                    "Magazine",
                    true,
                    FarmingGuideItemFilter.Empty)],
                [],
                [],
                [],
                false,
                false),
        };

        Assert.True(FarmingGuideSearchPolicy.IsDraggableInventoryItem(weapon));
    }

    private static GameItem Item(string id, IReadOnlyList<string>? types = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            types ?? [],
            1,
            1);
}
