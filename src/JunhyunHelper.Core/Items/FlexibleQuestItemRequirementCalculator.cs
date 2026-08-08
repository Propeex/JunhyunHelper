using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Core.Items;

public sealed record FlexibleQuestItemProgress(
    string QuestId,
    string ObjectiveId,
    IReadOnlyList<string> AcceptedItemIds,
    int RequiredTotal,
    int RequiredFir,
    int OwnedFir,
    int OwnedNonFir,
    int RemainingTotal,
    int RemainingFir)
{
    public int OwnedTotal => OwnedFir + OwnedNonFir;

    public bool IsFulfilled => RemainingTotal == 0 && RemainingFir == 0;
}

/// <summary>
/// Calculates progress for a single quest hand-in objective that accepts more than one
/// item id. Accepted items are interchangeable inputs to one objective; no user choice is
/// persisted. Per-item cleanup remains protected elsewhere because independent cleanup
/// amounts are not additive for an interchangeable group.
/// </summary>
public static class FlexibleQuestItemRequirementCalculator
{
    public static FlexibleQuestItemProgress Calculate(
        QuestItemRequirement requirement,
        IReadOnlyDictionary<string, InventoryQuantity>? inventory = null)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.Count <= 0)
        {
            throw new InvalidDataException(
                $"Quest '{requirement.QuestId}' objective '{requirement.ObjectiveId}' has invalid count '{requirement.Count}'.");
        }

        var acceptedIds = requirement.AcceptedItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (acceptedIds.Length != requirement.AcceptedItemIds.Count || acceptedIds.Length < 2)
        {
            throw new InvalidDataException(
                $"Quest '{requirement.QuestId}' objective '{requirement.ObjectiveId}' is not a valid flexible item requirement.");
        }

        inventory ??= new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);

        var ownedFir = 0;
        var ownedNonFir = 0;
        foreach (var itemId in acceptedIds)
        {
            if (!inventory.TryGetValue(itemId, out var rawQuantity))
                continue;

            var quantity = rawQuantity.Normalize();
            ownedFir += quantity.Fir;
            ownedNonFir += quantity.NonFir;
        }

        var requiredTotal = requirement.Count;
        var requiredFir = requirement.FoundInRaid ? requirement.Count : 0;

        var firSatisfied = Math.Min(ownedFir, requiredFir);
        var remainingFir = requiredFir - firSatisfied;

        var unrestrictedRequired = requiredTotal - requiredFir;
        var unrestrictedAvailable = ownedNonFir + Math.Max(0, ownedFir - requiredFir);
        var unrestrictedSatisfied = Math.Min(unrestrictedRequired, unrestrictedAvailable);
        var remainingUnrestricted = unrestrictedRequired - unrestrictedSatisfied;

        return new FlexibleQuestItemProgress(
            requirement.QuestId,
            requirement.ObjectiveId,
            acceptedIds,
            requiredTotal,
            requiredFir,
            ownedFir,
            ownedNonFir,
            remainingFir + remainingUnrestricted,
            remainingFir);
    }
}
