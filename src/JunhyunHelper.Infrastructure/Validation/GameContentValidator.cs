using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Quests;

namespace JunhyunHelper.Infrastructure.Validation;

public enum ContentValidationSeverity
{
    Warning,
    Fatal,
}

public sealed record ContentValidationIssue(
    ContentValidationSeverity Severity,
    string Code,
    string Message);

public sealed record ContentValidationResult(IReadOnlyList<ContentValidationIssue> Issues)
{
    public bool IsValid => Issues.All(issue => issue.Severity != ContentValidationSeverity.Fatal);
}

public sealed class GameContentValidator
{
    public ContentValidationResult Validate(GameContentCatalog content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var issues = new List<ContentValidationIssue>();
        var itemIds = content.Items.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var traderIds = content.Traders.Select(trader => trader.Id).ToHashSet(StringComparer.Ordinal);
        var mapIds = content.Maps.Select(map => map.Id).ToHashSet(StringComparer.Ordinal);
        var questIds = content.Quests.Select(quest => quest.Id).ToHashSet(StringComparer.Ordinal);
        var stationIds = content.HideoutStations.Select(station => station.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var quest in content.Quests)
        {
            var prerequisiteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var requirement in quest.TaskRequirements)
            {
                if (requirement.AcceptedStatuses.Count == 0)
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.status-empty",
                        $"Quest '{quest.Id}' prerequisite '{requirement.RequiredQuestId}' has no accepted status.");
                }

                if (string.Equals(quest.Id, requirement.RequiredQuestId, StringComparison.Ordinal))
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.self",
                        $"Quest '{quest.Id}' references itself as a prerequisite.");
                }

                if (!prerequisiteIds.Add(requirement.RequiredQuestId))
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.duplicate",
                        $"Quest '{quest.Id}' repeats prerequisite quest '{requirement.RequiredQuestId}'.");
                }

                if (!questIds.Contains(requirement.RequiredQuestId))
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.missing",
                        $"Quest '{quest.Id}' references missing prerequisite quest '{requirement.RequiredQuestId}'.");
                }
            }

            if (quest.SpecialTraderAccessRequirement is { } specialAccess)
            {
                if (specialAccess.AcceptedUnlockStatuses.Count == 0)
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.status-empty",
                        $"Quest '{quest.Id}' special trader access has no accepted unlock status.");
                }

                if (!traderIds.Contains(specialAccess.TraderId))
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.trader-missing",
                        $"Quest '{quest.Id}' references missing special trader '{specialAccess.TraderId}'.");
                }

                if (quest.TraderId is not null &&
                    !string.Equals(quest.TraderId, specialAccess.TraderId, StringComparison.Ordinal))
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.trader-mismatch",
                        $"Quest '{quest.Id}' is offered by trader '{quest.TraderId}' but its special access gate targets '{specialAccess.TraderId}'.");
                }

                if (string.Equals(quest.Id, specialAccess.UnlockQuestId, StringComparison.Ordinal))
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.self",
                        $"Quest '{quest.Id}' uses itself as its special trader access unlock quest.");
                }

                if (!questIds.Contains(specialAccess.UnlockQuestId))
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.unlock-missing",
                        $"Quest '{quest.Id}' references missing special trader unlock quest '{specialAccess.UnlockQuestId}'.");
                }

                if (prerequisiteIds.Contains(specialAccess.UnlockQuestId))
                {
                    Fatal(
                        issues,
                        "quest.special-trader-access.duplicate-gate",
                        $"Quest '{quest.Id}' evaluates unlock quest '{specialAccess.UnlockQuestId}' both as a normal prerequisite and as special trader access.");
                }
            }

            foreach (var failureCondition in quest.CompletionFailureConditions)
            {
                if (!questIds.Contains(failureCondition.TriggerQuestId))
                {
                    Fatal(
                        issues,
                        "quest.failure-trigger.missing",
                        $"Quest '{quest.Id}' references missing failure-trigger quest '{failureCondition.TriggerQuestId}'.");
                }
            }

            if (quest.TraderId is not null && !traderIds.Contains(quest.TraderId))
            {
                Fatal(
                    issues,
                    "quest.trader.missing",
                    $"Quest '{quest.Id}' references missing trader '{quest.TraderId}'.");
            }

            if (quest.MapId is not null && !mapIds.Contains(quest.MapId))
            {
                Fatal(
                    issues,
                    "quest.map.missing",
                    $"Quest '{quest.Id}' references missing map '{quest.MapId}'.");
            }

            foreach (var requirement in quest.TraderStandingRequirements)
            {
                if (!traderIds.Contains(requirement.TraderId))
                {
                    Fatal(
                        issues,
                        "quest.trader-standing.missing",
                        $"Quest '{quest.Id}' references missing trader '{requirement.TraderId}' in standing requirement.");
                }
            }

            foreach (var requirement in quest.TraderLoyaltyRequirements)
            {
                if (!traderIds.Contains(requirement.TraderId))
                {
                    Fatal(
                        issues,
                        "quest.trader-loyalty.missing",
                        $"Quest '{quest.Id}' references missing trader '{requirement.TraderId}' in loyalty requirement.");
                }
            }
        }

        ValidateQuestDependencyCycles(content.Quests, questIds, issues);

        foreach (var requirement in content.QuestItemRequirements)
        {
            if (!questIds.Contains(requirement.QuestId))
            {
                Fatal(
                    issues,
                    "quest-item.quest.missing",
                    $"Quest item requirement references missing quest '{requirement.QuestId}'.");
            }

            foreach (var itemId in requirement.AcceptedItemIds)
            {
                if (!itemIds.Contains(itemId))
                {
                    Fatal(
                        issues,
                        "quest-item.item.missing",
                        $"Quest '{requirement.QuestId}' requirement '{requirement.ObjectiveId}' references missing item '{itemId}'.");
                }
            }
        }

        foreach (var station in content.HideoutStations)
        {
            foreach (var level in station.Levels)
            {
                foreach (var requirement in level.ItemRequirements)
                {
                    if (!itemIds.Contains(requirement.ItemId))
                    {
                        Fatal(
                            issues,
                            "hideout-item.item.missing",
                            $"Hideout '{station.Id}' level '{level.Level}' references missing item '{requirement.ItemId}'.");
                    }
                }
            }
        }

        ValidateAmmo(
            content.Ammunition,
            itemIds,
            traderIds,
            stationIds,
            questIds,
            issues);
        ValidateEditions(content.Editions, questIds, issues);

        return new ContentValidationResult(issues);
    }

    private static void ValidateQuestDependencyCycles(
        IReadOnlyList<QuestDefinition> quests,
        IReadOnlySet<string> questIds,
        ICollection<ContentValidationIssue> issues)
    {
        var byId = quests.ToDictionary(quest => quest.Id, StringComparer.Ordinal);
        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<string>();
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var questId in byId.Keys)
            Visit(questId);

        void Visit(string questId)
        {
            if (state.TryGetValue(questId, out var existing) && existing == 2)
                return;
            if (existing == 1)
                return;

            state[questId] = 1;
            stack.Add(questId);

            foreach (var dependencyId in Dependencies(byId[questId]))
            {
                if (!questIds.Contains(dependencyId))
                    continue;

                state.TryGetValue(dependencyId, out var dependencyState);
                if (dependencyState == 0)
                {
                    Visit(dependencyId);
                    continue;
                }

                if (dependencyState != 1)
                    continue;

                var start = stack.IndexOf(dependencyId);
                if (start < 0)
                    continue;

                var cycle = stack.Skip(start).Append(dependencyId).ToArray();
                var key = string.Join("|", cycle.Take(cycle.Length - 1).Order(StringComparer.Ordinal));
                if (reported.Add(key))
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.cycle",
                        $"Quest prerequisite cycle detected: {string.Join(" -> ", cycle)}.");
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[questId] = 2;
        }

        static IEnumerable<string> Dependencies(QuestDefinition quest)
        {
            foreach (var requirement in quest.TaskRequirements)
                yield return requirement.RequiredQuestId;
            if (quest.SpecialTraderAccessRequirement is { } specialAccess)
                yield return specialAccess.UnlockQuestId;
        }
    }

    private static void ValidateAmmo(
        IEnumerable<AmmoDefinition> ammunition,
        IReadOnlySet<string> itemIds,
        IReadOnlySet<string> traderIds,
        IReadOnlySet<string> stationIds,
        IReadOnlySet<string> questIds,
        ICollection<ContentValidationIssue> issues)
    {
        var ammoIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ammo in ammunition)
        {
            if (!ammoIds.Add(ammo.ItemId))
            {
                Fatal(issues, "ammo.duplicate", $"Ammo item '{ammo.ItemId}' appears more than once.");
            }

            if (!itemIds.Contains(ammo.ItemId))
            {
                Fatal(issues, "ammo.item.missing", $"Ammo references missing item '{ammo.ItemId}'.");
            }

            foreach (var acquisition in ammo.Acquisitions)
            {
                if (acquisition.TraderId is not null && !traderIds.Contains(acquisition.TraderId))
                {
                    Fatal(
                        issues,
                        "ammo.trader.missing",
                        $"Ammo '{ammo.ItemId}' acquisition references missing trader '{acquisition.TraderId}'.");
                }

                if (acquisition.StationId is not null && !stationIds.Contains(acquisition.StationId))
                {
                    Fatal(
                        issues,
                        "ammo.station.missing",
                        $"Ammo '{ammo.ItemId}' craft references missing hideout station '{acquisition.StationId}'.");
                }

                if (acquisition.TaskUnlockQuestId is not null &&
                    !questIds.Contains(acquisition.TaskUnlockQuestId))
                {
                    Fatal(
                        issues,
                        "ammo.unlock-quest.missing",
                        $"Ammo '{ammo.ItemId}' acquisition references missing unlock quest '{acquisition.TaskUnlockQuestId}'.");
                }

                if (acquisition.CurrencyItemId is not null &&
                    !itemIds.Contains(acquisition.CurrencyItemId))
                {
                    Fatal(
                        issues,
                        "ammo.currency.missing",
                        $"Ammo '{ammo.ItemId}' purchase references missing currency item '{acquisition.CurrencyItemId}'.");
                }

                foreach (var requirement in acquisition.Requirements)
                {
                    if (!itemIds.Contains(requirement.ItemId))
                    {
                        Fatal(
                            issues,
                            "ammo.requirement-item.missing",
                            $"Ammo '{ammo.ItemId}' acquisition references missing required item '{requirement.ItemId}'.");
                    }
                }
            }
        }
    }

    private static void ValidateEditions(
        IEnumerable<EditionDefinition> editions,
        IReadOnlySet<string> questIds,
        ICollection<ContentValidationIssue> issues)
    {
        var editionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var edition in editions)
        {
            if (string.IsNullOrWhiteSpace(edition.Id) || string.IsNullOrWhiteSpace(edition.Title))
            {
                Fatal(issues, "edition.invalid", "Edition id and title must be non-empty.");
                continue;
            }

            if (!editionIds.Add(edition.Id))
            {
                Fatal(
                    issues,
                    "edition.duplicate",
                    $"Edition '{edition.Id}' appears more than once.");
            }

            foreach (var questId in edition.ExclusiveQuestIds.Intersect(
                         edition.ExcludedQuestIds,
                         StringComparer.Ordinal))
            {
                Fatal(
                    issues,
                    "edition.quest-rule.conflict",
                    $"Edition '{edition.Id}' marks quest '{questId}' as both exclusive and excluded.");
            }

            foreach (var questId in edition.ExclusiveQuestIds.Concat(edition.ExcludedQuestIds).Distinct(StringComparer.Ordinal))
            {
                if (!questIds.Contains(questId))
                {
                    Warning(
                        issues,
                        "edition.quest.missing-in-mode",
                        $"Edition '{edition.Id}' references quest '{questId}' that is absent from this game-mode catalog.");
                }
            }
        }
    }

    private static void Warning(
        ICollection<ContentValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(new ContentValidationIssue(ContentValidationSeverity.Warning, code, message));
    }

    private static void Fatal(
        ICollection<ContentValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(new ContentValidationIssue(ContentValidationSeverity.Fatal, code, message));
    }
}
