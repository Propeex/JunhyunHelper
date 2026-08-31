using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.FarmingGuide;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuidePocketLayoutPolicyTests
{
    [Fact]
    public void StandardProfileUsesFourSingleCells()
    {
        var grids = FarmingGuidePocketLayoutPolicy.Resolve(
            "standard",
            new HashSet<string>(StringComparer.Ordinal),
            [Edition("standard")]);

        Assert.Equal([(1, 1), (1, 1), (1, 1), (1, 1)], Shape(grids));
    }

    [Fact]
    public void EditionWithBuiltInRewardUsesExpandedPockets()
    {
        var grids = FarmingGuidePocketLayoutPolicy.Resolve(
            "unheard",
            new HashSet<string>(StringComparer.Ordinal),
            [Edition("unheard", FarmingGuidePocketLayoutPolicy.ExpandedPocketsQuestId)]);

        Assert.Equal([(1, 1), (1, 2), (1, 2), (1, 1)], Shape(grids));
    }

    [Fact]
    public void CompletingOldPatternsExpandsAnyEdition()
    {
        var completed = new HashSet<string>(StringComparer.Ordinal)
        {
            FarmingGuidePocketLayoutPolicy.ExpandedPocketsQuestId,
        };

        var grids = FarmingGuidePocketLayoutPolicy.Resolve("standard", completed, [Edition("standard")]);

        Assert.Equal([(1, 1), (1, 2), (1, 2), (1, 1)], Shape(grids));
    }

    private static EditionDefinition Edition(string id, params string[] excludedQuestIds) =>
        new(
            id,
            id,
            new HashSet<string>(StringComparer.Ordinal),
            excludedQuestIds.ToHashSet(StringComparer.Ordinal));

    private static (int Width, int Height)[] Shape(IReadOnlyList<FarmingGuideStorageGridDefinition> grids) =>
        grids.Select(grid => (grid.Width, grid.Height)).ToArray();
}
