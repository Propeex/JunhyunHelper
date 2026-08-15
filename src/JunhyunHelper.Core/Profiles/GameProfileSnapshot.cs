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

    public IReadOnlySet<string> FailedQuestIds { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Exceptional user facts for special trader access that the normal quest graph
    /// cannot reconstruct exactly. Missing keys mean "use automatic quest-based
    /// inference". This is intentionally sparse: normal BTR/Ref access never needs
    /// manual state, while Lightkeeper access can be lost and restored independently
    /// of monotonic quest completion facts.
    /// </summary>
    public IReadOnlyDictionary<string, bool> SpecialTraderAccessOverrides { get; init; } =
        new Dictionary<string, bool>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> HideoutLevels { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryQuantity> Inventory { get; init; } =
        new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryConsumption> QuestConsumptions { get; init; } =
        new Dictionary<string, InventoryConsumption>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InventoryConsumption> HideoutUpgradeConsumptions { get; init; } =
        new Dictionary<string, InventoryConsumption>(StringComparer.Ordinal);
}
