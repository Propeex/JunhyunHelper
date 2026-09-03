using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Quantity-complete v1.17 candidate-pool solve. The outer search chooses which physical
    /// roots exist; for every geometrically legal selected set, the inner exact quantity
    /// optimizer chooses retained units for unlocked stacks under the weight constraint.
    ///
    /// Full-quantity objective values are upper bounds for subset search. When a candidate
    /// must split stacks to satisfy weight, we do not return the first feasible subset: later
    /// subsets can trade a low-value mandatory root for more high-value stack units. Search
    /// therefore continues until every remaining upper bound is no better than the proven
    /// incumbent, or fails closed when either deterministic budget is exhausted.
    /// </summary>
    private bool TryFindBestUnifiedRaidStateWithQuantitiesV1170(
        FarmingGuideLoadoutSnapshot current,
        ScannerItemSnapshot scanned,
        GameItem incoming,
        FarmingGuideOptimizationScore currentScore,
        out RaidRecommendation recommendation,
        out FarmingGuideOptimizationScore score,
        out bool proofComplete)
    {
        if (!TryBuildCurrentOwnedRootsV1170(current, out var currentRoots))
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        var incomingRoot = new GlobalOwnedRootV1170(
            $"{V1170IncomingInstancePrefix}{Guid.NewGuid():N}",
            FarmingGuideItemState.Create(
                incoming.Id,
                raidAcquired: true,
                firStatus: scanned.FirStatus),
            incoming,
            Math.Max(1, scanned.Quantity),
            GlobalRootOriginV1170.Incoming);

        var hasStackChoices = currentRoots.Append(incomingRoot).Any(root => root.Quantity > 1);
        if (!hasStackChoices)
        {
            return TryFindBestUnifiedRaidStateV1170(
                current,
                scanned,
                incoming,
                currentScore,
                out recommendation,
                out score,
                out proofComplete);
        }

        var allSelected = currentRoots.Append(incomingRoot).ToArray();
        var allPacking = TryPackUnifiedSelectionV1170(current, allSelected, out var allProposed);
        if (allPacking == StoredPackingOutcomeV1170.Indeterminate)
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        // If every available unit fits geometrically and the complete full-quantity state is
        // already weight-admissible, retaining everything reaches the theoretical maximum of
        // both non-negative objectives. No destructive alternative can improve it.
        if (allPacking == StoredPackingOutcomeV1170.Found &&
            HasProvableWeightFactsV1170(allProposed) &&
            IsWeightAdmissibleV1170(current, allProposed))
        {
            recommendation = BuildUnifiedRecommendationV1170(
                current,
                allProposed,
                incomingRoot,
                removedCount: 0);
            score = ScoreRaidStateV1170(recommendation.ProposedSnapshot, scanned);
            proofComplete = true;
            return FarmingGuideOptimizationPolicy.IsBetter(score, currentScore);
        }

        // Once quantities must be reduced or a root must be removed, the exact candidate
        // domain and decision facts must be complete. Assemblies remain atomic in the current
        // complete-equipment runtime model; if a detachable assembly tree nevertheless reaches
        // this path, it can change the optimum and therefore forces an indeterminate result.
        if (HasUnmodeledAssemblyChoicesV1170(currentRoots) ||
            currentRoots.Any(root => !root.Fixed && !HasProvableOwnedRootFactsV1170(root)) ||
            !HasProvableIncomingEconomicValueV1170(scanned))
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        RaidRecommendation? best = null;
        var bestScore = currentScore;

        if (allPacking == StoredPackingOutcomeV1170.Found)
        {
            var quantityOutcome = TryOptimizeUnifiedCandidateQuantitiesV1170(
                current,
                allSelected,
                allProposed,
                scanned,
                out var adjusted,
                out var adjustedScore);
            if (quantityOutcome == StoredPackingOutcomeV1170.Indeterminate)
            {
                recommendation = IndeterminateRaidPlanV1170(current);
                score = currentScore;
                proofComplete = false;
                return false;
            }
            if (quantityOutcome == StoredPackingOutcomeV1170.Found &&
                FarmingGuideOptimizationPolicy.IsBetter(adjustedScore, bestScore))
            {
                best = BuildUnifiedRecommendationV1170(
                    current,
                    adjusted,
                    incomingRoot,
                    removedCount: 0);
                bestScore = adjustedScore;
            }
        }

        var victims = currentRoots
            .Where(root => !root.Fixed)
            .Where(HasProvableOwnedRootFactsV1170)
            .OrderBy(root => root.InstanceId, StringComparer.Ordinal)
            .ToArray();
        if (victims.Length == 0)
        {
            recommendation = best ?? ProvenDiscardV1170(current);
            score = bestScore;
            proofComplete = true;
            return best is not null;
        }

        var queue = new PriorityQueue<UnifiedSubsetV1170, UnifiedSubsetPriorityV1170>();
        for (var index = 0; index < victims.Length; index++)
            EnqueueUnifiedSubsetV1170(queue, currentRoots, incomingRoot, scanned, victims, [index]);

        var attempts = 0;
        while (queue.Count > 0 && attempts < V1170MaxUnifiedSubsetAttempts)
        {
            var subset = queue.Dequeue();
            attempts++;

            // Full quantities are an optimistic upper bound for this subset and every
            // descendant that removes still more roots. Once that upper bound cannot improve
            // the proven incumbent, the frontier is globally exhausted for our objective.
            if (!FarmingGuideOptimizationPolicy.IsBetter(subset.Score, bestScore))
            {
                recommendation = best ?? ProvenDiscardV1170(current);
                score = bestScore;
                proofComplete = true;
                return best is not null;
            }

            var removed = subset.VictimIndices
                .Select(index => victims[index].InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var selected = currentRoots
                .Where(root => !removed.Contains(root.InstanceId))
                .Append(incomingRoot)
                .ToArray();

            var packing = TryPackUnifiedSelectionV1170(current, selected, out var proposed);
            if (packing == StoredPackingOutcomeV1170.Indeterminate)
            {
                recommendation = IndeterminateRaidPlanV1170(current);
                score = currentScore;
                proofComplete = false;
                return false;
            }

            if (packing == StoredPackingOutcomeV1170.Found)
            {
                // Reaching the full-quantity upper bound proves this is optimal because the
                // priority queue guarantees no remaining subset has a better upper bound.
                if (HasProvableWeightFactsV1170(proposed) && IsWeightAdmissibleV1170(current, proposed))
                {
                    var fullScore = ScoreRaidStateV1170(proposed, scanned);
                    if (FarmingGuideOptimizationPolicy.IsBetter(fullScore, bestScore))
                    {
                        recommendation = BuildUnifiedRecommendationV1170(
                            current,
                            proposed,
                            incomingRoot,
                            removed.Count);
                        score = fullScore;
                        proofComplete = true;
                        return true;
                    }
                }
                else
                {
                    var quantityOutcome = TryOptimizeUnifiedCandidateQuantitiesV1170(
                        current,
                        selected,
                        proposed,
                        scanned,
                        out var adjusted,
                        out var adjustedScore);
                    if (quantityOutcome == StoredPackingOutcomeV1170.Indeterminate)
                    {
                        recommendation = IndeterminateRaidPlanV1170(current);
                        score = currentScore;
                        proofComplete = false;
                        return false;
                    }
                    if (quantityOutcome == StoredPackingOutcomeV1170.Found &&
                        FarmingGuideOptimizationPolicy.IsBetter(adjustedScore, bestScore))
                    {
                        best = BuildUnifiedRecommendationV1170(
                            current,
                            adjusted,
                            incomingRoot,
                            removed.Count);
                        bestScore = adjustedScore;
                    }
                }
            }

            // Even a geometrically feasible subset can have a suboptimal quantity tradeoff.
            // Its descendants must remain searchable because removing one mandatory root may
            // free weight for more valuable/FIR stack units.
            var last = subset.VictimIndices[^1];
            for (var next = last + 1; next < victims.Length; next++)
            {
                var expanded = new int[subset.VictimIndices.Length + 1];
                Array.Copy(subset.VictimIndices, expanded, subset.VictimIndices.Length);
                expanded[^1] = next;
                EnqueueUnifiedSubsetV1170(queue, currentRoots, incomingRoot, scanned, victims, expanded);
            }
        }

        if (queue.Count > 0)
        {
            recommendation = IndeterminateRaidPlanV1170(current);
            score = currentScore;
            proofComplete = false;
            return false;
        }

        recommendation = best ?? ProvenDiscardV1170(current);
        score = bestScore;
        proofComplete = true;
        return best is not null;
    }

    private StoredPackingOutcomeV1170 TryOptimizeUnifiedCandidateQuantitiesV1170(
        FarmingGuideLoadoutSnapshot current,
        IReadOnlyList<GlobalOwnedRootV1170> selected,
        FarmingGuideLoadoutSnapshot fullQuantityCandidate,
        ScannerItemSnapshot scanned,
        out FarmingGuideLoadoutSnapshot optimized,
        out FarmingGuideOptimizationScore score)
    {
        optimized = current;
        score = ScoreRaidStateV1170(current, scanned);

        if (!HasProvableWeightFactsV1170(fullQuantityCandidate))
            return StoredPackingOutcomeV1170.Indeterminate;

        var variables = selected
            .Where(root => !root.Fixed && root.Quantity > 1)
            .OrderBy(root => root.InstanceId, StringComparer.Ordinal)
            .ToArray();
        if (variables.Length == 0)
            return StoredPackingOutcomeV1170.NoSolution;

        var unitValues = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var root in variables)
        {
            var unitValue = ResolveUnitFleaValueV1170(root.Item.Id);
            if (unitValue is null)
                return StoredPackingOutcomeV1170.Indeterminate;
            unitValues[root.InstanceId] = Math.Max(0, unitValue.Value);
        }

        EnsureWeightSettingsLoadedV1160();
        var currentWeight = CalculateSnapshotWeightKgV1160(current);
        var configuredLimit = FarmingGuideWeightPolicy.MaximumCarryWeightKg(_weightSettingsV1160);
        var effectiveLimit = currentWeight > configuredLimit ? currentWeight : configuredLimit;

        var fullWeight = CalculateSnapshotWeightKgV1160(fullQuantityCandidate);
        var variableFullWeight = variables.Sum(root =>
            Math.Max(0m, root.Item.WeightKg!.Value) * root.Quantity);
        var fixedWeight = Math.Max(0m, fullWeight - variableFullWeight);

        // The stack optimizer's historical parameter names say "RaidAcquired", but v1.17 FIR
        // objective inputs are now populated exclusively from explicit FIR-qualified state.
        var fixedFirQualified = new Dictionary<string, int>(StringComparer.Ordinal);
        var variableIds = variables.Select(root => root.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var root in selected.Where(root =>
                     !variableIds.Contains(root.InstanceId) && root.State.IsFirQualified))
        {
            fixedFirQualified[root.Item.Id] = checked(
                fixedFirQualified.GetValueOrDefault(root.Item.Id) + root.Quantity);
        }

        var quantityVariables = variables.Select(root => new FarmingGuideStackQuantityVariable(
            root.InstanceId,
            root.Item.Id,
            MinimumQuantity: 1,
            MaximumQuantity: root.Quantity,
            RaidAcquired: root.State.IsFirQualified,
            UnitWeightKg: Math.Max(0m, root.Item.WeightKg!.Value),
            UnitEconomicValue: unitValues[root.InstanceId])).ToArray();

        var result = FarmingGuideStackQuantityOptimizer.Optimize(
            quantityVariables,
            fixedWeight,
            effectiveLimit,
            fixedFirQualified,
            itemId => ResolveRemainingFirNeedV1170(itemId, scanned));
        if (result.Status == FarmingGuideStackQuantityOptimizationStatus.BudgetExceeded)
            return StoredPackingOutcomeV1170.Indeterminate;
        if (!result.Found)
            return StoredPackingOutcomeV1170.NoSolution;

        var stored = fullQuantityCandidate.StoredItems
            .Select(value => result.Quantities.TryGetValue(value.InstanceId, out var quantity)
                ? value with { Quantity = quantity }
                : value)
            .ToArray();
        optimized = fullQuantityCandidate with { StoredItems = stored };
        if (!IsWeightAdmissibleV1170(current, optimized))
            return StoredPackingOutcomeV1170.NoSolution;

        score = ScoreRaidStateV1170(optimized, scanned);
        return StoredPackingOutcomeV1170.Found;
    }

    private int ResolveRemainingFirNeedV1170(string itemId, ScannerItemSnapshot currentScan)
    {
        var snapshot = _raidBridge?.ResolveSnapshot(itemId);
        if (snapshot is not null)
            return Math.Max(0, snapshot.CurrentNeededFir);
        return string.Equals(itemId, currentScan.ItemId, StringComparison.Ordinal)
            ? Math.Max(0, currentScan.CurrentNeededFir)
            : 0;
    }

    private long? ResolveUnitFleaValueV1170(string itemId)
    {
        if (_raidFleaAveragePrices.TryGetValue(itemId, out var remembered))
            return Math.Max(0, remembered);
        var value = _raidBridge?.ResolveSnapshot(itemId)?.FleaAveragePrice;
        return value is null ? null : Math.Max(0, value.Value);
    }

    private bool HasProvableWeightFactsV1170(FarmingGuideLoadoutSnapshot snapshot)
    {
        EnsureWeightSettingsLoadedV1160();
        foreach (var pair in snapshot.Equipment)
        {
            if (!FarmingGuideWeightPolicy.EquipmentCountsTowardWeight(pair.Key, _weightSettingsV1160))
                continue;
            if (ResolveItem(pair.Value)?.WeightKg is null)
                return false;
        }
        foreach (var state in new[] { snapshot.Rig, snapshot.Backpack, snapshot.SecureContainer })
        {
            if (state is not null && ResolveItem(state)?.WeightKg is null)
                return false;
        }
        foreach (var stored in snapshot.StoredItems)
        {
            if (ResolveItem(stored.Item)?.WeightKg is null)
                return false;
        }
        return true;
    }
}
