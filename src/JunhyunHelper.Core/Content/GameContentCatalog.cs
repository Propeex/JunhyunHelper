using System.Text.Json.Serialization;
using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Quests;
using JunhyunHelper.Core.Reference;

namespace JunhyunHelper.Core.Content;

public sealed record GameContentCatalog(
    IReadOnlyList<GameItem> Items,
    IReadOnlyList<TraderDefinition> Traders,
    IReadOnlyList<MapReference> Maps,
    IReadOnlyList<QuestDefinition> Quests,
    IReadOnlyList<QuestObjective> QuestObjectives,
    IReadOnlyList<QuestItemRequirement> QuestItemRequirements,
    IReadOnlyList<HideoutStation> HideoutStations,
    IReadOnlyList<AmmoDefinition>? Ammo = null,
    IReadOnlyList<EditionDefinition>? EditionData = null,
    ItemRelationshipCatalog? ItemRelationshipData = null)
{
    /// <summary>
    /// Optional persisted package mapping. Kept as an init property instead of a new
    /// positional constructor argument so existing snapshots and call sites remain
    /// source/data compatible; older snapshots simply deserialize with no mappings.
    /// </summary>
    public IReadOnlyList<AmmoPackDefinition>? AmmoPackData { get; init; }

    [JsonIgnore]
    public IReadOnlyList<AmmoDefinition> Ammunition => Ammo ?? Array.Empty<AmmoDefinition>();

    [JsonIgnore]
    public IReadOnlyList<AmmoPackDefinition> AmmoPacks => AmmoPackData ?? Array.Empty<AmmoPackDefinition>();

    [JsonIgnore]
    public IReadOnlyList<EditionDefinition> Editions => EditionData ?? Array.Empty<EditionDefinition>();

    [JsonIgnore]
    public ItemRelationshipCatalog ItemRelationships =>
        ItemRelationshipData ?? ItemRelationshipCatalog.Empty;
}
