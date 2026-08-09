using JunhyunHelper.Core.Ammo;
using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Editions;
using JunhyunHelper.Core.Maps;

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

        ValidateQuestMapLocations(content, mapIds, questIds, issues);
        ValidateMapMarkers(content.MapMarkers, mapIds, issues);

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

    private static void ValidateQuestMapLocations(
        GameContentCatalog content,
        IReadOnlySet<string> mapIds,
        IReadOnlySet<string> questIds,
        ICollection<ContentValidationIssue> issues)
    {
        var objectiveIds = new HashSet<(string QuestId, string ObjectiveId)>();
        foreach (var objective in content.QuestObjectives)
        {
            if (!questIds.Contains(objective.QuestId))
            {
                Fatal(
                    issues,
                    "quest-objective.quest.missing",
                    $"Quest objective '{objective.ObjectiveId}' references missing quest '{objective.QuestId}'.");
            }

            if (!objectiveIds.Add((objective.QuestId, objective.ObjectiveId)))
            {
                Fatal(
                    issues,
                    "quest-objective.duplicate",
                    $"Quest '{objective.QuestId}' contains duplicate objective '{objective.ObjectiveId}'.");
            }

            foreach (var mapId in objective.MapIds)
            {
                if (!mapIds.Contains(mapId))
                {
                    Fatal(
                        issues,
                        "quest-objective.map.missing",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' references missing map '{mapId}'.");
                }
            }

            foreach (var location in objective.MapLocations)
            {
                if (!mapIds.Contains(location.MapId))
                {
                    Fatal(
                        issues,
                        "quest-objective.location-map.missing",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' location references missing map '{location.MapId}'.");
                }

                if (!IsFinite(location.Position))
                {
                    Fatal(
                        issues,
                        "quest-objective.location.invalid",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' has a non-finite Map position.");
                }

                if (location.Outline.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Z)) ||
                    location.Top is { } top && !double.IsFinite(top) ||
                    location.Bottom is { } bottom && !double.IsFinite(bottom))
                {
                    Fatal(
                        issues,
                        "quest-objective.location-shape.invalid",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' has invalid Map geometry.");
                }
            }
        }
    }

    private static void ValidateMapMarkers(
        IEnumerable<MapMarkerDefinition> markers,
        IReadOnlySet<string> mapIds,
        ICollection<ContentValidationIssue> issues)
    {
        var markerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var marker in markers)
        {
            if (string.IsNullOrWhiteSpace(marker.Id) || !markerIds.Add(marker.Id))
            {
                Fatal(
                    issues,
                    "map-marker.duplicate-or-empty",
                    $"Map marker id '{marker.Id}' is empty or duplicated.");
            }

            if (!mapIds.Contains(marker.MapId))
            {
                Fatal(
                    issues,
                    "map-marker.map.missing",
                    $"Map marker '{marker.Id}' references missing map '{marker.MapId}'.");
            }

            if (!IsFinite(marker.Position) ||
                marker.Outline.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Z)) ||
                marker.Top is { } top && !double.IsFinite(top) ||
                marker.Bottom is { } bottom && !double.IsFinite(bottom))
            {
                Fatal(
                    issues,
                    "map-marker.geometry.invalid",
                    $"Map marker '{marker.Id}' has invalid geometry.");
            }
        }
    }

    private static bool IsFinite(MapWorldPosition position) =>
        double.IsFinite(position.X) &&
        double.IsFinite(position.Y) &&
        double.IsFinite(position.Z);

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
