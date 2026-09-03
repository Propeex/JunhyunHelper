using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideV1170LegalityTests
{
    [Fact]
    public void HeadwearPropertyUsesTheHeadEquipmentSlot()
    {
        var headwear = Item("cap") with
        {
            FarmingGuideData = Layout("ItemPropertiesHeadwear"),
        };

        Assert.True(FarmingGuideCompatibility.IsEquipmentSlotCompatible(
            FarmingGuideEquipmentSlot.Helmet,
            headwear));
    }

    [Fact]
    public void OptimizationScoreUsesStackQuantityForFirAndValue()
    {
        var stored = new FarmingGuideStoredItemState(
            "stack-instance",
            FarmingGuideItemState.Create("stack-item", raidAcquired: true),
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false)
        {
            Quantity = 30,
        };
        var snapshot = FarmingGuideLoadoutSnapshot.Empty with
        {
            StoredItems = [stored],
        };

        var score = FarmingGuideOptimizationPolicy.Score(
            snapshot,
            itemId => string.Equals(itemId, "stack-item", StringComparison.Ordinal) ? 20 : 0,
            itemId => string.Equals(itemId, "stack-item", StringComparison.Ordinal) ? 100 : 0);

        Assert.Equal(20, score.SatisfiedFirUnits);
        Assert.Equal(3_000, score.RetainedFleaValue);
    }

    [Fact]
    public void NonRaidAcquiredStackCountsValueButNotFirProgress()
    {
        var stored = new FarmingGuideStoredItemState(
            "baseline-stack-instance",
            FarmingGuideItemState.Create("stack-item"),
            FarmingGuideStorageKind.Pockets,
            0,
            0,
            0,
            false)
        {
            Quantity = 30,
        };
        var snapshot = FarmingGuideLoadoutSnapshot.Empty with
        {
            StoredItems = [stored],
        };

        var score = FarmingGuideOptimizationPolicy.Score(
            snapshot,
            _ => 20,
            _ => 100);

        Assert.Equal(0, score.SatisfiedFirUnits);
        Assert.Equal(3_000, score.RetainedFleaValue);
    }

    [Fact]
    public void AssemblyCandidateIsRejectedWhenOccupiedItemBlocksTargetSlot()
    {
        var slotA = AttachmentSlot("slot-a", "blocker");
        var slotB = AttachmentSlot("slot-b", "candidate");
        var rootItem = Item("root") with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesWeapon",
                attachmentSlots: [slotA, slotB]),
        };
        var blocker = Item("blocker") with
        {
            FarmingGuideData = Layout(
                null,
                conflictingSlotIds: ["slot-b"]),
        };
        var candidate = Item("candidate");
        var rootState = new FarmingGuideItemState(
            rootItem.Id,
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["slot-a"] = FarmingGuideItemState.Create(blocker.Id),
            },
            new Dictionary<string, FarmingGuideItemState?>());
        var catalog = Catalog(rootItem, blocker, candidate);

        Assert.False(FarmingGuideAssemblyPolicy.CanAttach(
            rootState,
            [],
            slotB,
            candidate,
            catalog));
    }

    [Fact]
    public void AssemblySanitizerRemovesItemInstalledInBlockedSlot()
    {
        var slotA = AttachmentSlot("slot-a", "blocker");
        var slotB = AttachmentSlot("slot-b", "candidate");
        var rootItem = Item("root") with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesWeapon",
                attachmentSlots: [slotA, slotB]),
        };
        var blocker = Item("blocker") with
        {
            FarmingGuideData = Layout(
                null,
                conflictingSlotIds: ["slot-b"]),
        };
        var candidate = Item("candidate");
        var rootState = new FarmingGuideItemState(
            rootItem.Id,
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["slot-a"] = FarmingGuideItemState.Create(blocker.Id),
                ["slot-b"] = FarmingGuideItemState.Create(candidate.Id),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        var sanitized = FarmingGuideAssemblyPolicy.Sanitize(
            rootState,
            Catalog(rootItem, blocker, candidate));

        Assert.NotNull(sanitized);
        Assert.NotNull(sanitized!.Attachments.GetValueOrDefault("slot-a"));
        Assert.Null(sanitized.Attachments.GetValueOrDefault("slot-b"));
    }

    [Fact]
    public void CandidateDeclaredSlotConflictIsCheckedInReverseDirection()
    {
        var slotA = AttachmentSlot("slot-a", "blocker");
        var slotB = AttachmentSlot("slot-b", "candidate");
        var rootItem = Item("root") with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesWeapon",
                attachmentSlots: [slotA, slotB]),
        };
        var blocker = Item("blocker");
        var candidate = Item("candidate") with
        {
            FarmingGuideData = Layout(
                null,
                conflictingSlotIds: ["slot-a"]),
        };
        var rootState = new FarmingGuideItemState(
            rootItem.Id,
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["slot-a"] = FarmingGuideItemState.Create(blocker.Id),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        Assert.False(FarmingGuideAssemblyPolicy.CanAttach(
            rootState,
            [],
            slotB,
            candidate,
            Catalog(rootItem, blocker, candidate)));
    }

    [Fact]
    public void ReplacingTargetSlotDoesNotTreatRemovedSubtreeAsStillOccupied()
    {
        var slot = AttachmentSlot("slot", "candidate", "old");
        var rootItem = Item("root") with
        {
            FarmingGuideData = Layout(
                "ItemPropertiesWeapon",
                attachmentSlots: [slot]),
        };
        var old = Item("old") with
        {
            FarmingGuideData = Layout(
                null,
                conflictingItemIds: ["candidate"]),
        };
        var candidate = Item("candidate");
        var rootState = new FarmingGuideItemState(
            rootItem.Id,
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["slot"] = FarmingGuideItemState.Create(old.Id),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        Assert.True(FarmingGuideAssemblyPolicy.CanAttach(
            rootState,
            [],
            slot,
            candidate,
            Catalog(rootItem, old, candidate)));
    }

    private static FarmingGuideAttachmentSlotDefinition AttachmentSlot(
        string slotId,
        params string[] allowedItemIds) =>
        new(
            slotId,
            slotId,
            slotId,
            false,
            new FarmingGuideItemFilter([], allowedItemIds, [], []));

    private static FarmingGuideItemLayout Layout(
        string? propertyType,
        IReadOnlyList<FarmingGuideAttachmentSlotDefinition>? attachmentSlots = null,
        IReadOnlyList<string>? conflictingItemIds = null,
        IReadOnlyList<string>? conflictingSlotIds = null) =>
        new(
            propertyType,
            [],
            attachmentSlots ?? [],
            [],
            conflictingItemIds ?? [],
            conflictingSlotIds ?? [],
            false,
            false);

    private static IReadOnlyDictionary<string, GameItem> Catalog(params GameItem[] items) =>
        items.ToDictionary(item => item.Id, StringComparer.Ordinal);

    private static GameItem Item(string id) =>
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
            [],
            1,
            1,
            1m,
            1_000,
            true);
}
