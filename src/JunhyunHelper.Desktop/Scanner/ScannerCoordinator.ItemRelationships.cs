using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Desktop.Scanner;

public sealed record ScannerItemLink(
    string ItemId,
    string OfficialName);

public sealed record ScannerItemMaterialRow(
    ScannerItemLink Item,
    decimal Count,
    bool IsTool);

public sealed record ScannerItemUsageRow(
    string SourceName,
    int RequiredLevel,
    ScannerItemLink Product,
    decimal ProductCount,
    IReadOnlyList<ScannerItemMaterialRow> Materials);

public enum ScannerItemAcquisitionKind
{
    TraderPurchase,
    TraderBarter,
    HideoutCraft,
    FleaMarket,
    Raid,
}

public sealed record ScannerItemAcquisitionRow(
    ScannerItemAcquisitionKind Kind,
    string SourceName,
    int? RequiredLevel,
    IReadOnlyList<ScannerItemMaterialRow> Materials);

public sealed record ScannerItemRelationshipDetails(
    IReadOnlyList<ScannerItemUsageRow> CraftUsages,
    IReadOnlyList<ScannerItemUsageRow> BarterUsages,
    IReadOnlyList<ScannerItemAcquisitionRow> Acquisitions);

public sealed partial class ScannerCoordinator
{
    private ScannerItemRelationshipDetails? BuildItemRelationshipDetails(
        ScannerDataContext context,
        string itemId)
    {
        // A null payload means this is a readable pre-v8 last-known-good snapshot.
        // Never infer "Raid" from missing relationship data; the next successful normal
        // Game Content update will populate the graph.
        if (context.Content.ItemRelationshipData is null)
            return null;

        var relation = ItemRelationshipQuery.ForItem(context.Content.ItemRelationshipData, itemId);
        var craftUsages = relation.CraftsUsingItem
            .Select(craft => new ScannerItemUsageRow(
                ResolveStationName(context, craft.StationId),
                craft.RequiredLevel,
                ResolveItemLink(context, craft.ProductItemId),
                craft.ProductCount,
                ResolveMaterials(context, craft.RequiredItems)))
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.RequiredLevel)
            .ThenBy(row => row.Product.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Product.ItemId, StringComparer.Ordinal)
            .ToArray();
        var barterUsages = relation.BartersUsingItem
            .Select(barter => new ScannerItemUsageRow(
                ResolveTraderName(context, barter.TraderId),
                barter.RequiredLevel,
                ResolveItemLink(context, barter.ProductItemId),
                barter.ProductCount,
                ResolveMaterials(context, barter.RequiredItems)))
            .OrderBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.RequiredLevel)
            .ThenBy(row => row.Product.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Product.ItemId, StringComparer.Ordinal)
            .ToArray();

        var acquisitions = new List<ScannerItemAcquisitionRow>();
        acquisitions.AddRange(relation.TraderPurchasesForItem.Select(purchase =>
            new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.TraderPurchase,
                ResolveTraderName(context, purchase.TraderId),
                purchase.RequiredLevel,
                Array.Empty<ScannerItemMaterialRow>())));
        acquisitions.AddRange(relation.BartersForItem.Select(barter =>
            new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.TraderBarter,
                ResolveTraderName(context, barter.TraderId),
                barter.RequiredLevel,
                ResolveMaterials(context, barter.RequiredItems))));
        acquisitions.AddRange(relation.CraftsForItem.Select(craft =>
            new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.HideoutCraft,
                ResolveStationName(context, craft.StationId),
                craft.RequiredLevel,
                ResolveMaterials(context, craft.RequiredItems))));
        if (relation.FleaMarketAvailable)
        {
            acquisitions.Add(new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.FleaMarket,
                "플리마켓",
                null,
                Array.Empty<ScannerItemMaterialRow>()));
        }

        if (acquisitions.Count == 0)
        {
            acquisitions.Add(new ScannerItemAcquisitionRow(
                ScannerItemAcquisitionKind.Raid,
                "레이드",
                null,
                Array.Empty<ScannerItemMaterialRow>()));
        }

        return new ScannerItemRelationshipDetails(
            craftUsages,
            barterUsages,
            acquisitions
                .OrderBy(row => AcquisitionRank(row.Kind))
                .ThenBy(row => row.SourceName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.RequiredLevel ?? 0)
                .ToArray());
    }

    private ScannerItemLink ResolveItemLink(ScannerDataContext context, string itemId)
    {
        if (_catalog.TryGetItem(itemId, out var scannerItem) &&
            !string.IsNullOrWhiteSpace(scannerItem.OfficialName))
        {
            return new ScannerItemLink(itemId, scannerItem.OfficialName);
        }

        var canonical = context.Content.Items.FirstOrDefault(item =>
            string.Equals(item.Id, itemId, StringComparison.Ordinal));
        var name = FirstNonBlank(canonical?.NameKo, canonical?.NameEn, itemId);
        return new ScannerItemLink(itemId, name);
    }

    private IReadOnlyList<ScannerItemMaterialRow> ResolveMaterials(
        ScannerDataContext context,
        IReadOnlyList<ItemIngredient> materials) =>
        materials
            .Select(material => new ScannerItemMaterialRow(
                ResolveItemLink(context, material.ItemId),
                material.Count,
                material.IsTool))
            .OrderBy(row => row.Item.OfficialName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Item.ItemId, StringComparer.Ordinal)
            .ToArray();

    private static string ResolveTraderName(ScannerDataContext context, string traderId)
    {
        var trader = context.Content.Traders.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, traderId, StringComparison.Ordinal));
        return FirstNonBlank(trader?.NameKo, trader?.NameEn, traderId);
    }

    private static string ResolveStationName(ScannerDataContext context, string stationId)
    {
        var station = context.Content.HideoutStations.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, stationId, StringComparison.Ordinal));
        return FirstNonBlank(station?.NameKo, station?.NameEn, stationId);
    }

    private static string FirstNonBlank(string? primary, string? secondary, string fallback) =>
        !string.IsNullOrWhiteSpace(primary)
            ? primary.Trim()
            : !string.IsNullOrWhiteSpace(secondary)
                ? secondary.Trim()
                : fallback;

    private static int AcquisitionRank(ScannerItemAcquisitionKind kind) => kind switch
    {
        ScannerItemAcquisitionKind.TraderPurchase => 0,
        ScannerItemAcquisitionKind.TraderBarter => 1,
        ScannerItemAcquisitionKind.HideoutCraft => 2,
        ScannerItemAcquisitionKind.FleaMarket => 3,
        ScannerItemAcquisitionKind.Raid => 4,
        _ => 9,
    };
}
