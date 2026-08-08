using JunhyunHelper.Core.Content;

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

        return new ContentValidationResult(issues);
    }

    private static void Fatal(
        ICollection<ContentValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(new ContentValidationIssue(ContentValidationSeverity.Fatal, code, message));
    }
}
