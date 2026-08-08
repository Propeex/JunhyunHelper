using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;

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
            foreach (var requirement in quest.TaskRequirements)
            {
                if (!questIds.Contains(requirement.RequiredQuestId))
                {
                    Fatal(
                        issues,
                        "quest.prerequisite.missing",
                        $"Quest '{quest.Id}' references missing prerequisite quest '{requirement.RequiredQuestId}'.");
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
