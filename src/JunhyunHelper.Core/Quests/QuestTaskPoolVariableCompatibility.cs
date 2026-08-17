using JunhyunHelper.Core.Profiles;

namespace JunhyunHelper.Core.Quests;

/// <summary>
/// Current-version compatibility for the 27 EFT 1.1 trader side-task pool counters.
///
/// Public task data exposes only the read-side condition (variable >= threshold), not
/// the server write rule. A 2026-08-17 live audit established a stable trader-local
/// LL1→LL4 staged structure (Ragman currently has three stages) and direct LL2–LL4
/// seed batches. We use that exact audited structure only while it continues to match.
///
/// Exact profile variable values always win. Synthetic values are runtime-only and are
/// never persisted back into user.db. Any structural drift fails closed.
/// </summary>
public sealed class QuestTaskPoolVariableCompatibility
{
    private const string Prapor = "54cb50c76803fa8b248b4571";
    private const string Therapist = "54cb57776803fa99248b456e";
    private const string Skier = "58330581ace78e27b8b10cee";
    private const string Peacekeeper = "5935c25fb3acc3127c3d8cd9";
    private const string Mechanic = "5a7c2eca46aef81a7ca2145d";
    private const string Ragman = "5ac3b934156ae10c4430e83c";
    private const string Jaeger = "5c0647fdd443bc2504c2d371";

    private static readonly IReadOnlyDictionary<string, PoolRule> Rules = BuildRules();

    private readonly GameProfileSnapshot _profile;
    private readonly IReadOnlyList<QuestDefinition> _quests;
    private readonly IReadOnlyDictionary<string, QuestDefinition[]> _poolQuests;
    private readonly HashSet<string> _validPools = new(StringComparer.Ordinal);

    public QuestTaskPoolVariableCompatibility(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(quests);
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _quests = quests.ToArray();
        _poolQuests = _quests
            .SelectMany(quest => quest.ProfileVariableRequirements.Select(requirement => (quest, requirement.VariableId)))
            .Where(entry => Rules.ContainsKey(entry.VariableId))
            .GroupBy(entry => entry.VariableId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.quest).DistinctBy(quest => quest.Id).ToArray(),
                StringComparer.Ordinal);

        foreach (var (variableId, rule) in Rules)
        {
            if (ValidatePool(variableId, rule))
                _validPools.Add(variableId);
        }
    }

    /// <summary>
    /// Adds only the task-pool values that can be reconstructed by the audited LL2–LL4
    /// model. Exact imported/profile values are preserved and take precedence.
    /// </summary>
    public static GameProfileSnapshot ApplyInferredProfileValues(
        IEnumerable<QuestDefinition> quests,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(quests);
        ArgumentNullException.ThrowIfNull(profile);

        var questList = quests.ToArray();
        var resolver = new QuestTaskPoolVariableCompatibility(questList, profile);
        var values = new Dictionary<string, int>(profile.ProfileVariables, StringComparer.Ordinal);
        var changed = false;

        foreach (var variableId in questList
                     .SelectMany(quest => quest.ProfileVariableRequirements)
                     .Select(requirement => requirement.VariableId)
                     .Distinct(StringComparer.Ordinal))
        {
            if (values.ContainsKey(variableId) || !resolver.TryInferCurrentValue(variableId, out var inferredValue))
                continue;

            values[variableId] = inferredValue;
            changed = true;
        }

        return changed ? profile with { ProfileVariables = values } : profile;
    }

    public bool TryEvaluate(QuestProfileVariableRequirement requirement, out bool satisfied)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        satisfied = false;

        if (requirement.Operator != ProfileVariableRequirementOperator.AtLeast ||
            !Rules.TryGetValue(requirement.VariableId, out var rule) ||
            !_validPools.Contains(requirement.VariableId) ||
            !_poolQuests.TryGetValue(requirement.VariableId, out var pool))
        {
            return false;
        }

        if (TryInferCurrentValue(requirement.VariableId, out var currentValue))
        {
            satisfied = currentValue >= requirement.RequiredValue;
            return true;
        }

        // The LL1 seed/write rule is not published. A completed gated quest is still a
        // conservative witness that its own threshold was reached; this can prove true
        // for equal/lower thresholds, but never prove false for a higher one.
        if (rule.LoyaltyLevel == 1)
        {
            var witnessedThreshold = pool
                .Where(quest => _profile.CompletedQuestIds.Contains(quest.Id))
                .SelectMany(quest => quest.ProfileVariableRequirements)
                .Where(candidate => string.Equals(candidate.VariableId, requirement.VariableId, StringComparison.Ordinal))
                .Select(candidate => candidate.RequiredValue)
                .DefaultIfEmpty(0)
                .Max();

            if (witnessedThreshold >= requirement.RequiredValue)
            {
                satisfied = true;
                return true;
            }
        }

        return false;
    }

    private bool TryInferCurrentValue(string variableId, out int inferredValue)
    {
        inferredValue = 0;
        if (!Rules.TryGetValue(variableId, out var rule) ||
            rule.LoyaltyLevel == 1 ||
            !_validPools.Contains(variableId) ||
            !_poolQuests.TryGetValue(variableId, out var pool) ||
            !_profile.Traders.TryGetValue(rule.TraderId, out var trader) ||
            trader.LoyaltyLevel is not { } currentLoyalty)
        {
            return false;
        }

        // Under the audited current-version stage mapping, future LL pools have not
        // begun yet, so zero is the deterministic counter value for this model.
        if (currentLoyalty < rule.LoyaltyLevel)
        {
            inferredValue = 0;
            return true;
        }

        var seedQuests = FindSeedQuests(rule);
        if (seedQuests.Length != rule.ExpectedSeedQuestCount)
            return false;

        inferredValue = seedQuests.Count(quest => _profile.CompletedQuestIds.Contains(quest.Id)) +
                        pool.Count(quest => _profile.CompletedQuestIds.Contains(quest.Id));
        return true;
    }

    private bool ValidatePool(string variableId, PoolRule rule)
    {
        if (!_poolQuests.TryGetValue(variableId, out var pool) || pool.Length != rule.ExpectedQuestCount)
            return false;

        if (pool.Any(quest =>
                !string.Equals(quest.TraderId, rule.TraderId, StringComparison.Ordinal) ||
                quest.TaskRequirements.Count != 0 ||
                quest.TraderLoyaltyRequirements.Count != 0 ||
                quest.TraderStandingRequirements.Count != 0 ||
                quest.UnsupportedAvailabilityRequirements.Count != 0 ||
                quest.ProfileVariableRequirements.Count != 1 ||
                !string.Equals(quest.ProfileVariableRequirements[0].VariableId, variableId, StringComparison.Ordinal) ||
                quest.ProfileVariableRequirements[0].Operator != ProfileVariableRequirementOperator.AtLeast))
        {
            return false;
        }

        var thresholds = pool
            .Select(quest => quest.ProfileVariableRequirements[0].RequiredValue)
            .Distinct()
            .Order()
            .ToArray();
        if (!thresholds.SequenceEqual(rule.ExpectedThresholds))
            return false;

        if (rule.LoyaltyLevel > 1)
        {
            var seeds = FindSeedQuests(rule);
            if (seeds.Length != rule.ExpectedSeedQuestCount ||
                seeds.Length < rule.ExpectedThresholds.Min())
            {
                return false;
            }
        }

        return true;
    }

    private QuestDefinition[] FindSeedQuests(PoolRule rule) =>
        _quests
            .Where(quest =>
                string.Equals(quest.TraderId, rule.TraderId, StringComparison.Ordinal) &&
                quest.ProfileVariableRequirements.Count == 0 &&
                quest.TraderLoyaltyRequirements.Any(requirement =>
                    string.Equals(requirement.TraderId, rule.TraderId, StringComparison.Ordinal) &&
                    requirement.RequiredLoyaltyLevel == rule.LoyaltyLevel))
            .ToArray();

    private static IReadOnlyDictionary<string, PoolRule> BuildRules()
    {
        var rules = new Dictionary<string, PoolRule>(StringComparer.Ordinal);
        Add(rules, "6a20540cf1b67a977cc5a088", Prapor, 1, 8, [1, 3, 5]);
        Add(rules, "6a2688488bba18e0b0187a04", Prapor, 2, 6, [3, 5], 4);
        Add(rules, "6a32651a811905ed0cac0973", Prapor, 3, 6, [1, 3], 5);
        Add(rules, "6a326525789ae12ecb0b2807", Prapor, 4, 5, [1, 2], 4);

        Add(rules, "6a4e4ab3ecd1145894d00990", Therapist, 1, 6, [1, 2, 4]);
        Add(rules, "6a4e4aed3ded7a18126603f6", Therapist, 2, 6, [1, 2, 4], 4);
        Add(rules, "6a4e4b28629dc64c4001967c", Therapist, 3, 5, [1, 3], 4);
        Add(rules, "6a56925b1c30ba5a77c7c518", Therapist, 4, 1, [1], 3);

        Add(rules, "6a59f3ba06c8949abad30871", Skier, 1, 8, [1, 2, 3]);
        Add(rules, "6a5a111de1f417ac80a163e5", Skier, 2, 9, [1, 3, 4], 3);
        Add(rules, "6a5a115181116e807b55f258", Skier, 3, 6, [1, 3], 3);
        Add(rules, "6a5a1192efde11cc7105b18f", Skier, 4, 2, [1], 4);

        Add(rules, "6a5ba40fe5c4eaef5610f232", Peacekeeper, 1, 6, [1, 3]);
        Add(rules, "6a5ba450a7851e16ce0bde44", Peacekeeper, 2, 9, [1, 3, 5], 4);
        Add(rules, "6a5ba48b8cfd0bddb3d4d2e1", Peacekeeper, 3, 4, [2, 4], 5);
        Add(rules, "6a5ba4c57cbb93b629051591", Peacekeeper, 4, 7, [1, 3], 3);

        Add(rules, "6a3171c927ca9591bf4db1c4", Mechanic, 1, 6, [1, 3]);
        Add(rules, "6a3c0fefbea2d2ad581c090b", Mechanic, 2, 10, [1, 3, 5], 4);
        Add(rules, "6a3cf95c6b35530c4a4f532e", Mechanic, 3, 12, [1, 3, 5], 5);
        Add(rules, "6a3d1c0990e9ffe15463e961", Mechanic, 4, 2, [1], 5);

        Add(rules, "6a4b339f18db62e03b4f7ded", Ragman, 1, 6, [1, 2]);
        Add(rules, "6a4b4e6a30dac4b01af220aa", Ragman, 2, 7, [1, 2, 4], 4);
        Add(rules, "6a4b9c9a60b56d421cceea18", Ragman, 3, 3, [1, 2], 5);

        Add(rules, "6a43a01ccc83aceedd35f09c", Jaeger, 1, 8, [1, 3]);
        Add(rules, "6a43a095bfef0cd74c298963", Jaeger, 2, 4, [2, 5], 6);
        Add(rules, "6a43a13633c97d216dfc85de", Jaeger, 3, 7, [2, 4], 8);
        Add(rules, "6a43a16dde81644a7951f31b", Jaeger, 4, 3, [1], 5);
        return rules;
    }

    private static void Add(
        IDictionary<string, PoolRule> rules,
        string variableId,
        string traderId,
        int loyaltyLevel,
        int expectedQuestCount,
        int[] thresholds,
        int expectedSeedQuestCount = 0) =>
        rules.Add(variableId, new PoolRule(
            traderId,
            loyaltyLevel,
            expectedQuestCount,
            thresholds,
            expectedSeedQuestCount));

    private sealed record PoolRule(
        string TraderId,
        int LoyaltyLevel,
        int ExpectedQuestCount,
        int[] ExpectedThresholds,
        int ExpectedSeedQuestCount);
}
