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
    public void RuntimeProjectionKeepsEverySourceBackedStorageSurface()
    {
        var unrestrictedGrid = new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty);
        var specializedFilter = new FarmingGuideItemFilter(["keys-category"], [], [], []);
        var specializedGrid = new FarmingGuideStorageGridDefinition(1, 4, specializedFilter);
        var backpack = Item("backpack", "ItemPropertiesBackpack", "bag.png") with
        {
            FarmingGuideData = Layout("ItemPropertiesBackpack", [unrestrictedGrid]),
        };
        var rig = Item("rig", "ItemPropertiesChestRig", "rig.png") with
        {
            FarmingGuideData = Layout("ItemPropertiesChestRig", [unrestrictedGrid]),
        };
        var specializedCase = new GameItem(
            "key-tool",
            "Key tool",
            "Key tool",
            "Key tool",
            "Key tool",
            "key-tool.png",
            null,
            [],
            ["container"],
            ["container"],
            1,
            1) with
        {
            FarmingGuideData = Layout("ItemPropertiesContainer", [specializedGrid]),
        };
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [backpack.Id] = backpack,
            [rig.Id] = rig,
            [specializedCase.Id] = specializedCase,
        };

        var runtimeBackpack = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(backpack, catalog);
        var runtimeRig = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(rig, catalog);
        var runtimeCase = FarmingGuideCompleteEquipmentPolicy.ToRuntimeItem(specializedCase, catalog);

        Assert.Single(runtimeBackpack.FarmingGuideData!.StorageGrids);
        Assert.Single(runtimeRig.FarmingGuideData!.StorageGrids);
        var caseGrid = Assert.Single(runtimeCase.FarmingGuideData!.StorageGrids);
        Assert.Equal(1, caseGrid.Width);
        Assert.Equal(4, caseGrid.Height);
        Assert.Same(specializedFilter, caseGrid.Filters);
        Assert.True(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeBackpack));
        Assert.True(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeRig));
        Assert.True(FarmingGuideCompleteEquipmentPolicy.SupportsNestedStorage(runtimeCase));
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
        new GameItem(
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
