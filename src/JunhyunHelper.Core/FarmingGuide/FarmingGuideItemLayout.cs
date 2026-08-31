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

    public int StorageCapacity => StorageGrids.Sum(grid => grid.Width * grid.Height);
}
