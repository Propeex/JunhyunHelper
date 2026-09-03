using JunhyunHelper.Core.FarmingGuide;
using JunhyunHelper.Core.Items;
using JunhyunHelper.Desktop.Scanner;

namespace JunhyunHelper.Desktop.FarmingGuide;

public partial class FarmingGuidePage
{
    /// <summary>
    /// v1.17's economic objective is denominated in roubles. Ordinary loot uses only the
    /// Scanner/Tarkov average-flea value. Tarkov currencies are the deliberate exception:
    /// they have no meaningful flea listing, so their canonical basePrice is the source-backed
    /// rouble denomination value (₽=1, while USD/EUR follow the current game-data rate).
    /// No trader/base-price fallback is allowed for non-currency loot.
    /// </summary>
    private int? ResolveUnitEconomicValueV1170(string itemId)
    {
        if (_raidFleaAveragePrices.TryGetValue(itemId, out var remembered) && remembered > 0)
            return remembered;

        var scannerValue = _raidBridge?.ResolveSnapshot(itemId)?.FleaAveragePrice;
        if (scannerValue is > 0)
            return scannerValue.Value;

        var item = ResolveItem(itemId);
        return item is not null &&
               FarmingGuideStackQuantityPolicy.IsCurrency(item) &&
               item.BasePrice is > 0
            ? item.BasePrice.Value
            : null;
    }

    private void RememberRaidEconomicUnitValueV1170(GameItem item, ScannerItemSnapshot scanned)
    {
        if (scanned.FleaAveragePrice is > 0)
        {
            _raidFleaAveragePrices[item.Id] = scanned.FleaAveragePrice.Value;
            return;
        }

        if (FarmingGuideStackQuantityPolicy.IsCurrency(item) && item.BasePrice is > 0)
            _raidFleaAveragePrices[item.Id] = item.BasePrice.Value;
    }
}
