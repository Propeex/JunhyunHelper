namespace JunhyunHelper.Core.FarmingGuide;

public sealed record FarmingGuideItemFilter(
    IReadOnlyList<string> AllowedCategoryIds,
    IReadOnlyList<string> AllowedItemIds,
    IReadOnlyList<string> ExcludedCategoryIds,
    IReadOnlyList<string> ExcludedItemIds)
{
    public static FarmingGuideItemFilter Empty { get; } = new([], [], [], []);
}

public sealed record FarmingGuideStorageGridDefinition(
    int Width,
    int Height,
    FarmingGuideItemFilter Filters);

public sealed record FarmingGuideAttachmentSlotDefinition(
    string Id,
    string NameId,
    string? Name,
    bool Required,
    FarmingGuideItemFilter Filters);

public sealed record FarmingGuideArmorSlotDefinition(
    string Id,
    string NameId,
    string? Name,
    bool Locked,
    IReadOnlyList<string> AllowedPlateIds);

/// <summary>
/// Tarkov equipment/storage structure required by the Farming Guide editor.
/// The source is the same validated item endpoint used to build canonical Game Content.
/// Older readable content snapshots may legitimately have this value absent.
/// </summary>
public sealed record FarmingGuideItemLayout(
    string? PropertiesType,
    IReadOnlyList<FarmingGuideStorageGridDefinition> StorageGrids,
    IReadOnlyList<FarmingGuideAttachmentSlotDefinition> AttachmentSlots,
    IReadOnlyList<FarmingGuideArmorSlotDefinition> ArmorSlots,
    IReadOnlyList<string> ConflictingItemIds,
    IReadOnlyList<string> ConflictingSlotIds,
    bool BlocksHeadphones,
    bool IsArmoredRig)
{
    /// <summary>
    /// Optional raw Tarkov GridLayoutName/RigLayoutName. It is visual metadata only;
    /// missing values are valid for older/current normalized content snapshots.
    /// </summary>
    public string? StorageLayoutName { get; init; }

    /// <summary>
    /// Source-backed protection class for armor/helmet/armored-rig style properties.
    /// Null means the current source did not prove a meaningful positive class. This is
    /// intentionally retained even though complete-equipment mode hides plate internals:
    /// the live raid advisor needs an objective top-level upgrade signal.
    /// </summary>
    public int? ArmorClass { get; init; }

    /// <summary>
    /// Source-backed active-headset hearing-distance fact. Distortion is retained as
    /// diagnostic/source metadata, but the v1.16+ automatic upgrade rule uses hearing
    /// distance only.
    /// </summary>
    public decimal? HeadsetDistanceModifier { get; init; }
    public decimal? HeadsetDistortion { get; init; }

    /// <summary>
    /// Source-backed provision effects. Positive energy/hydration identify whether the
    /// item can satisfy the Farming Guide's minimum raid food/drink reserve. Null means an
    /// older content snapshot or a source item that does not expose the fact.
    /// </summary>
    public int? Energy { get; init; }
    public int? Hydration { get; init; }

    /// <summary>
    /// Source-backed ammunition compatibility. AmmoCaliber is present on ammunition;
    /// WeaponCaliber/AllowedAmmoItemIds are present on weapons. AllowedAmmoItemIds is the
    /// authoritative match when available, with caliber retained as a safe fallback for
    /// older source rows.
    /// </summary>
    public string? AmmoCaliber { get; init; }
    public string? WeaponCaliber { get; init; }
    public IReadOnlyList<string> AllowedAmmoItemIds { get; init; } = Array.Empty<string>();

    public int StorageCapacity => StorageGrids.Sum(grid => grid.Width * grid.Height);
}
