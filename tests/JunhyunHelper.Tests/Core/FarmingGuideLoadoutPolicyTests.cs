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

    private static FarmingGuideStoredItemState Stored(
        string instanceId,
        string itemId,
        FarmingGuideStorageKind storage,
        int gridIndex,
        int x,
        int y,
        bool rotated = false) =>
        new(instanceId, FarmingGuideItemState.Create(itemId), storage, gridIndex, x, y, rotated);

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
