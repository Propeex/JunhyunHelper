using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Core.Items;

public sealed record NeededItemsQueryResult(
    IReadOnlyList<NeededItem> FixedItems,
    IReadOnlyList<QuestItemRequirement> AlternativeQuestRequirements);

public static class NeededItemsQuery
{
    public static NeededItemsQueryResult Calculate(
        IEnumerable<QuestItemRequirement> questRequirements,
        IEnumerable<HideoutItemRequirement> hideoutRequirements,
        IReadOnlyDictionary<string, InventoryQuantity> inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        var requirements = NeededItemRequirementBuilder.Build(
            questRequirements,
            hideoutRequirements);

        return new NeededItemsQueryResult(
            NeededItemCalculator.Calculate(requirements.FixedRequirements, inventory),
            requirements.AlternativeQuestRequirements);
    }
}
