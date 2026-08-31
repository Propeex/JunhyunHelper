namespace JunhyunHelper.Core.FarmingGuide;

public enum FarmingGuideEquipmentSlot
{
    Headset,
    Helmet,
    FaceCover,
    Armband,
    BodyArmor,
    Eyewear,
    PrimaryWeapon1,
    PrimaryWeapon2,
    Holster,
    Melee,
    Dogtag,
}

public enum FarmingGuideStorageKind
{
    Pockets,
    Rig,
    Backpack,
    SecureContainer,
    SpecialSlots,
}

public sealed record FarmingGuideItemState(
    string ItemId,
    IReadOnlyDictionary<string, FarmingGuideItemState?> Attachments,
    IReadOnlyDictionary<string, FarmingGuideItemState?> ArmorPlates)
{
    public static FarmingGuideItemState Create(string itemId) =>
        new(itemId, new Dictionary<string, FarmingGuideItemState?>(), new Dictionary<string, FarmingGuideItemState?>());
}

public sealed record FarmingGuideStoredItemState(
    string InstanceId,
    FarmingGuideItemState Item,
    FarmingGuideStorageKind Storage,
    int GridIndex,
    int X,
    int Y,
    bool Rotated,
    string? ParentInstanceId = null);

public sealed record FarmingGuideLoadoutSnapshot(
    IReadOnlyDictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState> Equipment,
    FarmingGuideItemState? Rig,
    FarmingGuideItemState? Backpack,
    FarmingGuideItemState? SecureContainer,
    IReadOnlyList<FarmingGuideStoredItemState> StoredItems)
{
    public static FarmingGuideLoadoutSnapshot Empty { get; } = new(
        new Dictionary<FarmingGuideEquipmentSlot, FarmingGuideItemState>(),
        null,
        null,
        null,
        []);
}

public sealed record FarmingGuidePreset(
    string Name,
    FarmingGuideLoadoutSnapshot Snapshot,
    DateTimeOffset SavedAt);
