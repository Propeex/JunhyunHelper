using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Hideout;
using JunhyunHelper.Core.Profiles;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Core.Items;

public enum CleanupProtectionKind
{
    AlternativeQuestRequirement,
    UnenteredHideoutLevel,
}

public sealed record CleanupProtection(
    string ItemId,
    CleanupProtectionKind Kind,
    string SourceId,
    string? DetailId = null);

public sealed record InventorySurplusItem(
    string ItemId,
    int RequiredTotal,
    int RequiredFir,
    int OwnedFir,
    int OwnedNonFir,
    int SurplusFir,
    int SurplusNonFir,
    IReadOnlyList<ItemRequirementSource> Sources)
{
    public int OwnedTotal => OwnedFir + OwnedNonFir;

    public int SurplusTotal => SurplusFir + SurplusNonFir;
}

public sealed record FutureNeededItemsPlan(
    IReadOnlyList<NeededItem> NeededItems,
    IReadOnlyList<InventorySurplusItem> CleanupItems,
    IReadOnlyList<QuestItemRequirement> AlternativeQuestRequirements,
    IReadOnlyList<CleanupProtection> CleanupProtections,
    IReadOnlyList<string> UnenteredHideoutStationIds,
    IReadOnlyDictionary<string, QuestFutureReachabilityResult> QuestReachability);

/// <summary>
/// The expensive part of Needed Items planning. None of these values depend on the
/// current inventory quantities, so inventory-only mutations can reuse this basis.
/// </summary>
public sealed record FutureNeededItemsBasis(
    IReadOnlyList<ItemRequirement> FixedRequirements,
    IReadOnlyList<QuestItemRequirement> AlternativeQuestRequirements,
    IReadOnlyList<CleanupProtection> CleanupProtections,
    IReadOnlyList<string> UnenteredHideoutStationIds,
    IReadOnlyDictionary<string, QuestFutureReachabilityResult> QuestReachability);

public static class FutureNeededItemsPlanner
{
    public static FutureNeededItemsPlan Calculate(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);

        return Calculate(BuildBasis(content, profile), profile.Inventory);
    }

    public static FutureNeededItemsBasis BuildBasis(
        GameContentCatalog content,
        GameProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profile);

        var reachability = QuestFutureReachabilityEvaluator.Evaluate(
            content.Quests,
            profile,
            content.Editions);

        var futureQuestIds = reachability.Values
            .Where(static result => result.IncludeFutureRequirements)
            .Select(static result => result.QuestId)
            .ToHashSet(StringComparer.Ordinal);

        var questRequirements = content.QuestItemRequirements
            .Where(requirement => futureQuestIds.Contains(requirement.QuestId))
            .ToArray();

        var hideoutRequirements = new List<HideoutItemRequirement>();
        var protections = new List<CleanupProtection>();

        foreach (var station in content.HideoutStations)
        {
            // Product rule: no saved station value means the station is at Lv.0.
            var currentLevel = profile.HideoutLevels.TryGetValue(station.Id, out var savedLevel)
                ? savedLevel
                : 0;

            hideoutRequirements.AddRange(
                station.Levels
                    .Where(level => level.Level > currentLevel)
                    .SelectMany(level => level.ItemRequirements));
        }

        var built = NeededItemRequirementBuilder.Build(questRequirements, hideoutRequirements);

        foreach (var alternative in built.AlternativeQuestRequirements)
        {
            foreach (var itemId in alternative.AcceptedItemIds)
            {
                protections.Add(new CleanupProtection(
                    itemId,
                    CleanupProtectionKind.AlternativeQuestRequirement,
                    alternative.QuestId,
                    alternative.ObjectiveId));
            }
        }

        return new FutureNeededItemsBasis(
            built.FixedRequirements.ToArray(),
            built.AlternativeQuestRequirements,
            protections
                .Distinct()
                .OrderBy(static protection => protection.ItemId, StringComparer.Ordinal)
                .ThenBy(static protection => protection.Kind)
                .ThenBy(static protection => protection.SourceId, StringComparer.Ordinal)
                .ToArray(),
            Array.Empty<string>(),
            reachability);
    }

    public static FutureNeededItemsPlan Calculate(
        FutureNeededItemsBasis basis,
        IReadOnlyDictionary<string, InventoryQuantity> inventory)
    {
        ArgumentNullException.ThrowIfNull(basis);
        ArgumentNullException.ThrowIfNull(inventory);

        var neededItems = NeededItemCalculator.Calculate(basis.FixedRequirements, inventory);
        var protectedItemIds = basis.CleanupProtections
            .Select(static protection => protection.ItemId)
            .ToHashSet(StringComparer.Ordinal);
        var cleanupItems = InventorySurplusCalculator.Calculate(
            basis.FixedRequirements,
            inventory,
            protectedItemIds);

        return new FutureNeededItemsPlan(
            neededItems,
            cleanupItems,
            basis.AlternativeQuestRequirements,
            basis.CleanupProtections,
            basis.UnenteredHideoutStationIds,
            basis.QuestReachability);
    }
}

public static class InventorySurplusCalculator
{
    public static IReadOnlyList<InventorySurplusItem> Calculate(
        IEnumerable<ItemRequirement> requirements,
        IReadOnlyDictionary<string, InventoryQuantity> inventory,
        IReadOnlySet<string>? protectedItemIds = null)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(inventory);

        protectedItemIds ??= new HashSet<string>(StringComparer.Ordinal);

        var groupedRequirements = requirements
            .Select(static requirement => requirement.Normalize())
            .Where(static requirement =>
                !string.IsNullOrWhiteSpace(requirement.ItemId) && requirement.RequiredTotal > 0)
            .GroupBy(static requirement => requirement.ItemId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.Ordinal);

        var cleanup = new List<InventorySurplusItem>();
        foreach (var (itemId, rawOwned) in inventory)
        {
            if (string.IsNullOrWhiteSpace(itemId) || protectedItemIds.Contains(itemId))
                continue;

            var owned = rawOwned.Normalize();
            if (owned.Total <= 0)
                continue;

            var itemRequirements = groupedRequirements.TryGetValue(itemId, out var found)
                ? found
                : Array.Empty<ItemRequirement>();

            var requiredTotal = itemRequirements.Sum(static requirement => requirement.RequiredTotal);
            var requiredFir = itemRequirements.Sum(static requirement => requirement.RequiredFir);
            var unrestrictedRequired = Math.Max(0, requiredTotal - requiredFir);

            var usefulNonFir = Math.Min(owned.NonFir, unrestrictedRequired);
            var surplusNonFir = Math.Max(0, owned.NonFir - unrestrictedRequired);
            var firNeededForUnrestricted = Math.Max(0, unrestrictedRequired - usefulNonFir);
            var usefulFir = requiredFir + firNeededForUnrestricted;
            var surplusFir = Math.Max(0, owned.Fir - usefulFir);

            if (surplusFir + surplusNonFir <= 0)
                continue;

            cleanup.Add(new InventorySurplusItem(
                itemId,
                requiredTotal,
                requiredFir,
                owned.Fir,
                owned.NonFir,
                surplusFir,
                surplusNonFir,
                itemRequirements.Select(static requirement => requirement.Source).ToArray()));
        }

        return cleanup
            .OrderBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
    }
}
