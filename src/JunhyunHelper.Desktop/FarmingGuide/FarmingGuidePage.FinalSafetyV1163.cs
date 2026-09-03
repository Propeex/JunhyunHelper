using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Final fail-closed boundary for automatic raid advice. Individual planners optimize
    /// different transitions; this validator re-checks explicit user locks and any remaining
    /// destructive economic proof against the actual complete proposed snapshot.
    ///
    /// v1.17 deliberately has no category-based tactical reserve. Food, drink, loose ammo,
    /// magazines and medicine are ordinary economic loot unless the exact item is currently
    /// FIR-needed or the user explicitly locked it.
    /// </summary>
    private RaidRecommendation ApplyFinalRaidSafetyV1163(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        ScannerItemSnapshot decisionScan)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.Discard)
            return recommendation;

        var proposed = recommendation.ProposedSnapshot;
        if (!PreservesExplicitLocksV1163(current, proposed))
            return RejectUnsafeRaidPlanV1163(current);

        if (recommendation.Action == FarmingGuideInstructionAction.Replace)
        {
            var incoming = ToMetrics(decisionScan, adjustAcceptedCount: true);
            var proposedIds = proposed.StoredItems
                .Select(value => value.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var victims = current.StoredItems
                .Where(value => !proposedIds.Contains(value.InstanceId))
                .Select(MetricsForStoredV1163)
                .Where(static value => value is not null)
                .Cast<FarmingGuideLootMetrics>()
                .ToArray();

            // A removed item whose source facts can no longer be resolved is not safe to
            // auto-sacrifice. The economic proof must cover every actual victim. This
            // compatibility guard remains until the v1.17 complete-state optimizer replaces
            // legacy victim-oriented transition paths.
            var removedCount = current.StoredItems.Count(value => !proposedIds.Contains(value.InstanceId));
            if (victims.Length != removedCount ||
                !FarmingGuideLootRetentionPolicy.CanSacrificeFor(incoming, victims))
            {
                return RejectUnsafeRaidPlanV1163(current);
            }
        }

        return recommendation;
    }

    private FarmingGuideLootMetrics? MetricsForStoredV1163(FarmingGuideStoredItemState stored)
    {
        var item = ResolveItem(stored.Item);
        return item is null ? null : MetricsForStoredV1163(stored, item);
    }

    private FarmingGuideLootMetrics MetricsForStoredV1163(
        FarmingGuideStoredItemState stored,
        GameItem item) =>
        MetricsForExistingRulebookV1163(item) with
        {
            Quantity = stored.NormalizedQuantity,
            UnitWeightKg = item.WeightKg,
        };

    /// <summary>
    /// Existing loot uses the same FIR-only special-priority contract as incoming loot.
    /// Scanner's general CurrentNeeded value is presentation truth for the Items feature;
    /// Farming Guide must not reinterpret a non-FIR requirement as protected FIR loot.
    /// </summary>
    private FarmingGuideLootMetrics MetricsForExistingRulebookV1163(GameItem item)
    {
        var snapshot = _raidBridge?.ResolveSnapshot(item.Id);
        if (snapshot is not null)
        {
            var accepted = _acceptedRaidItemCounts.GetValueOrDefault(snapshot.ItemId);
            return new FarmingGuideLootMetrics(
                Math.Max(0, snapshot.CurrentNeededFir - accepted),
                snapshot.TraderSellPrice,
                snapshot.FleaAveragePrice,
                Math.Max(1, snapshot.Slots));
        }

        var slots = Math.Max(1, (item.Width ?? 1) * (item.Height ?? 1));
        return new FarmingGuideLootMetrics(0, item.BasePrice, null, slots);
    }

    private bool PreservesExplicitLocksV1163(
        FarmingGuideLoadoutSnapshot current,
        FarmingGuideLoadoutSnapshot proposed)
    {
        foreach (var slot in _lockedEquipmentSlots)
        {
            current.Equipment.TryGetValue(slot, out var before);
            proposed.Equipment.TryGetValue(slot, out var after);
            if (!SameRootItemV1155(before, after))
                return false;
        }

        foreach (var kind in _lockedCarriers)
        {
            if (!SameRootItemV1155(
                    CarrierStateV1155(current, kind),
                    CarrierStateV1155(proposed, kind)))
            {
                return false;
            }
        }

        if (_lockedItemInstanceIds.Count == 0)
            return true;

        var proposedIds = proposed.StoredItems
            .Select(value => value.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        return current.StoredItems
            .Where(value => _lockedItemInstanceIds.Contains(value.InstanceId))
            .All(value => proposedIds.Contains(value.InstanceId));
    }

    private static RaidRecommendation RejectUnsafeRaidPlanV1163(
        FarmingGuideLoadoutSnapshot current) =>
        new(
            "버리기",
            FarmingGuideInstructionAction.Discard,
            current);
}
