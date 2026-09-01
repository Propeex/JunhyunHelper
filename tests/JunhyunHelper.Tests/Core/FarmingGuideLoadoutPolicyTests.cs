using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideLoadoutPolicyTests
{
    [Fact]
    public void PopulatedCarrierCannotBeReplacedByAnotherCarrier()
    {
        Assert.False(FarmingGuideLoadoutPolicy.CanReplaceCarrier(
            movingSameCarrier: false,
            targetContainsItems: true));
        Assert.True(FarmingGuideLoadoutPolicy.CanReplaceCarrier(
            movingSameCarrier: true,
            targetContainsItems: true));
        Assert.True(FarmingGuideLoadoutPolicy.CanReplaceCarrier(
            movingSameCarrier: false,
            targetContainsItems: false));
    }

    [Fact]
    public void SanitizeSnapshotDropsStaleOutOfBoundsOverlapAndMissingGridPlacements()
    {
        var backpack = Item(
            "backpack",
            "ItemPropertiesBackpack",
            [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)]);
        var small = Item("small", null, [], width: 1, height: 1);
        var wide = Item("wide", null, [], width: 2, height: 1);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [backpack.Id] = backpack,
            [small.Id] = small,
            [wide.Id] = wide,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create(backpack.Id),
            null,
            [
                Stored("valid", small.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0),
                Stored("overlap", small.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0),
                Stored("out-of-bounds", wide.Id, FarmingGuideStorageKind.Backpack, 0, 1, 1),
                Stored("missing-grid", small.Id, FarmingGuideStorageKind.Backpack, 1, 0, 0),
                Stored("missing-item", "gone", FarmingGuideStorageKind.Backpack, 0, 1, 1),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        var kept = Assert.Single(sanitized.StoredItems);
        Assert.Equal("valid", kept.InstanceId);
    }

    [Fact]
    public void SanitizeSnapshotDropsPlacementRejectedByCurrentGridFilter()
    {
        var allowedCategory = "allowed-category";
        var backpack = Item(
            "backpack",
            "ItemPropertiesBackpack",
            [
                new FarmingGuideStorageGridDefinition(
                    2,
                    1,
                    new FarmingGuideItemFilter([allowedCategory], [], [], [])),
            ]);
        var disallowed = Item("disallowed", null, [], width: 1, height: 1);
        var allowed = Item(
            "allowed",
            null,
            [],
            width: 1,
            height: 1,
            categoryIds: [allowedCategory]);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [backpack.Id] = backpack,
            [disallowed.Id] = disallowed,
            [allowed.Id] = allowed,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create(backpack.Id),
            null,
            [
                Stored("denied", disallowed.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0),
                Stored("allowed", allowed.Id, FarmingGuideStorageKind.Backpack, 0, 1, 0),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        var kept = Assert.Single(sanitized.StoredItems);
        Assert.Equal("allowed", kept.InstanceId);
    }

    [Fact]
    public void SanitizeSnapshotUsesResolvedExpandedPocketGeometry()
    {
        var tall = Item("tall", null, [], width: 1, height: 2);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [tall.Id] = tall,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            null,
            null,
            [Stored("pocket-item", tall.Id, FarmingGuideStorageKind.Pockets, 1, 0, 0)]);

        var standard = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);
        var expanded = FarmingGuideLoadoutPolicy.SanitizeSnapshot(
            snapshot,
            catalog,
            FarmingGuidePocketLayoutPolicy.ExpandedGrids);

        Assert.Empty(standard.StoredItems);
        var kept = Assert.Single(expanded.StoredItems);
        Assert.Equal("pocket-item", kept.InstanceId);
        Assert.Equal(1, kept.GridIndex);
    }

    [Fact]
    public void SanitizeSnapshotPreservesNestedContainerTree()
    {
        var outer = Item(
            "outer",
            "ItemPropertiesBackpack",
            [new FarmingGuideStorageGridDefinition(3, 3, FarmingGuideItemFilter.Empty)],
            width: 2,
            height: 2);
        var inner = Item(
            "inner",
            "ItemPropertiesBackpack",
            [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)],
            width: 2,
            height: 2);
        var loot = Item("loot", null, [], width: 1, height: 1);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [outer.Id] = outer,
            [inner.Id] = inner,
            [loot.Id] = loot,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create(outer.Id),
            null,
            [
                Stored("inner-instance", inner.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0),
                Stored("loot-instance", loot.Id, FarmingGuideStorageKind.Backpack, 0, 1, 1, parentInstanceId: "inner-instance"),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        Assert.Equal(2, sanitized.StoredItems.Count);
        var nested = Assert.Single(sanitized.StoredItems, item => item.ParentInstanceId is not null);
        Assert.Equal("inner-instance", nested.ParentInstanceId);
        Assert.Equal("loot-instance", nested.InstanceId);
    }

    [Fact]
    public void SanitizeSnapshotAppliesSpecializedFilterInsideGenericNestedContainer()
    {
        const string keyCategory = "key-category";
        var secure = Item(
            "secure",
            "ItemPropertiesContainer",
            [new FarmingGuideStorageGridDefinition(3, 3, FarmingGuideItemFilter.Empty)],
            width: 2,
            height: 2);
        var keyTool = Item(
            "key-tool",
            "ItemPropertiesContainer",
            [
                new FarmingGuideStorageGridDefinition(
                    1,
                    4,
                    new FarmingGuideItemFilter([keyCategory], [], [], [])),
            ],
            width: 1,
            height: 1);
        var key = Item("key", null, [], categoryIds: [keyCategory]);
        var unrelated = Item("unrelated", null, []);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [secure.Id] = secure,
            [keyTool.Id] = keyTool,
            [key.Id] = key,
            [unrelated.Id] = unrelated,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            null,
            FarmingGuideItemState.Create(secure.Id),
            [
                Stored("key-tool-instance", keyTool.Id, FarmingGuideStorageKind.SecureContainer, 0, 0, 0),
                Stored("key-instance", key.Id, FarmingGuideStorageKind.SecureContainer, 0, 0, 0, parentInstanceId: "key-tool-instance"),
                Stored("denied-instance", unrelated.Id, FarmingGuideStorageKind.SecureContainer, 0, 0, 1, parentInstanceId: "key-tool-instance"),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        Assert.Equal(2, sanitized.StoredItems.Count);
        Assert.Contains(sanitized.StoredItems, item => item.InstanceId == "key-tool-instance");
        Assert.Contains(sanitized.StoredItems, item => item.InstanceId == "key-instance");
        Assert.DoesNotContain(sanitized.StoredItems, item => item.InstanceId == "denied-instance");
    }

    [Fact]
    public void SanitizeSnapshotDropsOrphanAndCyclicNestedPlacements()
    {
        var bag = Item(
            "bag",
            "ItemPropertiesBackpack",
            [new FarmingGuideStorageGridDefinition(2, 2, FarmingGuideItemFilter.Empty)]);
        var catalog = new Dictionary<string, GameItem>(StringComparer.Ordinal)
        {
            [bag.Id] = bag,
        };
        var snapshot = new FarmingGuideLoadoutSnapshot(
            new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
            null,
            FarmingGuideItemState.Create(bag.Id),
            null,
            [
                Stored("orphan", bag.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0, parentInstanceId: "missing"),
                Stored("cycle-a", bag.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0, parentInstanceId: "cycle-b"),
                Stored("cycle-b", bag.Id, FarmingGuideStorageKind.Backpack, 0, 0, 0, parentInstanceId: "cycle-a"),
            ]);

        var sanitized = FarmingGuideLoadoutPolicy.SanitizeSnapshot(snapshot, catalog);

        Assert.Empty(sanitized.StoredItems);
    }

    private static FarmingGuideStoredItemState Stored(
        string instanceId,
        string itemId,
        FarmingGuideStorageKind storage,
        int gridIndex,
        int x,
        int y,
        bool rotated = false,
        string? parentInstanceId = null) =>
        new(instanceId, FarmingGuideItemState.Create(itemId), storage, gridIndex, x, y, rotated, parentInstanceId);

    private static GameItem Item(
        string id,
        string? propertiesType,
        IReadOnlyList<FarmingGuideStorageGridDefinition> grids,
        int width = 1,
        int height = 1,
        IReadOnlyList<string>? categoryIds = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            categoryIds ?? Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            width,
            height)
        {
            FarmingGuideData = string.IsNullOrWhiteSpace(propertiesType) && grids.Count == 0
                ? null
                : new FarmingGuideItemLayout(
                    propertiesType,
                    grids,
                    [],
                    [],
                    [],
                    [],
                    false,
                    false),
        };
}
