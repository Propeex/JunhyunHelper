using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;
using Xunit;

namespace JunhyunHelper.Tests.Quests;

public sealed class QuestTaskPoolVariableCompatibilityTests
{
    private const string Prapor = "54cb50c76803fa8b248b4571";
    private const string PraporLl1Pool = "6a20540cf1b67a977cc5a088";
    private const string PraporLl2Pool = "6a2688488bba18e0b0187a04";

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
    public void Ll1PoolWithoutPublishedSeedRuleRemainsUnknown()
    {
        var quests = Enumerable.Range(1, 8)
            .Select(index => PoolQuest(
                $"ll1-{index}",
                PraporLl1Pool,
                index <= 2 ? 1 : index <= 5 ? 3 : 5))
            .ToArray();
        var profile = CreateProfile(loyalty: 4, completed: new HashSet<string>(StringComparer.Ordinal));

        var enriched = QuestTaskPoolVariableCompatibility.ApplyInferredProfileValues(quests, profile);

        Assert.False(enriched.ProfileVariables.ContainsKey(PraporLl1Pool));
    }

    private static QuestDefinition[] BuildPraporLl2Shape()
    {
        var seeds = Enumerable.Range(1, 4)
            .Select(index => SeedQuest($"seed-{index}", 2));
        var pool = new[]
        {
            PoolQuest("pool-1", PraporLl2Pool, 3),
            PoolQuest("pool-2", PraporLl2Pool, 3),
            PoolQuest("pool-3", PraporLl2Pool, 3),
            PoolQuest("pool-4", PraporLl2Pool, 5),
            PoolQuest("pool-5", PraporLl2Pool, 5),
            PoolQuest("pool-6", PraporLl2Pool, 5),
        };
        return seeds.Concat(pool).ToArray();
    }

    private static QuestDefinition SeedQuest(string id, int loyalty) =>
        CreateQuest(
            id,
            profileVariableRequirements: [],
            loyaltyRequirements: [new QuestTraderLoyaltyRequirement(Prapor, loyalty)]);

    private static QuestDefinition PoolQuest(string id, string variableId, int threshold) =>
        CreateQuest(
            id,
            profileVariableRequirements:
            [
                new QuestProfileVariableRequirement(
                    variableId,
                    threshold,
                    ProfileVariableRequirementOperator.AtLeast),
            ],
            loyaltyRequirements: []);

    private static QuestDefinition CreateQuest(
        string id,
        IReadOnlyList<QuestProfileVariableRequirement> profileVariableRequirements,
        IReadOnlyList<QuestTraderLoyaltyRequirement> loyaltyRequirements) =>
        new(
            Id: id,
            NameKo: null,
            NameEn: id,
            TraderId: Prapor,
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
        IReadOnlyDictionary<string, int>? variables = null) =>
        new()
        {
            ProfileId = "profile-a",
            GameMode = GameMode.Regular,
            Level = 50,
            Faction = PmcFaction.Usec,
            Traders = new Dictionary<string, TraderProgress>
            {
                [Prapor] = new TraderProgress(loyalty, 1m),
            },
            CompletedQuestIds = completed,
            ProfileVariables = variables ?? new Dictionary<string, int>(),
        };
}
