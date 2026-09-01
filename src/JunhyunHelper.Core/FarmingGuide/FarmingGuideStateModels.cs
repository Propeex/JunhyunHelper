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

/// <summary>
/// One reserved inventory cell. ParentInstanceId distinguishes a root storage surface
/// from the same grid index inside a nested carrier instance.
/// </summary>
public sealed record FarmingGuideLockedCell(
    FarmingGuideStorageKind Storage,
    int GridIndex,
    int X,
    int Y,
    string? ParentInstanceId = null);

/// <summary>
/// User-owned constraints for automatic Farming Guide decisions. Locks constrain the
/// recommendation engine only; direct user editing remains authoritative.
/// </summary>
public sealed record FarmingGuideLockState(
    IReadOnlySet<FarmingGuideEquipmentSlot> EquipmentSlots,
    IReadOnlySet<FarmingGuideStorageKind> Carriers,
    IReadOnlySet<string> ItemInstanceIds,
    IReadOnlySet<FarmingGuideLockedCell> ReservedCells)
{
    public static FarmingGuideLockState Empty { get; } = new(
        new HashSet<FarmingGuideEquipmentSlot>(),
        new HashSet<FarmingGuideStorageKind>(),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<FarmingGuideLockedCell>());

    public FarmingGuideLockState Clone() => new(
        new HashSet<FarmingGuideEquipmentSlot>(EquipmentSlots),
        new HashSet<FarmingGuideStorageKind>(Carriers),
        new HashSet<string>(ItemInstanceIds, StringComparer.Ordinal),
        new HashSet<FarmingGuideLockedCell>(ReservedCells));
}

public sealed record FarmingGuidePreset(
    string Name,
    FarmingGuideLoadoutSnapshot Snapshot,
    DateTimeOffset SavedAt,
    FarmingGuideLockState? Locks = null);
