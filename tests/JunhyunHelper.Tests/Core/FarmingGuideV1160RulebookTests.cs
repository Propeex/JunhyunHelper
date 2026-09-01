using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class FarmingGuideV1160RulebookTests
{
    [Fact]
    public void FirNeedOutranksMuchHigherOrdinaryFleaValue()
    {
        var firNeeded = new FarmingGuideLootMetrics(1, 999_999, 1_000, 1);
        var ordinary = new FarmingGuideLootMetrics(0, 1, 1_000_000, 1);

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(firNeeded, ordinary) > 0);
    }

    [Fact]
    public void TraderValueNeverChangesEconomicPriority()
    {
        var highTraderLowFlea = new FarmingGuideLootMetrics(0, 1_000_000, 20_000, 1);
        var lowTraderHighFlea = new FarmingGuideLootMetrics(0, 1, 30_000, 1);

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(lowTraderHighFlea, highTraderLowFlea) > 0);
    }

    [Fact]
    public void StackQuantityContributesToTotalFleaValue()
    {
        var stack = new FarmingGuideLootMetrics(0, null, 5_000, 1) { Quantity = 20 };
        var single = new FarmingGuideLootMetrics(0, null, 90_000, 1);

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(stack, single) > 0);
    }

    [Fact]
    public void EqualValuePrefersLighterKnownItem()
    {
        var light = new FarmingGuideLootMetrics(0, null, 50_000, 2) { UnitWeightKg = 0.4m };
        var heavy = new FarmingGuideLootMetrics(0, null, 50_000, 2) { UnitWeightKg = 1.1m };

        Assert.True(FarmingGuideLootPriorityPolicy.Compare(light, heavy) > 0);
    }

    [Theory]
    [InlineData(0, 77.0)]
    [InlineData(10, 81.62)]
    [InlineData(50, 100.1)]
    [InlineData(51, 100.0)]
    public void StrengthControlsMaximumCarryWeight(int level, double expected)
    {
        var actual = FarmingGuideWeightPolicy.MaximumCarryWeightKg(new FarmingGuideWeightSettings(level));
        Assert.Equal((decimal)expected, actual, 2);
    }

    [Fact]
    public void EliteStrengthExcludesSlingBackAndHolsterWeaponsOnly()
    {
        var elite = new FarmingGuideWeightSettings(51);

        Assert.False(FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(
            FarmingGuideEquipmentSlot.PrimaryWeapon1,
            elite));
        Assert.False(FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(
            FarmingGuideEquipmentSlot.PrimaryWeapon2,
            elite));
        Assert.False(FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(
            FarmingGuideEquipmentSlot.Holster,
            elite));
        Assert.True(FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(
            FarmingGuideEquipmentSlot.Melee,
            elite));
        Assert.True(FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(
            FarmingGuideEquipmentSlot.Helmet,
            elite));
    }

    [Fact]
    public void ItemWeightUsesConcreteStackQuantity()
    {
        var ammo = Item("ammo", typeKeys: ["ammo"], weightKg: 0.012m);
        Assert.Equal(0.60m, FarmingGuideWeightPolicy.ItemWeightKg(ammo, 50));
    }

    [Fact]
    public void AmmoAndCanonicalCurrenciesRequireQuantity()
    {
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(Item("ammo", typeKeys: ["ammo"])));
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(Item("5449016a4bdc2d6f028b456f")));
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(Item("5696686a4bdc2da3298b456a")));
        Assert.True(FarmingGuideStackQuantityPolicy.RequiresQuantity(Item("569668774bdc2da2298b4568")));
        Assert.False(FarmingGuideStackQuantityPolicy.RequiresQuantity(Item("ordinary")));
    }

    [Fact]
    public void RaidAcceptanceCommitsSnapshotAndMigratedLocksAtomically()
    {
        var baseline = FarmingGuideLoadoutSnapshot.Empty;
        var session = new FarmingGuideRaidSession(baseline, FarmingGuideLockState.Empty);
        var proposed = baseline with
        {
            StoredItems =
            [
                new FarmingGuideStoredItemState(
                    "stack",
                    FarmingGuideItemState.Create("ammo"),
                    FarmingGuideStorageKind.Rig,
                    0,
                    0,
                    0,
                    false,
                    Quantity: 30),
            ],
        };
        var locks = new FarmingGuideLockState(
            [],
            [],
            ["stack"],
            [new FarmingGuideLockedCell(FarmingGuideStorageKind.Rig, 0, 1, 0)]);

        session.SetPending(
            "ammo",
            "리그에 보관",
            FarmingGuideInstructionAction.Store,
            proposed,
            locks);

        Assert.True(session.TryAccept(out var accepted, out var acceptedLocks));
        Assert.Equal(30, accepted.StoredItems.Single().Quantity);
        Assert.Contains("stack", acceptedLocks.ItemInstanceIds);
        Assert.Contains(acceptedLocks.ReservedCells, cell => cell.X == 1 && cell.Y == 0);
    }

    private static GameItem Item(
        string id,
        IReadOnlyList<string>? typeKeys = null,
        decimal? weightKg = null) =>
        new(
            id,
            id,
            id,
            id,
            id,
            null,
            null,
            [],
            [],
            typeKeys ?? [],
            1,
            1,
            weightKg);
}
