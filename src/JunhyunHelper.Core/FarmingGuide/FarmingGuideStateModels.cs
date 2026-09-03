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

/// <summary>
/// Explicit Tarkov Found-in-Raid provenance for one modeled item instance. Unknown is the
/// safe default for historical state and Scanner paths that cannot prove the in-game FIR mark.
/// Raid acquisition and FIR are intentionally separate facts.
/// </summary>
public enum FarmingGuideFirStatus
{
    Unknown,
    NotFoundInRaid,
    FoundInRaid,
}

public sealed record FarmingGuideItemState(
    string ItemId,
    IReadOnlyDictionary<string, FarmingGuideItemState?> Attachments,
    IReadOnlyDictionary<string, FarmingGuideItemState?> ArmorPlates)
{
    /// <summary>
    /// True only for an item instance acquired during the active modeled raid. This says when
    /// the helper acquired the instance; it does not imply Tarkov Found-in-Raid eligibility.
    /// </summary>
    public bool RaidAcquired { get; init; }

    /// <summary>
    /// Tarkov FIR provenance. Only FoundInRaid may satisfy FIR quest/hideout requirements.
    /// Unknown must never be promoted to FIR merely because RaidAcquired is true.
    /// </summary>
    public FarmingGuideFirStatus FirStatus { get; init; } = FarmingGuideFirStatus.Unknown;

    public bool IsFirQualified => FirStatus == FarmingGuideFirStatus.FoundInRaid;

    public static FarmingGuideItemState Create(
        string itemId,
        bool raidAcquired = false,
        FarmingGuideFirStatus firStatus = FarmingGuideFirStatus.Unknown) =>
        new(itemId, new Dictionary<string, FarmingGuideItemState?>(), new Dictionary<string, FarmingGuideItemState?>())
        {
            RaidAcquired = raidAcquired,
            FirStatus = firStatus,
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
