using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Profiles;
using Xunit;

namespace JunhyunHelper.Tests;

public sealed class HideoutFirPlanningTests
{
    [Fact]
    public void FirHideoutRequirement_DoesNotConsumeNonFirInventoryAndMarksItForCleanup()
    {
        const string itemId = "construction-measuring-tape";
        var station = new HideoutStation(
            "shooting-range",
            "사격장",
            "Shooting Range",
            null,
            [
                new HideoutLevel(
                    "shooting-range",
                    2,
                    null,
                    [new HideoutItemRequirement("shooting-range", 2, itemId, 1, true)]),
            ]);
        var content = new GameContentCatalog(
            Array.Empty<GameItem>(),
            Array.Empty<JunhyunHelper.Core.Reference.TraderDefinition>(),
            Array.Empty<JunhyunHelper.Core.Reference.MapReference>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestDefinition>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestObjective>(),
            Array.Empty<JunhyunHelper.Core.Quests.QuestItemRequirement>(),
            [station]);
        var profile = new GameProfileSnapshot
        {
            ProfileId = "test",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            Inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
            {
                [itemId] = new InventoryQuantity(0, 1),
            },
        };

        var plan = FutureNeededItemsPlanner.Calculate(content, profile);

        var needed = Assert.Single(plan.NeededItems);
        Assert.Equal(itemId, needed.ItemId);
        Assert.Equal(1, needed.RequiredTotal);
        Assert.Equal(1, needed.RequiredFir);
        Assert.Equal(1, needed.RemainingTotal);
        Assert.Equal(1, needed.RemainingFir);

        var cleanup = Assert.Single(plan.CleanupItems);
        Assert.Equal(itemId, cleanup.ItemId);
        Assert.Equal(1, cleanup.SurplusNonFir);
        Assert.Equal(0, cleanup.SurplusFir);
    }

    [Fact]
    public void NonFirHideoutRequirement_CanStillUseNonFirInventory()
    {
        const string itemId = "construction-measuring-tape";
        var requirements = new[]
        {
            new ItemRequirement(
                itemId,
                1,
                0,
                new ItemRequirementSource(ItemRequirementSourceKind.Hideout, "security", "1")),
        };
        var inventory = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal)
        {
            [itemId] = new InventoryQuantity(0, 1),
        };

        var needed = Assert.Single(NeededItemCalculator.Calculate(requirements, inventory));
        var cleanup = InventorySurplusCalculator.Calculate(requirements, inventory);

        Assert.True(needed.IsFulfilled);
        Assert.Equal(0, needed.RemainingTotal);
        Assert.Empty(cleanup);
    }
}