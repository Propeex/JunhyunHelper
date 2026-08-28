using System.Windows.Media;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerItemSearchHit(string ItemId, string OfficialName, ImageSource? Icon, string? WikiUrl);

public sealed record ScannerItemBasicDetails(
    string TypeName,
    int Width,
    int Height,
    decimal? WeightKg,
    bool? FleaTradable,
    int? BasePrice);

public sealed record ScannerItemSearchDetails(
    ScannerItemSnapshot Snapshot,
    string? WikiUrl,
    ScannerItemBasicDetails Basic,
    ScannerItemRelationshipDetails? Relationships = null);

public sealed partial class ScannerCoordinator
{
    public IReadOnlyList<ScannerItemSearchHit> SearchItems(string? query, int maximumResults = 20)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = query?.Trim() ?? string.Empty;
        if (text.Length == 0 || maximumResults <= 0)
            return [];

        var context = GetContext();
        if (context is null || _catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
            return [];

        var contentById = CreateContentById(context.Content.Items);

        return _catalog.GetItemsSnapshot()
            .Select(item => new { Item = item, Rank = SearchRank(item.OfficialName, item.ShortName, text) })
            .Where(entry => entry.Rank < int.MaxValue)
            .OrderBy(entry => entry.Rank)
            .ThenBy(entry => entry.Item.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(maximumResults, 1, 50))
            .Select(entry => CreateSearchHit(entry.Item, contentById)).ToArray();
    }

    /// <summary>
    /// Resolves current-mode presentation for a canonical saved item ID without building
    /// expensive quest/hideout/craft relationship details. Favorites and recent-history
    /// lists use this path because their persisted authority is identity/order only.
    /// </summary>
    public ScannerItemSearchHit? GetSearchItemHit(string? itemId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var context = GetContext();
        if (context is null || _catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
            return null;

        var normalizedItemId = itemId.Trim();
        var catalogItem = _catalog.GetItemsSnapshot().FirstOrDefault(item =>
            string.Equals(item.Id, normalizedItemId, StringComparison.Ordinal));
        if (catalogItem is null)
            return null;

        return CreateSearchHit(catalogItem, CreateContentById(context.Content.Items));
    }

    public ScannerItemSearchDetails? GetSearchItemDetails(string? itemId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var context = GetContext();
        if (context is null || _catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
            return null;

        var normalizedItemId = itemId.Trim();
        var snapshot = Presentation.CreateSnapshot(normalizedItemId);
        if (snapshot is null)
            return null;

        var canonical = context.Content.Items.FirstOrDefault(item =>
            string.Equals(item.Id, snapshot.ItemId, StringComparison.Ordinal));
        _catalog.TryGetItem(snapshot.ItemId, out var catalogItem);
        var width = canonical?.Width ?? catalogItem?.Width ?? 0;
        var height = canonical?.Height ?? catalogItem?.Height ?? 0;
        var basic = new ScannerItemBasicDetails(
            ResolveItemType(canonical),
            width,
            height,
            canonical?.WeightKg,
            canonical?.FleaTradable,
            canonical?.BasePrice);

        return new ScannerItemSearchDetails(
            snapshot,
            canonical?.WikiUrl,
            basic,
            BuildItemRelationshipDetails(context, snapshot.ItemId));
    }

    private static IReadOnlyDictionary<string, GameItem> CreateContentById(IEnumerable<GameItem> items) =>
        items.Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    private ScannerItemSearchHit CreateSearchHit(ScannerCatalogItem item, IReadOnlyDictionary<string, GameItem> contentById)
    {
        contentById.TryGetValue(item.Id, out var canonicalItem);
        return new ScannerItemSearchHit(
            item.Id,
            item.OfficialName,
            _icons.Load($"item-{item.Id}", canonicalItem?.IconUrl ?? item.IconUrl),
            canonicalItem?.WikiUrl);
    }

    private static string ResolveItemType(GameItem? item)
    {
        if (item is null)
            return "정보 없음";
        var type = item.Types.FirstOrDefault(value =>
            !string.Equals(value, "any", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(value, "noFlea", StringComparison.OrdinalIgnoreCase));
        if (type is null)
            return item.Categories.FirstOrDefault() ?? "정보 없음";
        return type.ToLowerInvariant() switch
        {
            "ammo" => "탄약",
            "ammobox" => "탄약 상자",
            "armor" => "방어구",
            "armorplate" => "방탄판",
            "backpack" => "백팩",
            "barter" => "교환품",
            "container" => "보관함",
            "food" => "식품",
            "grenade" => "투척물",
            "headphones" => "헤드셋",
            "helmet" => "헬멧",
            "key" => "열쇠",
            "medical" => "의료품",
            "mods" => "무기 부품",
            "money" => "화폐",
            "rig" => "전술 조끼",
            "weapon" => "무기",
            _ => type,
        };
    }

    private static int SearchRank(string officialName, string shortName, string query)
    {
        if (string.Equals(officialName, query, StringComparison.CurrentCultureIgnoreCase)) return 0;
        if (officialName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)) return 1;
        if (!string.IsNullOrWhiteSpace(shortName) && shortName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)) return 2;
        if (officialName.Contains(query, StringComparison.CurrentCultureIgnoreCase)) return 3;
        if (!string.IsNullOrWhiteSpace(shortName) && shortName.Contains(query, StringComparison.CurrentCultureIgnoreCase)) return 4;
        return int.MaxValue;
    }
}
