using System.Text.Json.Serialization;

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
    /// <summary>
    /// Ephemeral active-raid provenance. Farming Guide sets this only for items that enter
    /// the modeled raid through a confirmed Scanner identification while the raid session is
    /// active. The product rule treats those incoming items as FIR. This fact is deliberately
    /// not persisted to presets/working state and is cleared naturally when raid state returns
    /// to the raid-start baseline.
    /// </summary>
    [JsonIgnore]
    public bool RaidAcquired { get; init; }

    public static FarmingGuideItemState Create(string itemId, bool raidAcquired = false) =>
        new(itemId, new Dictionary<string, FarmingGuideItemState?>(), new Dictionary<string, FarmingGuideItemState?>())
        {
            RaidAcquired = raidAcquired,
        };
}

public sealed record FarmingGuideStoredItemState(
    string InstanceId,
    FarmingGuideItemState Item,
    FarmingGuideStorageKind Storage,
    int GridIndex,
    int X,
    int Y,
    bool Rotated,
    string? ParentInstanceId = null,
    int Quantity = 1)
{
    public int NormalizedQuantity => Math.Max(1, Quantity);
}

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
/// Character facts needed for deterministic weight calculations. Strength is persisted
/// per profile and edited through the lightweight Farming Guide weight popup.
/// </summary>
public sealed record FarmingGuideWeightSettings(int StrengthLevel = 0)
{
    public static FarmingGuideWeightSettings Default { get; } = new();

    public FarmingGuideWeightSettings Normalized() => this with
    {
        StrengthLevel = Math.Clamp(StrengthLevel, 0, 51),
    };
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
    IReadOnlyList<FarmingGuideEquipmentSlot> EquipmentSlots,
    IReadOnlyList<FarmingGuideStorageKind> Carriers,
    IReadOnlyList<string> ItemInstanceIds,
    IReadOnlyList<FarmingGuideLockedCell> ReservedCells)
{
    public static FarmingGuideLockState Empty { get; } = new([], [], [], []);

    public FarmingGuideLockState CopyNormalized() => new(
        EquipmentSlots.Distinct().ToArray(),
        Carriers.Distinct().ToArray(),
        ItemInstanceIds
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray(),
        ReservedCells.Distinct().ToArray());
}

public sealed record FarmingGuidePreset(
    string Name,
    FarmingGuideLoadoutSnapshot Snapshot,
    DateTimeOffset SavedAt,
    FarmingGuideLockState? Locks = null);
