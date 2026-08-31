using JunhyunHelper.Core.Editions;

namespace JunhyunHelper.Core.FarmingGuide;

/// <summary>
/// Canonical Farming Guide pocket geometry. Tarkov grants expanded pockets either
/// directly through an edition perk or by completing Ragman's Old Patterns quest.
/// The edition overlay represents a built-in perk by excluding Old Patterns because
/// the reward is already owned.
/// </summary>
public static class FarmingGuidePocketLayoutPolicy
{
    public const string ExpandedPocketsQuestId = "666314a50aa5c7436c00908a";

    private static readonly IReadOnlyList<FarmingGuideStorageGridDefinition> Standard =
        Array.AsReadOnly(
        [
            Grid(1, 1),
            Grid(1, 1),
            Grid(1, 1),
            Grid(1, 1),
        ]);

    private static readonly IReadOnlyList<FarmingGuideStorageGridDefinition> Expanded =
        Array.AsReadOnly(
        [
            Grid(1, 1),
            Grid(1, 2),
            Grid(1, 2),
            Grid(1, 1),
        ]);

    public static IReadOnlyList<FarmingGuideStorageGridDefinition> StandardGrids => Standard;

    public static IReadOnlyList<FarmingGuideStorageGridDefinition> ExpandedGrids => Expanded;

    public static IReadOnlyList<FarmingGuideStorageGridDefinition> Resolve(
        string? editionId,
        IReadOnlySet<string>? completedQuestIds,
        IReadOnlyList<EditionDefinition>? editions)
    {
        if (completedQuestIds?.Contains(ExpandedPocketsQuestId) == true)
            return Expanded;

        if (!string.IsNullOrWhiteSpace(editionId) && editions is not null)
        {
            var edition = editions.FirstOrDefault(value =>
                string.Equals(value.Id, editionId, StringComparison.Ordinal));
            if (edition?.ExcludedQuestIds.Contains(ExpandedPocketsQuestId) == true)
                return Expanded;
        }

        return Standard;
    }

    private static FarmingGuideStorageGridDefinition Grid(int width, int height) =>
        new(width, height, FarmingGuideItemFilter.Empty);
}
