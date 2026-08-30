using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Profiles;
using Xunit;

namespace JunhyunHelper.Tests.Core;

public sealed class AmmoPickupEvaluatorTests
{
    [Fact]
    public void PicksOnlyUnavailableAmmoOutsideTwoPurchaseEndpoints()
    {
        var ammunition = new[]
        {
            Ammo("1", 50),
            Ammo("2", 40, Purchase("trader", 2)),
            Ammo("3", 30),
            Ammo("4", 20, Purchase("trader", 2)),
            Ammo("5", 10),
        };
        var profile = Profile(loyaltyLevel: 2);

        Assert.True(AmmoPickupEvaluator.Evaluate("1", ammunition, profile)!.ShouldPickUp);
        Assert.False(AmmoPickupEvaluator.Evaluate("2", ammunition, profile)!.ShouldPickUp);
        Assert.False(AmmoPickupEvaluator.Evaluate("3", ammunition, profile)!.ShouldPickUp);
        Assert.False(AmmoPickupEvaluator.Evaluate("4", ammunition, profile)!.ShouldPickUp);
        Assert.True(AmmoPickupEvaluator.Evaluate("5", ammunition, profile)!.ShouldPickUp);
    }

    [Fact]
    public void MatchesThreePurchaseEndpointExample()
    {
        var ammunition = new[]
        {
            Ammo("1", 70),
            Ammo("2", 60),
            Ammo("3", 50, Purchase("trader", 3)),
            Ammo("4", 40),
            Ammo("5", 30, Purchase("trader", 3)),
            Ammo("6", 20, Purchase("trader", 3)),
            Ammo("7", 10),
        };
        var profile = Profile(loyaltyLevel: 3);

        Assert.True(AmmoPickupEvaluator.Evaluate("1", ammunition, profile)!.ShouldPickUp);
        Assert.True(AmmoPickupEvaluator.Evaluate("2", ammunition, profile)!.ShouldPickUp);
        Assert.False(AmmoPickupEvaluator.Evaluate("4", ammunition, profile)!.ShouldPickUp);
        Assert.True(AmmoPickupEvaluator.Evaluate("7", ammunition, profile)!.ShouldPickUp);
    }

    [Fact]
    public void NoDirectPurchaseMakesEveryUnavailableAmmoPickupCandidate()
    {
        var ammunition = new[]
        {
            Ammo("a", 45, Barter("trader", 1)),
            Ammo("b", 30),
        };
        var profile = Profile(loyaltyLevel: 4);

        Assert.True(AmmoPickupEvaluator.Evaluate("a", ammunition, profile)!.ShouldPickUp);
        Assert.True(AmmoPickupEvaluator.Evaluate("b", ammunition, profile)!.ShouldPickUp);
    }

    [Fact]
    public void SingleDirectPurchaseKeepsEqualPenetrationTieInsideBand()
    {
        var ammunition = new[]
        {
            Ammo("higher", 41),
            Ammo("buy", 30, Purchase("trader", 1)),
            Ammo("tie", 30),
            Ammo("lower", 29),
        };
        var profile = Profile(loyaltyLevel: 1);

        var higher = AmmoPickupEvaluator.Evaluate("higher", ammunition, profile)!;
        var tie = AmmoPickupEvaluator.Evaluate("tie", ammunition, profile)!;
        var lower = AmmoPickupEvaluator.Evaluate("lower", ammunition, profile)!;

        Assert.True(higher.ShouldPickUp);
        Assert.False(tie.ShouldPickUp);
        Assert.True(lower.ShouldPickUp);
        Assert.Equal(2, AmmoPickupEvaluator.Evaluate("buy", ammunition, profile)!.Rank);
        Assert.Equal(3, tie.Rank);
    }

    [Fact]
    public void HigherLoyaltyAndIncompleteQuestDoNotCountAsPurchasable()
    {
        var ammunition = new[]
        {
            Ammo("ll", 50, Purchase("trader", 4)),
            Ammo("quest", 40, Purchase("trader", 2, "unlock")),
            Ammo("target", 30),
        };
        var profile = Profile(loyaltyLevel: 3);

        Assert.True(AmmoPickupEvaluator.Evaluate("target", ammunition, profile)!.ShouldPickUp);
        Assert.False(AmmoPickupEvaluator.IsDirectlyPurchasable(ammunition[0], profile));
        Assert.False(AmmoPickupEvaluator.IsDirectlyPurchasable(ammunition[1], profile));
    }

    [Fact]
    public void CompletedQuestUnlockCountsAsDirectPurchase()
    {
        var ammo = Ammo("quest", 40, Purchase("trader", 2, "unlock"));
        var profile = Profile(loyaltyLevel: 2, completedQuestIds: ["unlock"]);

        var decision = AmmoPickupEvaluator.Evaluate("quest", [ammo], profile)!;

        Assert.True(decision.IsDirectlyPurchasable);
        Assert.False(decision.ShouldPickUp);
        Assert.Equal(40, decision.PurchasableMinPenetration);
        Assert.Equal(40, decision.PurchasableMaxPenetration);
    }

    private static AmmoDefinition Ammo(
        string id,
        int penetration,
        params AmmoAcquisition[] acquisitions) =>
        new(
            ItemId: id,
            Caliber: "caliber",
            AmmoType: null,
            ProjectileCount: 1,
            Damage: 0,
            ArmorDamage: 0,
            PenetrationPower: penetration,
            FragmentationChance: 0,
            RicochetChance: 0,
            AccuracyModifier: 0,
            RecoilModifier: 0,
            InitialSpeed: 0,
            HeavyBleedModifier: 0,
            LightBleedModifier: 0,
            Tracer: false,
            TracerColor: null,
            Acquisitions: acquisitions);

    private static AmmoAcquisition Purchase(
        string traderId,
        int requiredLevel,
        string? taskUnlockQuestId = null) =>
        new(
            AmmoAcquisitionKind.TraderPurchase,
            ReferenceId: null,
            TraderId: traderId,
            StationId: null,
            RequiredLevel: requiredLevel,
            TaskUnlockQuestId: taskUnlockQuestId,
            OutputCount: 1,
            Price: 100,
            CurrencyItemId: "rouble",
            CurrencyCode: "RUB",
            DurationSeconds: null,
            BuyLimit: null,
            Requirements: []);

    private static AmmoAcquisition Barter(string traderId, int requiredLevel) =>
        Purchase(traderId, requiredLevel) with
        {
            Kind = AmmoAcquisitionKind.TraderBarter,
            Price = null,
            CurrencyItemId = null,
            CurrencyCode = null,
        };

    private static GameProfileSnapshot Profile(
        int? loyaltyLevel,
        IReadOnlySet<string>? completedQuestIds = null) => new()
        {
            ProfileId = "profile",
            GameMode = GameMode.Regular,
            Level = 1,
            Faction = PmcFaction.Usec,
            Traders = new Dictionary<string, TraderProgress>(StringComparer.Ordinal)
            {
                ["trader"] = new TraderProgress(loyaltyLevel, 0m),
            },
            CompletedQuestIds = completedQuestIds ?? new HashSet<string>(StringComparer.Ordinal),
        };
}
