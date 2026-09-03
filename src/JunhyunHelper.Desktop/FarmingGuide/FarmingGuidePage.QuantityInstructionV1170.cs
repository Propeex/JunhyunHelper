using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// Adds the quantity operations introduced by the v1.17 exact stack solver to the
    /// existing glance-readable instruction. Placement presentation intentionally remains in
    /// the established v1.15.5 layer; this pass only reports quantity differences that would
    /// otherwise be invisible because the same physical stack instance remains present.
    /// </summary>
    private RaidRecommendation ApplyRaidQuantityInstructionPresentationV1170(
        FarmingGuideLoadoutSnapshot current,
        RaidRecommendation recommendation,
        GameItem incoming,
        int scannedQuantity)
    {
        if (recommendation.Action == FarmingGuideInstructionAction.Indeterminate)
            return recommendation;

        var additions = new List<string>();
        var proposedById = recommendation.ProposedSnapshot.StoredItems
            .ToDictionary(value => value.InstanceId, StringComparer.Ordinal);

        foreach (var before in current.StoredItems)
        {
            if (!proposedById.TryGetValue(before.InstanceId, out var after))
                continue;
            if (after.NormalizedQuantity >= before.NormalizedQuantity)
                continue;

            var removed = before.NormalizedQuantity - after.NormalizedQuantity;
            var item = ResolveItem(before.Item);
            var name = item is null ? "아이템" : DisplayName(item);
            additions.Add($"{name} {removed:N0}개 버리기");
        }

        if (scannedQuantity > 1)
        {
            var currentIds = current.StoredItems
                .Select(value => value.InstanceId)
                .ToHashSet(StringComparer.Ordinal);
            var retainedIncoming = recommendation.ProposedSnapshot.StoredItems
                .FirstOrDefault(value =>
                    !currentIds.Contains(value.InstanceId) &&
                    value.Item.RaidAcquired &&
                    string.Equals(value.Item.ItemId, incoming.Id, StringComparison.Ordinal));
            if (retainedIncoming is not null && retainedIncoming.NormalizedQuantity < scannedQuantity)
            {
                additions.Insert(
                    0,
                    $"새 아이템 {retainedIncoming.NormalizedQuantity:N0}/{scannedQuantity:N0}개만 보관");
            }
        }

        if (additions.Count == 0)
            return recommendation;

        return recommendation with
        {
            Instruction = $"{recommendation.Instruction} + {string.Join(", ", additions)}",
        };
    }
}
