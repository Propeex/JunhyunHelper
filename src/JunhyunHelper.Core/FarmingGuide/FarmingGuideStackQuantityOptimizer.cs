namespace JunhyunHelper.Core.FarmingGuide;

public sealed record FarmingGuideStackQuantityVariable(
    string Key,
    string ItemId,
    int MinimumQuantity,
    int MaximumQuantity,
    bool RaidAcquired,
    decimal UnitWeightKg,
    long UnitEconomicValue);

public enum FarmingGuideStackQuantityOptimizationStatus
{
    Found,
    NoSolution,
    BudgetExceeded,
}

public sealed record FarmingGuideStackQuantityOptimizationResult(
    FarmingGuideStackQuantityOptimizationStatus Status,
    IReadOnlyDictionary<string, int> Quantities,
    int SatisfiedFirUnits,
    long VariableEconomicValue,
    decimal TotalWeightKg)
{
    public bool Found => Status == FarmingGuideStackQuantityOptimizationStatus.Found;
    public bool ProofComplete => Status != FarmingGuideStackQuantityOptimizationStatus.BudgetExceeded;
}

/// <summary>
/// Exact bounded quantity optimizer for already-selected stack roots.
///
/// Presence/geometry is decided by the global packing solver. Every selected stack therefore
/// has a mandatory minimum quantity (normally one), while the remaining units may be split
/// off under Tarkov stack mechanics. The optimizer maximizes the same lexicographic Farming
/// Guide objective for optional units: needed FIR units first, economic value second. A final
/// retained-unit count is only a deterministic no-objective-change tie breaker so equal-value
/// stacks are not split unnecessarily.
///
/// Decimal Tarkov weights are converted exactly to a common integer scale. If an unusual data
/// scale or capacity would make the exact dynamic program exceed its deterministic budget,
/// the result is BudgetExceeded rather than an approximate quantity recommendation.
/// </summary>
public static class FarmingGuideStackQuantityOptimizer
{
    public const int DefaultMaxCapacityStates = 250_000;
    public const int MaximumSupportedWeightScale = 6;

    public static FarmingGuideStackQuantityOptimizationResult Optimize(
        IReadOnlyList<FarmingGuideStackQuantityVariable> variables,
        decimal fixedWeightKg,
        decimal maximumWeightKg,
        IReadOnlyDictionary<string, int> fixedRaidAcquiredUnits,
        Func<string, int> remainingFirNeed,
        int maxCapacityStates = DefaultMaxCapacityStates)
    {
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(fixedRaidAcquiredUnits);
        ArgumentNullException.ThrowIfNull(remainingFirNeed);
        if (maxCapacityStates <= 0)
            return BudgetExceeded();

        fixedWeightKg = Math.Max(0m, fixedWeightKg);
        maximumWeightKg = Math.Max(0m, maximumWeightKg);

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var variable in variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Key) ||
                string.IsNullOrWhiteSpace(variable.ItemId) ||
                !keys.Add(variable.Key) ||
                variable.MinimumQuantity < 1 ||
                variable.MaximumQuantity < variable.MinimumQuantity ||
                variable.UnitWeightKg < 0m ||
                variable.UnitEconomicValue < 0)
            {
                return NoSolution();
            }
        }

        var quantities = variables.ToDictionary(
            variable => variable.Key,
            variable => variable.MinimumQuantity,
            StringComparer.Ordinal);
        var mandatoryAcquired = fixedRaidAcquiredUnits
            .ToDictionary(pair => pair.Key, pair => Math.Max(0, pair.Value), StringComparer.Ordinal);

        var mandatoryWeight = fixedWeightKg;
        long mandatoryValue = 0;
        foreach (var variable in variables)
        {
            mandatoryWeight += variable.UnitWeightKg * variable.MinimumQuantity;
            mandatoryValue = checked(
                mandatoryValue + variable.UnitEconomicValue * variable.MinimumQuantity);
            if (variable.RaidAcquired)
            {
                mandatoryAcquired[variable.ItemId] = checked(
                    mandatoryAcquired.GetValueOrDefault(variable.ItemId) + variable.MinimumQuantity);
            }
        }

        if (mandatoryWeight > maximumWeightKg)
            return NoSolution();

        var mandatoryFir = 0;
        foreach (var itemId in mandatoryAcquired.Keys)
        {
            mandatoryFir = checked(mandatoryFir + Math.Min(
                mandatoryAcquired.GetValueOrDefault(itemId),
                Math.Max(0, remainingFirNeed(itemId))));
        }

        var groups = BuildOptionalGroups(variables, mandatoryAcquired, remainingFirNeed);
        if (groups.Count == 0)
        {
            return new FarmingGuideStackQuantityOptimizationResult(
                FarmingGuideStackQuantityOptimizationStatus.Found,
                quantities,
                mandatoryFir,
                mandatoryValue,
                mandatoryWeight);
        }

        // Weightless optional units cannot hurt feasibility and all objective coefficients are
        // non-negative. Keep all of them; retained count supplies the deterministic tie when
        // both FIR and value are zero.
        var fixedGroupSelections = new int[groups.Count];
        for (var index = 0; index < groups.Count; index++)
        {
            if (groups[index].UnitWeightKg == 0m)
                fixedGroupSelections[index] = groups[index].MaximumCount;
        }

        var weightedGroupIndices = Enumerable.Range(0, groups.Count)
            .Where(index => groups[index].UnitWeightKg > 0m)
            .ToArray();
        var remainingWeight = maximumWeightKg - mandatoryWeight;
        if (!TryBuildIntegerWeights(
                remainingWeight,
                weightedGroupIndices.Select(index => groups[index].UnitWeightKg).ToArray(),
                maxCapacityStates,
                out var capacity,
                out var integerWeights))
        {
            return BudgetExceeded();
        }

        var pseudoItems = new List<PseudoItem>();
        for (var weightedIndex = 0; weightedIndex < weightedGroupIndices.Length; weightedIndex++)
        {
            var groupIndex = weightedGroupIndices[weightedIndex];
            var group = groups[groupIndex];
            var remaining = group.MaximumCount;
            var chunk = 1;
            while (remaining > 0)
            {
                var count = Math.Min(chunk, remaining);
                pseudoItems.Add(new PseudoItem(
                    groupIndex,
                    count,
                    checked(integerWeights[weightedIndex] * count),
                    checked(group.PrimaryPerUnit * count),
                    checked(group.UnitEconomicValue * count)));
                remaining -= count;
                if (chunk <= int.MaxValue / 2)
                    chunk *= 2;
                else
                    chunk = remaining;
            }
        }

        var states = new DpState?[capacity + 1];
        states[0] = new DpState(default, null);
        foreach (var pseudo in pseudoItems)
        {
            if (pseudo.Weight > capacity)
                continue;
            for (var weight = capacity; weight >= pseudo.Weight; weight--)
            {
                var previous = states[weight - pseudo.Weight];
                if (previous is null)
                    continue;

                var candidateScore = previous.Score.Add(
                    pseudo.Primary,
                    pseudo.Value,
                    pseudo.Count);
                var existing = states[weight];
                if (existing is not null && existing.Score.CompareTo(candidateScore) >= 0)
                    continue;

                states[weight] = new DpState(
                    candidateScore,
                    new PathNode(pseudo.GroupIndex, pseudo.Count, previous.Path));
            }
        }

        var bestWeight = 0;
        var best = states[0]!;
        for (var weight = 1; weight < states.Length; weight++)
        {
            var state = states[weight];
            if (state is null)
                continue;
            var compare = state.Score.CompareTo(best.Score);
            if (compare > 0 || (compare == 0 && weight < bestWeight))
            {
                best = state;
                bestWeight = weight;
            }
        }

        var groupSelections = (int[])fixedGroupSelections.Clone();
        for (var node = best.Path; node is not null; node = node.Previous)
        {
            groupSelections[node.GroupIndex] = checked(
                groupSelections[node.GroupIndex] + node.Count);
        }

        ApplySelections(variables, groups, groupSelections, quantities);

        var totalWeight = fixedWeightKg;
        long totalValue = 0;
        var acquired = fixedRaidAcquiredUnits
            .ToDictionary(pair => pair.Key, pair => Math.Max(0, pair.Value), StringComparer.Ordinal);
        foreach (var variable in variables)
        {
            var quantity = quantities[variable.Key];
            totalWeight += variable.UnitWeightKg * quantity;
            totalValue = checked(totalValue + variable.UnitEconomicValue * quantity);
            if (variable.RaidAcquired)
            {
                acquired[variable.ItemId] = checked(
                    acquired.GetValueOrDefault(variable.ItemId) + quantity);
            }
        }

        var fir = 0;
        foreach (var itemId in acquired.Keys)
        {
            fir = checked(fir + Math.Min(
                acquired.GetValueOrDefault(itemId),
                Math.Max(0, remainingFirNeed(itemId))));
        }

        return new FarmingGuideStackQuantityOptimizationResult(
            FarmingGuideStackQuantityOptimizationStatus.Found,
            quantities,
            fir,
            totalValue,
            totalWeight);
    }

    private static List<OptionalGroup> BuildOptionalGroups(
        IReadOnlyList<FarmingGuideStackQuantityVariable> variables,
        IReadOnlyDictionary<string, int> mandatoryAcquired,
        Func<string, int> remainingFirNeed)
    {
        var groups = new List<OptionalGroup>();
        foreach (var itemGroup in variables.GroupBy(variable => variable.ItemId, StringComparer.Ordinal))
        {
            var sample = itemGroup.First();
            var acquiredVariables = itemGroup.Where(variable => variable.RaidAcquired).ToArray();
            var ordinaryVariables = itemGroup.Where(variable => !variable.RaidAcquired).ToArray();
            var acquiredOptional = acquiredVariables.Sum(variable =>
                variable.MaximumQuantity - variable.MinimumQuantity);
            var ordinaryOptional = ordinaryVariables.Sum(variable =>
                variable.MaximumQuantity - variable.MinimumQuantity);
            var remainingNeed = Math.Max(
                0,
                Math.Max(0, remainingFirNeed(itemGroup.Key)) -
                mandatoryAcquired.GetValueOrDefault(itemGroup.Key));
            var primaryCapacity = Math.Min(acquiredOptional, remainingNeed);
            if (primaryCapacity > 0)
            {
                groups.Add(new OptionalGroup(
                    itemGroup.Key,
                    Primary: true,
                    primaryCapacity,
                    sample.UnitWeightKg,
                    sample.UnitEconomicValue));
            }

            var valueOnlyCapacity = checked(
                acquiredOptional - primaryCapacity + ordinaryOptional);
            if (valueOnlyCapacity > 0)
            {
                groups.Add(new OptionalGroup(
                    itemGroup.Key,
                    Primary: false,
                    valueOnlyCapacity,
                    sample.UnitWeightKg,
                    sample.UnitEconomicValue));
            }
        }
        return groups;
    }

    private static void ApplySelections(
        IReadOnlyList<FarmingGuideStackQuantityVariable> variables,
        IReadOnlyList<OptionalGroup> groups,
        IReadOnlyList<int> selections,
        Dictionary<string, int> quantities)
    {
        foreach (var itemGroup in variables
                     .GroupBy(variable => variable.ItemId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var primarySelected = 0;
            var valueSelected = 0;
            for (var index = 0; index < groups.Count; index++)
            {
                if (!string.Equals(groups[index].ItemId, itemGroup.Key, StringComparison.Ordinal))
                    continue;
                if (groups[index].Primary)
                    primarySelected += selections[index];
                else
                    valueSelected += selections[index];
            }

            var acquired = itemGroup
                .Where(variable => variable.RaidAcquired)
                .OrderBy(variable => variable.Key, StringComparer.Ordinal)
                .ToArray();
            foreach (var variable in acquired)
            {
                if (primarySelected <= 0)
                    break;
                var room = variable.MaximumQuantity - quantities[variable.Key];
                var add = Math.Min(room, primarySelected);
                quantities[variable.Key] += add;
                primarySelected -= add;
            }

            var all = itemGroup.OrderBy(variable => variable.Key, StringComparer.Ordinal).ToArray();
            foreach (var variable in all)
            {
                if (valueSelected <= 0)
                    break;
                var room = variable.MaximumQuantity - quantities[variable.Key];
                var add = Math.Min(room, valueSelected);
                quantities[variable.Key] += add;
                valueSelected -= add;
            }
        }
    }

    private static bool TryBuildIntegerWeights(
        decimal capacityKg,
        IReadOnlyList<decimal> weightsKg,
        int maxCapacityStates,
        out int capacity,
        out int[] weights)
    {
        var scale = DecimalScale(Math.Max(0m, capacityKg));
        foreach (var weight in weightsKg)
            scale = Math.Max(scale, DecimalScale(Math.Max(0m, weight)));
        if (scale > MaximumSupportedWeightScale)
        {
            capacity = 0;
            weights = [];
            return false;
        }

        decimal multiplier = 1m;
        for (var index = 0; index < scale; index++)
            multiplier *= 10m;

        long rawCapacity;
        try
        {
            rawCapacity = decimal.ToInt64(decimal.Floor(Math.Max(0m, capacityKg) * multiplier));
        }
        catch (OverflowException)
        {
            capacity = 0;
            weights = [];
            return false;
        }

        var rawWeights = new long[weightsKg.Count];
        long gcd = 0;
        for (var index = 0; index < weightsKg.Count; index++)
        {
            try
            {
                rawWeights[index] = decimal.ToInt64(
                    decimal.Round(weightsKg[index] * multiplier, 0, MidpointRounding.ToEven));
            }
            catch (OverflowException)
            {
                capacity = 0;
                weights = [];
                return false;
            }
            if (rawWeights[index] <= 0)
                continue;
            gcd = gcd == 0 ? rawWeights[index] : GreatestCommonDivisor(gcd, rawWeights[index]);
        }

        if (gcd == 0)
        {
            capacity = 0;
            weights = new int[weightsKg.Count];
            return true;
        }

        var normalizedCapacity = rawCapacity / gcd;
        if (normalizedCapacity > maxCapacityStates || normalizedCapacity > int.MaxValue)
        {
            capacity = 0;
            weights = [];
            return false;
        }

        capacity = (int)normalizedCapacity;
        weights = rawWeights.Select(weight => checked((int)(weight / gcd))).ToArray();
        return true;
    }

    private static int DecimalScale(decimal value) =>
        (decimal.GetBits(value)[3] >> 16) & 0x7F;

    private static long GreatestCommonDivisor(long left, long right)
    {
        left = Math.Abs(left);
        right = Math.Abs(right);
        while (right != 0)
            (left, right) = (right, left % right);
        return left;
    }

    private static FarmingGuideStackQuantityOptimizationResult NoSolution() =>
        new(FarmingGuideStackQuantityOptimizationStatus.NoSolution, new Dictionary<string, int>(), 0, 0, 0m);

    private static FarmingGuideStackQuantityOptimizationResult BudgetExceeded() =>
        new(FarmingGuideStackQuantityOptimizationStatus.BudgetExceeded, new Dictionary<string, int>(), 0, 0, 0m);

    private sealed record OptionalGroup(
        string ItemId,
        bool Primary,
        int MaximumCount,
        decimal UnitWeightKg,
        long UnitEconomicValue)
    {
        public int PrimaryPerUnit => Primary ? 1 : 0;
    }

    private readonly record struct DpScore(
        int Primary,
        long Value,
        int RetainedUnits) : IComparable<DpScore>
    {
        public DpScore Add(int primary, long value, int retainedUnits) =>
            new(
                checked(Primary + primary),
                checked(Value + value),
                checked(RetainedUnits + retainedUnits));

        public int CompareTo(DpScore other)
        {
            var primary = Primary.CompareTo(other.Primary);
            if (primary != 0)
                return primary;
            var value = Value.CompareTo(other.Value);
            if (value != 0)
                return value;
            return RetainedUnits.CompareTo(other.RetainedUnits);
        }
    }

    private sealed record DpState(DpScore Score, PathNode? Path);
    private sealed record PathNode(int GroupIndex, int Count, PathNode? Previous);
    private readonly record struct PseudoItem(
        int GroupIndex,
        int Count,
        int Weight,
        int Primary,
        long Value);
}
