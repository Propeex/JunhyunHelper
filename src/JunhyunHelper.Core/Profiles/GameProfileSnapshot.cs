using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.Profiles;

public sealed record GameProfileSnapshot
{
    public required string ProfileId { get; init; }

    public required GameMode GameMode { get; init; }

    public required int Level { get; init; }

    public required PmcFaction Faction { get; init; }

    public string? EditionId { get; init; }

    public int? PrestigeLevel { get; init; }

    public IReadOnlyDictionary<string, TraderProgress> Traders { get; init; } =
        new Dictionary<string, TraderProgress>(StringComparer.Ordinal);

    public IReadOnlySet<string> CompletedQuestIds { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Completion times known to JunhyunHelper. Old profiles intentionally have no
    /// timestamps for historical completions; callers must treat a missing value as
    /// legacy/unknown instead of inventing a recent completion time.
    /// </summary>
    public IReadOnlyDictionary<string, DateTimeOffset> QuestCompletedAtUtc { get; init; } =
        new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);

    public IReadOnlySet<string> FailedQuestIds { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> HideoutLevels { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryQuantity> Inventory { get; init; } =
        new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryConsumption> QuestConsumptions { get; init; } =
        new Dictionary<string, InventoryConsumption>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryConsumption> HideoutUpgradeConsumptions { get; init; } =
        new Dictionary<string, InventoryConsumption>(StringComparer.Ordinal);
}
