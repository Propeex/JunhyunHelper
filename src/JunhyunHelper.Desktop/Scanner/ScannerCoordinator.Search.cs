using System.Windows.Media;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerItemSearchHit(
    string ItemId,
    string OfficialName,
    ImageSource? Icon,
    string? WikiUrl);

public sealed record ScannerItemSearchDetails(
    ScannerItemSnapshot Snapshot,
    string? WikiUrl,
    int Width = 0,
    int Height = 0,
    ScannerItemRelationshipDetails? Relationships = null);

public sealed partial class ScannerCoordinator
{
    /// <summary>
    /// Searches the already-loaded Scanner catalog. This path never refreshes data or
    /// performs network I/O; the normal Game Data update remains the only user-facing
    /// catalog refresh path.
    /// </summary>
    public IReadOnlyList<ScannerItemSearchHit> SearchItems(string? query, int maximumResults = 20)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var text = query?.Trim() ?? string.Empty;
        if (text.Length == 0 || maximumResults <= 0)
            return [];

        var context = GetContext();
        if (context is null || _catalog.LoadedMode != context.GameMode || !_catalog.HasHealthyCatalog)
            return [];

        var contentById = context.Content.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return _catalog.GetItemsSnapshot()
            .Select(item => new { Item = item, Rank = SearchRank(item.OfficialName, item.ShortName, text) })
            .Where(entry => entry.Rank < int.MaxValue)
            .OrderBy(entry => entry.Rank)
            .ThenBy(entry => entry.Item.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Clamp(maximumResults, 1, 50))
            .Select(entry => CreateSearchHit(entry.Item, contentById))
            .ToArray();
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

        var wikiUrl = context.Content.Items
            .FirstOrDefault(item => string.Equals(item.Id, snapshot.ItemId, StringComparison.Ordinal))
            ?.WikiUrl;
        var width = 0;
        var height = 0;
        if (_catalog.TryGetItem(snapshot.ItemId, out var catalogItem))
        {
            width = catalogItem.Width;
            height = catalogItem.Height;
        }

        return new ScannerItemSearchDetails(
            snapshot,
            wikiUrl,
            width,
            height,
            BuildItemRelationshipDetails(context, snapshot.ItemId));
    }

    private ScannerItemSearchHit CreateSearchHit(
        ScannerCatalogItem item,
        IReadOnlyDictionary<string, GameItem> contentById)
    {
        contentById.TryGetValue(item.Id, out var canonicalItem);
        var iconUrl = canonicalItem?.IconUrl ?? item.IconUrl;
        return new ScannerItemSearchHit(
            item.Id,
            item.OfficialName,
            _icons.Load($"item-{item.Id}", iconUrl),
            canonicalItem?.WikiUrl);
    }

    private static int SearchRank(string officialName, string shortName, string query)
    {
        if (string.Equals(officialName, query, StringComparison.CurrentCultureIgnoreCase))
            return 0;
        if (officialName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
            return 1;
        if (!string.IsNullOrWhiteSpace(shortName) &&
            shortName.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 2;
        }
        if (officialName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            return 3;
        if (!string.IsNullOrWhiteSpace(shortName) &&
            shortName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 4;
        }
        return int.MaxValue;
    }
}
