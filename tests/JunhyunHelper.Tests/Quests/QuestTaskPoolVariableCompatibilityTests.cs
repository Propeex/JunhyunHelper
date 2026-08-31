using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestTaskPoolVariableCompatibilityTests
{
    private const string Prapor = "54cb50c76803fa8b248b4571";
    private const string Skier = "58330581ace78e27b8b10cee";
    private const string PraporLl1Pool = "6a20540cf1b67a977cc5a088";
    private const string PraporLl2Pool = "6a2688488bba18e0b0187a04";
    private const string SkierLl2Pool = "6a5a111de1f417ac80a163e5";

    [Fact]
    public void Ll2PoolReconstructsFromAuditedSeedAndCompletedPoolQuests()
    {
        var quests = BuildPraporLl2Shape();
        var completed = new HashSet<string>(StringComparer.Ordinal)
        {
            "seed-1", "seed-2", "seed-3",
        };
        var profile = CreateProfile(loyalty: 2, completed: completed);

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(3, enriched.ProfileVariables[PraporLl2Pool]);
    }

    [Fact]
    public void ExistingExactProfileValueAlwaysWins()
    {
        var quests = BuildPraporLl2Shape();
        var profile = CreateProfile(
            loyalty: 4,
            completed: new HashSet<string> { "seed-1", "seed-2", "seed-3", "seed-4", "pool-1" },
            variables: new Dictionary<string, int> { [PraporLl2Pool] = 1 });

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(1, enriched.ProfileVariables[PraporLl2Pool]);
    }

    [Fact]
    public void FutureLoyaltyPoolCanBeDeterministicallyZero()
    {
        var quests = BuildPraporLl2Shape();
        var profile = CreateProfile(loyalty: 1, completed: new HashSet<string>(StringComparer.Ordinal));

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(0, enriched.ProfileVariables[PraporLl2Pool]);
    }

    [Fact]
    public void StructuralDriftFailsClosedWithoutSyntheticValue()
    {
        var quests = BuildPraporLl2Shape().Where(quest => quest.Id != "pool-6").ToArray();
        var profile = CreateProfile(loyalty: 2, completed: new HashSet<string> { "seed-1", "seed-2", "seed-3" });

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.False(enriched.ProfileVariables.ContainsKey(PraporLl2Pool));
    }

    [Fact]
    public void Ll1PoolAtLl1WithNoCompletedTraderQuestIsDeterministicallyZero()
    {
        var quests = BuildPraporLl1Shape(includeOrdinaryQuest: true);
        var profile = CreateProfile(loyalty: 1, completed: new HashSet<string>(StringComparer.Ordinal));

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(0, enriched.ProfileVariables[PraporLl1Pool]);
    }

    [Fact]
    public void Ll1PoolAfterAnySameTraderCompletionRemainsUnknownWhileStillLl1()
    {
        var quests = BuildPraporLl1Shape(includeOrdinaryQuest: true);
        var profile = CreateProfile(loyalty: 1, completed: new HashSet<string> { "ordinary-ll1" });

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.False(enriched.ProfileVariables.ContainsKey(PraporLl1Pool));
    }

    [Fact]
    public void HigherLoyaltySatisfiesPastLl1PoolInsteadOfCreatingFortyEightUnknownQuests()
    {
        var quests = BuildPraporLl1Shape(includeOrdinaryQuest: false);
        var profile = CreateProfile(loyalty: 2, completed: new HashSet<string>(StringComparer.Ordinal));

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(5, enriched.ProfileVariables[PraporLl1Pool]);
    }

    [Fact]
    public void HigherLoyaltySatisfiesPastLl2PoolWithoutInventingCompletionHistory()
    {
        var quests = BuildPraporLl2Shape();
        var profile = CreateProfile(loyalty: 3, completed: new HashSet<string>(StringComparer.Ordinal));

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(5, enriched.ProfileVariables[PraporLl2Pool]);
    }

    [Fact]
    public void SkierLl2RegularUsesFourSeedShapeFromCurrentAudit()
    {
        var quests = BuildSkierLl2Shape(seedCount: 4);
        var completed = new HashSet<string> { "skier-seed-1", "skier-seed-2", "skier-seed-3" };
        var profile = CreateProfile(
            loyalty: 2,
            completed: completed,
            traderId: Skier,
            gameMode: GameMode.Regular);

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(3, enriched.ProfileVariables[SkierLl2Pool]);
    }

    [Fact]
    public void SkierLl2PvpSeasonUsesThreeSeedShapeFromCurrentAudit()
    {
        var quests = BuildSkierLl2Shape(seedCount: 3);
        var completed = new HashSet<string> { "skier-seed-1", "skier-seed-2" };
        var profile = CreateProfile(
            loyalty: 2,
            completed: completed,
            traderId: Skier,
            gameMode: GameMode.PvpSeason);

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.Equal(2, enriched.ProfileVariables[SkierLl2Pool]);
    }

    [Fact]
    public void SkierLl2PvpSeasonRejectsRegularFourSeedShape()
    {
        var quests = BuildSkierLl2Shape(seedCount: 4);
        var profile = CreateProfile(
            loyalty: 2,
            completed: new HashSet<string>(),
            traderId: Skier,
            gameMode: GameMode.PvpSeason);

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.False(enriched.ProfileVariables.ContainsKey(SkierLl2Pool));
    }

    private static QuestDefinition[] BuildPraporLl1Shape(bool includeOrdinaryQuest)
    {
        var pool = Enumerable.Range(1, 8)
            .Select(index => PoolQuest(
                $"ll1-{index}",
                PraporLl1Pool,
                index <= 2 ? 1 : index <= 5 ? 3 : 5,
                Prapor))
            .ToList();
        if (includeOrdinaryQuest)
            pool.Add(CreateQuest("ordinary-ll1", [], [], Prapor));
        return pool.ToArray();
    }

    private static QuestDefinition[] BuildPraporLl2Shape()
    {
        var seeds = Enumerable.Range(1, 4)
            .Select(index => SeedQuest($"seed-{index}", 2, Prapor));
        var pool = new[]
        {
            PoolQuest("pool-1", PraporLl2Pool, 3, Prapor),
            PoolQuest("pool-2", PraporLl2Pool, 3, Prapor),
            PoolQuest("pool-3", PraporLl2Pool, 3, Prapor),
            PoolQuest("pool-4", PraporLl2Pool, 5, Prapor),
            PoolQuest("pool-5", PraporLl2Pool, 5, Prapor),
            PoolQuest("pool-6", PraporLl2Pool, 5, Prapor),
        };
        return seeds.Concat(pool).ToArray();
    }

    private static QuestDefinition[] BuildSkierLl2Shape(int seedCount)
    {
        var seeds = Enumerable.Range(1, seedCount)
            .Select(index => SeedQuest($"skier-seed-{index}", 2, Skier));
        var pool = Enumerable.Range(1, 9)
            .Select(index => PoolQuest(
                $"skier-pool-{index}",
                SkierLl2Pool,
                index <= 3 ? 1 : index <= 6 ? 3 : 4,
                Skier));
        return seeds.Concat(pool).ToArray();
    }

    private static QuestDefinition SeedQuest(string id, int loyalty, string traderId) =>
        CreateQuest(
            id,
            profileVariableRequirements: [],
            loyaltyRequirements: [new QuestTraderLoyaltyRequirement(traderId, loyalty)],
            traderId: traderId);

    private static QuestDefinition PoolQuest(
        string id,
        string variableId,
        int threshold,
        string traderId) =>
        CreateQuest(
            id,
            profileVariableRequirements:
            [
                new QuestProfileVariableRequirement(
                    variableId,
                    threshold,
                    ProfileVariableRequirementOperator.AtLeast),
            ],
            loyaltyRequirements: [],
            traderId: traderId);

    private static QuestDefinition CreateQuest(
        string id,
        IReadOnlyList<QuestProfileVariableRequirement> profileVariableRequirements,
        IReadOnlyList<QuestTraderLoyaltyRequirement> loyaltyRequirements,
        string traderId) =>
        new(
            Id: id,
            NameKo: null,
            NameEn: id,
            TraderId: traderId,
            MapId: null,
            WikiUrl: null,
            Experience: null,
            KappaRequired: false,
            LightkeeperRequired: false,
            Disabled: false,
            MinimumPlayerLevel: 1,
            RequiredFaction: null,
            RequiredPrestigeLevel: null,
            TaskRequirements: [],
            TraderStandingRequirements: [],
            TraderLoyaltyRequirements: loyaltyRequirements,
            ProfileVariableRequirementData: profileVariableRequirements);

    private static GameProfileSnapshot CreateProfile(
        int loyalty,
        IReadOnlySet<string> completed,
        IReadOnlyDictionary<string, int>? variables = null,
        string traderId = Prapor,
        GameMode gameMode = GameMode.Regular) =>
        new()
        {
            ProfileId = "profile-a",
            GameMode = gameMode,
            Level = 50,
            Faction = PmcFaction.Usec,
            Traders = new Dictionary<string, TraderProgress>
            {
                [traderId] = new TraderProgress(loyalty, 1m),
            },
            CompletedQuestIds = completed,
            ProfileVariables = variables ?? new Dictionary<string, int>(),
        };
}
