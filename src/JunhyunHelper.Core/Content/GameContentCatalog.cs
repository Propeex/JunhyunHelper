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
    IReadOnlyList<HideoutStation> HideoutStations);
