using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.FarmingGuide;

public sealed class FarmingGuideAssemblyPolicyTests
{
    [Fact]
    public void SetAttachment_UpdatesDeepOwnerWithoutMutatingOriginalTree()
    {
        var handguard = new FarmingGuideItemState(
            "handguard",
            new Dictionary<string, FarmingGuideItemState?>(),
            new Dictionary<string, FarmingGuideItemState?>());
        var root = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?> { ["handguard-slot"] = handguard },
            new Dictionary<string, FarmingGuideItemState?>());

        var updated = FarmingGuideAssemblyPolicy.SetAttachment(
            root,
            ["handguard-slot"],
            "rail-slot",
            FarmingGuideItemState.Create("rail"));

        Assert.Null(handguard.Attachments.GetValueOrDefault("rail-slot"));
        Assert.Equal(
            "rail",
            FarmingGuideAssemblyPolicy.GetNode(updated, ["handguard-slot"])?
                .Attachments.GetValueOrDefault("rail-slot")?.ItemId);
        Assert.Equal("weapon", updated.ItemId);
    }

    [Fact]
    public void Sanitize_PreservesCompatibleNestedTreeAndDropsUnknownSlots()
    {
        var railFilter = Filter(allowedItems: ["rail"]);
        var handguardFilter = Filter(allowedItems: ["handguard"]);
        var catalog = Catalog(
            Item("weapon", Layout(attachments: [Slot("handguard-slot", handguardFilter)])),
            Item("handguard", Layout(attachments: [Slot("rail-slot", railFilter)])),
            Item("rail"),
            Item("unknown"));
        var state = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["handguard-slot"] = new FarmingGuideItemState(
                    "handguard",
                    new Dictionary<string, FarmingGuideItemState?>
                    {
                        ["rail-slot"] = FarmingGuideItemState.Create("rail"),
                        ["obsolete-slot"] = FarmingGuideItemState.Create("unknown"),
                    },
                    new Dictionary<string, FarmingGuideItemState?>()),
                ["obsolete-root-slot"] = FarmingGuideItemState.Create("unknown"),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        var sanitized = FarmingGuideAssemblyPolicy.Sanitize(state, catalog);

        Assert.NotNull(sanitized);
        Assert.False(sanitized.Attachments.ContainsKey("obsolete-root-slot"));
        var sanitizedHandguard = Assert.IsType<FarmingGuideItemState>(
            sanitized.Attachments["handguard-slot"]);
        Assert.Equal("rail", sanitizedHandguard.Attachments["rail-slot"]?.ItemId);
        Assert.False(sanitizedHandguard.Attachments.ContainsKey("obsolete-slot"));
    }

    [Fact]
    public void Sanitize_DropsLaterSiblingWhenInstalledPartsConflict()
    {
        var catalog = Catalog(
            Item("weapon", Layout(attachments:
            [
                Slot("left", FarmingGuideItemFilter.Empty),
                Slot("right", FarmingGuideItemFilter.Empty),
            ])),
            Item("left-part", Layout(conflictingItems: ["right-part"])),
            Item("right-part"));
        var state = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["left"] = FarmingGuideItemState.Create("left-part"),
                ["right"] = FarmingGuideItemState.Create("right-part"),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        var sanitized = FarmingGuideAssemblyPolicy.Sanitize(state, catalog);

        Assert.NotNull(sanitized);
        Assert.Equal("left-part", sanitized.Attachments["left"]?.ItemId);
        Assert.False(sanitized.Attachments.ContainsKey("right"));
    }

    [Fact]
    public void CanAttach_RejectsCandidateThatConflictsWithExistingNestedPart()
    {
        var targetSlot = Slot("target", FarmingGuideItemFilter.Empty);
        var catalog = Catalog(
            Item("weapon", Layout(attachments:
            [
                Slot("existing", FarmingGuideItemFilter.Empty),
                targetSlot,
            ])),
            Item("existing-part"),
            Item("candidate", Layout(conflictingItems: ["existing-part"])));
        var root = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["existing"] = FarmingGuideItemState.Create("existing-part"),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        var allowed = FarmingGuideAssemblyPolicy.CanAttach(
            root,
            [],
            targetSlot,
            catalog["candidate"],
            catalog);

        Assert.False(allowed);
    }

    [Fact]
    public void HasMissingRequiredSlots_RecursesIntoInstalledChildren()
    {
        var catalog = Catalog(
            Item("weapon", Layout(attachments:
            [
                Slot("handguard-slot", Filter(allowedItems: ["handguard"])),
            ])),
            Item("handguard", Layout(attachments:
            [
                Slot("required-rail", Filter(allowedItems: ["rail"]), required: true),
            ])),
            Item("rail"));
        var root = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["handguard-slot"] = FarmingGuideItemState.Create("handguard"),
            },
            new Dictionary<string, FarmingGuideItemState?>());

        Assert.True(FarmingGuideAssemblyPolicy.HasMissingRequiredSlots(root, catalog));

        var complete = FarmingGuideAssemblyPolicy.SetAttachment(
            root,
            ["handguard-slot"],
            "required-rail",
            FarmingGuideItemState.Create("rail"));
        Assert.False(FarmingGuideAssemblyPolicy.HasMissingRequiredSlots(complete, catalog));
    }

    [Fact]
    public void AssemblySignature_IsStableAcrossDictionaryInsertionOrder()
    {
        var first = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["z-slot"] = FarmingGuideItemState.Create("z-part"),
                ["a-slot"] = FarmingGuideItemState.Create("a-part"),
            },
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate-b"] = FarmingGuideItemState.Create("plate-b"),
                ["plate-a"] = FarmingGuideItemState.Create("plate-a"),
            });
        var second = new FarmingGuideItemState(
            "weapon",
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["a-slot"] = FarmingGuideItemState.Create("a-part"),
                ["z-slot"] = FarmingGuideItemState.Create("z-part"),
            },
            new Dictionary<string, FarmingGuideItemState?>
            {
                ["plate-a"] = FarmingGuideItemState.Create("plate-a"),
                ["plate-b"] = FarmingGuideItemState.Create("plate-b"),
            });

        Assert.Equal(
            FarmingGuideAssemblyPolicy.AssemblySignature(first),
            FarmingGuideAssemblyPolicy.AssemblySignature(second));
    }

    private static FarmingGuideAttachmentSlotDefinition Slot(
        string id,
        FarmingGuideItemFilter filter,
        bool required = false) =>
        new(id, id, id, required, filter);

    private static FarmingGuideItemFilter Filter(IReadOnlyList<string>? allowedItems = null) =>
        new([], allowedItems ?? [], [], []);

    private static FarmingGuideItemLayout Layout(
        IReadOnlyList<FarmingGuideAttachmentSlotDefinition>? attachments = null,
        IReadOnlyList<string>? conflictingItems = null) =>
        new(
            "ItemPropertiesWeapon",
            [],
            attachments ?? [],
            [],
            conflictingItems ?? [],
            [],
            false,
            false);

    private static GameItem Item(string id, FarmingGuideItemLayout? layout = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [])
        {
            FarmingGuideData = layout,
        };

    private static IReadOnlyDictionary<string, GameItem> Catalog(params GameItem[] items) =>
        items.ToDictionary(static item => item.Id, StringComparer.Ordinal);
}
