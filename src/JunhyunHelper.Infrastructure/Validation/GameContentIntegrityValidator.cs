using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Infrastructure.Validation;

/// <summary>
/// Product-level semantic validation layered on top of the relationship validator.
/// This guards the canonical snapshot boundary against syntactically valid but unusable
/// or internally inconsistent upstream payloads.
/// </summary>
public sealed class GameContentIntegrityValidator
{
    private readonly GameContentValidator _relationshipValidator;

    public GameContentIntegrityValidator(GameContentValidator? relationshipValidator = null)
    {
        _relationshipValidator = relationshipValidator ?? new GameContentValidator();
    }

    public ContentValidationResult Validate(GameContentCatalog content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var issues = _relationshipValidator.Validate(content).Issues.ToList();
        ValidateRequiredDomains(content, issues);
        ValidateItems(content, issues);
        ValidateTraders(content, issues);
        ValidateMaps(content, issues);
        ValidateQuests(content, issues);
        ValidateQuestObjectives(content, issues);
        ValidateHideout(content, issues);
        ValidateAmmo(content, issues);
        return new ContentValidationResult(issues);
    }

    private static void ValidateRequiredDomains(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        RequireNonEmpty(content.Items, "domain.items.empty", "Item catalog is empty.", issues);
        RequireNonEmpty(content.Traders, "domain.traders.empty", "Trader catalog is empty.", issues);
        RequireNonEmpty(content.Maps, "domain.maps.empty", "Map catalog is empty.", issues);
        RequireNonEmpty(content.Quests, "domain.quests.empty", "Quest catalog is empty.", issues);
        RequireNonEmpty(content.QuestObjectives, "domain.quest-objectives.empty", "Quest objective catalog is empty.", issues);
        RequireNonEmpty(content.QuestItemRequirements, "domain.quest-items.empty", "Quest item requirement catalog is empty.", issues);
        RequireNonEmpty(content.HideoutStations, "domain.hideout.empty", "Hideout station catalog is empty.", issues);
        RequireNonEmpty(content.Ammunition, "domain.ammo.empty", "Ammunition catalog is empty.", issues);
        RequireNonEmpty(content.Editions, "domain.editions.empty", "Edition catalog is empty.", issues);
    }

    private static void ValidateItems(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in content.Items)
        {
            ValidateIdentity(item.Id, item.NameKo, item.NameEn, "item", seen, issues);
            ValidateOptionalWebUrl(item.IconUrl, "item.icon.invalid", $"Item '{item.Id}' has an invalid icon URL.", issues);
            ValidateOptionalWebUrl(item.WikiUrl, "item.wiki.invalid", $"Item '{item.Id}' has an invalid wiki URL.", issues);
        }
    }

    private static void ValidateTraders(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var trader in content.Traders)
            ValidateIdentity(trader.Id, trader.NameKo, trader.NameEn, "trader", seen, issues);
    }

    private static void ValidateMaps(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var map in content.Maps)
            ValidateIdentity(map.Id, map.NameKo, map.NameEn, "map", seen, issues);
    }

    private static void ValidateQuests(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var quest in content.Quests)
        {
            ValidateIdentity(quest.Id, quest.TitleKo, quest.TitleEn, "quest", seen, issues);
            if (quest.MinPlayerLevel is < 0)
            {
                Fatal(issues, "quest.level.negative", $"Quest '{quest.Id}' has negative minimum player level.");
            }
        }
    }

    private static void ValidateQuestObjectives(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var questIds = content.Quests.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        var itemIds = content.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        var mapIds = content.Maps.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal);
        var objectiveKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var objective in content.QuestObjectives)
        {
            if (string.IsNullOrWhiteSpace(objective.QuestId) || string.IsNullOrWhiteSpace(objective.ObjectiveId))
            {
                Fatal(issues, "quest-objective.identity.invalid", "Quest objective has an empty quest/objective id.");
                continue;
            }

            var key = objective.QuestId + "\u001f" + objective.ObjectiveId;
            if (!objectiveKeys.Add(key))
            {
                Fatal(
                    issues,
                    "quest-objective.duplicate",
                    $"Quest '{objective.QuestId}' repeats objective '{objective.ObjectiveId}'.");
            }

            if (!questIds.Contains(objective.QuestId))
            {
                Fatal(
                    issues,
                    "quest-objective.quest.missing",
                    $"Objective '{objective.ObjectiveId}' references missing quest '{objective.QuestId}'.");
            }

            if (objective.Count is <= 0)
            {
                Fatal(
                    issues,
                    "quest-objective.count.nonpositive",
                    $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' has non-positive count '{objective.Count}'.");
            }

            foreach (var itemId in objective.ItemIds)
            {
                if (!itemIds.Contains(itemId))
                {
                    Fatal(
                        issues,
                        "quest-objective.item.missing",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' references missing item '{itemId}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(objective.QuestItemId) && !itemIds.Contains(objective.QuestItemId))
            {
                Fatal(
                    issues,
                    "quest-objective.quest-item.missing",
                    $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' references missing quest item '{objective.QuestItemId}'.");
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
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' has a location on missing map '{location.MapId}'.");
                }

                if (!IsFinite(location.Position.X) ||
                    (location.Position.Height is { } height && !IsFinite(height)) ||
                    !IsFinite(location.Position.Z) ||
                    (location.Top is { } top && !IsFinite(top)) ||
                    (location.Bottom is { } bottom && !IsFinite(bottom)) ||
                    location.Outline.Any(point => !IsFinite(point.X) || !IsFinite(point.Z)))
                {
                    Fatal(
                        issues,
                        "quest-objective.location.invalid",
                        $"Quest '{objective.QuestId}' objective '{objective.ObjectiveId}' has non-finite map geometry.");
                }
            }
        }

        foreach (var requirement in content.QuestItemRequirements)
        {
            var key = requirement.QuestId + "\u001f" + requirement.ObjectiveId;
            if (!objectiveKeys.Contains(key))
            {
                Fatal(
                    issues,
                    "quest-item.objective.missing",
                    $"Quest item requirement references missing objective '{requirement.QuestId}/{requirement.ObjectiveId}'.");
            }
        }
    }

    private static void ValidateHideout(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        var stationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var station in content.HideoutStations)
        {
            ValidateIdentity(station.Id, station.NameKo, station.NameEn, "hideout", stationIds, issues);
            ValidateOptionalWebUrl(
                station.ImageUrl,
                "hideout.image.invalid",
                $"Hideout station '{station.Id}' has an invalid image URL.",
                issues);

            if (station.Levels.Count == 0)
                Fatal(issues, "hideout.levels.empty", $"Hideout station '{station.Id}' has no levels.");

            var levels = new HashSet<int>();
            foreach (var level in station.Levels)
            {
                if (!string.Equals(level.StationId, station.Id, StringComparison.Ordinal))
                {
                    Fatal(
                        issues,
                        "hideout.level.station-mismatch",
                        $"Hideout station '{station.Id}' contains level data for '{level.StationId}'.");
                }
                if (level.Level <= 0)
                    Fatal(issues, "hideout.level.nonpositive", $"Hideout station '{station.Id}' has invalid level '{level.Level}'.");
                if (!levels.Add(level.Level))
                    Fatal(issues, "hideout.level.duplicate", $"Hideout station '{station.Id}' repeats level '{level.Level}'.");
                if (level.ConstructionTimeSeconds is < 0)
                    Fatal(issues, "hideout.time.negative", $"Hideout station '{station.Id}' level '{level.Level}' has negative construction time.");

                foreach (var requirement in level.ItemRequirements)
                {
                    if (!string.Equals(requirement.StationId, station.Id, StringComparison.Ordinal) ||
                        requirement.TargetLevel != level.Level)
                    {
                        Fatal(
                            issues,
                            "hideout-item.level-mismatch",
                            $"Hideout '{station.Id}' level '{level.Level}' has an item requirement bound to another station/level.");
                    }
                }
            }
        }
    }

    private static void ValidateAmmo(
        GameContentCatalog content,
        ICollection<ContentValidationIssue> issues)
    {
        foreach (var ammo in content.Ammunition)
        {
            if (string.IsNullOrWhiteSpace(ammo.Caliber))
                Fatal(issues, "ammo.caliber.empty", $"Ammo '{ammo.ItemId}' has no caliber.");
            if (ammo.ProjectileCount <= 0)
                Fatal(issues, "ammo.projectile-count.nonpositive", $"Ammo '{ammo.ItemId}' has non-positive projectile count.");
            if (ammo.Damage < 0 || ammo.ArmorDamage < 0 || ammo.PenetrationPower < 0)
                Fatal(issues, "ammo.ballistics.negative", $"Ammo '{ammo.ItemId}' has negative ballistic values.");
            if (ammo.FragmentationChance is < 0 or > 1 || ammo.RicochetChance is < 0 or > 1)
                Fatal(issues, "ammo.probability.invalid", $"Ammo '{ammo.ItemId}' has a probability outside 0..1.");
            if (ammo.InitialSpeed < 0)
                Fatal(issues, "ammo.speed.negative", $"Ammo '{ammo.ItemId}' has negative initial speed.");
            if (ammo.ArmorEffectiveness is { IsValid: false })
                Fatal(issues, "ammo.effectiveness.invalid", $"Ammo '{ammo.ItemId}' has invalid armor effectiveness values.");

            foreach (var acquisition in ammo.Acquisitions)
            {
                if (acquisition.RequiredLevel < 0)
                    Fatal(issues, "ammo.acquisition.level.negative", $"Ammo '{ammo.ItemId}' acquisition has negative required level.");
                if (acquisition.OutputCount <= 0)
                    Fatal(issues, "ammo.acquisition.output.nonpositive", $"Ammo '{ammo.ItemId}' acquisition has non-positive output count.");
                if (acquisition.Price is < 0)
                    Fatal(issues, "ammo.acquisition.price.negative", $"Ammo '{ammo.ItemId}' acquisition has negative price.");
                if (acquisition.DurationSeconds is < 0)
                    Fatal(issues, "ammo.acquisition.duration.negative", $"Ammo '{ammo.ItemId}' acquisition has negative duration.");
                if (acquisition.BuyLimit is <= 0)
                    Fatal(issues, "ammo.acquisition.buy-limit.nonpositive", $"Ammo '{ammo.ItemId}' acquisition has non-positive buy limit.");
                foreach (var requirement in acquisition.Requirements)
                {
                    if (requirement.Count <= 0)
                        Fatal(issues, "ammo.requirement.count.nonpositive", $"Ammo '{ammo.ItemId}' acquisition has non-positive requirement count.");
                }
            }
        }
    }

    private static void ValidateIdentity(
        string id,
        string? nameKo,
        string? nameEn,
        string kind,
        ISet<string> seen,
        ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Fatal(issues, $"{kind}.id.empty", $"{kind} has an empty id.");
            return;
        }

        if (!seen.Add(id))
            Fatal(issues, $"{kind}.duplicate", $"{kind} '{id}' appears more than once.");

        if (string.IsNullOrWhiteSpace(nameKo) && string.IsNullOrWhiteSpace(nameEn))
            Fatal(issues, $"{kind}.name.empty", $"{kind} '{id}' has no display name.");
    }

    private static void ValidateOptionalWebUrl(
        string? value,
        string code,
        string message,
        ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            Fatal(issues, code, message);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void RequireNonEmpty<T>(
        IReadOnlyCollection<T> values,
        string code,
        string message,
        ICollection<ContentValidationIssue> issues)
    {
        if (values.Count == 0)
            Fatal(issues, code, message);
    }

    private static void Fatal(
        ICollection<ContentValidationIssue> issues,
        string code,
        string message) =>
        issues.Add(new ContentValidationIssue(ContentValidationSeverity.Fatal, code, message));
}
