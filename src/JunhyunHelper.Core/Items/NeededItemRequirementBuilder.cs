using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Core.Items;

public sealed record NeededItemRequirementSet(
    IReadOnlyList<ItemRequirement> FixedRequirements,
    IReadOnlyList<QuestItemRequirement> AlternativeQuestRequirements);

public static class NeededItemRequirementBuilder
{
    public static NeededItemRequirementSet Build(
        IEnumerable<QuestItemRequirement> questRequirements,
        IEnumerable<HideoutItemRequirement> hideoutRequirements)
    {
        ArgumentNullException.ThrowIfNull(questRequirements);
        ArgumentNullException.ThrowIfNull(hideoutRequirements);

        var fixedRequirements = new List<ItemRequirement>();
        var alternatives = new List<QuestItemRequirement>();

        foreach (var requirement in questRequirements)
        {
            if (requirement.Count <= 0 || requirement.AcceptedItemIds.Count == 0)
                throw new InvalidDataException(
                    $"Quest '{requirement.QuestId}' objective '{requirement.ObjectiveId}' has an invalid item requirement.");

            var acceptedIds = requirement.AcceptedItemIds
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (acceptedIds.Length != requirement.AcceptedItemIds.Count)
            {
                throw new InvalidDataException(
                    $"Quest '{requirement.QuestId}' objective '{requirement.ObjectiveId}' has invalid or duplicate accepted item ids.");
            }

            if (acceptedIds.Length > 1)
            {
                alternatives.Add(requirement);
                continue;
            }

            fixedRequirements.Add(new ItemRequirement(
                acceptedIds[0],
                requirement.Count,
                requirement.FoundInRaid ? requirement.Count : 0,
                new ItemRequirementSource(
                    ItemRequirementSourceKind.Quest,
                    requirement.QuestId,
                    requirement.ObjectiveId)));
        }

        foreach (var requirement in hideoutRequirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.ItemId) || requirement.Count <= 0)
            {
                throw new InvalidDataException(
                    $"Hideout '{requirement.StationId}' level '{requirement.TargetLevel}' has an invalid item requirement.");
            }

            fixedRequirements.Add(new ItemRequirement(
                requirement.ItemId,
                requirement.Count,
                requirement.FoundInRaid ? requirement.Count : 0,
                new ItemRequirementSource(
                    ItemRequirementSourceKind.Hideout,
                    requirement.StationId,
                    requirement.TargetLevel.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        }

        return new NeededItemRequirementSet(fixedRequirements, alternatives);
    }
}
