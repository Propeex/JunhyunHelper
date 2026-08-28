using JunhyunHelper.Core.Items;

namespace JunhyunHelper.Core.Scanner;

/// <summary>
/// Primitive presentation values resolved from one Scanner-confirmed Tarkov item ID.
/// ScannerCatalogItem remains the identity/market authority after recognition; canonical
/// content metadata and the derived needed-item plan may only join on that exact ID.
/// </summary>
public sealed record ScannerPresentationJoinResult(
    string ItemId,
    string OfficialName,
    string? IconUrl,
    string? WikiUrl,
    int? TraderSellPrice,
    int? FleaAveragePrice,
    int? TraderPricePerSlot,
    int? FleaPricePerSlot,
    int Slots,
    int CurrentNeeded,
    string? BestTraderName)
{
    public int? FleaMinimumPrice { get; init; }
}

public static class ScannerPresentationJoin
{
    public static ScannerPresentationJoinResult Resolve(
        ScannerCatalogItem catalogItem,
        IEnumerable<GameItem> canonicalItems,
        IEnumerable<(string ItemId, int RemainingTotal)> neededItems)
    {
        ArgumentNullException.ThrowIfNull(catalogItem);
        ArgumentNullException.ThrowIfNull(canonicalItems);
        ArgumentNullException.ThrowIfNull(neededItems);

        var canonicalItem = canonicalItems.FirstOrDefault(item =>
            string.Equals(item.Id, catalogItem.Id, StringComparison.Ordinal));
        var needed = neededItems.FirstOrDefault(item =>
            string.Equals(item.ItemId, catalogItem.Id, StringComparison.Ordinal));

        return new ScannerPresentationJoinResult(
            catalogItem.Id,
            catalogItem.OfficialName,
            canonicalItem?.IconUrl ?? catalogItem.IconUrl,
            canonicalItem?.WikiUrl,
            catalogItem.BestTraderSellPrice,
            catalogItem.FleaAveragePrice,
            catalogItem.TraderPricePerSlot,
            catalogItem.FleaPricePerSlot,
            catalogItem.Slots,
            needed.RemainingTotal,
            catalogItem.BestTraderName)
        {
            FleaMinimumPrice = catalogItem.FleaMinimumPrice,
        };
    }
}