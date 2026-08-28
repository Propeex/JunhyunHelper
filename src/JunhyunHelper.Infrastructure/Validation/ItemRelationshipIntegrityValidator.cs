using JunhyunHelper.Core.Content;
using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Infrastructure.Validation;

public sealed class ItemRelationshipIntegrityValidator
{
    public ContentValidationResult Validate(GameContentCatalog content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var issues = new List<ContentValidationIssue>();
        var items = content.Items.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var traders = content.Traders.Select(trader => trader.Id).ToHashSet(StringComparer.Ordinal);
        var stations = content.HideoutStations.Select(station => station.Id).ToHashSet(StringComparer.Ordinal);
        var quests = content.Quests.Select(quest => quest.Id).ToHashSet(StringComparer.Ordinal);
        var relationships = content.ItemRelationships;

        var purchaseKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var purchase in relationships.TraderPurchases)
        {
            RequireItem(items, purchase.ItemId, "item-relationship.purchase.item.missing", "Trader purchase", issues);
            RequireTrader(traders, purchase.TraderId, "item-relationship.purchase.trader.missing", "Trader purchase", issues);
            RequireLevel(purchase.RequiredLevel, "item-relationship.purchase.level.negative", "Trader purchase", issues);
            RequireQuest(quests, purchase.TaskUnlockQuestId, "item-relationship.purchase.quest.missing", "Trader purchase", issues);
            RequirePositive(purchase.Price ?? 0, "item-relationship.purchase.price.nonpositive", "Trader purchase", issues);
            RequireItem(items, purchase.CurrencyItemId ?? string.Empty, "item-relationship.purchase.currency.missing", "Trader purchase", issues);
            RequireOptionalNonNegative(purchase.BuyLimit, "item-relationship.purchase.limit.negative", "Trader purchase", issues);

            var key = $"{purchase.ItemId}\u001f{purchase.TraderId}\u001f{purchase.RequiredLevel}\u001f{purchase.TaskUnlockQuestId}\u001f{purchase.Price}\u001f{purchase.CurrencyItemId}";
            if (!purchaseKeys.Add(key))
                Fatal(issues, "item-relationship.purchase.duplicate", $"Duplicate trader purchase relationship '{key}'.");
        }

        var barterIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var barter in relationships.Barters)
        {
            if (string.IsNullOrWhiteSpace(barter.Id) || !barterIds.Add(barter.Id))
                Fatal(issues, "item-relationship.barter.identity.invalid", $"Barter relationship has invalid/duplicate id '{barter.Id}'.");
            RequireTrader(traders, barter.TraderId, "item-relationship.barter.trader.missing", $"Barter '{barter.Id}'", issues);
            RequireLevel(barter.RequiredLevel, "item-relationship.barter.level.negative", $"Barter '{barter.Id}'", issues);
            RequireItem(items, barter.ProductItemId, "item-relationship.barter.product.missing", $"Barter '{barter.Id}'", issues);
            RequirePositive(barter.ProductCount, "item-relationship.barter.product.nonpositive", $"Barter '{barter.Id}' product", issues);
            RequireQuest(quests, barter.TaskUnlockQuestId, "item-relationship.barter.quest.missing", $"Barter '{barter.Id}'", issues);
            RequireOptionalNonNegative(barter.BuyLimit, "item-relationship.barter.limit.negative", $"Barter '{barter.Id}'", issues);
            ValidateIngredients(items, barter.RequiredItems, $"Barter '{barter.Id}'", "barter", issues);
        }

        var craftIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var craft in relationships.Crafts)
        {
            if (string.IsNullOrWhiteSpace(craft.Id) || !craftIds.Add(craft.Id))
                Fatal(issues, "item-relationship.craft.identity.invalid", $"Craft relationship has invalid/duplicate id '{craft.Id}'.");
            if (!stations.Contains(craft.StationId))
                Fatal(issues, "item-relationship.craft.station.missing", $"Craft '{craft.Id}' references missing station '{craft.StationId}'.");
            RequireLevel(craft.RequiredLevel, "item-relationship.craft.level.negative", $"Craft '{craft.Id}'", issues);
            RequireItem(items, craft.ProductItemId, "item-relationship.craft.product.missing", $"Craft '{craft.Id}'", issues);
            RequirePositive(craft.ProductCount, "item-relationship.craft.product.nonpositive", $"Craft '{craft.Id}' product", issues);
            RequireQuest(quests, craft.TaskUnlockQuestId, "item-relationship.craft.quest.missing", $"Craft '{craft.Id}'", issues);
            RequireOptionalNonNegative(craft.DurationSeconds, "item-relationship.craft.duration.negative", $"Craft '{craft.Id}'", issues);
            ValidateIngredients(items, craft.RequiredItems, $"Craft '{craft.Id}'", "craft", issues);
        }

        var fleaIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var itemId in relationships.FleaMarketItemIds)
        {
            RequireItem(items, itemId, "item-relationship.flea.item.missing", "Flea market relationship", issues);
            if (!fleaIds.Add(itemId))
                Fatal(issues, "item-relationship.flea.duplicate", $"Flea market item '{itemId}' appears more than once.");
        }

        return new ContentValidationResult(issues);
    }

    private static void ValidateIngredients(IReadOnlySet<string> itemIds, IReadOnlyList<ItemIngredient> ingredients,
        string owner, string kind, ICollection<ContentValidationIssue> issues)
    {
        if (ingredients.Count == 0)
        {
            Fatal(issues, $"item-relationship.{kind}.requirements.empty", $"{owner} has no required items.");
            return;
        }
        foreach (var ingredient in ingredients)
        {
            RequireItem(itemIds, ingredient.ItemId, $"item-relationship.{kind}.requirement.missing", owner, issues);
            RequirePositive(ingredient.Count, $"item-relationship.{kind}.requirement.nonpositive", $"{owner} requirement '{ingredient.ItemId}'", issues);
        }
    }

    private static void RequireItem(IReadOnlySet<string> ids, string itemId, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(itemId) || !ids.Contains(itemId)) Fatal(issues, code, $"{owner} references missing item '{itemId}'.");
    }
    private static void RequireTrader(IReadOnlySet<string> ids, string traderId, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(traderId) || !ids.Contains(traderId)) Fatal(issues, code, $"{owner} references missing trader '{traderId}'.");
    }
    private static void RequireQuest(IReadOnlySet<string> ids, string? questId, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (questId is not null && !ids.Contains(questId)) Fatal(issues, code, $"{owner} references missing unlock quest '{questId}'.");
    }
    private static void RequireLevel(int level, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (level < 0) Fatal(issues, code, $"{owner} has negative required level '{level}'.");
    }
    private static void RequireOptionalNonNegative(int? value, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (value < 0) Fatal(issues, code, $"{owner} has negative value '{value}'.");
    }
    private static void RequirePositive(decimal value, string code, string owner, ICollection<ContentValidationIssue> issues)
    {
        if (value <= 0) Fatal(issues, code, $"{owner} has non-positive value '{value}'.");
    }
    private static void Fatal(ICollection<ContentValidationIssue> issues, string code, string message) =>
        issues.Add(new ContentValidationIssue(ContentValidationSeverity.Fatal, code, message));
}
