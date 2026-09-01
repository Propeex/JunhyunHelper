using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideCompleteEquipmentPolicyTests
{
    [Fact]
    public void RuntimeProjectionRemovesEquipmentInternalsAndUsesAuthoritativeDefaultPresetImage()
    {
        var modFilter = new FarmingGuideItemFilter([], ["mod"], [], []);
        var baseWeapon = Item("weapon", "ItemPropertiesWeapon", "receiver.png") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [new FarmingGuideAttachmentSlotDefinition("scope", "mod_scope", "Scope", false, modFilter)],
                [new FarmingGuideArmorSlotDefinition("plate", "front_plate", "Plate", false, ["plate-item"])],
                [],
                [],
                false,
                false),
            FarmingGuideAssembly = new FarmingGuideAssemblySource(null, null, "preset", []),
        };
        var preset = Item("preset", "ItemPropertiesWeapon", "preset-icon.png") with
        {
            FarmingGuideAssembly = new FarmingGuideAssemblySource(
                "preset-grid.png",
                "preset-512.png",
                null,
                ["weapon", "mod"]),
        };
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [baseWeapon.Id] = baseWeapon,
            [preset.Id] = preset,
        };

        var runtime = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(baseWeapon, catalog);

        Assert.NotNull(runtime.FarmingGuideData);
        Assert.Empty(runtime.FarmingGuideData!.AttachmentSlots);
        Assert.Empty(runtime.FarmingGuideData.ArmorSlots);
        Assert.Equal("preset-512.png", runtime.IconUrl);
    }

    [Fact]
    public void RuntimeProjectionKeepsOnlySupportedCarrierStorageSurfaces()
    {
        var grids = new[] { new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty) };
        var backpack = Item("backpack", "ItemPropertiesBackpack", "bag.png") with
        {
            FarmingGuideData = Layout("ItemPropertiesBackpack", grids),
        };
        var rig = Item("rig", "ItemPropertiesChestRig", "rig.png") with
        {
            FarmingGuideData = Layout("ItemPropertiesChestRig", grids),
        };
        var genericCase = new GameItem(
            "case",
            "Case",
            "Case",
            "Case",
            "Case",
            "case.png",
            null,
            [],
            ["container"],
            ["container"],
            2,
            2) with
        {
            FarmingGuideData = Layout("ItemPropertiesContainer", grids),
        };
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [backpack.Id] = backpack,
            [rig.Id] = rig,
            [genericCase.Id] = genericCase,
        };

        var runtimeBackpack = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(backpack, catalog);
        var runtimeRig = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(rig, catalog);
        var runtimeCase = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(genericCase, catalog);

        Assert.Single(runtimeBackpack.FarmingGuideData!.StorageGrids);
        Assert.Single(runtimeRig.FarmingGuideData!.StorageGrids);
        Assert.Empty(runtimeCase.FarmingGuideData!.StorageGrids);
        Assert.True(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeBackpack));
        Assert.True(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeRig));
        Assert.False(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeCase));
    }

    [Fact]
    public void RuntimeCatalogCausesLegacyAssemblyStateToSanitizeToRootOnlyEquipment()
    {
        var modFilter = new FarmingGuideItemFilter([], ["mod"], [], []);
        var weapon = Item("weapon", "ItemPropertiesWeapon", "weapon.png") with
        {
            FarmingGuideData = new FarmingGuideItemLayout(
                "ItemPropertiesWeapon",
                [],
                [new FarmingGuideAttachmentSlotDefinition("scope", "mod_scope", "Scope", false, modFilter)],
                [new FarmingGuideArmorSlotDefinition("plate", "front_plate", "Plate", false, ["plate"])],
                [],
                [],
                false,
                false),
        };
        var mod = Item("mod", null, "mod.png");
        var plate = Item("plate", null, "plate.png");
        var source = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [weapon.Id] = weapon,
            [mod.Id] = mod,
            [plate.Id] = plate,
        };
        var runtime = source.Values
            .Select(item => FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(item, source))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        var legacyState = new FarmingGuideItemState(
            weapon.Id,
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["scope"] = FarmingGuideItemState.Create(mod.Id),
            },
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate"] = FarmingGuideItemState.Create(plate.Id),
            });
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>
            {
                [FarmingGuideEquipmentSlot.PrimaryWeapon1] = legacyState,
            },
            null,
            null,
            null,
            []);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, runtime);

        var equipped = Assert.Single(sanitized.Equipment).Value;
        Assert.Equal(weapon.Id, equipped.ItemId);
        Assert.Empty(equipped.Attachments);
        Assert.Empty(equipped.ArmorPlates);
    }

    [Fact]
    public void NormalizeStateDropsAllLegacyInternalComposition()
    {
        var legacy = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["scope"] = FarmingGuideItemState.Create("scope"),
            },
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate"] = FarmingGuideItemState.Create("plate"),
            });

        var normalized = FarmingGuideCompleteEquipmentPolicy.NormalizeState(legacy);

        Assert.Equal("weapon", normalized.ItemId);
        Assert.Empty(normalized.Attachments);
        Assert.Empty(normalized.ArmorPlates);
    }

    private static GameItem Item(string id, string? propertiesType, string? iconUrl) =>
        new(
            id,
            id,
            id,
            id,
            id,
            iconUrl,
            null,
            [],
            [],
            propertiesType is null ? [] : ["weapon"],
            1,
            1) with
        {
            FarmingGuideData = propertiesType is null ? null : Layout(propertiesType, []),
        };

    private static FarmingGuideItemLayout Layout(
        string propertiesType,
        IReadOnlyList<FarmingGuideStorageGridDefinition> grids) =>
        new(
            propertiesType,
            grids,
            [],
            [],
            [],
            [],
            false,
            false);
}
